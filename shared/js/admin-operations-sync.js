/**
 * Admin Portal Operations Data Synchronization Service
 * Handles real-time data synchronization between admin and operations portals
 */
class AdminOperationsSync {
    constructor() {
        this.baseApiUrl = '/api/v1';
        this.tenantId = null;
        this.userId = null;
        this.portalType = 'admin';
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
                this.userId = parsed.userId;
            }
        } catch (error) {
            console.error('Error loading auth data:', error);
        }
    }

    /**
     * Start automatic synchronization
     */
    startSync(intervalMinutes = 3) {
        if (this.syncInterval) {
            clearInterval(this.syncInterval);
        }

        this.syncInterval = setInterval(() => {
            if (this.isOnline && !document.hidden) {
                this.syncAll();
            }
        }, intervalMinutes * 60 * 1000);

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
    }

    /**
     * Synchronize all data types
     */
    async syncAll() {
        if (!this.isOnline) {
            return;
        }

        try {
            await Promise.allSettled([
                this.syncTenants(),
                this.syncTransactions(),
                this.syncUsers(),
                this.syncSubscriptions()
            ]);

            this.emit('sync-completed', { timestamp: new Date() });
        } catch (error) {
            console.error('Error during full sync:', error);
            this.emit('sync-error', { error, timestamp: new Date() });
        }
    }

    /**
     * Synchronize tenants data
     */
    async syncTenants() {
        try {
            const response = await this.apiCall('/adminoperations/tenants', {
                page: 1,
                pageSize: 100
            });

            if (response.success) {
                const cacheKey = 'tenants';
                this.cache.set(cacheKey, response.data.tenants);
                this.emit('tenants-updated', response.data.tenants);
            }
        } catch (error) {
            console.error('Error syncing tenants:', error);
            this.emit('tenants-sync-error', { error });
        }
    }

    /**
     * Synchronize transactions data
     */
    async syncTransactions() {
        try {
            const response = await this.apiCall('/adminoperations/transactions', {
                page: 1,
                pageSize: 50,
                startDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString()
            });

            if (response.success) {
                const cacheKey = 'transactions';
                this.cache.set(cacheKey, response.data.transactions);
                this.emit('transactions-updated', response.data.transactions);
            }
        } catch (error) {
            console.error('Error syncing transactions:', error);
            this.emit('transactions-sync-error', { error });
        }
    }

    /**
     * Synchronize users data
     */
    async syncUsers() {
        try {
            const response = await this.apiCall('/adminoperations/users', {
                page: 1,
                pageSize: 100
            });

            if (response.success) {
                const cacheKey = 'users';
                this.cache.set(cacheKey, response.data.users);
                this.emit('users-updated', response.data.users);
            }
        } catch (error) {
            console.error('Error syncing users:', error);
            this.emit('users-sync-error', { error });
        }
    }

    /**
     * Synchronize subscriptions data
     */
    async syncSubscriptions() {
        try {
            const response = await this.apiCall('/adminoperations/subscriptions', {
                page: 1,
                pageSize: 100
            });

            if (response.success) {
                const cacheKey = 'subscriptions';
                this.cache.set(cacheKey, response.data.subscriptions);
                this.emit('subscriptions-updated', response.data.subscriptions);
            }
        } catch (error) {
            console.error('Error syncing subscriptions:', error);
            this.emit('subscriptions-sync-error', { error });
        }
    }

    /**
     * Get dashboard statistics
     */
    async getDashboardStats() {
        try {
            const response = await this.apiCall('/adminoperations/dashboard');
            
            if (response.success) {
                this.emit('dashboard-stats-updated', response.data);
                return response.data;
            }
            
            throw new Error(response.message || 'Failed to fetch dashboard stats');
        } catch (error) {
            console.error('Error fetching dashboard stats:', error);
            throw error;
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
            tenants: '/adminoperations/tenants',
            transactions: '/adminoperations/transactions',
            users: '/adminoperations/users',
            subscriptions: '/adminoperations/subscriptions',
            dashboard: '/adminoperations/dashboard'
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
                'Authorization': `Bearer ${token}`,
                'X-Portal-Type': this.portalType
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
     * Broadcast data change to operations portal
     */
    async broadcastDataChange(entityType, data, tenantId = null) {
        try {
            await this.apiCall(`/cross-portal/broadcast/${entityType}`, {
                data: data,
                sourcePortal: this.portalType,
                targetPortal: 'operations',
                includeSelf: false,
                tenantId: tenantId
            }, 'POST');
        } catch (error) {
            console.error('Error broadcasting data change:', error);
        }
    }

    /**
     * Create tenant with cross-portal sync
     */
    async createTenant(tenantData) {
        if (this.isOnline) {
            try {
                const response = await this.apiCall('/adminoperations/tenants', tenantData, 'POST');
                if (response.success) {
                    this.broadcastDataChange('tenants', response.data);
                    this.invalidateCache('tenants');
                    return response.data;
                }
                throw new Error(response.message);
            } catch (error) {
                this.queueOperation('createTenant', tenantData);
                throw error;
            }
        } else {
            this.queueOperation('createTenant', tenantData);
            throw new Error('Offline: Operation queued for sync');
        }
    }

    /**
     * Update tenant with cross-portal sync
     */
    async updateTenant(tenantId, tenantData) {
        if (this.isOnline) {
            try {
                const response = await this.apiCall(`/adminoperations/tenants/${tenantId}`, tenantData, 'PUT');
                if (response.success) {
                    this.broadcastDataChange('tenants', { id: tenantId, ...tenantData });
                    this.invalidateCache('tenants');
                    return response.data;
                }
                throw new Error(response.message);
            } catch (error) {
                this.queueOperation('updateTenant', { tenantId, ...tenantData });
                throw error;
            }
        } else {
            this.queueOperation('updateTenant', { tenantId, ...tenantData });
            throw new Error('Offline: Operation queued for sync');
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
            case 'createTenant':
                return await this.apiCall('/adminoperations/tenants', operation.data, 'POST');
            case 'updateTenant':
                return await this.apiCall(`/adminoperations/tenants/${operation.data.tenantId}`, operation.data, 'PUT');
            default:
                throw new Error(`Unknown operation type: ${operation.type}`);
        }
    }

    /**
     * Save pending operations to localStorage
     */
    savePendingOperations() {
        try {
            localStorage.setItem('adminOperationsPending', JSON.stringify(this.pendingOperations));
        } catch (error) {
            console.error('Error saving pending operations:', error);
        }
    }

    /**
     * Load pending operations from localStorage
     */
    loadPendingOperations() {
        try {
            const saved = localStorage.getItem('adminOperationsPending');
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
            isSyncing: false,
            lastSync: localStorage.getItem('adminLastSyncTime'),
            pendingOperations: this.pendingOperations.length,
            cacheSize: this.cache.size,
            portalType: this.portalType
        };
    }

    /**
     * Cleanup resources
     */
    destroy() {
        this.stopSync();
        this.cache.clear();
        this.eventListeners.clear();
        this.pendingOperations = [];
    }
}

// Initialize global admin operations data sync instance
window.adminOperationsSync = new AdminOperationsSync();

// Auto-start when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.adminOperationsSync.loadPendingOperations();
    
    // Start sync if user is authenticated
    if (window.adminOperationsSync.tenantId) {
        window.adminOperationsSync.startSync();
    }
});

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AdminOperationsSync;
}
