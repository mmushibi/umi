/**
 * Pharmacist Portal Data Synchronization Service
 * Extends the base DataSync with pharmacist-specific functionality
 * Integrates with backend APIs and manages real-time data updates
 */

class PharmacistDataSync extends DataSync {
    constructor() {
        super();
        this.api = window.pharmacistApi;
        this.syncIntervals = new Map();
        this.offlineQueue = [];
        this.isOnline = navigator.onLine;
        this.setupEventListeners();
    }

    /**
     * Initialize pharmacist-specific data sync
     */
    async init() {
        super.init();
        await this.loadInitialData();
        this.startPeriodicSync();
    }

    /**
     * Load initial data from backend
     */
    async loadInitialData() {
        try {
            await Promise.all([
                this.loadProducts(),
                this.loadPatients(),
                this.loadPrescriptions(),
                this.loadPayments(),
                this.loadUserProfile()
            ]);
        } catch (error) {
            console.error('Failed to load initial data:', error);
            this.showNotification('Failed to load data. Some features may be limited.', 'error');
        }
    }

    /**
     * Setup event listeners for online/offline and storage events
     */
    setupEventListeners() {
        super.setupEventListeners();

        // Online/Offline detection
        window.addEventListener('online', () => {
            this.isOnline = true;
            this.processOfflineQueue();
            this.showNotification('Connection restored', 'success');
        });

        window.addEventListener('offline', () => {
            this.isOnline = false;
            this.showNotification('Connection lost. Working offline.', 'warning');
        });

        // Focus/Blur detection for sync
        window.addEventListener('focus', () => {
            this.syncAll();
        });
    }

    /**
     * Load products from API
     */
    async loadProducts() {
        try {
            const response = await this.api.inventory.getProducts();
            this.set('products', response.products || []);
            console.log('Products loaded:', response.products?.length || 0);
        } catch (error) {
            console.error('Failed to load products:', error);
        }
    }

    /**
     * Load patients from API
     */
    async loadPatients() {
        try {
            const response = await this.api.patients.getPatients();
            this.set('patients', response.patients || []);
            console.log('Patients loaded:', response.patients?.length || 0);
        } catch (error) {
            console.error('Failed to load patients:', error);
        }
    }

    /**
     * Load prescriptions from API
     */
    async loadPrescriptions() {
        try {
            const response = await this.api.prescriptions.getPrescriptions();
            this.set('prescriptions', response.prescriptions || []);
            console.log('Prescriptions loaded:', response.prescriptions?.length || 0);
        } catch (error) {
            console.error('Failed to load prescriptions:', error);
        }
    }

    /**
     * Load payments from API
     */
    async loadPayments() {
        try {
            const response = await this.api.payments.getPayments();
            this.set('payments', response.payments || []);
            console.log('Payments loaded:', response.payments?.length || 0);
        } catch (error) {
            console.error('Failed to load payments:', error);
        }
    }

    /**
     * Load user profile from API
     */
    async loadUserProfile() {
        try {
            const response = await this.api.account.getProfile();
            this.setCurrentUser(response.user);
            this.set('pharmacySettings', response.tenant || {});
            console.log('User profile loaded');
        } catch (error) {
            console.error('Failed to load user profile:', error);
        }
    }

    /**
     * Start periodic synchronization
     */
    startPeriodicSync() {
        // Sync every 5 minutes
        const syncInterval = setInterval(() => {
            if (this.isOnline) {
                this.syncAll();
            }
        }, 5 * 60 * 1000);

        this.syncIntervals.set('main', syncInterval);

        // Quick sync for critical data every 2 minutes
        const quickSyncInterval = setInterval(() => {
            if (this.isOnline) {
                this.syncCriticalData();
            }
        }, 2 * 60 * 1000);

        this.syncIntervals.set('quick', quickSyncInterval);
    }

    /**
     * Sync all data
     */
    async syncAll() {
        try {
            await Promise.all([
                this.syncProducts(),
                this.syncPatients(),
                this.syncPrescriptions(),
                this.syncPayments()
            ]);
        } catch (error) {
            console.error('Sync failed:', error);
        }
    }

