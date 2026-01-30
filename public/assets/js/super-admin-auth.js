// Super Admin Authentication Service for Umi Health
window.UmiHealthInformationSystemsAuth = {
    async login(identifier, password) {
        try {
            const response = await fetch('/api/v1/auth/super-admin/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ identifier, password })
            });
            
            const result = await response.json();
            
            if (result.success) {
                // Store auth data
                localStorage.setItem('umi_access_token', result.accessToken);
                localStorage.setItem('umi_refresh_token', result.refreshToken);
                localStorage.setItem('umi_current_user', JSON.stringify(result.user));
                localStorage.setItem('umi_current_tenant', JSON.stringify(result.tenant));
                localStorage.setItem('umi_current_subscription', JSON.stringify(result.subscription));
                localStorage.setItem('umi_tenant_id', result.tenant.id);
                
                return result;
            } else {
                return { success: false, message: result.message || 'Invalid credentials' };
            }
        } catch (error) {
            console.error('Super admin login error:', error);
            return { success: false, message: 'Failed to connect to authentication service' };
        }
    },
    
    async forgotPassword(email) {
        try {
            const response = await fetch('/api/v1/auth/forgot-password', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ email })
            });
            
            return await response.json();
        } catch (error) {
            console.error('Forgot password error:', error);
            return { success: false, message: 'Failed to send password reset email' };
        }
    },
    
    clearAuth() {
        localStorage.removeItem('umi_access_token');
        localStorage.removeItem('umi_refresh_token');
        localStorage.removeItem('umi_current_user');
        localStorage.removeItem('umi_current_tenant');
        localStorage.removeItem('umi_current_subscription');
        localStorage.removeItem('umi_tenant_id');
    },
    
    updateAuthHeaders() {
        // Update API client headers if needed
        const token = localStorage.getItem('umi_access_token');
        if (token && window.apiClient) {
            window.apiClient.setAuthToken(token);
        }
    },
    
    redirectBasedOnRole() {
        const userStr = localStorage.getItem('umi_current_user');
        if (!userStr) {
            window.location.href = '/public/signin.html';
            return;
        }
        
        const user = JSON.parse(userStr);
        
        // Redirect super admin to super-admin portal
        if (user.role === 'superadmin') {
            window.location.href = '../portals/super-admin/home.html';
        } else {
            // If not super admin, redirect to regular sign-in
            this.clearAuth();
            window.location.href = '/public/signin.html';
        }
    }
};
