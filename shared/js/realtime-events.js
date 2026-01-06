/**
 * Real-time Events Manager
 * Handles business-specific real-time events for Umi Health
 */

class RealtimeEventsManager {
    constructor() {
        this.signalRClient = window.signalRClient;
        this.eventHandlers = new Map();
        this.isConnected = false;
        this.userRole = null;
        this.tenantId = null;
        this.branchId = null;
    }

    /**
     * Initialize real-time events for the current user
     */
    async initialize(userInfo) {
        this.userRole = userInfo.roles?.[0] || 'User';
        this.tenantId = userInfo.tenantId;
        this.branchId = userInfo.branchId;

        try {
            // Initialize SignalR connections based on user role
            await this.signalRClient.initializeForRole(this.userRole);
            this.isConnected = true;

            // Setup event handlers
            this.setupEventHandlers();
            
            // Setup connection status monitoring
            this.setupConnectionMonitoring();

            console.log(`✅ Real-time events initialized for ${this.userRole}`);
        } catch (error) {
            console.error('❌ Failed to initialize real-time events:', error);
            throw error;
        }
    }

    /**
     * Setup event handlers for different business events
     */
    setupEventHandlers() {
        // Notification events (all users)
        this.signalRClient.subscribe('notifications', 'ReceiveNotification', (notification) => {
            this.handleNotification(notification);
        });

        // Inventory events (Admin, Pharmacist)
        if (['Admin', 'Pharmacist', 'SuperAdmin'].includes(this.userRole)) {
            this.signalRClient.subscribe('inventory', 'StockLevelChanged', (data) => {
                this.handleStockLevelChange(data);
            });

            this.signalRClient.subscribe('inventory', 'ProductAdded', (product) => {
                this.handleProductAdded(product);
            });

            this.signalRClient.subscribe('inventory', 'ProductUpdated', (product) => {
                this.handleProductUpdated(product);
            });
        }

        // Prescription events (Pharmacist, Admin)
        if (['Pharmacist', 'Admin', 'SuperAdmin'].includes(this.userRole)) {
            this.signalRClient.subscribe('prescriptions', 'NewPrescription', (prescription) => {
                this.handleNewPrescription(prescription);
            });

            this.signalRClient.subscribe('prescriptions', 'PrescriptionStatusChanged', (data) => {
                this.handlePrescriptionStatusChange(data);
            });
        }

        // Sales events (Cashier, Admin, SuperAdmin)
        if (['Cashier', 'Admin', 'SuperAdmin'].includes(this.userRole)) {
            this.signalRClient.subscribe('sales', 'NewSale', (sale) => {
                this.handleNewSale(sale);
            });

            this.signalRClient.subscribe('sales', 'SaleCompleted', (sale) => {
                this.handleSaleCompleted(sale);
            });
        }

        // Patient events (Admin, Pharmacist, SuperAdmin)
        if (['Admin', 'Pharmacist', 'SuperAdmin'].includes(this.userRole)) {
            this.signalRClient.subscribe('patients', 'PatientRegistered', (patient) => {
                this.handlePatientRegistered(patient);
            });

            this.signalRClient.subscribe('patients', 'PatientUpdated', (patient) => {
                this.handlePatientUpdated(patient);
            });
        }
    }

    /**
     * Handle notification events
     */
    handleNotification(notification) {
        console.log('🔔 New notification:', notification);

        // Show notification toast
        if (window.backendHelper) {
            window.backendHelper.showNotification(
                notification.message,
                notification.type || 'info',
                8000
            );
        }

        // Update notification badge
        this.updateNotificationBadge();

        // Trigger custom event
        this.triggerEvent('notification-received', notification);
    }

    /**
     * Handle stock level changes
     */
    handleStockLevelChange(data) {
        console.log('📦 Stock level changed:', data);

        // Update inventory UI if on inventory page
        if (window.location.pathname.includes('inventory')) {
            this.triggerEvent('stock-level-changed', data);
        }

        // Show warning for low stock
        if (data.isLowStock) {
            if (window.backendHelper) {
                window.backendHelper.showNotification(
                    `⚠️ Low stock: ${data.productName} (${data.currentStock} remaining)`,
                    'warning',
                    10000
                );
            }
        }
    }

    /**
     * Handle new product additions
     */
    handleProductAdded(product) {
        console.log('➕ New product added:', product);
        this.triggerEvent('product-added', product);
    }

    /**
     * Handle product updates
     */
    handleProductUpdated(product) {
        console.log('✏️ Product updated:', product);
        this.triggerEvent('product-updated', product);
    }

