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
            // Get refresh token from storage
            const refreshToken = localStorage.getItem('auth_tokens') 
                ? JSON.parse(localStorage.getItem('auth_tokens')).refreshToken 
                : null;
            
            await this.apiClient.logout(refreshToken);
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
        // Bypass authentication for admin access
        if (this.isAdminAccess()) {
            return true;
        }
        return this.apiClient.isAuthenticated() && !!this.currentUser;
    }

    /**
     * Check if this is admin bypass access
     */
    isAdminAccess() {
        // Check if we're on an admin page and admin bypass is enabled
        const isAdminPage = window.location.pathname.includes('/portals/admin/');
        const adminBypassEnabled = localStorage.getItem('admin_bypass_enabled') === 'true';
        return isAdminPage && adminBypassEnabled;
    }

    /**
     * Check if user has a specific role
     */
    hasRole(role) {
        if (this.isAdminAccess()) {
            return true; // Admin bypass has all roles
        }
        if (!this.currentUser) return false;
        return this.currentUser.roles?.includes(role);
    }

    /**
     * Check if user has a specific permission
     */
    hasPermission(permission) {
        if (this.isAdminAccess()) {
            return true; // Admin bypass has all permissions
        }
        if (!this.currentUser) return false;
        return this.currentUser.permissions?.includes(permission);
    }

    /**
     * Check if user has any of the specified roles
     */
    hasAnyRole(roles) {
        if (this.isAdminAccess()) {
            return true; // Admin bypass has all roles
        }
        if (!this.currentUser) return false;
        return roles.some(role => this.currentUser.roles?.includes(role));
    }

    /**
     * Get current user
     */
    getCurrentUser() {
        // Return mock admin user for bypass access
        if (this.isAdminAccess()) {
            return {
                id: 'admin-bypass',
                username: 'admin',
                email: 'admin@umihealth.com',
                firstName: 'System',
                lastName: 'Administrator',
                role: 'admin',
                roles: ['admin', 'superadmin'],
                permissions: ['*'],
                tenantId: 'system',
                branchId: null
            };
        }
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

// Setup inactivity auto-logout (syncs across tabs)
(function setupInactivityLogout() {
    const getTimeoutMinutes = () => {
        const v = localStorage.getItem('sessionTimeoutMinutes');
        return v ? parseInt(v, 10) : 30;
    };
    const timeoutMs = () => getTimeoutMinutes() * 60 * 1000;
    let inactivityTimer = null;

    function resetTimer(broadcast = true) {
        localStorage.setItem('umi_last_activity', Date.now().toString());
        if (inactivityTimer) clearTimeout(inactivityTimer);
        inactivityTimer = setTimeout(async () => {
            const last = parseInt(localStorage.getItem('umi_last_activity')||'0',10);
            if (Date.now() - last >= timeoutMs()) {
                try { await authManager.logout(); } catch(e) { console.error(e); }
                localStorage.setItem('umi_logged_out', Date.now().toString());
            } else {
                resetTimer(false);
            }
        }, timeoutMs());
    }

    ['click','mousemove','keydown','scroll','touchstart'].forEach(evt => {
        window.addEventListener(evt, () => resetTimer(true), {passive:true});
    });

    window.addEventListener('storage', (e) => {
        if (e.key === 'umi_last_activity') {
            if (inactivityTimer) clearTimeout(inactivityTimer);
            inactivityTimer = setTimeout(() => {
                const last = parseInt(localStorage.getItem('umi_last_activity')||'0',10);
                if (Date.now() - last >= timeoutMs()) {
                    localStorage.setItem('umi_logged_out', Date.now().toString());
                    try { authManager.logout(); } catch(e) { window.location.href = '/auth/signin.html'; }
                }
            }, timeoutMs());
        }
        if (e.key === 'umi_logged_out') {
            try { authManager.logout(); } catch(e) { window.location.href = '/auth/signin.html'; }
        }
    });

    resetTimer(true);
})();

// Export for use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = authManager;
}
