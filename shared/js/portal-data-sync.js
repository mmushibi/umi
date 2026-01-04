// Shared data sync utilities for all portals
class PortalDataSync {
    constructor(portalType, baseURL) {
        this.portalType = portalType;
        this.baseURL = baseURL;
        this.token = localStorage.getItem('authToken');
        this.refreshInterval = 30000;
        this.intervals = {};
        this.cache = new Map();
        this.listeners = new Map();
        this.isOnline = navigator.onLine;
        this.offlineQueue = [];
        
        this.setupInterceptors();
        this.setupOfflineListeners();
        this.initializeOfflineStorage();
    }

    setupInterceptors() {
        const originalFetch = window.fetch;
        window.fetch = async (...args) => {
            const [url, options = {}] = args;
            
            if (url.startsWith('/api/') && this.token) {
                options.headers = {
                    ...options.headers,
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                };
            }
            
            return originalFetch(url, options);
        };
    }

    setupOfflineListeners() {
        window.addEventListener('online', () => {
            this.isOnline = true;
            this.notifySuccess('Connection restored');
            this.processOfflineQueue();
        });

        window.addEventListener('offline', () => {
            this.isOnline = false;
            this.notifyError('Connection lost - using offline mode');
        });
    }

    initializeOfflineStorage() {
        const storageKey = `${this.portalType}_offlineData`;
        const queueKey = `${this.portalType}_offlineQueue`;
        
        if (!localStorage.getItem(storageKey)) {
            localStorage.setItem(storageKey, JSON.stringify({}));
        }
        if (!localStorage.getItem(queueKey)) {
            localStorage.setItem(queueKey, JSON.stringify([]));
        }
    }

    async request(endpoint, options = {}) {
        const cacheKey = `${endpoint}_${JSON.stringify(options)}`;
        
        if (!this.isOnline) {
            return this.handleOfflineRequest(endpoint, options, cacheKey);
        }
        
        try {
            const response = await fetch(`${this.baseURL}${endpoint}`, {
                ...options,
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || `HTTP ${response.status}`);
            }

            const data = await response.json();
            
            this.setCache(cacheKey, data);
            this.storeOfflineData(endpoint, data);
            
            return data;
        } catch (error) {
            console.error('API request failed:', error);
            
            if (this.isOnline) {
                const cachedData = this.getCache(cacheKey);
                if (cachedData && this.isCacheValid(cacheKey)) {
                    console.warn('Using cached data due to API failure:', error);
                    this.notifyError('Using cached data - connection issues detected');
                    return cachedData;
                }
                
                const offlineData = this.getOfflineData(endpoint);
                if (offlineData) {
                    console.warn('Using offline data due to API failure:', error);
                    this.notifyError('Using offline data - connection issues detected');
                    return offlineData;
                }
            }
            
            this.notifyError(error.message);
            throw error;
        }
    }

    storeOfflineData(endpoint, data) {
        try {
            const storageKey = `${this.portalType}_offlineData`;
            const offlineData = JSON.parse(localStorage.getItem(storageKey) || '{}');
            offlineData[endpoint] = {
                data,
                timestamp: Date.now(),
                endpoint
            };
            localStorage.setItem(storageKey, JSON.stringify(offlineData));
        } catch (error) {
            console.error('Failed to store offline data:', error);
        }
    }

    getOfflineData(endpoint) {
        try {
            const storageKey = `${this.portalType}_offlineData`;
            const offlineData = JSON.parse(localStorage.getItem(storageKey) || '{}');
            const stored = offlineData[endpoint];
            
            if (stored && this.isOfflineDataValid(stored.timestamp)) {
                return stored.data;
            }
            
            return null;
        } catch (error) {
            console.error('Failed to get offline data:', error);
            return null;
        }
    }

    isOfflineDataValid(timestamp) {
        const maxAge = 24 * 60 * 60 * 1000; // 24 hours
        return Date.now() - timestamp < maxAge;
    }

    handleOfflineRequest(endpoint, options, cacheKey) {
        console.log('Handling offline request for:', endpoint);
        
        const cachedData = this.getCache(cacheKey);
        if (cachedData && this.isCacheValid(cacheKey)) {
            this.notifyError('Using cached data - offline mode');
            return Promise.resolve(cachedData);
        }
        
        const offlineData = this.getOfflineData(endpoint);
        if (offlineData) {
            this.notifyError('Using offline data - offline mode');
            return Promise.resolve(offlineData);
        }
        
        if (options.method && options.method !== 'GET') {
            this.queueOfflineRequest(endpoint, options);
        }
        
        return Promise.resolve(this.getMockData(endpoint));
    }