    /**
     * Handle new prescriptions
     */
    handleNewPrescription(prescription) {
        console.log('💊 New prescription:', prescription);

        // Show notification for pharmacists
        if (this.userRole === 'Pharmacist') {
            if (window.backendHelper) {
                window.backendHelper.showNotification(
                    `📋 New prescription for ${prescription.patientName}`,
                    'info',
                    8000
                );
            }
        }

        this.triggerEvent('new-prescription', prescription);
    }

    /**
     * Handle prescription status changes
     */
    handlePrescriptionStatusChange(data) {
        console.log('🔄 Prescription status changed:', data);
        this.triggerEvent('prescription-status-changed', data);
    }

    /**
     * Handle new sales
     */
    handleNewSale(sale) {
        console.log('💰 New sale:', sale);
        this.triggerEvent('new-sale', sale);
    }

    /**
     * Handle completed sales
     */
    handleSaleCompleted(sale) {
        console.log('✅ Sale completed:', sale);

        // Update dashboard stats
        if (window.location.pathname.includes('home') || window.location.pathname.includes('dashboard')) {
            this.triggerEvent('sale-completed', sale);
        }
    }

    /**
     * Handle patient registration
     */
    handlePatientRegistered(patient) {
        console.log('👤 New patient registered:', patient);

        // Show notification
        if (window.backendHelper) {
            window.backendHelper.showNotification(
                `👋 New patient: ${patient.firstName} ${patient.lastName}`,
                'success',
                6000
            );
        }

        this.triggerEvent('patient-registered', patient);
    }

    /**
     * Handle patient updates
     */
    handlePatientUpdated(patient) {
        console.log('📝 Patient updated:', patient);
        this.triggerEvent('patient-updated', patient);
    }

    /**
     * Setup connection status monitoring
     */
    setupConnectionMonitoring() {
        document.addEventListener('signalr-connection-status', (event) => {
            const { hubName, status, error } = event.detail;
            
            if (status === 'disconnected') {
                console.warn(`🔌 Real-time connection lost for ${hubName}`);
                this.isConnected = false;
            } else if (status === 'connected') {
                console.log(`🔗 Real-time connection restored for ${hubName}`);
                this.isConnected = true;
            }

            // Update UI connection indicator
            this.updateConnectionIndicator(status);
        });
    }

    /**
     * Update connection indicator in UI
     */
    updateConnectionIndicator(status) {
        const indicator = document.querySelector('.realtime-indicator');
        if (!indicator) return;

        indicator.className = `realtime-indicator status-${status}`;
        
        const statusText = {
            connected: '🟢 Live',
            reconnecting: '🟡 Reconnecting...',
            disconnected: '🔴 Offline'
        };

        indicator.textContent = statusText[status] || '❓ Unknown';
    }

    /**
     * Update notification badge
     */
    updateNotificationBadge() {
        const badge = document.querySelector('.notification-badge');
        if (badge) {
            // Increment badge count
            const currentCount = parseInt(badge.textContent) || 0;
            badge.textContent = currentCount + 1;
            badge.style.display = currentCount + 1 > 0 ? 'flex' : 'none';
        }
    }

    /**
     * Trigger custom event for UI components
     */
    triggerEvent(eventName, data) {
        const event = new CustomEvent(`realtime-${eventName}`, {
            detail: data
        });
        document.dispatchEvent(event);
    }

    /**
     * Subscribe to custom real-time events
     */
    on(eventName, callback) {
        document.addEventListener(`realtime-${eventName}`, callback);
    }

    /**
     * Unsubscribe from custom real-time events
     */
    off(eventName, callback) {
        document.removeEventListener(`realtime-${eventName}`, callback);
    }

    /**
     * Send real-time message
     */
    async send(hubName, methodName, ...args) {
        if (!this.isConnected) {
            throw new Error('Real-time connection not established');
        }

        return await this.signalRClient.send(hubName, methodName, ...args);
    }

    /**
     * Invoke real-time method
     */
    async invoke(hubName, methodName, ...args) {
        if (!this.isConnected) {
            throw new Error('Real-time connection not established');
        }

        return await this.signalRClient.invoke(hubName, methodName, ...args);
    }

    /**
     * Get connection status
     */
    getStatus() {
        return {
            isConnected: this.isConnected,
            userRole: this.userRole,
            tenantId: this.tenantId,
            branchId: this.branchId,
            connections: Array.from(this.signalRClient.connections.keys())
        };
    }

    /**
     * Disconnect all real-time connections
     */
    async disconnect() {
        await this.signalRClient.disconnectAll();
        this.isConnected = false;
        console.log('🔌 Real-time events disconnected');
    }
}

// Create global instance
window.realtimeEvents = new RealtimeEventsManager();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = RealtimeEventsManager;
}
