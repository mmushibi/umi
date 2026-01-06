/**
 * Page Integration Template
 * Standardized template for integrating frontend pages with backend API
 * This template provides common functionality that can be extended for specific pages
 */

class PageIntegrationTemplate {
    constructor(pageName, options = {}) {
        this.pageName = pageName;
        this.options = {
            requireAuth: true,
            enableRealTime: false,
            enableCaching: true,
            pageSize: 20,
            ...options
        };
        
        this.backendHelper = window.backendHelper;
        this.apiClient = window.apiClient;
        this.authManager = window.authManager;
        
        this.data = {};
        this.loading = {};
        this.error = null;
        this.searchResults = [];
        this.pagination = {
            currentPage: 1,
            totalPages: 0,
            totalItems: 0,
            pageSize: this.options.pageSize
        };
    }

    /**
     * Initialize the page
     */
    async init() {
        try {
            console.log(`🚀 Initializing ${this.pageName} page...`);
            
            // Initialize backend integration
            await this.backendHelper.initPage(this.pageName, {
                enableRealTime: this.options.enableRealTime,
                initialData: this.getInitialDataConfig()
            });
            
            // Check authentication if required
            if (this.options.requireAuth && !this.authManager.isAuthenticated()) {
                this.handleAuthRequired();
                return;
            }
            
            // Load initial data
            await this.loadInitialData();
            
            // Set up event listeners
            this.setupEventListeners();
            
            // Initialize UI components
            this.initializeUI();
            
            console.log(`✅ ${this.pageName} page initialized successfully`);
            
        } catch (error) {
            console.error(`❌ Failed to initialize ${this.pageName} page:`, error);
            this.error = error.message;
            this.showErrorState();
        }
    }

    /**
     * Override this method to define initial data configuration
     */
    getInitialDataConfig() {
        return {};
    }

    /**
     * Load initial data for the page
     */
    async loadInitialData() {
        const dataConfig = this.getInitialDataConfig();
        
        if (Object.keys(dataConfig).length === 0) {
            return;
        }
        
        try {
            this.setLoading(true);
            this.data = await this.backendHelper.loadInitialData(dataConfig);
            this.error = null;
            this.updateUIWithData();
        } catch (error) {
            this.error = error.message;
            this.showErrorState();
        } finally {
            this.setLoading(false);
        }
    }

    /**
     * Set loading state
     */
    setLoading(loading) {
        this.loading = { ...this.loading, global: loading };
        this.updateLoadingUI();
    }

    /**
     * Update UI to show loading state
     */
    updateLoadingUI() {
        // Show/hide loading indicators
        const loadingElements = document.querySelectorAll('[data-loading]');
        loadingElements.forEach(element => {
            if (this.loading.global) {
                element.classList.add('loading');
                element.disabled = true;
            } else {
                element.classList.remove('loading');
                element.disabled = false;
            }
        });
        
        // Show/hide loading sections
        const loadingSections = document.querySelectorAll('[data-loading-section]');
        loadingSections.forEach(section => {
            section.style.display = this.loading.global ? 'block' : 'none';
        });
        
        // Show/hide content sections
        const contentSections = document.querySelectorAll('[data-content-section]');
        contentSections.forEach(section => {
            section.style.display = this.loading.global ? 'none' : 'block';
        });
    }

    /**
     * Show error state
     */
    showErrorState() {
        const errorElements = document.querySelectorAll('[data-error]');
        errorElements.forEach(element => {
            element.textContent = this.error || 'An error occurred';
            element.style.display = 'block';
        });
        
        // Hide content sections
        const contentSections = document.querySelectorAll('[data-content-section]');
        contentSections.forEach(section => {
            section.style.display = 'none';
        });
    }

    /**
     * Update UI with loaded data
     */
    updateUIWithData() {
        // This should be implemented by specific page classes
        console.log('📊 Data loaded:', this.data);
    }

    /**
     * Set up event listeners
     */
    setupEventListeners() {
        // Common event listeners
        this.setupSearchListeners();
        this.setupPaginationListeners();
        this.setupFormListeners();
        this.setupActionListeners();
    }

    /**
     * Set up search functionality
     */
    setupSearchListeners() {
        const searchInputs = document.querySelectorAll('[data-search]');
        searchInputs.forEach(input => {
            const searchType = input.dataset.search;
            const searchConfig = this.getSearchConfig(searchType);
            
            if (searchConfig) {
                input.addEventListener('input', this.backendHelper.debounce(async (e) => {
                    const query = e.target.value;
                    await this.performSearch(searchType, query, searchConfig);
                }, 300));
            }
        });
    }

    /**
     * Get search configuration for a specific type
     */
    getSearchConfig(searchType) {
        // Override this method to provide search configurations
        return null;
    }

