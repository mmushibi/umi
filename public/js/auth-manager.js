/**
 * Authentication Manager for Umi Health Portals
 * Handles authentication checks and toast notifications
 */

class AuthManager {
    constructor(apiClient = null) {
        this.apiClient = apiClient;
        this.isAuthenticated = this.checkAuthStatus();
        this.init();
    }

    init() {
        // Listen for storage changes (for multi-tab support)
        window.addEventListener('storage', (e) => {
            if (e.key === 'authToken' || e.key === 'umi_access_token') {
                this.isAuthenticated = this.checkAuthStatus();
                this.notifyAuthChange();
            }
        });
    }

    checkAuthStatus() {
        const token = localStorage.getItem('authToken') || 
                    localStorage.getItem('umi_access_token') || 
                    localStorage.getItem('umi_currentUser');
        return !!token;
    }

    notifyAuthChange() {
        // Dispatch custom event for components to listen to
        window.dispatchEvent(new CustomEvent('authStatusChanged', {
            detail: { isAuthenticated: this.isAuthenticated }
        }));
    }

    showToast(message, type = 'info', duration = 3000) {
        // Remove existing toast if any
        const existingToast = document.querySelector('.auth-toast');
        if (existingToast) {
            existingToast.remove();
        }

        // Create toast element
        const toast = document.createElement('div');
        toast.className = `auth-toast toast-${type}`;
        
        // Set toast styles
        Object.assign(toast.style, {
            position: 'fixed',
            top: '20px',
            right: '20px',
            padding: '12px 20px',
            borderRadius: '6px',
            color: 'white',
            fontWeight: '500',
            zIndex: '10000',
            fontSize: '14px',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
            transition: 'all 0.3s ease'
        });

        // Set background color based on type
        switch(type) {
            case 'success':
                toast.style.backgroundColor = '#10b981';
                break;
            case 'error':
                toast.style.backgroundColor = '#ef4444';
                break;
            case 'warning':
                toast.style.backgroundColor = '#f59e0b';
                break;
            default:
                toast.style.backgroundColor = '#3b82f6';
        }

        toast.textContent = message;
        document.body.appendChild(toast);

        // Auto remove after duration
        setTimeout(() => {
            if (toast.parentNode) {
                toast.style.opacity = '0';
                toast.style.transform = 'translateX(100%)';
                setTimeout(() => toast.remove(), 300);
            }
        }, duration);
    }

    async logout() {
        try {
            if (this.apiClient) {
                await this.apiClient.post('/auth/logout');
            }
        } catch (error) {
            console.error('Logout error:', error);
        } finally {
            this.clearAuth();
            this.showToast('Logged out successfully', 'success');
            setTimeout(() => {
                window.location.href = '/signin.html';
            }, 1000);
        }
    }

    clearAuth() {
        localStorage.removeItem('authToken');
        localStorage.removeItem('umi_access_token');
        localStorage.removeItem('umi_refresh_token');
        localStorage.removeItem('umi_current_user');
        localStorage.removeItem('umi_current_tenant');
        localStorage.removeItem('umi_current_subscription');
        localStorage.removeItem('umi_tenant_id');
        this.isAuthenticated = false;
        this.notifyAuthChange();
    }

    getCurrentUser() {
        const userStr = localStorage.getItem('umi_current_user');
        return userStr ? JSON.parse(userStr) : null;
    }

    getCurrentTenant() {
        const tenantStr = localStorage.getItem('umi_current_tenant');
        return tenantStr ? JSON.parse(tenantStr) : null;
    }

    requireAuth() {
        if (!this.isAuthenticated) {
            this.showToast('Please login to continue', 'warning');
            setTimeout(() => {
                window.location.href = '/signin.html';
            }, 1000);
            return false;
        }
        return true;
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AuthManager;
}
