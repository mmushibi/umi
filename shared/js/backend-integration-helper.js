/**
 * Backend Integration Helper
 * Provides standardized functions for integrating frontend pages with the Umi Health API
 * Includes authentication, error handling, data binding, and real-time updates
 */

class BackendIntegrationHelper {
    constructor() {
        this.apiClient = window.apiClient;
        this.authManager = window.authManager;
        this.signalRConnection = null;
        this.currentPage = null;
        this.loadingStates = {};
        this.errorStates = {};
        this.dataCache = new Map();
        this.cacheTimeout = 5 * 60 * 1000; // 5 minutes
    }

    /**
     * Initialize integration for a specific page
     */
    async initPage(pageName, options = {}) {
        this.currentPage = pageName;
        
        // Initialize authentication
        await this.initAuthentication();
        
        // Initialize SignalR if real-time updates are needed
        if (options.enableRealTime) {
            await this.initSignalR();
        }
        
        // Set up global error handlers
        this.setupErrorHandlers();
        
        // Initialize page-specific data
        if (options.initialData) {
            await this.loadInitialData(options.initialData);
        }
        
        console.log(`✅ Backend integration initialized for ${pageName}`);
    }

    /**
     * Initialize authentication
     */
    async initAuthentication() {
        if (!this.authManager) {
            console.error('❌ AuthManager not found. Please include auth-manager.js');
            return false;
        }

        // Check authentication status
        const authInfo = this.authManager.getAuthInfo();
        
        if (!authInfo.isAuthenticated) {
            console.warn('⚠️ User not authenticated');
            return false;
        }

        console.log('✅ User authenticated:', authInfo.user);
        return true;
    }

    /**
     * Initialize SignalR connection for real-time updates
     */
    async initSignalR() {
        // Check if SignalR libraries are loaded
        if (typeof window.signalRClient === 'undefined') {
            console.warn('⚠️ SignalR client not loaded. Including SignalR libraries...');
            
            // Dynamically load SignalR libraries if not present
            try {
                await this.loadScript('https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/7.0.5/signalr.min.js');
                await this.loadScript('/shared/js/signalr-client.js');
                await this.loadScript('/shared/js/realtime-events.js');
                
                // Wait a bit for the scripts to initialize
                await new Promise(resolve => setTimeout(resolve, 500));
            } catch (error) {
                console.error('❌ Failed to load SignalR libraries:', error);
                return false;
            }
        }

        if (!window.signalRClient || !window.realtimeEvents) {
            console.warn('⚠️ SignalR libraries not available');
            return false;
        }

        try {
            // Get current user info
            const authInfo = this.authManager.getAuthInfo();
            if (!authInfo.isAuthenticated) {
                console.warn('⚠️ Cannot initialize SignalR: User not authenticated');
                return false;
            }

            // Initialize real-time events
            await window.realtimeEvents.initialize(authInfo.user);
            
            console.log('✅ SignalR and real-time events initialized');
            return true;
        } catch (error) {
            console.error('❌ Failed to initialize SignalR:', error);
            return false;
        }
    }