    /**
     * Perform search
     */
    async performSearch(searchType, query, config) {
        try {
            this.setLoading({ [searchType]: true });
            
            const results = await config.search(query);
            this.searchResults = results;
            
            // Update search results UI
            this.updateSearchResults(searchType, results);
            
        } catch (error) {
            this.backendHelper.handleError(error, `Search: ${searchType}`);
        } finally {
            this.setLoading({ [searchType]: false });
        }
    }

    /**
     * Update search results UI
     */
    updateSearchResults(searchType, results) {
        const resultsContainer = document.querySelector(`[data-search-results="${searchType}"]`);
        if (resultsContainer) {
            // Clear existing results
            resultsContainer.innerHTML = '';
            
            if (results.length === 0) {
                resultsContainer.innerHTML = '<div class="no-results">No results found</div>';
                return;
            }
            
            // Render results
            results.forEach(result => {
                const resultElement = this.createSearchResultElement(searchType, result);
                resultsContainer.appendChild(resultElement);
            });
        }
    }

    /**
     * Create search result element
     */
    createSearchResultElement(searchType, result) {
        const element = document.createElement('div');
        element.className = 'search-result-item';
        element.textContent = JSON.stringify(result); // Default implementation
        return element;
    }

    /**
     * Set up pagination listeners
     */
    setupPaginationListeners() {
        const paginationButtons = document.querySelectorAll('[data-pagination]');
        paginationButtons.forEach(button => {
            button.addEventListener('click', async (e) => {
                const action = e.target.dataset.pagination;
                await this.handlePaginationAction(action);
            });
        });
    }

    /**
     * Handle pagination actions
     */
    async handlePaginationAction(action) {
        switch (action) {
            case 'first':
                this.pagination.currentPage = 1;
                break;
            case 'prev':
                this.pagination.currentPage = Math.max(1, this.pagination.currentPage - 1);
                break;
            case 'next':
                this.pagination.currentPage = Math.min(this.pagination.totalPages, this.pagination.currentPage + 1);
                break;
            case 'last':
                this.pagination.currentPage = this.pagination.totalPages;
                break;
            default:
                // Assume it's a page number
                this.pagination.currentPage = parseInt(action);
        }
        
        await this.loadPageData();
        this.updatePaginationUI();
    }

    /**
     * Load data for current page
     */
    async loadPageData() {
        // Override this method to implement page-specific data loading
        console.log(`📄 Loading page ${this.pagination.currentPage}`);
    }

    /**
     * Update pagination UI
     */
    updatePaginationUI() {
        const paginationElements = document.querySelectorAll('[data-pagination-info]');
        paginationElements.forEach(element => {
            element.textContent = `Page ${this.pagination.currentPage} of ${this.pagination.totalPages}`;
        });
        
        // Update button states
        const prevButtons = document.querySelectorAll('[data-pagination="prev"], [data-pagination="first"]');
        prevButtons.forEach(button => {
            button.disabled = this.pagination.currentPage === 1;
        });
        
        const nextButtons = document.querySelectorAll('[data-pagination="next"], [data-pagination="last"]');
        nextButtons.forEach(button => {
            button.disabled = this.pagination.currentPage === this.pagination.totalPages;
        });
    }

    /**
     * Set up form listeners
     */
    setupFormListeners() {
        const forms = document.querySelectorAll('[data-form]');
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
        const formType = form.dataset.form;
        const formData = new FormData(form);
        const data = Object.fromEntries(formData.entries());
        
        try {
            this.setLoading({ [formType]: true });
            
            const result = await this.submitForm(formType, data);
            
            if (result.success) {
                this.backendHelper.showNotification('Form submitted successfully', 'success');
                this.handleFormSuccess(formType, result);
            } else {
                throw new Error(result.message || 'Form submission failed');
            }
            
        } catch (error) {
            this.backendHelper.handleError(error, `Form: ${formType}`);
        } finally {
            this.setLoading({ [formType]: false });
        }
    }

    /**
     * Submit form data - override in specific page classes
     */
    async submitForm(formType, data) {
        console.log(`📝 Submitting ${formType} form:`, data);
        return { success: true, data: data };
    }

    /**
     * Handle successful form submission
     */
    handleFormSuccess(formType, result) {
        // Reset form
        const form = document.querySelector(`[data-form="${formType}"]`);
        if (form) {
            form.reset();
        }
        
        // Reload data if needed
        this.loadInitialData();
    }

    /**
     * Set up action listeners
     */
    setupActionListeners() {
        const actionButtons = document.querySelectorAll('[data-action]');
        actionButtons.forEach(button => {
            button.addEventListener('click', async (e) => {
                const action = e.target.dataset.action;
                const target = e.target.dataset.actionTarget;
                await this.handleAction(action, target);
            });
        });
    }

    /**
     * Handle button actions
     */
    async handleAction(action, target) {
        try {
            this.setLoading({ [action]: true });
            
            const result = await this.executeAction(action, target);
            
            if (result.success) {
                this.backendHelper.showNotification('Action completed successfully', 'success');
                this.handleActionSuccess(action, target, result);
            } else {
                throw new Error(result.message || 'Action failed');
            }
            
        } catch (error) {
            this.backendHelper.handleError(error, `Action: ${action}`);
        } finally {
            this.setLoading({ [action]: false });
        }
    }