    queueOfflineRequest(endpoint, options) {
        try {
            const queueKey = `${this.portalType}_offlineQueue`;
            const queue = JSON.parse(localStorage.getItem(queueKey) || '[]');
            queue.push({
                endpoint,
                options,
                timestamp: Date.now()
            });
            localStorage.setItem(queueKey, JSON.stringify(queue));
        } catch (error) {
            console.error('Failed to queue offline request:', error);
        }
    }

    async processOfflineQueue() {
        try {
            const queueKey = `${this.portalType}_offlineQueue`;
            const queue = JSON.parse(localStorage.getItem(queueKey) || '[]');
            
            for (const request of queue) {
                try {
                    await this.request(request.endpoint, request.options);
                    console.log('Processed queued request:', request.endpoint);
                } catch (error) {
                    console.error('Failed to process queued request:', error);
                }
            }
            
            localStorage.setItem(queueKey, JSON.stringify([]));
        } catch (error) {
            console.error('Failed to process offline queue:', error);
        }
    }

    getMockData(endpoint) {
        const mockData = {
            // Admin portal mock data
            '/dashboard': {
                totalSales: 0,
                totalPrescriptions: 0,
                totalPatients: 0,
                totalInventory: 0
            },
            '/patients': [],
            '/prescriptions': [],
            '/inventory': [],
            
            // Cashier portal mock data
            '/sales': [],
            '/products': [],
            '/customers': [],
            
            // Pharmacist portal mock data
            '/medications': [],
            '/prescriptions': [],
            '/patients': []
        };
        
        return mockData[endpoint] || null;
    }

    setCache(key, data, ttl = 300000) {
        this.cache.set(key, {
            data,
            timestamp: Date.now(),
            ttl
        });
    }

    getCache(key) {
        const cached = this.cache.get(key);
        return cached ? cached.data : null;
    }

    isCacheValid(key) {
        const cached = this.cache.get(key);
        if (!cached) return false;
        
        return Date.now() - cached.timestamp < cached.ttl;
    }

    clearCache(key) {
        if (key) {
            this.cache.delete(key);
        } else {
            this.cache.clear();
        }
    }

    startAutoRefresh(endpoint, callback, interval = this.refreshInterval) {
        const key = `refresh_${endpoint}`;
        
        if (this.intervals[key]) {
            clearInterval(this.intervals[key]);
        }

        this.intervals[key] = setInterval(async () => {
            try {
                const data = await this.request(endpoint);
                callback(data);
            } catch (error) {
                console.error(`Auto refresh failed for ${endpoint}:`, error);
            }
        }, interval);

        this.request(endpoint).then(callback).catch(console.error);
    }

    stopAutoRefresh(endpoint) {
        const key = `refresh_${endpoint}`;
        if (this.intervals[key]) {
            clearInterval(this.intervals[key]);
            delete this.intervals[key];
        }
    }

    stopAllAutoRefresh() {
        Object.keys(this.intervals).forEach(key => {
            clearInterval(this.intervals[key]);
        });
        this.intervals = {};
    }

    addEventListener(event, callback) {
        if (!this.listeners.has(event)) {
            this.listeners.set(event, []);
        }
        this.listeners.get(event).push(callback);
    }

    removeEventListener(event, callback) {
        if (this.listeners.has(event)) {
            const callbacks = this.listeners.get(event);
            const index = callbacks.indexOf(callback);
            if (index > -1) {
                callbacks.splice(index, 1);
            }
        }
    }

    notify(event, data) {
        if (this.listeners.has(event)) {
            this.listeners.get(event).forEach(callback => {
                try {
                    callback(data);
                } catch (error) {
                    console.error(`Event listener error for ${event}:`, error);
                }
            });
        }
    }

    notifyError(message) {
        this.notify('error', message);
        if (window.showToast) {
            window.showToast(message, 'error');
        }
    }

    notifySuccess(message) {
        this.notify('success', message);
        if (window.showToast) {
            window.showToast(message, 'success');
        }
    }

    setToken(token) {
        this.token = token;
        localStorage.setItem('authToken', token);
    }

    clearToken() {
        this.token = null;
        localStorage.removeItem('authToken');
    }

    isAuthenticated() {
        return !!this.token;
    }

    getConnectionStatus() {
        const storageKey = `${this.portalType}_offlineData`;
        const queueKey = `${this.portalType}_offlineQueue`;
        
        return {
            isOnline: this.isOnline,
            hasCachedData: this.cache.size > 0,
            hasOfflineData: Object.keys(JSON.parse(localStorage.getItem(storageKey) || '{}')).length > 0,
            queuedRequests: JSON.parse(localStorage.getItem(queueKey) || '[]').length
        };
    }
}

// Portal-specific instances
window.adminDataSync = new PortalDataSync('admin', '/api/v1/admin');
window.cashierDataSync = new PortalDataSync('cashier', '/api/v1/cashier');
window.pharmacistDataSync = new PortalDataSync('pharmacist', '/api/v1/pharmacist');

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { PortalDataSync };
}