    /**
     * Dynamically load a script
     */
    loadScript(src) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = src;
            script.onload = resolve;
            script.onerror = reject;
            document.head.appendChild(script);
        });
    }

    /**
     * Set up global error handlers
     */
    setupErrorHandlers() {
        window.addEventListener('unhandledrejection', (event) => {
            this.handleError(event.reason, 'Unhandled Promise Rejection');
        });

        window.addEventListener('error', (event) => {
            this.handleError(event.error, 'JavaScript Error');
        });
    }

    /**
     * Handle errors with user-friendly notifications
     */
    handleError(error, context = 'API Error') {
        console.error(`❌ ${context}:`, error);
        
        const errorMessage = this.getErrorMessage(error);
        this.showNotification(errorMessage, 'error');
        
        // Update error state for current page
        if (this.currentPage) {
            this.errorStates[this.currentPage] = {
                message: errorMessage,
                context: context,
                timestamp: new Date()
            };
        }
    }

    /**
     * Extract user-friendly error message
     */
    getErrorMessage(error) {
        if (typeof error === 'string') {
            return error;
        }
        
        if (error?.message) {
            return error.message;
        }
        
        if (error?.data?.message) {
            return error.data.message;
        }
        
        return 'An unexpected error occurred. Please try again.';
    }

    /**
     * Show notification to user
     */
    showNotification(message, type = 'info', duration = 5000) {
        // Remove existing notifications
        const existingNotification = document.querySelector('.backend-notification');
        if (existingNotification) {
            existingNotification.remove();
        }

        // Create notification element
        const notification = document.createElement('div');
        notification.className = `backend-notification notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <span class="notification-icon">${this.getNotificationIcon(type)}</span>
                <span class="notification-message">${message}</span>
                <button class="notification-close" onclick="this.parentElement.parentElement.remove()">×</button>
            </div>
        `;

        // Add styles if not already present
        if (!document.querySelector('#backend-notification-styles')) {
            const style = document.createElement('style');
            style.id = 'backend-notification-styles';
            style.textContent = `
                .backend-notification {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    z-index: 10000;
                    max-width: 400px;
                    border-radius: 8px;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
                    animation: slideIn 0.3s ease-out;
                }
                
                .notification-content {
                    display: flex;
                    align-items: center;
                    gap: 0.75rem;
                    padding: 1rem 1.25rem;
                    color: white;
                    font-weight: 500;
                }
                
                .notification-success { background: #22c55e; }
                .notification-error { background: #ef4444; }
                .notification-warning { background: #f59e0b; }
                .notification-info { background: #3b82f6; }
                
                .notification-close {
                    background: none;
                    border: none;
                    color: white;
                    font-size: 1.25rem;
                    cursor: pointer;
                    margin-left: auto;
                    opacity: 0.8;
                }
                
                .notification-close:hover { opacity: 1; }
                
                @keyframes slideIn {
                    from { transform: translateX(100%); opacity: 0; }
                    to { transform: translateX(0); opacity: 1; }
                }
            `;
            document.head.appendChild(style);
        }

        document.body.appendChild(notification);

        // Auto-remove after duration
        setTimeout(() => {
            if (notification.parentElement) {
                notification.remove();
            }
        }, duration);
    }

    /**
     * Get notification icon based on type
     */
    getNotificationIcon(type) {
        const icons = {
            success: '✅',
            error: '❌',
            warning: '⚠️',
            info: 'ℹ️'
        };
        return icons[type] || icons.info;
    }

    /**
     * Load initial data for a page
     */
    async loadInitialData(dataConfig) {
        const results = {};
        
        for (const [key, config] of Object.entries(dataConfig)) {
            try {
                this.setLoadingState(key, true);
                
                // Check cache first
                const cacheKey = this.getCacheKey(config.endpoint, config.params);
                const cached = this.getFromCache(cacheKey);
                
                if (cached && !config.skipCache) {
                    results[key] = cached;
                } else {
                    const data = await this.apiClient.request(config.endpoint, {
                        method: config.method || 'GET',
                        body: config.body ? JSON.stringify(config.body) : undefined
                    });
                    
                    if (data.success) {
                        results[key] = data.data;
                        this.setCache(cacheKey, data.data);
                    } else {
                        throw new Error(data.message || 'Failed to load data');
                    }
                }
                
                this.setLoadingState(key, false);
            } catch (error) {
                this.setLoadingState(key, false);
                this.handleError(error, `Failed to load ${key}`);
                results[key] = null;
            }
        }
        
        return results;
    }

    /**
     * Set loading state for a data key
     */
    setLoadingState(key, loading) {
        if (!this.loadingStates[this.currentPage]) {
            this.loadingStates[this.currentPage] = {};
        }
        this.loadingStates[this.currentPage][key] = loading;
        
        // Update UI elements with loading state
        this.updateLoadingUI(key, loading);
    }

    /**
     * Update UI elements to show loading state
     */
    updateLoadingUI(key, loading) {
        const elements = document.querySelectorAll(`[data-loading="${key}"]`);
        elements.forEach(element => {
            if (loading) {
                element.classList.add('loading');
                element.disabled = true;
            } else {
                element.classList.remove('loading');
                element.disabled = false;
            }
        });
    }

    /**
     * Get cache key for endpoint and params
     */
    getCacheKey(endpoint, params = {}) {
        const paramString = new URLSearchParams(params).toString();
        return `${endpoint}${paramString ? `?${paramString}` : ''}`;
    }

    /**
     * Get data from cache
     */
    getFromCache(key) {
        const cached = this.dataCache.get(key);
        if (cached && Date.now() - cached.timestamp < this.cacheTimeout) {
            return cached.data;
        }
        this.dataCache.delete(key);
        return null;
    }

    /**
     * Set data in cache
     */
    setCache(key, data) {
        this.dataCache.set(key, {
            data: data,
            timestamp: Date.now()
        });
    }

    /**
     * Handle real-time notifications
     */
    handleRealtimeNotification(notification) {
        console.log('📨 Real-time notification:', notification);
        
        // Invalidate relevant cache entries
        if (notification.type === 'data_update') {
            this.invalidateCache(notification.entity);
        }
        
        // Show notification to user
        this.showNotification(notification.message, notification.type || 'info');
        
        // Trigger page refresh if needed
        if (notification.requiresRefresh) {
            this.refreshCurrentPage();
        }
    }

    /**
     * Invalidate cache for specific entity
     */
    invalidateCache(entity) {
        for (const [key] of this.dataCache) {
            if (key.includes(entity)) {
                this.dataCache.delete(key);
            }
        }
    }

    /**
     * Refresh current page data
     */
    async refreshCurrentPage() {
        if (this.currentPage && window.location.reload) {
            window.location.reload();
        }
    }

    /**
     * Standard API call wrapper with error handling
     */
    async apiCall(endpoint, options = {}) {
        try {
            const response = await this.apiClient.request(endpoint, options);
            
            if (!response.success) {
                throw new Error(response.message || 'API call failed');
            }
            
            return response;
        } catch (error) {
            this.handleError(error, `API Call: ${endpoint}`);
            throw error;
        }
    }

    /**
     * Bind data to UI elements
     */
    bindData(selector, data, formatter = null) {
        const elements = document.querySelectorAll(selector);
        elements.forEach(element => {
            const value = formatter ? formatter(data) : data;
            
            if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {
                element.value = value;
            } else {
                element.textContent = value;
            }
        });
    }

    /**
     * Get current user info
     */
    getCurrentUser() {
        return this.authManager?.getCurrentUser() || null;
    }

    /**
     * Check if user has specific role
     */
    hasRole(role) {
        return this.authManager?.hasRole(role) || false;
    }

    /**
     * Check if user has specific permission
     */
    hasPermission(permission) {
        return this.authManager?.hasPermission(permission) || false;
    }

    /**
     * Format currency
     */
    formatCurrency(amount, currency = 'USD') {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: currency
        }).format(amount);
    }

    /**
     * Format date
     */
    formatDate(date, format = 'short') {
        const dateObj = new Date(date);
        
        switch (format) {
            case 'short':
                return dateObj.toLocaleDateString();
            case 'long':
                return dateObj.toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                });
            case 'datetime':
                return dateObj.toLocaleString();
            default:
                return dateObj.toLocaleDateString();
        }
    }

    /**
     * Debounce function for search inputs
     */
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Create search functionality
     */
    createSearchConfig(endpoint, searchParam = 'q') {
        return {
            search: this.debounce(async (query) => {
                if (!query || query.length < 2) {
                    return [];
                }
                
                try {
                    const response = await this.apiCall(`${endpoint}?${searchParam}=${encodeURIComponent(query)}`);
                    return response.data || [];
                } catch (error) {
                    this.handleError(error, 'Search failed');
                    return [];
                }
            }, 300)
        };
    }

    /**
     * Create pagination functionality
     */
    createPaginationConfig(endpoint, pageSize = 20) {
        return {
            loadPage: async (page = 1, filters = {}) => {
                const params = new URLSearchParams({
                    page: page.toString(),
                    pageSize: pageSize.toString(),
                    ...filters
                });
                
                try {
                    const response = await this.apiCall(`${endpoint}?${params}`);
                    return response.data || [];
                } catch (error) {
                    this.handleError(error, 'Failed to load page');
                    return [];
                }
            }
        };
    }
}

// Create global instance
const backendHelper = new BackendIntegrationHelper();

// Export for use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = backendHelper;
}
