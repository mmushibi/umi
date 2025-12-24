/**
 * Authentication Manager for Frontend
 * Handles login, logout, token refresh, and user session management
 */

class AuthManager {
    constructor(apiClient) {
        this.apiClient = apiClient;
        this.currentUser = null;
        this.isLoggingIn = false;
        this.authChangeListeners = [];

        // Initialize from stored auth
        this.initializeAuth();
    }

    /**
     * Initialize authentication from stored tokens
     */
    initializeAuth() {
        const authStatus = this.apiClient.getAuthStatus();
        if (authStatus.isAuthenticated) {
            this.currentUser = this.apiClient.getCurrentUser();
            this.notifyAuthChange();
        }
    }

    /**
     * Login with email and password
     */
    async login(email, password, tenantSubdomain) {
        if (this.isLoggingIn) {
            throw new Error('Login already in progress');
        }

        this.isLoggingIn = true;

        try {
            const response = await this.apiClient.login(
                email,
                password,
                tenantSubdomain
            );

            if (!response.success) {
                throw new Error(response.message || 'Login failed');
            }

            this.currentUser = this.apiClient.getCurrentUser();
            this.notifyAuthChange();

            return {
                success: true,
                user: this.currentUser,
                message: 'Login successful'
            };
        } catch (error) {
            console.error('Login error:', error);
            return {
                success: false,
                message: error.message || 'Login failed',
                error
            };
        } finally {
            this.isLoggingIn = false;
        }
    }

    /**
     * Register new user
     */
    async register(registrationData) {
        try {
            const response = await this.apiClient.register(registrationData);

            if (!response.success) {
                throw new Error(response.message || 'Registration failed');
            }

            return {
                success: true,
                message: 'Registration successful. Please login.'
            };
        } catch (error) {
            console.error('Registration error:', error);
            return {
                success: false,
                message: error.message || 'Registration failed',
                error
            };
        }
    }

    /**
     * Logout current user
     */
    async logout() {
        try {
            await this.apiClient.logout();
            this.currentUser = null;
            this.notifyAuthChange();

            return { success: true, message: 'Logout successful' };
        } catch (error) {
            console.error('Logout error:', error);
            this.currentUser = null;
            this.notifyAuthChange();

            return { success: false, message: error.message };
        }
    }

    /**
     * Request password reset
     */
    async requestPasswordReset(email) {
        try {
            const response = await this.apiClient.resetPassword(email);

            if (!response.success) {
                throw new Error(response.message || 'Password reset request failed');
            }

            return {
                success: true,
                message: 'Password reset link sent to your email'
            };
        } catch (error) {
            console.error('Password reset error:', error);
            return {
                success: false,
                message: error.message || 'Password reset failed',
                error
            };
        }
    }

    /**
     * Check if user is authenticated
     */
    isAuthenticated() {
        return this.apiClient.isAuthenticated() && !!this.currentUser;
    }

    /**
     * Check if user has a specific role
     */
    hasRole(role) {
        if (!this.currentUser) return false;
        return this.currentUser.roles?.includes(role);
    }

    /**
     * Check if user has a specific permission
     */
    hasPermission(permission) {
        if (!this.currentUser) return false;
        return this.currentUser.permissions?.includes(permission);
    }

    /**
     * Check if user has any of the specified roles
     */
    hasAnyRole(roles) {
        if (!this.currentUser) return false;
        return roles.some(role => this.currentUser.roles?.includes(role));
    }

    /**
     * Get current user
     */
    getCurrentUser() {
        return this.currentUser;
    }

    /**
     * Get user ID
     */
    getUserId() {
        return this.currentUser?.id;
    }

    /**
     * Get tenant ID
     */
    getTenantId() {
        return this.currentUser?.tenantId;
    }

    /**
     * Get branch ID
     */
    getBranchId() {
        return this.currentUser?.branchId;
    }

    /**
     * Subscribe to auth changes
     */
    onAuthChange(callback) {
        this.authChangeListeners.push(callback);

        // Return unsubscribe function
        return () => {
            const index = this.authChangeListeners.indexOf(callback);
            if (index > -1) {
                this.authChangeListeners.splice(index, 1);
            }
        };
    }

    /**
     * Notify all listeners of auth change
     */
    notifyAuthChange() {
        this.authChangeListeners.forEach(callback => {
            try {
                callback({
                    isAuthenticated: this.isAuthenticated(),
                    user: this.currentUser
                });
            } catch (error) {
                console.error('Auth change listener error:', error);
            }
        });
    }

    /**
     * Get authentication info for UI
     */
    getAuthInfo() {
        return {
            isAuthenticated: this.isAuthenticated(),
            user: this.currentUser,
            roles: this.currentUser?.roles || [],
            permissions: this.currentUser?.permissions || [],
            tenantId: this.currentUser?.tenantId,
            branchId: this.currentUser?.branchId
        };
    }
}

// Create global instance
const authManager = new AuthManager(apiClient);

// Export for use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = authManager;
}
