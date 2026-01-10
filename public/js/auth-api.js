/**
 * Authentication API Service
 * Handles all authentication and tenant management API calls
 */

class AuthAPI {
    constructor() {
        this.baseURL = this.getBaseURL();
        this.accessToken = localStorage.getItem('umi_access_token');
        this.refreshToken = localStorage.getItem('umi_refresh_token');
    }

    getBaseURL() {
        // Check for environment variable first
        if (typeof process !== 'undefined' && process.env?.UMI_API_BASE_URL) {
            return process.env.UMI_API_BASE_URL + '/api/v1';
        }
        
        // Check for global configuration
        if (typeof window !== 'undefined' && window.UMI_CONFIG?.apiBaseUrl) {
            return window.UMI_CONFIG.apiBaseUrl + '/api/v1';
        }
        
        // Check for localStorage configuration
        if (typeof window !== 'undefined') {
            const storedUrl = localStorage.getItem('umi_api_base_url');
            if (storedUrl) {
                return storedUrl + '/api/v1';
            }
        }
        
        // Fallback to environment-based defaults
        if (typeof window !== 'undefined') {
            const hostname = window.location.hostname;
            const port = window.location.port;
            
            if (hostname === 'localhost' || hostname === '127.0.0.1') {
                // Development environment
                return `http://localhost:${parseInt(port) + 1 || 5001}/api/v1`;
            } else if (hostname.includes('staging') || hostname.includes('dev')) {
                // Staging environment
                return `https://api-staging.umihealth.com/api/v1`;
            } else {
                // Production environment - extract subdomain for tenant-specific API calls
                const subdomain = hostname.split('.')[0];
                if (subdomain && subdomain !== 'www' && subdomain !== 'umihealth') {
                    return `https://${subdomain}.umihealth.com/api/v1`;
                }
                return `https://api.umihealth.com/api/v1`;
            }
        }
        
        // Default fallback
        return 'http://localhost:5001/api/v1';
    }

    async request(endpoint, options = {}) {
        const url = `${this.baseURL}${endpoint}`;
        
        const config = {
            headers: {
                'Content-Type': 'application/json',
                ...options.headers
            },
            ...options
        };

        // Add auth token if available
        if (this.accessToken) {
            config.headers['Authorization'] = `Bearer ${this.accessToken}`;
        }

        // Add tenant context if available
        const tenantId = localStorage.getItem('umi_tenant_id');
        if (tenantId) {
            config.headers['X-Tenant-ID'] = tenantId;
        }

        try {
            const response = await fetch(url, config);
            const data = await response.json();

            if (!response.ok) {
                throw new Error(data.message || `HTTP error! status: ${response.status}`);
            }

            return data;
        } catch (error) {
            console.error('API request failed:', error);
            throw error;
        }
    }

    async post(endpoint, data) {
        return this.request(endpoint, {
            method: 'POST',
            body: JSON.stringify(data)
        });
    }

    async get(endpoint) {
        return this.request(endpoint, {
            method: 'GET'
        });
    }

    async put(endpoint, data) {
        return this.request(endpoint, {
            method: 'PUT',
            body: JSON.stringify(data)
        });
    }

    async delete(endpoint) {
        return this.request(endpoint, {
            method: 'DELETE'
        });
    }

    // Authentication endpoints
    async login(identifier, password) {
        const response = await this.post('/auth/login', {
            identifier,
            password
        });

        if (response.success) {
            this.setTokens(response.accessToken, response.refreshToken);
            this.setUserInfo(response.user, response.tenant, response.subscription);
            
            // Check if user needs to complete setup
            if (response.requiresSetup) {
                return {
                    ...response,
                    redirectUrl: '/account/setup'
                };
            }
        }

        return response;
    }

    async register(userData) {
        const response = await this.post('/auth/register', userData);

        if (response.success) {
            this.setTokens(response.accessToken, response.refreshToken);
            this.setUserInfo(response.user, response.tenant, response.subscription);
            
            // New users always need setup
            return {
                ...response,
                redirectUrl: '/account/setup'
            };
        }

        return response;
    }

    async refreshToken() {
        if (!this.refreshToken) {
            throw new Error('No refresh token available');
        }

        const response = await this.post('/auth/refresh', {
            refreshToken: this.refreshToken
        });

        if (response.success) {
            this.setTokens(response.accessToken, response.refreshToken);
        }

        return response;
    }

