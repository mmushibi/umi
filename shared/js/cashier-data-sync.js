/**
 * Cashier Portal Data Synchronization Service
 * Handles real-time data synchronization between frontend and backend
 */
class CashierDataSync {
    constructor() {
        this.baseApiUrl = '/api/v1';
        this.tenantId = null;
        this.branchId = null;
        this.userId = null;
        this.syncInterval = null;
        this.cache = new Map();
        this.eventListeners = new Map();
        this.isOnline = navigator.onLine;
        this.pendingOperations = [];
        
        this.initializeEventListeners();
        this.loadAuthData();
    }

    /**
     * Initialize event listeners for network status and visibility changes
     */
    initializeEventListeners() {
        window.addEventListener('online', () => {
            this.isOnline = true;
            this.processPendingOperations();
            this.syncAll();
        });

        window.addEventListener('offline', () => {
            this.isOnline = false;
        });

        document.addEventListener('visibilitychange', () => {
            if (!document.hidden && this.isOnline) {
                this.syncAll();
            }
        });
    }

    /**
     * Load authentication data from localStorage or current session
     */
    loadAuthData() {
        try {
            const authData = localStorage.getItem('authData') || sessionStorage.getItem('authData');
            if (authData) {
                const parsed = JSON.parse(authData);
                this.tenantId = parsed.tenantId;
                this.branchId = parsed.branchId;
                this.userId = parsed.userId;
            }
        } catch (error) {
            console.error('Error loading auth data:', error);
        }
    }

    /**
     * Start automatic synchronization
     */
    startSync(intervalMinutes = 2) {
        if (this.syncInterval) {
            clearInterval(this.syncInterval);
        }

        this.syncInterval = setInterval(() => {
            if (this.isOnline && !document.hidden) {
                this.syncAll();
            }
        }, intervalMinutes * 60 * 1000);

        // Register this portal with backend
        this.registerPortal();

        // Initial sync
        this.syncAll();
    }

    /**
     * Stop automatic synchronization
     */
    stopSync() {
        if (this.syncInterval) {
            clearInterval(this.syncInterval);
            this.syncInterval = null;
        }

        // Unregister this portal from backend
        this.unregisterPortal();
    }

    /**
     * Register this cashier portal with backend
     */
    async registerPortal() {
        if (!this.tenantId || !this.branchId || !this.userId) {
            return;
        }

        try {
            await this.apiCall('/cashier/register', {}, 'POST');
            this.emit('portal-registered', { timestamp: new Date() });
        } catch (error) {
            console.error('Error registering portal:', error);
        }
    }

    /**
     * Unregister this cashier portal from backend
     */
    async unregisterPortal() {
        if (!this.tenantId || !this.branchId || !this.userId) {
            return;
        }

        try {
            await this.apiCall('/cashier/unregister', {}, 'POST');
            this.emit('portal-unregistered', { timestamp: new Date() });
        } catch (error) {
            console.error('Error unregistering portal:', error);
        }
    }

    /**
     * Synchronize all data types
     */
    async syncAll() {
        if (!this.isOnline || !this.tenantId || !this.branchId) {
            return;
        }

        try {
            await Promise.allSettled([
                this.syncPatients(),
                this.syncSales(),
                this.syncPayments(),
                this.syncInventory()
            ]);

            this.emit('sync-completed', { timestamp: new Date() });
        } catch (error) {
            console.error('Error during full sync:', error);
            this.emit('sync-error', { error, timestamp: new Date() });
        }
    }

    /**
     * Synchronize patients data
     */
    async syncPatients() {
        try {
            const response = await this.apiCall('/patients', {
                page: 1,
                pageSize: 100
            });

            if (response.success) {
                const cacheKey = 'patients';
                this.cache.set(cacheKey, response.data.patients);
                this.emit('patients-updated', response.data.patients);
            }
        } catch (error) {
            console.error('Error syncing patients:', error);
            this.emit('patients-sync-error', { error });
        }
    }

    /**
     * Synchronize sales data
     */
    async syncSales() {
        try {
            const response = await this.apiCall('/sales', {
                page: 1,
                pageSize: 50,
                startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString() // Last 7 days
            });

            if (response.success) {
                const cacheKey = 'sales';
                this.cache.set(cacheKey, response.data.sales);
                this.emit('sales-updated', response.data.sales);
            }
        } catch (error) {
            console.error('Error syncing sales:', error);
            this.emit('sales-sync-error', { error });
        }
    }

