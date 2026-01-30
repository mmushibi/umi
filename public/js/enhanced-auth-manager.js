/**
 * Enhanced Authentication Manager for Umi Health Portals
 * Provides loading states, better UX, and comprehensive auth handling
 */

class EnhancedAuthManager {
    constructor(options = {}) {
        this.options = {
            signInUrl: '../../public/signin.html',
            adminSignInUrl: '/public/admin-signin.html',
            loadingTimeout: 10000,
            enableLogging: true,
            ...options
        };
        
        this.loadingElement = null;
        this.isAuthenticated = this.checkAuthStatus();
        this.init();
    }

    init() {
        this.createLoadingElement();
        this.setupStorageListener();
        this.log('Enhanced Auth Manager initialized');
    }

    createLoadingElement() {
        // Create global loading overlay
        this.loadingElement = document.createElement('div');
        this.loadingElement.id = 'auth-loading-overlay';
        this.loadingElement.innerHTML = `
            <div class="auth-loading-content">
                <div class="auth-loading-spinner"></div>
                <div class="auth-loading-text">Verifying authentication...</div>
            </div>
        `;
        this.loadingElement.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            background: rgba(255, 255, 255, 0.95);
            display: none;
            justify-content: center;
            align-items: center;
            z-index: 99999;
            font-family: 'Nunito', sans-serif;
        `;

        const style = document.createElement('style');
        style.textContent = `
            .auth-loading-content {
                text-align: center;
                padding: 2rem;
            }
            .auth-loading-spinner {
                width: 40px;
                height: 40px;
                border: 4px solid #f3f4f6;
                border-top: 4px solid #14B8A6;
                border-radius: 50%;
                animation: auth-spin 1s linear infinite;
                margin: 0 auto 1rem;
            }
            .auth-loading-text {
                color: #374151;
                font-size: 16px;
                font-weight: 500;
            }
            @keyframes auth-spin {
                0% { transform: rotate(0deg); }
                100% { transform: rotate(360deg); }
            }
        `;
        document.head.appendChild(style);
        document.body.appendChild(this.loadingElement);
    }

    setupStorageListener() {
        window.addEventListener('storage', (e) => {
            if (e.key === 'umi_access_token' || e.key === 'authToken') {
                this.isAuthenticated = this.checkAuthStatus();
                this.notifyAuthChange();
            }
        });
    }

    checkAuthStatus() {
        const token = localStorage.getItem('umi_access_token') || 
                    localStorage.getItem('authToken') || 
                    localStorage.getItem('umi_currentUser');
        return !!token;
    }

    showLoading(message = 'Verifying authentication...') {
        if (this.loadingElement) {
            const textElement = this.loadingElement.querySelector('.auth-loading-text');
            if (textElement) {
                textElement.textContent = message;
            }
            this.loadingElement.style.display = 'flex';
            this.log('Loading overlay shown');
        }

        // Auto-hide after timeout
        setTimeout(() => {
            this.hideLoading();
        }, this.options.loadingTimeout);
    }

    hideLoading() {
        if (this.loadingElement) {
            this.loadingElement.style.display = 'none';
            this.log('Loading overlay hidden');
        }
    }

    async requireAuth(showLoading = true) {
        if (showLoading) {
            this.showLoading();
        }

        // Simulate auth check delay for better UX
        await new Promise(resolve => setTimeout(resolve, 500));

        if (!this.isAuthenticated) {
            this.log('Authentication required - redirecting to sign in');
            this.showToast('Please sign in to continue', 'warning');
            
            setTimeout(() => {
                window.location.href = this.options.signInUrl;
            }, 1000);
            return false;
        }

        this.hideLoading();
        return true;
    }

    async requireRole(requiredRole, showLoading = true) {
        if (showLoading) {
            this.showLoading('Verifying permissions...');
        }

        const hasAuth = await this.requireAuth(false);
        if (!hasAuth) return false;

        const userData = localStorage.getItem('umi_current_user');
        if (!userData) {
            this.log('No user data found');
            return false;
        }

        try {
            const user = JSON.parse(userData);
            if (user.role !== requiredRole) {
                this.log(`Role mismatch: expected ${requiredRole}, got ${user.role}`);
                this.showToast('Access denied: insufficient permissions', 'error');
                
                setTimeout(() => {
                    window.location.href = this.options.signInUrl;
                }, 2000);
                return false;
            }

            this.hideLoading();
            return true;
        } catch (error) {
            this.log('Error parsing user data:', error);
            return false;
        }
    }

    showToast(message, type = 'info', duration = 3000) {
        // Remove existing toast
        const existingToast = document.querySelector('.enhanced-auth-toast');
        if (existingToast) {
            existingToast.remove();
        }

        const toast = document.createElement('div');
        toast.className = `enhanced-auth-toast toast-${type}`;
        
        const icons = {
            success: 'check_circle',
            error: 'error',
            warning: 'warning',
            info: 'info'
        };

        toast.innerHTML = `
            <div class="enhanced-auth-toast-content">
                <span class="material-symbols-rounded">${icons[type]}</span>
                <span>${message}</span>
            </div>
        `;

        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 12px 20px;
            border-radius: 8px;
            color: white;
            font-weight: 500;
            z-index: 100000;
            font-size: 14px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            transition: all 0.3s ease;
            display: flex;
            align-items: center;
            gap: 8px;
        `;

        // Set background color based on type
        const colors = {
            success: '#10b981',
            error: '#ef4444',
            warning: '#f59e0b',
            info: '#3b82f6'
        };
        toast.style.backgroundColor = colors[type];

        document.body.appendChild(toast);

        // Animate in
        setTimeout(() => {
            toast.style.transform = 'translateX(0)';
            toast.style.opacity = '1';
        }, 100);

        // Auto remove
        setTimeout(() => {
            toast.style.transform = 'translateX(100%)';
            toast.style.opacity = '0';
            setTimeout(() => toast.remove(), 300);
        }, duration);
    }

    notifyAuthChange() {
        window.dispatchEvent(new CustomEvent('authStatusChanged', {
            detail: { isAuthenticated: this.isAuthenticated }
        }));
    }

    logout() {
        this.showLoading('Signing out...');
        
        // Clear all auth data
        const keysToRemove = [
            'umi_access_token',
            'umi_refresh_token',
            'umi_current_user',
            'umi_current_tenant',
            'umi_current_subscription',
            'umi_tenant_id',
            'authToken',
            'umi_currentUser'
        ];

        keysToRemove.forEach(key => localStorage.removeItem(key));
        this.isAuthenticated = false;

        this.showToast('Signed out successfully', 'success');
        
        setTimeout(() => {
            window.location.href = this.options.signInUrl;
        }, 1500);
    }

    log(message, data = null) {
        if (this.options.enableLogging) {
            console.log(`[EnhancedAuthManager] ${message}`, data || '');
        }
    }

    // Static method for easy initialization
    static create(options = {}) {
        return new EnhancedAuthManager(options);
    }
}

// Global instance
window.EnhancedAuthManager = EnhancedAuthManager;

// Auto-initialize if data-auth-enhanced attribute is present
document.addEventListener('DOMContentLoaded', () => {
    if (document.querySelector('[data-auth-enhanced]')) {
        window.enhancedAuth = EnhancedAuthManager.create();
    }
});
