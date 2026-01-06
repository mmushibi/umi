/**
 * Umi Health Frontend API Client
 * Provides centralized API communication with token management,
 * error handling, and request/response interceptors
 */

class ApiClient {
    constructor(baseUrl = 'http://localhost:5000/api/v1') {
        this.baseUrl = baseUrl;
        this.accessToken = null;
        this.refreshToken = null;
        this.requestTimeout = 30000; // 30 seconds
        this.isRefreshing = false;
        this.failedQueue = [];

        // Load tokens from storage if available
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
     * Save tokens to local storage
     */
    saveTokens(accessToken, refreshToken) {
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

    /**
     * Clear tokens (logout)
     */
    clearTokens() {
        this.accessToken = null;
        this.refreshToken = null;
        localStorage.removeItem('auth_tokens');
    }

    /**
     * Process the queue of failed requests after token refresh
     */
    processQueue(error, token = null) {
        this.failedQueue.forEach(prom => {
            if (error) {
                prom.reject(error);
            } else {
                prom.resolve(token);
            }
        });

        this.isRefreshing = false;
        this.failedQueue = [];
    }

    /**
     * Refresh access token using refresh token
     */
    async refreshAccessToken() {
        if (this.isRefreshing) {
            return new Promise((resolve, reject) => {
                this.failedQueue.push({ resolve, reject });
            });
        }

        this.isRefreshing = true;

        try {
            const response = await fetch(`${this.baseUrl}/auth/refresh`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    refreshToken: this.refreshToken
                })
            });

            if (!response.ok) {
                throw new Error('Token refresh failed');
            }

            const data = await response.json();
            const newAccessToken = data.data?.token || data.data?.accessToken;

            if (!newAccessToken) {
                throw new Error('No token in refresh response');
            }

            this.saveTokens(newAccessToken, this.refreshToken);
            this.processQueue(null, newAccessToken);
            return newAccessToken;
        } catch (error) {
            this.clearTokens();
            this.processQueue(error, null);
            window.location.href = '/signin';
            throw error;
        }
    }

    /**
     * Make an HTTP request with automatic token refresh
     */
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        // Add authorization header if token exists
        if (this.accessToken) {
            headers['Authorization'] = `Bearer ${this.accessToken}`;
        }

        const config = {
            ...options,
            headers,
            timeout: options.timeout || this.requestTimeout
        };

        try {
            let response = await fetch(url, config);

            // Handle 401 Unauthorized - try to refresh token
            if (response.status === 401 && this.refreshToken) {
                try {
                    const newToken = await this.refreshAccessToken();
                    headers['Authorization'] = `Bearer ${newToken}`;
                    
                    // Retry the original request
                    response = await fetch(url, {
                        ...config,
                        headers
                    });
                } catch (refreshError) {
                    throw new Error('Authentication failed. Please login again.');
                }
            }

            return await this.handleResponse(response);
        } catch (error) {
            return this.handleError(error);
        }
    }

    /**
     * Handle successful response
     */
    async handleResponse(response) {
        let data;
        try {
            data = await response.json();
        } catch {
            data = null;
        }

        if (!response.ok) {
            const error = {
                status: response.status,
                statusText: response.statusText,
                message: data?.message || 'Request failed',
                errors: data?.errors,
                data
            };

            throw error;
        }

        return {
            success: true,
            status: response.status,
            data: data?.data,
            message: data?.message,
            pagination: data?.pagination
        };
    }

    /**
     * Handle network or parsing errors
     */
    handleError(error) {
        console.error('API Error:', error);
        
        return {
            success: false,
            status: error.status || 0,
            message: error.message || 'Network error',
            error
        };
    }

    // ==================== AUTH ENDPOINTS ====================

    async login(email, password, tenantSubdomain) {
        const response = await this.request('/auth/login', {
            method: 'POST',
            body: JSON.stringify({
                email,
                password,
                tenantSubdomain
            })
        });

        if (response.success) {
            this.saveTokens(
                response.data?.token,
                response.data?.refreshToken
            );
        }

        return response;
    }

    async register(userData) {
        return this.request('/auth/register', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    }

    async logout() {
        await this.request('/auth/logout', { method: 'POST' });
        this.clearTokens();
    }

    async getProfile() {
        return this.request('/auth/me', { method: 'GET' });
    }

    async resetPassword(email) {
        return this.request('/auth/forgot-password', {
            method: 'POST',
            body: JSON.stringify({ email })
        });
    }

    // ==================== INVENTORY ENDPOINTS ====================

    async getInventory(branchId, page = 1, pageSize = 50) {
        return this.request(`/inventory?branchId=${branchId}&page=${page}&pageSize=${pageSize}`);
    }

    async getInventoryByBranch(branchId) {
        return this.request(`/inventory/branch/${branchId}`);
    }

    async updateStock(productId, branchId, quantity) {
        return this.request('/inventory/update-stock', {
            method: 'PUT',
            body: JSON.stringify({
                productId,
                branchId,
                quantity
            })
        });
    }

    async reserveInventory(productId, branchId, quantity) {
        return this.request('/inventory/reserve', {
            method: 'POST',
            body: JSON.stringify({
                productId,
                branchId,
                quantity
            })
        });
    }

    async getLowStockProducts(branchId) {
        return this.request(`/inventory/low-stock?branchId=${branchId}`);
    }

    async getExpiringProducts(branchId, daysUntilExpiry = 30) {
        return this.request(`/inventory/expiring?branchId=${branchId}&daysUntilExpiry=${daysUntilExpiry}`);
    }

    // ==================== PATIENT ENDPOINTS ====================

    async getPatients(page = 1, pageSize = 50) {
        return this.request(`/patients?page=${page}&pageSize=${pageSize}`);
    }

    async getPatient(patientId) {
        return this.request(`/patients/${patientId}`);
    }

    async createPatient(patientData) {
        return this.request('/patients', {
            method: 'POST',
            body: JSON.stringify(patientData)
        });
    }

    async updatePatient(patientId, patientData) {
        return this.request(`/patients/${patientId}`, {
            method: 'PUT',
            body: JSON.stringify(patientData)
        });
    }

    async searchPatients(searchTerm) {
        return this.request(`/patients/search?q=${encodeURIComponent(searchTerm)}`);
    }

    async deletePatient(patientId) {
        return this.request(`/patients/${patientId}`, {
            method: 'DELETE'
        });
    }

    // ==================== PRESCRIPTION ENDPOINTS ====================

    async getPrescriptions(patientId = null, page = 1, pageSize = 50) {
        let endpoint = `/prescriptions?page=${page}&pageSize=${pageSize}`;
        if (patientId) {
            endpoint += `&patientId=${patientId}`;
        }
        return this.request(endpoint);
    }

    async createPrescription(prescriptionData) {
        return this.request('/prescriptions', {
            method: 'POST',
            body: JSON.stringify(prescriptionData)
        });
    }

    async dispensePrescription(prescriptionId) {
        return this.request(`/prescriptions/${prescriptionId}/dispense`, {
            method: 'POST'
        });
    }

    // ==================== POS ENDPOINTS ====================

    async createSale(saleData) {
        return this.request('/pos/sales', {
            method: 'POST',
            body: JSON.stringify(saleData)
        });
    }

    async getSales(branchId, page = 1, pageSize = 50) {
        return this.request(`/pos/sales?branchId=${branchId}&page=${page}&pageSize=${pageSize}`);
    }

    async getSale(saleId) {
        return this.request(`/pos/sales/${saleId}`);
    }

    async processPayment(saleId, paymentData) {
        return this.request(`/pos/payments`, {
            method: 'POST',
            body: JSON.stringify({
                saleId,
                ...paymentData
            })
        });
    }

    async getReceipt(saleId) {
        return this.request(`/pos/receipts/${saleId}`);
    }

    async createReturn(returnData) {
        return this.request('/pos/returns', {
            method: 'POST',
            body: JSON.stringify(returnData)
        });
    }

    // ==================== PRODUCTS ENDPOINTS ====================

    async getProducts(page = 1, pageSize = 50) {
        return this.request(`/products?page=${page}&pageSize=${pageSize}`);
    }

    async getProduct(productId) {
        return this.request(`/products/${productId}`);
    }

    async createProduct(productData) {
        return this.request('/products', {
            method: 'POST',
            body: JSON.stringify(productData)
        });
    }

    async updateProduct(productId, productData) {
        return this.request(`/products/${productId}`, {
            method: 'PUT',
            body: JSON.stringify(productData)
        });
    }

    async deleteProduct(productId) {
        return this.request(`/products/${productId}`, {
            method: 'DELETE'
        });
    }

    async searchProducts(searchTerm) {
        return this.request(`/products/search?q=${encodeURIComponent(searchTerm)}`);
    }

    // ==================== REPORTS ENDPOINTS ====================

    async getSalesReport(branchId, startDate, endDate) {
        return this.request(
            `/reports/sales?branchId=${branchId}&startDate=${startDate}&endDate=${endDate}`
        );
    }

    async getInventoryReport(branchId) {
        return this.request(`/reports/inventory?branchId=${branchId}`);
    }

    async getPatientReport(startDate, endDate) {
        return this.request(`/reports/patients?startDate=${startDate}&endDate=${endDate}`);
    }

    async getDashboardAnalytics(branchId) {
        return this.request(`/analytics/dashboard?branchId=${branchId}`);
    }

    // ==================== BRANCH ENDPOINTS ====================

    async getBranches(page = 1, pageSize = 50) {
        return this.request(`/branch?page=${page}&pageSize=${pageSize}`);
    }

    async getBranch(branchId) {
        return this.request(`/branch/${branchId}`);
    }

    async createBranch(branchData) {
        return this.request('/branch', {
            method: 'POST',
            body: JSON.stringify(branchData)
        });
    }

    // ==================== USERS ENDPOINTS ====================

    async getUsers(page = 1, pageSize = 50) {
        return this.request(`/users?page=${page}&pageSize=${pageSize}`);
    }

    async createUser(userData) {
        return this.request('/users', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    }

    // ==================== UTILITY METHODS ====================

    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        return !!this.accessToken;
    }

    /**
     * Get current authentication status
     */
    getAuthStatus() {
        return {
            isAuthenticated: this.isAuthenticated(),
            hasToken: !!this.accessToken,
            hasRefreshToken: !!this.refreshToken
        };
    }

    /**
     * Decode JWT token (without verification)
     */
    decodeToken(token) {
        try {
            const parts = token.split('.');
            if (parts.length !== 3) {
                throw new Error('Invalid token');
            }

            const decoded = JSON.parse(atob(parts[1]));
            return decoded;
        } catch (error) {
            console.error('Failed to decode token:', error);
            return null;
        }
    }

    /**
     * Get current user info from token
     */
    getCurrentUser() {
        if (!this.accessToken) {
            return null;
        }

        const decoded = this.decodeToken(this.accessToken);
        return decoded ? {
            id: decoded.sub || decoded.user_id,
            email: decoded.email,
            name: decoded.name,
            roles: decoded.roles || [],
            permissions: decoded.permissions || [],
            tenantId: decoded.tenant_id,
            branchId: decoded.branch_id
        } : null;
    }
}

// Create global instance
const apiClient = new ApiClient(
    window.API_BASE_URL || 'http://localhost:5000/api/v1'
);

// Export for use in modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = apiClient;
}