    /**
     * Execute action - override in specific page classes
     */
    async executeAction(action, target) {
        console.log(`🎯 Executing action: ${action} on target: ${target}`);
        return { success: true };
    }

    /**
     * Handle successful action
     */
    handleActionSuccess(action, target, result) {
        // Reload data if needed
        this.loadInitialData();
    }

    /**
     * Handle authentication requirement
     */
    handleAuthRequired() {
        // Show authentication required message
        const authElements = document.querySelectorAll('[data-auth-required]');
        authElements.forEach(element => {
            element.style.display = 'block';
        });
        
        // Hide content sections
        const contentSections = document.querySelectorAll('[data-content-section]');
        contentSections.forEach(section => {
            section.style.display = 'none';
        });
        
        // Show login prompt
        this.backendHelper.showNotification('Please login to access this page', 'warning');
    }

    /**
     * Initialize UI components
     */
    initializeUI() {
        // Initialize tooltips, modals, etc.
        this.initializeTooltips();
        this.initializeModals();
        this.initializeDropdowns();
    }

    /**
     * Initialize tooltips
     */
    initializeTooltips() {
        const tooltipElements = document.querySelectorAll('[data-tooltip]');
        tooltipElements.forEach(element => {
            element.addEventListener('mouseenter', (e) => {
                this.showTooltip(e.target, e.target.dataset.tooltip);
            });
            
            element.addEventListener('mouseleave', (e) => {
                this.hideTooltip();
            });
        });
    }

    /**
     * Show tooltip
     */
    showTooltip(element, text) {
        // Create tooltip element
        const tooltip = document.createElement('div');
        tooltip.className = 'tooltip';
        tooltip.textContent = text;
        
        // Position tooltip
        const rect = element.getBoundingClientRect();
        tooltip.style.position = 'fixed';
        tooltip.style.top = `${rect.bottom + 5}px`;
        tooltip.style.left = `${rect.left}px`;
        tooltip.style.zIndex = '10000';
        
        document.body.appendChild(tooltip);
    }

    /**
     * Hide tooltip
     */
    hideTooltip() {
        const tooltip = document.querySelector('.tooltip');
        if (tooltip) {
            tooltip.remove();
        }
    }

    /**
     * Initialize modals
     */
    initializeModals() {
        const modalTriggers = document.querySelectorAll('[data-modal]');
        modalTriggers.forEach(trigger => {
            trigger.addEventListener('click', (e) => {
                const modalId = e.target.dataset.modal;
                this.showModal(modalId);
            });
        });
        
        const modalCloses = document.querySelectorAll('[data-modal-close]');
        modalCloses.forEach(close => {
            close.addEventListener('click', (e) => {
                const modal = e.target.closest('.modal');
                if (modal) {
                    this.hideModal(modal.id);
                }
            });
        });
    }

    /**
     * Show modal
     */
    showModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.style.display = 'block';
            modal.classList.add('show');
        }
    }

    /**
     * Hide modal
     */
    hideModal(modalId) {
        const modal = document.getElementById(modalId);
        if (modal) {
            modal.style.display = 'none';
            modal.classList.remove('show');
        }
    }

    /**
     * Initialize dropdowns
     */
    initializeDropdowns() {
        const dropdownTriggers = document.querySelectorAll('[data-dropdown]');
        dropdownTriggers.forEach(trigger => {
            trigger.addEventListener('click', (e) => {
                e.stopPropagation();
                const dropdownId = e.target.dataset.dropdown;
                this.toggleDropdown(dropdownId);
            });
        });
        
        // Close dropdowns when clicking outside
        document.addEventListener('click', () => {
            const dropdowns = document.querySelectorAll('.dropdown.show');
            dropdowns.forEach(dropdown => {
                dropdown.classList.remove('show');
            });
        });
    }

    /**
     * Toggle dropdown
     */
    toggleDropdown(dropdownId) {
        const dropdown = document.getElementById(dropdownId);
        if (dropdown) {
            dropdown.classList.toggle('show');
        }
    }

    /**
     * Get current user info
     */
    getCurrentUser() {
        return this.authManager?.getCurrentUser() || null;
    }

    /**
     * Check if user has specific role
     */
    hasRole(role) {
        return this.authManager?.hasRole(role) || false;
    }

    /**
     * Check if user has specific permission
     */
    hasPermission(permission) {
        return this.authManager?.hasPermission(permission) || false;
    }

    /**
     * Format currency
     */
    formatCurrency(amount) {
        return this.backendHelper.formatCurrency(amount);
    }

    /**
     * Format date
     */
    formatDate(date, format = 'short') {
        return this.backendHelper.formatDate(date, format);
    }
}

// Export for use
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PageIntegrationTemplate;
}
