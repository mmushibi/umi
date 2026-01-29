/**
 * API Client Class
 * Base API client for Umi Health system
 */
class ApiClient {
    constructor(baseURL = 'http://localhost:5001/api/v1') {
        this.baseURL = baseURL;
        this.token = localStorage.getItem('umi_access_token');
        this.tenantId = localStorage.getItem('umi_tenant_id');
    }

    // Set authentication token
    setToken(token) {
        this.token = token;
        localStorage.setItem('umi_access_token', token);
    }

    // Get auth headers
    getHeaders() {
        const headers = {
            'Content-Type': 'application/json'
        };

        if (this.token) {
            headers['Authorization'] = `Bearer ${this.token}`;
        }

        if (this.tenantId) {
            headers['X-Tenant-ID'] = this.tenantId;
        }

        return headers;
    }

    // Generic API request method
    async request(endpoint, options = {}) {
        let url = `${this.baseURL}${endpoint}`;
        
        // Handle query parameters for GET requests
        if (options.params && options.method === 'GET') {
            const params = new URLSearchParams(options.params);
            url += `?${params.toString()}`;
            delete options.params;
        }
        
        const config = {
            headers: this.getHeaders(),
            ...options
        };

        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.message || `HTTP ${response.status}`);
            }
            
            return await response.json();
        } catch (error) {
            console.error('API request failed:', error);
            throw error;
        }
    }

    // HTTP method helpers
    async get(endpoint, params) {
        return this.request(endpoint, { method: 'GET', params });
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
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ApiClient;
}
