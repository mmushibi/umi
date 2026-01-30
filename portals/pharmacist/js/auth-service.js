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
            // Call the real authentication API
            const response = await fetch((window.API_BASE || 'http://localhost:5000') + '/api/v1/auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    username: credentials.email,
                    password: credentials.password
                })
            });
            
            const result = await response.json();
            
            if (result.success) {
                const user = result.data;
                
                this.setAuthData(
                    result.accessToken,
                    user.tenantId,
                    user.branchId || user.tenantId, // Use tenantId as fallback for branchId
                    user
                );

                // Store refresh token
                if (result.refreshToken) {
                    localStorage.setItem('umi_refresh_token', result.refreshToken);
                }

                return { success: true, user: user };
            } else {
                throw new Error(result.message || 'Login failed');
            }
        } catch (error) {
            console.error('Login error:', error);
            throw error;
        }
    }

    // Logout method
    async logout() {
        try {
            // Call server logout endpoint
            const refreshToken = localStorage.getItem('umi_refresh_token');
            if (refreshToken) {
                await fetch(window.API_BASE || 'http://localhost:5000/api/v1', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ refreshToken })
                }).catch(e => console.warn('Server logout failed:', e));
            }
        } catch (e) {
            console.error('Logout error:', e);
        } finally {
            this.clearAuthData();
            // Redirect to login page
            window.location.href = '/public/signin.html';
        }
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
                try { window.authService.clearAuthData(); } catch(e){}
                localStorage.setItem('umi_logged_out', Date.now().toString());
                try { window.authService.logout(); } catch(e){ window.location.href = '../auth/signin.html'; }
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
                    try { window.authService.logout(); } catch(e){ window.location.href = '../auth/signin.html'; }
                }
            }, timeoutMs());
        }
        if (e.key === 'umi_logged_out') {
            try { window.authService.clearAuthData(); } catch(e){}
            setTimeout(()=> { window.location.href = '../auth/signin.html'; }, 800);
        }
    });
    resetTimer(true);
})();
