/**
 * Page Integration Template
 * Provides standardized template for integrating pages with Umi Health backend
 * This file should be included in all portal pages for consistent functionality
 */

// Page Integration Template Class
class PageIntegrationTemplate {
    constructor() {
        this.apiClient = window.apiClient;
        this.authManager = window.authManager;
        this.backendHelper = window.backendHelper;
        this.pageName = this.detectPageName();
        this.initialized = false;
    }

    /**
     * Detect current page name from URL
     */
    detectPageName() {
        const path = window.location.pathname;
        const filename = path.split('/').pop();
        return filename.replace('.html', '') || 'unknown';
    }

    /**
     * Initialize page integration
     */
    async initialize(options = {}) {
        try {
            console.log(`🚀 Initializing ${this.pageName} page...`);

            // Wait for DOM to be ready
            if (document.readyState === 'loading') {
                await new Promise(resolve => {
                    document.addEventListener('DOMContentLoaded', resolve);
                });
            }

            // Initialize backend helper if available
            if (window.backendHelper) {
                await window.backendHelper.initPage(this.pageName, {
                    enableRealTime: options.enableRealTime || false,
                    initialData: options.initialData || {}
                });
            }

            // Set up common page functionality
            this.setupCommonFunctionality();
            this.setupPageSpecificFunctionality();
            this.setupEventListeners();

            this.initialized = true;
            console.log(`✅ ${this.pageName} page initialized successfully`);

        } catch (error) {
            console.error(`❌ Failed to initialize ${this.pageName} page:`, error);
        }
    }

    /**
     * Setup common functionality for all pages
     */
    setupCommonFunctionality() {
        // Setup mobile menu toggle
        this.setupMobileMenu();
        
        // Setup logout functionality
        this.setupLogout();
        
        // Setup user profile display
        this.setupUserProfile();
        
        // Setup notification system
        this.setupNotifications();
    }

    /**
     * Setup mobile menu toggle
     */
    setupMobileMenu() {
        const mobileMenuButtons = document.querySelectorAll('[data-mobile-menu-toggle]');
        mobileMenuButtons.forEach(button => {
            button.addEventListener('click', () => {
                const sidebar = document.querySelector('.sidebar');
                if (sidebar) {
                    sidebar.classList.toggle('open');
                }
            });
        });
    }

    /**
     * Setup logout functionality
     */
    setupLogout() {
        const logoutButtons = document.querySelectorAll('[data-logout]');
        logoutButtons.forEach(button => {
            button.addEventListener('click', async (e) => {
                e.preventDefault();
                await this.performLogout();
            });
        });
    }

    /**
     * Perform logout
     */
    async performLogout() {
        try {
            if (this.authManager) {
                await this.authManager.logout();
            }
            
            // Redirect to signin page
            window.location.href = '../../public/signin.html';
        } catch (error) {
            console.error('Logout failed:', error);
            // Force redirect even on error
            window.location.href = '../../public/signin.html';
        }
    }

    /**
     * Setup user profile display
     */
    setupUserProfile() {
        if (this.authManager) {
            const authInfo = this.authManager.getAuthInfo();
            if (authInfo.isAuthenticated && authInfo.user) {
                // Update user name displays
                const userNameElements = document.querySelectorAll('[data-user-name]');
                userNameElements.forEach(element => {
                    element.textContent = `${authInfo.user.firstName} ${authInfo.user.lastName}`;
                });

                // Update user email displays
                const userEmailElements = document.querySelectorAll('[data-user-email]');
                userEmailElements.forEach(element => {
                    element.textContent = authInfo.user.email;
                });

                // Update user role displays
                const userRoleElements = document.querySelectorAll('[data-user-role]');
                userRoleElements.forEach(element => {
                    element.textContent = authInfo.user.role;
                });
            }
        }
    }

    /**
     * Setup notification system
     */
    setupNotifications() {
        // Setup toast notifications
        this.setupToastNotifications();
        
        // Setup modal notifications
        this.setupModalNotifications();
    }

