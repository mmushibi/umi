// Operations Portal API Service
class OperationsAPI {
    constructor() {
        this.baseURL = 'http://localhost:5000/api/v1/operations';
        this.accessToken = null;
        this.refreshToken = null;
        this.loadTokens();
    }

    /**
     * Load tokens from local storage
     */
    loadTokens() {
        try {
            const storedTokens = localStorage.getItem('auth_tokens');
            if (storedTokens) {
                const { accessToken, refreshToken } = JSON.parse(storedTokens);
                this.accessToken = accessToken;
                this.refreshToken = refreshToken;
            }
        } catch (error) {
            console.error('Failed to load tokens from storage:', error);
        }
    }

    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        return !!this.accessToken;
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseURL}${endpoint}`;
        const config = {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.accessToken}`,
                ...options.headers
            },
            ...options
        };

        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            return data;
        } catch (error) {
            console.error('API request failed:', error);
            throw error;
        }
    }

    // Dashboard APIs
    async getDashboardStats() {
        return this.request('/dashboard/stats');
    }

    async getRecentTenants() {
        return this.request('/dashboard/recent-tenants');
    }

    // Tenant Management APIs
    async getTenants(page = 1, pageSize = 10, search = '', status = '') {
        const params = new URLSearchParams({
            page: page.toString(),
            pageSize: pageSize.toString()
        });
        
        if (search) params.append('search', search);
        if (status) params.append('status', status);
        
        return this.request(`/tenants?${params}`);
    }

    async createTenant(tenantData) {
        return this.request('/tenants', {
            method: 'POST',
            body: JSON.stringify(tenantData)
        });
    }

    async updateTenant(id, tenantData) {
        return this.request(`/tenants/${id}`, {
            method: 'PUT',
            body: JSON.stringify(tenantData)
        });
    }

    // User Management APIs
    async getUsers(page = 1, pageSize = 10, search = '', status = '', tenantId = '') {
        const params = new URLSearchParams({
            page: page.toString(),
            pageSize: pageSize.toString()
        });
        
        if (search) params.append('search', search);
        if (status) params.append('status', status);
        if (tenantId) params.append('tenantId', tenantId);
        
        return this.request(`/users?${params}`);
    }

    async updateUser(id, userData) {
        return this.request(`/users/${id}`, {
            method: 'PUT',
            body: JSON.stringify(userData)
        });
    }

    // Subscription Management APIs
    async getSubscriptions(page = 1, pageSize = 10, search = '', status = '', tenantId = '') {
        const params = new URLSearchParams({
            page: page.toString(),
            pageSize: pageSize.toString()
        });
        
        if (search) params.append('search', search);
        if (status) params.append('status', status);
        if (tenantId) params.append('tenantId', tenantId);
        
        return this.request(`/subscriptions?${params}`);
    }

    async updateSubscription(id, subscriptionData) {
        return this.request(`/subscriptions/${id}`, {
            method: 'PUT',
            body: JSON.stringify(subscriptionData)
        });
    }

    async upgradeSubscription(id, upgradeData) {
        return this.request(`/subscriptions/${id}/upgrade`, {
            method: 'POST',
            body: JSON.stringify(upgradeData)
        });
    }

    // Transaction APIs
    async getTransactions(page = 1, pageSize = 10, search = '', status = '', tenantId = '', startDate = '', endDate = '') {
        const params = new URLSearchParams({
            page: page.toString(),
            pageSize: pageSize.toString()
        });
        
        if (search) params.append('search', search);
        if (status) params.append('status', status);
        if (tenantId) params.append('tenantId', tenantId);
        if (startDate) params.append('startDate', startDate);
        if (endDate) params.append('endDate', endDate);
        
        return this.request(`/transactions?${params}`);
    }

    async downloadTransactionReceipt(id) {
        const url = `${this.baseURL}/transactions/${id}/receipt`;
        const config = {
            headers: {
                'Authorization': `Bearer ${this.accessToken}`
            }
        };

        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const blob = await response.blob();
            const downloadUrl = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = downloadUrl;
            a.download = `receipt-${id}.pdf`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(downloadUrl);
            
            return { success: true };
        } catch (error) {
            console.error('Receipt download failed:', error);
            throw error;
        }
    }

    // Sync APIs
    async getSyncStatus() {
        return this.request('/sync/status');
    }

    async triggerSync(syncType = 'full') {
        return this.request('/sync/trigger', {
            method: 'POST',
            body: JSON.stringify({ syncType })
        });
    }

    // Helper method to set auth tokens
    setAuthTokens(accessToken, refreshToken) {
        this.accessToken = accessToken;
        this.refreshToken = refreshToken;
        
        try {
            localStorage.setItem('auth_tokens', JSON.stringify({
                accessToken,
                refreshToken
            }));
        } catch (error) {
            console.error('Failed to save tokens to storage:', error);
        }
    }

    // Helper method to clear auth tokens
    clearAuthTokens() {
        this.accessToken = null;
        this.refreshToken = null;
        localStorage.removeItem('auth_tokens');
    }
}

// Create global instance
window.operationsAPI = new OperationsAPI();