    /**
     * Sync critical data only
     */
    async syncCriticalData() {
        try {
            await Promise.all([
                this.syncLowStockProducts(),
                this.syncPendingPrescriptions(),
                this.syncRecentPayments()
            ]);
        } catch (error) {
            console.error('Critical sync failed:', error);
        }
    }

    /**
     * Sync products with server
     */
    async syncProducts() {
        try {
            const serverProducts = await this.api.inventory.getProducts();
            const localProducts = this.get('products');
            
            const merged = this.mergeData(localProducts, serverProducts, 'products');
            this.set('products', merged);
        } catch (error) {
            console.error('Product sync failed:', error);
        }
    }

    /**
     * Sync patients with server
     */
    async syncPatients() {
        try {
            const serverPatients = await this.api.patients.getPatients();
            const localPatients = this.get('patients');
            
            const merged = this.mergeData(localPatients, serverPatients, 'patients');
            this.set('patients', merged);
        } catch (error) {
            console.error('Patient sync failed:', error);
        }
    }

    /**
     * Sync prescriptions with server
     */
    async syncPrescriptions() {
        try {
            const serverPrescriptions = await this.api.prescriptions.getPrescriptions();
            const localPrescriptions = this.get('prescriptions');
            
            const merged = this.mergeData(localPrescriptions, serverPrescriptions, 'prescriptions');
            this.set('prescriptions', merged);
        } catch (error) {
            console.error('Prescription sync failed:', error);
        }
    }

    /**
     * Sync payments with server
     */
    async syncPayments() {
        try {
            const serverPayments = await this.api.payments.getPayments();
            const localPayments = this.get('payments');
            
            const merged = this.mergeData(localPayments, serverPayments, 'payments');
            this.set('payments', merged);
        } catch (error) {
            console.error('Payment sync failed:', error);
        }
    }

    /**
     * Sync low stock products
     */
    async syncLowStockProducts() {
        try {
            const lowStock = await this.api.inventory.getLowStock();
            this.set('lowStockProducts', lowStock);
            
            // Show notification if there are low stock items
            if (lowStock.length > 0) {
                this.showNotification(`${lowStock.length} products are low in stock`, 'warning');
            }
        } catch (error) {
            console.error('Low stock sync failed:', error);
        }
    }

    /**
     * Sync pending prescriptions
     */
    async syncPendingPrescriptions() {
        try {
            const pending = await this.api.prescriptions.getPending();
            this.set('pendingPrescriptions', pending);
            
            // Show notification if there are pending prescriptions
            if (pending.length > 0) {
                this.showNotification(`${pending.length} prescriptions pending verification`, 'info');
            }
        } catch (error) {
            console.error('Pending prescriptions sync failed:', error);
        }
    }

    /**
     * Sync recent payments
     */
    async syncRecentPayments() {
        try {
            const response = await this.api.payments.getPayments({ 
                startDate: new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString() 
            });
            this.set('recentPayments', response.payments || []);
        } catch (error) {
            console.error('Recent payments sync failed:', error);
        }
    }

    /**
     * Merge local and server data
     */
    mergeData(localData, serverData, type) {
        if (!Array.isArray(localData) || !Array.isArray(serverData)) {
            return serverData || localData || [];
        }

        const serverMap = new Map(serverData.map(item => [item.id, item]));
        const localMap = new Map(localData.map(item => [item.id, item]));
        
        const merged = [];

        // Add server items
        serverData.forEach(serverItem => {
            const localItem = localMap.get(serverItem.id);
            if (localItem && localItem.updatedAt > serverItem.updatedAt) {
                merged.push(localItem);
            } else {
                merged.push(serverItem);
            }
        });

        // Add local items that don't exist on server
        localData.forEach(localItem => {
            if (!serverMap.has(localItem.id)) {
                merged.push(localItem);
            }
        });

        return merged;
    }

    /**
     * Add item to queue for offline processing
     */
    queueOfflineOperation(operation) {
        this.offlineQueue.push({
            ...operation,
            timestamp: new Date().toISOString(),
            id: this.generateId('queue')
        });
        
        this.saveToLocalStorage('offlineQueue');
    }