    /**
     * Setup toast notifications
     */
    setupToastNotifications() {
        // Create toast container if it doesn't exist
        if (!document.querySelector('.toast-container')) {
            const container = document.createElement('div');
            container.className = 'toast-container';
            document.body.appendChild(container);
        }
    }

    /**
     * Setup modal notifications
     */
    setupModalNotifications() {
        // Handle subscription modal if present
        const subscriptionModal = document.querySelector('[data-subscription-modal]');
        if (subscriptionModal) {
            this.setupSubscriptionModal(subscriptionModal);
        }
    }

    /**
     * Setup subscription modal
     */
    setupSubscriptionModal(modal) {
        const closeButtons = modal.querySelectorAll('[data-modal-close]');
        closeButtons.forEach(button => {
            button.addEventListener('click', () => {
                modal.classList.remove('show');
                modal.style.display = 'none';
            });
        });
    }

    /**
     * Setup page-specific functionality
     */
    setupPageSpecificFunctionality() {
        // Override in specific page implementations
        console.log(`Setting up ${this.pageName} specific functionality`);
    }

    /**
     * Setup event listeners
     */
    setupEventListeners() {
        // Setup form submissions
        this.setupFormSubmissions();
        
        // Setup data table interactions
        this.setupDataTables();
        
        // Setup search functionality
        this.setupSearch();
    }

    /**
     * Setup form submissions
     */
    setupFormSubmissions() {
        const forms = document.querySelectorAll('form[data-api-submit]');
        forms.forEach(form => {
            form.addEventListener('submit', async (e) => {
                e.preventDefault();
                await this.handleFormSubmit(form);
            });
        });
    }

    /**
     * Handle form submission
     */
    async handleFormSubmit(form) {
        try {
            const formData = new FormData(form);
            const data = Object.fromEntries(formData.entries());
            const endpoint = form.dataset.apiSubmit;
            const method = form.dataset.method || 'POST';

            if (this.apiClient) {
                const response = await this.apiClient.request(endpoint, {
                    method: method,
                    body: JSON.stringify(data)
                });

                if (response.success) {
                    this.showToast('Success!', 'success');
                    if (form.dataset.resetOnSuccess !== 'false') {
                        form.reset();
                    }
                } else {
                    this.showToast(response.message || 'Error occurred', 'error');
                }
            }
        } catch (error) {
            console.error('Form submission error:', error);
            this.showToast('An error occurred', 'error');
        }
    }

    /**
     * Setup data tables
     */
    setupDataTables() {
        const tables = document.querySelectorAll('[data-table]');
        tables.forEach(table => {
            this.setupDataTable(table);
        });
    }

    /**
     * Setup individual data table
     */
    setupDataTable(table) {
        const endpoint = table.dataset.table;
        const refreshButton = table.querySelector('[data-table-refresh]');
        
        if (refreshButton) {
            refreshButton.addEventListener('click', async () => {
                await this.refreshTable(table, endpoint);
            });
        }

        // Initial load
        this.refreshTable(table, endpoint);
    }

    /**
     * Refresh data table
     */
    async refreshTable(table, endpoint) {
        try {
            const tbody = table.querySelector('tbody');
            if (tbody) {
                tbody.innerHTML = '<tr><td colspan="100%" class="text-center">Loading...</td></tr>';
            }

            if (this.apiClient) {
                const response = await this.apiClient.request(endpoint);
                if (response.success && tbody) {
                    this.populateTable(tbody, response.data);
                }
            }
        } catch (error) {
            console.error('Table refresh error:', error);
            const tbody = table.querySelector('tbody');
            if (tbody) {
                tbody.innerHTML = '<tr><td colspan="100%" class="text-center text-danger">Error loading data</td></tr>';
            }
        }
    }