    /**
     * Synchronize payments data
     */
    async syncPayments() {
        try {
            const response = await this.apiCall('/payments', {
                page: 1,
                pageSize: 50,
                startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString() // Last 7 days
            });

            if (response.success) {
                const cacheKey = 'payments';
                this.cache.set(cacheKey, response.data.payments);
                this.emit('payments-updated', response.data.payments);
            }
        } catch (error) {
            console.error('Error syncing payments:', error);
            this.emit('payments-sync-error', { error });
        }
    }

    /**
     * Synchronize inventory data
     */
    async syncInventory() {
        try {
            const response = await this.apiCall('/pointofsale/inventory', {
                page: 1,
                pageSize: 100
            });

            if (response.success) {
                const cacheKey = 'inventory';
                this.cache.set(cacheKey, response.data.inventory);
                this.emit('inventory-updated', response.data.inventory);
            }
        } catch (error) {
            console.error('Error syncing inventory:', error);
            this.emit('inventory-sync-error', { error });
        }
    }

    /**
     * Get cached data or fetch from API
     */
    async getData(type, params = {}) {
        const cacheKey = type;
        
        // Check cache first
        if (this.cache.has(cacheKey)) {
            return this.cache.get(cacheKey);
        }

        // Fetch from API
        try {
            const endpoint = this.getEndpointForType(type);
            const response = await this.apiCall(endpoint, params);
            
            if (response.success) {
                const data = response.data[type] || response.data;
                this.cache.set(cacheKey, data);
                return data;
            }
            
            throw new Error(response.message || 'Failed to fetch data');
        } catch (error) {
            console.error(`Error fetching ${type}:`, error);
            throw error;
        }
    }

    /**
     * Get API endpoint for data type
     */
    getEndpointForType(type) {
        const endpoints = {
            patients: '/patients',
            sales: '/sales',
            payments: '/payments',
            inventory: '/pointofsale/inventory',
            products: '/pointofsale/products',
            stats: '/pointofsale/stats'
        };
        
        return endpoints[type] || `/${type}`;
    }

    /**
     * Make API call with authentication
     */
    async apiCall(endpoint, params = {}, method = 'GET') {
        if (!this.isOnline) {
            throw new Error('Network is offline');
        }

        const token = localStorage.getItem('authToken') || sessionStorage.getItem('authToken');
        if (!token) {
            throw new Error('No authentication token found');
        }

        const url = new URL(`${this.baseApiUrl}${endpoint}`, window.location.origin);
        
        if (method === 'GET' && Object.keys(params).length > 0) {
            Object.keys(params).forEach(key => {
                if (params[key] !== null && params[key] !== undefined) {
                    url.searchParams.append(key, params[key]);
                }
            });
        }

        const options = {
            method,
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            }
        };

        if (method !== 'GET' && Object.keys(params).length > 0) {
            options.body = JSON.stringify(params);
        }

