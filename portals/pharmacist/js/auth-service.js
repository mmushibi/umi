class AuthService {
    constructor() {
        this.token = localStorage.getItem('authToken') || '';
        this.tenantId = localStorage.getItem('tenantId') || '';
        this.branchId = localStorage.getItem('branchId') || '';
        this.user = JSON.parse(localStorage.getItem('userInfo') || '{}');
    }

    // Check if user is authenticated
    isAuthenticated() {
        return !!this.token && !this.isTokenExpired();
    }

    // Check if token is expired
    isTokenExpired() {
        if (!this.token) return true;
        
        try {
            const payload = JSON.parse(atob(this.token.split('.')[1]));
            const now = Date.now() / 1000;
            return payload.exp < now;
        } catch (error) {
            console.error('Error parsing token:', error);
            return true;
        }
    }

    // Set authentication data
    setAuthData(token, tenantId, branchId, user) {
        this.token = token;
        this.tenantId = tenantId;
        this.branchId = branchId;
        this.user = user;

        localStorage.setItem('authToken', token);
        localStorage.setItem('tenantId', tenantId);
        localStorage.setItem('branchId', branchId);
        localStorage.setItem('userInfo', JSON.stringify(user));
    }

    // Clear authentication data
    clearAuthData() {
        this.token = '';
        this.tenantId = '';
        this.branchId = '';
        this.user = {};

        localStorage.removeItem('authToken');
        localStorage.removeItem('tenantId');
        localStorage.removeItem('branchId');
        localStorage.removeItem('userInfo');
    }

    // Get authorization header
    getAuthHeader() {
        return this.token ? `Bearer ${this.token}` : '';
    }

    // Get current user
    getCurrentUser() {
        return this.user;
    }

    // Get tenant ID
    getTenantId() {
        return this.tenantId;
    }

    // Get branch ID
    getBranchId() {
        return this.branchId;
    }

    // Login method (for demo purposes - in production, this would call an API)
    async login(credentials) {
        try {
            // This is a mock login for demonstration
            // In production, this would call your authentication API
            
            if (credentials.email === 'pharmacist@umihealth.com' && credentials.password === 'demo123') {
                const mockResponse = {
                    token: 'mock.jwt.token.' + Date.now(),
                    tenantId: '00000000-0000-0000-0000-000000000001',
                    branchId: '00000000-0000-0000-0000-000000000001',
                    user: {
                        id: 'pharmacist-001',
                        name: 'Dr. Sarah Johnson',
                        firstName: 'Sarah',
                        lastName: 'Johnson',
                        email: 'pharmacist@umihealth.com',
                        role: 'Senior Pharmacist'
                    }
                };

                this.setAuthData(
                    mockResponse.token,
                    mockResponse.tenantId,
                    mockResponse.branchId,
                    mockResponse.user
                );

                return { success: true, user: mockResponse.user };
            } else {
                throw new Error('Invalid credentials');
            }
        } catch (error) {
            console.error('Login error:', error);
            throw error;
        }
    }

    // Logout method
    logout() {
        this.clearAuthData();
        // Redirect to login page
        window.location.href = '../login.html';
    }

    // Refresh token (if needed)
    async refreshToken() {
        try {
            // In production, this would call your refresh token API
            // For now, just check if token is still valid
            if (this.isTokenExpired()) {
                this.logout();
                return false;
            }
            return true;
        } catch (error) {
            console.error('Token refresh error:', error);
            this.logout();
            return false;
        }
    }

    // Setup automatic token refresh
    setupTokenRefresh() {
        // Check token every 5 minutes
        setInterval(() => {
            if (this.isAuthenticated()) {
                this.refreshToken();
            }
        }, 5 * 60 * 1000);
    }
}

// Export singleton instance
window.authService = new AuthService();

// Setup automatic token refresh
window.authService.setupTokenRefresh();
