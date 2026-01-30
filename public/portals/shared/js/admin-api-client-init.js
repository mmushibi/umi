/**
 * Umi Health Admin Portal - Global API Client Initialization
 * Ensures consistent API client setup across all admin pages
 */

(function() {
    'use strict';
    
    // Prevent multiple initializations
    if (window.umiAdminInitialized) {
        console.log('Umi Admin API Client already initialized');
        return;
    }
    
    console.log('Initializing Umi Admin API Client...');
    
    try {
        // Initialize API Client
        if (typeof ApiClient !== 'undefined') {
            window.apiClient = new ApiClient('http://localhost:5001/api/v1');
            console.log('✅ API Client initialized:', window.apiClient);
        } else {
            console.error('❌ ApiClient class not found');
            return;
        }
        
        // Initialize Auth Manager
        if (typeof AuthManager !== 'undefined') {
            window.authManager = new AuthManager(window.apiClient);
            console.log('✅ Auth Manager initialized:', window.authManager);
        } else {
            console.error('❌ AuthManager class not found');
        }
        
        // Initialize Backend Helper
        if (typeof BackendIntegrationHelper !== 'undefined') {
            window.backendHelper = new BackendIntegrationHelper();
            console.log('✅ Backend Helper initialized:', window.backendHelper);
        } else {
            console.warn('⚠️ BackendIntegrationHelper not found');
        }
        
        // Initialize Admin API
        if (typeof AdminAPI !== 'undefined') {
            window.adminAPI = new AdminAPI(window.apiClient);
            console.log('✅ Admin API initialized:', window.adminAPI);
        } else {
            console.warn('⚠️ AdminAPI not found');
        }
        
        // Global authentication check
        window.checkAdminAuth = function() {
            const token = localStorage.getItem('umi_access_token') || 
                         localStorage.getItem('auth_tokens');
            const tenant = localStorage.getItem('umi_tenant_id');
            const user = localStorage.getItem('umi_current_user');
            
            return {
                isAuthenticated: !!(token && tenant && user),
                token: token,
                tenant: tenant,
                user: user ? JSON.parse(user) : null
            };
        };
        
        // Global redirect to login if not authenticated
        window.requireAdminAuth = function() {
            const auth = window.checkAdminAuth();
            if (!auth.isAuthenticated) {
                console.warn('User not authenticated, redirecting to login...');
                // In production, redirect to login page
                // window.location.href = '../../signin.html';
                return false;
            }
            return true;
        };
        
        // Global API request helper
        window.adminAPIRequest = async function(endpoint, options = {}) {
            if (!window.apiClient) {
                throw new Error('API Client not initialized');
            }
            
            const auth = window.checkAdminAuth();
            if (!auth.isAuthenticated) {
                throw new Error('Not authenticated');
            }
            
            const defaultOptions = {
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${auth.token}`,
                    'X-Tenant-ID': auth.tenant
                }
            };
            
            const finalOptions = { ...defaultOptions, ...options };
            
            try {
                const response = await fetch(`${window.apiClient.baseUrl}${endpoint}`, finalOptions);
                return await response.json();
            } catch (error) {
                console.error('API Request failed:', error);
                throw error;
            }
        };
        
        // Mark as initialized
        window.umiAdminInitialized = true;
        
        console.log('✅ Umi Admin API Client initialization complete');
        
        // Dispatch event for other scripts to listen
        window.dispatchEvent(new CustomEvent('umiAdminReady'));
        
    } catch (error) {
        console.error('❌ Failed to initialize Umi Admin API Client:', error);
    }
})();

// Also ensure initialization runs after DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function() {
        setTimeout(function() {
            if (!window.umiAdminInitialized) {
                console.log('Retrying API Client initialization after DOM load...');
                // Re-run initialization
                if (typeof ApiClient !== 'undefined') {
                    window.apiClient = new ApiClient('http://localhost:5001/api/v1');
                    window.authManager = new AuthManager(window.apiClient);
                    window.umiAdminInitialized = true;
                    console.log('✅ API Client initialized on retry');
                }
            }
        }, 100);
    });
}