        try {
            const response = await fetch(url.toString(), options);
            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.message || `HTTP ${response.status}: ${response.statusText}`);
            }

            return { success: true, data };
        } catch (error) {
            console.error('API call failed:', error);
            return { success: false, message: error.message };
        }
    }

    /**
     * Create patient with offline support
     */
    async createPatient(patientData) {
        if (this.isOnline) {
            try {
                const response = await this.apiCall('/patients', patientData, 'POST');
                if (response.success) {
                    this.invalidateCache('patients');
                    return response.data;
                }
                throw new Error(response.message);
            } catch (error) {
                // Queue for offline processing
                this.queueOperation('createPatient', patientData);
                throw error;
            }
        } else {
            // Queue for offline processing
            this.queueOperation('createPatient', patientData);
            throw new Error('Offline: Operation queued for sync');
        }
    }

    /**
     * Process checkout with inventory management
     */
    async processCheckout(checkoutData) {
        if (this.isOnline) {
            try {
                const response = await this.apiCall('/pointofsale/checkout', checkoutData, 'POST');
                if (response.success) {
                    this.invalidateCache(['sales', 'payments', 'inventory']);
                    return response.data;
                }
                throw new Error(response.message);
            } catch (error) {
                // Queue for offline processing (with warning about inventory)
                this.queueOperation('processCheckout', checkoutData);
                throw error;
            }
        } else {
            // Queue for offline processing
            this.queueOperation('processCheckout', checkoutData);
            throw new Error('Offline: Checkout queued for sync (inventory may be affected)');
        }
    }

    /**
     * Queue operation for offline processing
     */
    queueOperation(type, data) {
        const operation = {
            id: Date.now() + Math.random(),
            type,
            data,
            timestamp: new Date().toISOString(),
            retryCount: 0
        };

        this.pendingOperations.push(operation);
        this.savePendingOperations();
        this.emit('operation-queued', { operation });
    }

    /**
     * Process pending operations when back online
     */
    async processPendingOperations() {
        if (this.pendingOperations.length === 0) {
            return;
        }

        const operations = [...this.pendingOperations];
        this.pendingOperations = [];

        for (const operation of operations) {
            try {
                await this.processQueuedOperation(operation);
                this.emit('operation-completed', { operation });
            } catch (error) {
                operation.retryCount++;
                if (operation.retryCount < 3) {
                    this.pendingOperations.push(operation);
                } else {
                    this.emit('operation-failed', { operation, error });
                }
            }
        }

        this.savePendingOperations();
    }

    /**
     * Process a queued operation
     */
    async processQueuedOperation(operation) {
        switch (operation.type) {
            case 'createPatient':
                return await this.apiCall('/patients', operation.data, 'POST');
            case 'processCheckout':
                return await this.apiCall('/pointofsale/checkout', operation.data, 'POST');
            default:
                throw new Error(`Unknown operation type: ${operation.type}`);
        }
    }

    /**
     * Save pending operations to localStorage
     */
    savePendingOperations() {
        try {
            localStorage.setItem('pendingOperations', JSON.stringify(this.pendingOperations));
        } catch (error) {
            console.error('Error saving pending operations:', error);
        }
    }

    /**
     * Load pending operations from localStorage
     */
    loadPendingOperations() {
        try {
            const saved = localStorage.getItem('pendingOperations');
            if (saved) {
                this.pendingOperations = JSON.parse(saved);
            }
        } catch (error) {
            console.error('Error loading pending operations:', error);
            this.pendingOperations = [];
        }
    }

    /**
     * Invalidate cache for specific data types
     */
    invalidateCache(types) {
        const typesToInvalidate = Array.isArray(types) ? types : [types];
        
        typesToInvalidate.forEach(type => {
            this.cache.delete(type);
        });

        this.emit('cache-invalidated', { types: typesToInvalidate });
    }

    /**
     * Add event listener
     */
    on(event, callback) {
        if (!this.eventListeners.has(event)) {
            this.eventListeners.set(event, []);
        }
        this.eventListeners.get(event).push(callback);
    }

    /**
     * Remove event listener
     */
    off(event, callback) {
        if (this.eventListeners.has(event)) {
            const listeners = this.eventListeners.get(event);
            const index = listeners.indexOf(callback);
            if (index > -1) {
                listeners.splice(index, 1);
            }
        }
    }

    /**
     * Emit event to listeners
     */
    emit(event, data) {
        if (this.eventListeners.has(event)) {
            this.eventListeners.get(event).forEach(callback => {
                try {
                    callback(data);
                } catch (error) {
                    console.error(`Error in event listener for ${event}:`, error);
                }
            });
        }
    }

    /**
     * Get sync status
     */
    getSyncStatus() {
        return {
            isOnline: this.isOnline,
            isSyncing: false, // Could be tracked more precisely
            lastSync: localStorage.getItem('lastSyncTime'),
            pendingOperations: this.pendingOperations.length,
            cacheSize: this.cache.size
        };
    }

    /**
     * Cleanup resources
     */
    destroy() {
        this.unregisterPortal(); // Unregister before cleanup
        this.stopSync();
        this.cache.clear();
        this.eventListeners.clear();
        this.pendingOperations = [];
    }

    /**
     * Get portal status
     */
    async getPortalStatus() {
        if (!this.isOnline || !this.tenantId || !this.branchId) {
            return { isRegistered: false, status: 'offline' };
        }

        try {
            const response = await this.apiCall('/cashier/status');
            if (response.success) {
                return response.data;
            }
            return { isRegistered: false, status: 'error' };
        } catch (error) {
            console.error('Error getting portal status:', error);
            return { isRegistered: false, status: 'error' };
        }
    }
}

// Initialize global data sync instance
window.cashierDataSync = new CashierDataSync();

// Auto-start when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.cashierDataSync.loadPendingOperations();
    
    // Start sync if user is authenticated
    if (window.cashierDataSync.tenantId && window.cashierDataSync.branchId) {
        window.cashierDataSync.startSync();
    }
});

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = CashierDataSync;
}
