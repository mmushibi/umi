/**
 * Backend Integration Helper for Umi Health Portals
 * Provides common backend integration utilities
 */

class BackendIntegrationHelper {
    constructor(apiClient = null) {
        this.apiClient = apiClient;
        this.baseUrl = 'http://localhost:5000/api/v1';
    }

    // Generic API request method
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        
        const config = {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        };

        // Add auth token if available
        const token = localStorage.getItem('umi_access_token');
        if (token) {
            config.headers['Authorization'] = `Bearer ${token}`;
        }

        // Add tenant context if available
        const tenantId = localStorage.getItem('umi_tenant_id');
        if (tenantId) {
            config.headers['X-Tenant-ID'] = tenantId;
        }

        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                const error = await response.json().catch(() => ({}));
                throw new Error(error.message || `HTTP ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('Backend request failed:', error);
            throw error;
        }
    }

    // HTTP method helpers
    async get(endpoint, params) {
        let url = endpoint;
        if (params) {
            const queryString = new URLSearchParams(params).toString();
            url += `?${queryString}`;
        }
        return this.request(url, { method: 'GET' });
    }

    async post(endpoint, data) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    async put(endpoint, data) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    async delete(endpoint) {
        return this.request(endpoint, { method: 'DELETE' });
    }

    // Sync data between portals
    async syncUserData(userId) {
        try {
            const response = await this.get(`/admin/users/${userId}`);
            return response;
        } catch (error) {
            console.error('Failed to sync user data:', error);
            throw error;
        }
    }

    async syncTenantData(tenantId) {
        try {
            const response = await this.get(`/admin/tenants/${tenantId}`);
            return response;
        } catch (error) {
            console.error('Failed to sync tenant data:', error);
            throw error;
        }
    }

    // Real-time updates (placeholder for WebSocket implementation)
    subscribeToUpdates(callback) {
        // This would integrate with SignalR or WebSocket for real-time updates
        console.log('Subscribed to backend updates');
        
        // Mock real-time updates for demo
        const mockInterval = setInterval(() => {
            callback({
                type: 'heartbeat',
                timestamp: new Date().toISOString()
            });
        }, 30000);

        return () => clearInterval(mockInterval);
    }

    // Error handling wrapper
    async withErrorHandling(operation, errorMessage = 'Operation failed') {
        try {
            return await operation();
        } catch (error) {
            console.error(errorMessage, error);
            
            // Show toast notification if available
            if (typeof window !== 'undefined' && window.authManager) {
                window.authManager.showToast(errorMessage, 'error');
            }
            
            throw error;
        }
    }

    // Cache management
    cache = new Map();

    async cachedRequest(key, fetchFn, ttl = 300000) { // 5 minutes default TTL
        const cached = this.cache.get(key);
        if (cached && Date.now() - cached.timestamp < ttl) {
            return cached.data;
        }

        const data = await fetchFn();
        this.cache.set(key, {
            data,
            timestamp: Date.now()
        });

        return data;
    }

    clearCache(pattern = null) {
        if (pattern) {
            for (const key of this.cache.keys()) {
                if (key.includes(pattern)) {
                    this.cache.delete(key);
                }
            }
        } else {
            this.cache.clear();
        }
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = BackendIntegrationHelper;
}