    /**
     * Populate table with data
     */
    populateTable(tbody, data) {
        if (!data || data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="100%" class="text-center">No data available</td></tr>';
            return;
        }

        tbody.innerHTML = '';
        data.forEach(item => {
            const row = this.createTableRow(item);
            tbody.appendChild(row);
        });
    }

    /**
     * Create table row for data item
     */
    createTableRow(item) {
        const row = document.createElement('tr');
        // Override in specific implementations
        row.innerHTML = `<td>${JSON.stringify(item)}</td>`;
        return row;
    }

    /**
     * Setup search functionality
     */
    setupSearch() {
        const searchInputs = document.querySelectorAll('[data-search]');
        searchInputs.forEach(input => {
            input.addEventListener('input', this.debounce((e) => {
                this.handleSearch(e.target.value, e.target.dataset.search);
            }, 300));
        });
    }

    /**
     * Handle search
     */
    async handleSearch(query, endpoint) {
        try {
            if (this.apiClient && query.length >= 2) {
                const response = await this.apiClient.request(`${endpoint}?q=${encodeURIComponent(query)}`);
                // Handle search results - override in specific implementations
                console.log('Search results:', response.data);
            }
        } catch (error) {
            console.error('Search error:', error);
        }
    }

    /**
     * Show toast notification
     */
    showToast(message, type = 'info', duration = 5000) {
        const container = document.querySelector('.toast-container');
        if (!container) return;

        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <div class="toast-content">
                <span class="toast-message">${message}</span>
                <button class="toast-close" onclick="this.parentElement.parentElement.remove()">×</button>
            </div>
        `;

        // Add styles if not already present
        if (!document.querySelector('#toast-styles')) {
            const style = document.createElement('style');
            style.id = 'toast-styles';
            style.textContent = `
                .toast-container {
                    position: fixed;
                    top: 20px;
                    right: 20px;
                    z-index: 10000;
                }
                .toast {
                    background: white;
                    border-radius: 8px;
                    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
                    margin-bottom: 10px;
                    min-width: 300px;
                    animation: slideIn 0.3s ease-out;
                }
                .toast-success { border-left: 4px solid #22c55e; }
                .toast-error { border-left: 4px solid #ef4444; }
                .toast-warning { border-left: 4px solid #f59e0b; }
                .toast-info { border-left: 4px solid #3b82f6; }
                .toast-content {
                    display: flex;
                    align-items: center;
                    justify-content: space-between;
                    padding: 1rem;
                }
                .toast-close {
                    background: none;
                    border: none;
                    font-size: 1.25rem;
                    cursor: pointer;
                    opacity: 0.6;
                }
                .toast-close:hover { opacity: 1; }
                @keyframes slideIn {
                    from { transform: translateX(100%); opacity: 0; }
                    to { transform: translateX(0); opacity: 1; }
                }
            `;
            document.head.appendChild(style);
        }

        container.appendChild(toast);

        // Auto-remove after duration
        setTimeout(() => {
            if (toast.parentElement) {
                toast.remove();
            }
        }, duration);
    }

    /**
     * Debounce function
     */
    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Show loading state
     */
    setLoading(element, loading = true) {
        if (loading) {
            element.classList.add('loading');
            element.disabled = true;
        } else {
            element.classList.remove('loading');
            element.disabled = false;
        }
    }

    /**
     * Format currency
     */
    formatCurrency(amount, currency = 'USD') {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: currency
        }).format(amount);
    }

    /**
     * Format date
     */
    formatDate(date, format = 'short') {
        const dateObj = new Date(date);
        
        switch (format) {
            case 'short':
                return dateObj.toLocaleDateString();
            case 'long':
                return dateObj.toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric'
                });
            case 'datetime':
                return dateObj.toLocaleString();
            default:
                return dateObj.toLocaleDateString();
        }
    }
}

// Create global instance
const pageIntegration = new PageIntegrationTemplate();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    pageIntegration.initialize();
});

// Export for use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = pageIntegration;
}

// Make available globally
window.pageIntegration = pageIntegration;
