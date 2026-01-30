/**
 * Page Integration Template for Umi Health Portals
 * Provides common page initialization and integration utilities
 */

class PageIntegrationTemplate {
    constructor(options = {}) {
        this.options = {
            requireAuth: true,
            apiClient: null,
            pageName: 'Unknown',
            ...options
        };
        
        this.init();
    }

    async init() {
        console.log(`Initializing ${this.options.pageName} page...`);
        
        // Wait for DOM to be ready
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', () => this.setup());
        } else {
            this.setup();
        }
    }

    async setup() {
        try {
            // Check authentication if required
            if (this.options.requireAuth) {
                await this.checkAuthentication();
            }

            // Initialize API client if provided
            if (this.options.apiClient) {
                await this.initializeApiClient();
            }

            // Setup common page features
            this.setupCommonFeatures();
            
            // Page-specific initialization
            if (typeof this.pageInit === 'function') {
                await this.pageInit();
            }

            console.log(`${this.options.pageName} page initialized successfully`);
        } catch (error) {
            console.error(`Failed to initialize ${this.options.pageName} page:`, error);
            this.handleInitializationError(error);
        }
    }

    async checkAuthentication() {
        const token = localStorage.getItem('umi_access_token');
        if (!token) {
            console.warn('No authentication token found');
            this.redirectToLogin();
            return false;
        }
        return true;
    }

    redirectToLogin() {
        const currentPath = window.location.pathname;
        const returnUrl = encodeURIComponent(currentPath);
        window.location.href = `/signin.html?returnUrl=${returnUrl}`;
    }

    async initializeApiClient() {
        if (!window.apiClient) {
            console.warn('API Client not available');
            return false;
        }

        // Set token if available
        const token = localStorage.getItem('umi_access_token');
        if (token) {
            window.apiClient.setToken(token);
        }

        // Set tenant ID if available
        const tenantId = localStorage.getItem('umi_tenant_id');
        if (tenantId) {
            window.apiClient.tenantId = tenantId;
        }

        return true;
    }

    setupCommonFeatures() {
        // Setup navigation
        this.setupNavigation();
        
        // Setup user menu
        this.setupUserMenu();
        
        // Setup notifications
        this.setupNotifications();
        
        // Setup theme
        this.setupTheme();
        
        // Setup responsive behavior
        this.setupResponsiveBehavior();
    }

    setupNavigation() {
        // Add active state to current navigation item
        const navLinks = document.querySelectorAll('nav a[href]');
        navLinks.forEach(link => {
            if (link.getAttribute('href') === window.location.pathname) {
                link.classList.add('active');
            }
        });
    }

    setupUserMenu() {
        const userMenuToggle = document.querySelector('[data-user-menu-toggle]');
        const userMenu = document.querySelector('[data-user-menu]');
        
        if (userMenuToggle && userMenu) {
            userMenuToggle.addEventListener('click', (e) => {
                e.stopPropagation();
                userMenu.classList.toggle('hidden');
            });

            document.addEventListener('click', () => {
                userMenu.classList.add('hidden');
            });
        }
    }

    setupNotifications() {
        // Setup notification bell and dropdown
        const notificationBell = document.querySelector('[data-notifications]');
        if (notificationBell) {
            notificationBell.addEventListener('click', () => {
                // Toggle notification dropdown
                const dropdown = document.querySelector('[data-notification-dropdown]');
                if (dropdown) {
                    dropdown.classList.toggle('hidden');
                }
            });
        }
    }

    setupTheme() {
        // Check for saved theme preference
        const savedTheme = localStorage.getItem('umi_theme') || 'light';
        document.documentElement.setAttribute('data-theme', savedTheme);

        // Setup theme toggle
        const themeToggle = document.querySelector('[data-theme-toggle]');
        if (themeToggle) {
            themeToggle.addEventListener('click', () => {
                const currentTheme = document.documentElement.getAttribute('data-theme');
                const newTheme = currentTheme === 'light' ? 'dark' : 'light';
                
                document.documentElement.setAttribute('data-theme', newTheme);
                localStorage.setItem('umi_theme', newTheme);
            });
        }
    }

    setupResponsiveBehavior() {
        // Setup mobile menu toggle
        const mobileMenuToggle = document.querySelector('[data-mobile-menu-toggle]');
        const mobileMenu = document.querySelector('[data-mobile-menu]');
        
        if (mobileMenuToggle && mobileMenu) {
            mobileMenuToggle.addEventListener('click', () => {
                mobileMenu.classList.toggle('hidden');
            });
        }

        // Handle window resize
        window.addEventListener('resize', () => {
            this.handleResize();
        });
    }

    handleResize() {
        // Add responsive behavior as needed
        const isMobile = window.innerWidth < 768;
        document.documentElement.setAttribute('data-mobile', isMobile);
    }

    handleInitializationError(error) {
        // Show error message to user
        if (typeof window !== 'undefined' && window.authManager) {
            window.authManager.showToast('Page initialization failed. Please refresh.', 'error');
        }

        // Log detailed error for debugging
        console.error('Page initialization error details:', {
            page: this.options.pageName,
            error: error.message,
            stack: error.stack,
            timestamp: new Date().toISOString()
        });
    }

    // Utility methods for page implementations
    showLoading(selector = '[data-loading]') {
        const elements = document.querySelectorAll(selector);
        elements.forEach(el => {
            el.style.display = 'block';
        });
    }

    hideLoading(selector = '[data-loading]') {
        const elements = document.querySelectorAll(selector);
        elements.forEach(el => {
            el.style.display = 'none';
        });
    }

    showContent(selector = '[data-content]') {
        const elements = document.querySelectorAll(selector);
        elements.forEach(el => {
            el.style.display = 'block';
        });
    }

    hideContent(selector = '[data-content]') {
        const elements = document.querySelectorAll(selector);
        elements.forEach(el => {
            el.style.display = 'none';
        });
    }

    // API wrapper with error handling
    async apiCall(endpoint, options = {}) {
        if (!window.apiClient) {
            throw new Error('API Client not available');
        }

        try {
            this.showLoading();
            const response = await window.apiClient.request(endpoint, options);
            return response;
        } catch (error) {
            console.error('API call failed:', error);
            if (window.authManager) {
                window.authManager.showToast('API request failed', 'error');
            }
            throw error;
        } finally {
            this.hideLoading();
        }
    }
}

// Export for use in other modules
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PageIntegrationTemplate;
}