    /**
     * Process offline queue when back online
     */
    async processOfflineQueue() {
        if (this.offlineQueue.length === 0) return;

        console.log(`Processing ${this.offlineQueue.length} offline operations`);
        
        const queue = [...this.offlineQueue];
        this.offlineQueue = [];
        this.saveToLocalStorage('offlineQueue');

        for (const operation of queue) {
            try {
                await this.processOperation(operation);
            } catch (error) {
                console.error('Failed to process offline operation:', error);
                this.offlineQueue.push(operation);
            }
        }

        if (this.offlineQueue.length > 0) {
            this.showNotification('Some operations failed to sync', 'error');
        }
    }

    /**
     * Process individual operation
     */
    async processOperation(operation) {
        const { type, data, endpoint } = operation;
        
        switch (type) {
            case 'create':
                await this.api.request(endpoint, {
                    method: 'POST',
                    body: JSON.stringify(data)
                });
                break;
            case 'update':
                await this.api.request(`${endpoint}/${data.id}`, {
                    method: 'PUT',
                    body: JSON.stringify(data)
                });
                break;
            case 'delete':
                await this.api.request(`${endpoint}/${data.id}`, {
                    method: 'DELETE'
                });
                break;
        }
    }

    /**
     * Create product with sync
     */
    async createProduct(product) {
        if (this.isOnline) {
            try {
                const result = await this.api.inventory.createProduct(product);
                this.add('products', result);
                return result;
            } catch (error) {
                throw error;
            }
        } else {
            const localProduct = {
                ...product,
                id: this.generateId('product'),
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                isOffline: true
            };
            
            this.add('products', localProduct);
            this.queueOfflineOperation({
                type: 'create',
                data: localProduct,
                endpoint: '/inventory'
            });
            
            return localProduct;
        }
    }

    /**
     * Update product with sync
     */
    async updateProduct(id, updates) {
        if (this.isOnline) {
            try {
                const result = await this.api.inventory.updateProduct(id, updates);
                this.update('products', id, result);
                return result;
            } catch (error) {
                throw error;
            }
        } else {
            this.update('products', id, updates);
            this.queueOfflineOperation({
                type: 'update',
                data: { id, ...updates },
                endpoint: '/inventory'
            });
        }
    }

    /**
     * Create prescription with sync
     */
    async createPrescription(prescription) {
        if (this.isOnline) {
            try {
                const result = await this.api.prescriptions.createPrescription(prescription);
                this.add('prescriptions', result);
                return result;
            } catch (error) {
                throw error;
            }
        } else {
            const localPrescription = {
                ...prescription,
                id: this.generateId('prescription'),
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                isOffline: true
            };
            
            this.add('prescriptions', localPrescription);
            this.queueOfflineOperation({
                type: 'create',
                data: localPrescription,
                endpoint: '/prescriptions'
            });
            
            return localPrescription;
        }
    }

    /**
     * Show notification to user
     */
    showNotification(message, type = 'info') {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        
        // Add styles
        Object.assign(notification.style, {
            position: 'fixed',
            top: '20px',
            right: '20px',
            padding: '12px 20px',
            borderRadius: '8px',
            color: 'white',
            fontWeight: '500',
            zIndex: '9999',
            opacity: '0',
            transform: 'translateY(-20px)',
            transition: 'all 0.3s ease'
        });

        // Set background color based on type
        const colors = {
            success: '#22c55e',
            error: '#ef4444',
            warning: '#f59e0b',
            info: '#3b82f6'
        };
        notification.style.backgroundColor = colors[type] || colors.info;

        // Add to page
        document.body.appendChild(notification);

        // Animate in
        setTimeout(() => {
            notification.style.opacity = '1';
            notification.style.transform = 'translateY(0)';
        }, 100);

        // Remove after 5 seconds
        setTimeout(() => {
            notification.style.opacity = '0';
            notification.style.transform = 'translateY(-20px)';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 5000);
    }

    /**
     * Cleanup intervals and event listeners
     */
    cleanup() {
        this.syncIntervals.forEach(interval => clearInterval(interval));
        this.syncIntervals.clear();
    }
}

// Create global instance
window.pharmacistDataSync = new PharmacistDataSync();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PharmacistDataSync;
}
