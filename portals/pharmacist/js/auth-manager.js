/**
 * Authentication Manager for Pharmacist Portal
 * Handles authentication checks and toast notifications
 */

class AuthManager {
    constructor() {
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
            padding: '1rem 1.5rem',
            borderRadius: '8px',
            color: 'white',
            fontWeight: '500',
            zIndex: '10000',
            opacity: '0',
            transform: 'translateX(100%)',
            transition: 'all 0.3s ease',
            maxWidth: '400px',
            boxShadow: '0 4px 12px rgba(0, 0, 0, 0.15)'
        });

        // Set background color based on type
        const colors = {
            success: '#22c55e',
            error: '#ef4444',
            warning: '#f59e0b',
            info: '#000000'
        };
        toast.style.background = colors[type] || colors.info;

        // Add icon based on type
        const icons = {
            success: '✓',
            error: '✕',
            warning: '⚠',
            info: 'ℹ'
        };
        
        toast.innerHTML = `
            <div style="display: flex; align-items: center; gap: 0.5rem;">
                <span style="font-size: 1.2rem;">${icons[type] || icons.info}</span>
                <span>${message}</span>
            </div>
        `;

        document.body.appendChild(toast);

        // Animate in
        setTimeout(() => {
            toast.style.opacity = '1';
            toast.style.transform = 'translateX(0)';
        }, 100);

        // Remove after duration
        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }, duration);
    }

    requireAuth(featureName = 'this feature') {
        if (!this.isAuthenticated) {
            this.showToast(`Please login to access ${featureName}`, 'warning');
            
            // Optionally redirect to login after a delay
            setTimeout(() => {
                if (confirm('Would you like to login now?')) {
                    window.location.href = '../login.html';
                }
            }, 1000);
            
            return false;
        }
        return true;
    }

    loginRequired(action) {
        return this.requireAuth(action);
    }

    // Get current user info
    getCurrentUser() {
        try {
            const userStr = localStorage.getItem('umi_currentUser') || 
                          localStorage.getItem('umi_current_user');
            return userStr ? JSON.parse(userStr) : null;
        } catch (error) {
            console.error('Failed to parse user data:', error);
            return null;
        }
    }

    // Get tenant info
    getTenantInfo() {
        try {
            const tenantId = localStorage.getItem('umi_tenant_id') || 
                             localStorage.getItem('tenantId');
            return tenantId || null;
        } catch (error) {
            console.error('Failed to get tenant info:', error);
            return null;
        }
    }
}

// Export singleton instance
window.authManager = new AuthManager();

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
        inactivityTimer = setTimeout(() => {
            const last = parseInt(localStorage.getItem('umi_last_activity')||'0',10);
            if (Date.now() - last >= timeoutMs()) {
                window.authManager.showToast('Session expired due to inactivity', 'warning');
                localStorage.removeItem('authToken');
                localStorage.removeItem('umi_access_token');
                localStorage.removeItem('umi_currentUser');
                localStorage.setItem('umi_logged_out', Date.now().toString());
                setTimeout(()=> { window.location.href = '/public/admin-signin.html'; }, 1000);
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
                    window.location.href = '/public/admin-signin.html';
                }
            }, timeoutMs());
        }
        if (e.key === 'umi_logged_out') {
            window.authManager.showToast('You have been logged out', 'info');
            localStorage.removeItem('authToken');
            localStorage.removeItem('umi_access_token');
            localStorage.removeItem('umi_currentUser');
            setTimeout(()=> { window.location.href = '/public/admin-signin.html'; }, 800);
        }
    });

    resetTimer(true);
})();

// Make it available globally for Alpine.js
window.isAuthenticated = window.authManager.isAuthenticated;
window.showToast = (message, type, duration) => window.authManager.showToast(message, type, duration);