    async logout() {
        try {
            await this.post('/auth/logout');
        } catch (error) {
            console.error('Logout error:', error);
        } finally {
            this.clearAuth();
        }
    }

    async getCurrentUser() {
        return await this.get('/auth/me');
    }

    async getSubscriptionStatus() {
        return await this.get('/auth/subscription-status');
    }

    async checkSetupStatus() {
        return await this.get('/auth/check-setup');
    }

    // Account management endpoints
    async getUserProfile() {
        return await this.get('/account/profile');
    }

    async updateProfile(profileData) {
        return await this.put('/account/profile', profileData);
    }

    async getTenantSettings() {
        return await this.get('/account/tenant-settings');
    }

    async updateTenantSettings(settings) {
        return await this.put('/account/tenant-settings', settings);
    }

    async getSubscriptionPlans() {
        return await this.get('/account/subscription-plans');
    }

    async upgradeSubscription(planData) {
        return await this.post('/account/upgrade-subscription', planData);
    }

    // Token management
    setTokens(accessToken, refreshToken) {
        this.accessToken = accessToken;
        this.refreshToken = refreshToken;
        
        localStorage.setItem('umi_access_token', accessToken);
        localStorage.setItem('umi_refresh_token', refreshToken);
    }

    clearAuth() {
        this.accessToken = null;
        this.refreshToken = null;
        
        localStorage.removeItem('umi_access_token');
        localStorage.removeItem('umi_refresh_token');
        localStorage.removeItem('umi_current_user');
        localStorage.removeItem('umi_current_tenant');
        localStorage.removeItem('umi_current_subscription');
        localStorage.removeItem('umi_tenant_id');
        
        // Redirect to login
        window.location.href = '/signin.html';
    }

    setUserInfo(user, tenant, subscription) {
        localStorage.setItem('umi_current_user', JSON.stringify(user));
        localStorage.setItem('umi_current_tenant', JSON.stringify(tenant));
        localStorage.setItem('umi_current_subscription', JSON.stringify(subscription));
        
        if (tenant) {
            localStorage.setItem('umi_tenant_id', tenant.id);
        }
    }

    // Utility methods
    isAuthenticated() {
        return !!this.accessToken;
    }

    getCurrentUser() {
        const userStr = localStorage.getItem('umi_current_user');
        return userStr ? JSON.parse(userStr) : null;
    }

    getCurrentTenant() {
        const tenantStr = localStorage.getItem('umi_current_tenant');
        return tenantStr ? JSON.parse(tenantStr) : null;
    }

    getCurrentSubscription() {
        const subStr = localStorage.getItem('umi_current_subscription');
        return subStr ? JSON.parse(subStr) : null;
    }

    // Auto-refresh token logic
    async setupAutoRefresh() {
        setInterval(async () => {
            if (this.accessToken && this.isTokenExpiringSoon()) {
                try {
                    await this.refreshToken();
                } catch (error) {
                    console.error('Auto refresh failed:', error);
                    this.clearAuth();
                }
            }
        }, 5 * 60 * 1000); // Check every 5 minutes
    }

    isTokenExpiringSoon() {
        try {
            const payload = JSON.parse(atob(this.accessToken.split('.')[1]));
            const exp = payload.exp * 1000; // Convert to milliseconds
            return Date.now() >= (exp - 5 * 60 * 1000); // 5 minutes before expiry
        } catch (error) {
            return true; // If we can't parse, assume it's expiring
        }
    }

    // Role-based redirect
    getRedirectUrl(user, requiresSetup = false) {
        if (requiresSetup) {
            return '/account/setup';
        }

        switch (user.role) {
            case 'tenant_admin':
                return '/portals/admin/home.html';
            case 'super_admin':
                return '/portals/super-admin/home.html';
            case 'pharmacist':
                return '/portals/pharmacist/home.html';
            case 'cashier':
                return '/portals/cashier/home.html';
            case 'operations':
                return '/portals/operations/home.html';
            default:
                return '/portals/admin/home.html';
        }
    }
}

// Create global instance
window.authAPI = new AuthAPI();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AuthAPI;
}
