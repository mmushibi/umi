/**
 * Backend Integration Batch Script
 * Automatically integrates frontend pages with the Umi Health backend API
 * This script processes multiple pages and adds the necessary backend integration
 */

const fs = require('fs');
const path = require('path');

class BackendIntegrationBatch {
    constructor() {
        this.portalsDir = path.join(__dirname, '../portals');
        this.integrationTemplate = this.getIntegrationTemplate();
        this.processedPages = 0;
        this.errors = [];
    }

    /**
     * Get the integration template for adding to pages
     */
    getIntegrationTemplate() {
        return {
            scripts: `
    <!-- Backend Integration Libraries -->
    <script src="../../js/api-client.js"></script>
    <script src="../../js/auth-manager.js"></script>
    <script src="../shared/js/backend-integration-helper.js"></script>
    <script src="../shared/js/page-integration-template.js"></script>
    <script src="../shared/js/signalr-client.js"></script>
    <script src="../shared/js/realtime-events.js"></script>
    <script src="https://unpkg.com/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>
    <script src="https://unpkg.com/alpinejs@3.x.x/dist/cdn.min.js" defer></script>`,
            
            alpineInit: (pageName, enableRealTime) => `
        // Initialize backend integration
        const pageIntegration = new PageIntegrationTemplate('${pageName}', {
            requireAuth: true,
            enableRealTime: ${enableRealTime},
            enableCaching: true
        });
        
        return {
            ...pageIntegration,
            backendHelper: window.backendHelper,
            apiClient: window.apiClient,
            authManager: window.authManager,
            isAuthenticated: false,
            loading: false,
            error: null,`,
            
            initMethod: (pageName, enableRealTime) => `
        async init() {
            try {
                // Check authentication
                this.isAuthenticated = this.authManager?.isAuthenticated() || false;
                
                if (!this.isAuthenticated) {
                    this.error = 'Please login to access this page';
                    return;
                }
                
                // Initialize backend integration
                await this.backendHelper.initPage('${pageName}', {
                    enableRealTime: ${enableRealTime},
                    initialData: this.getInitialDataConfig()
                });
                
                // Load page data
                await this.loadPageData();
                
                // Set up event listeners
                this.setupEventListeners();
                
                console.log('${pageName} page initialized with backend integration');
            } catch (error) {
                console.error('Failed to initialize ${pageName} page:', error);
                this.error = 'Failed to initialize page';
            }
        },`
        };
    }

    /**
     * Process all pages in a portal
     */
    async processPortal(portalName, pages) {
        console.log(`🚀 Processing ${portalName} portal...`);
        
        const portalDir = path.join(this.portalsDir, portalName);
        
        for (const page of pages) {
            try {
                await this.processPage(portalDir, page, portalName);
                this.processedPages++;
            } catch (error) {
                this.errors.push({
                    portal: portalName,
                    page: page,
                    error: error.message
                });
                console.error(`❌ Error processing ${portalName}/${page}:`, error.message);
            }
        }
    }

    /**
     * Process a single page
     */
    async processPage(portalDir, pageName, portalName) {
        const pagePath = path.join(portalDir, pageName);
        
        if (!fs.existsSync(pagePath)) {
            throw new Error(`Page file not found: ${pagePath}`);
        }

        let content = fs.readFileSync(pagePath, 'utf8');
        
        // Check if already integrated
        if (content.includes('backend-integration-helper.js')) {
            console.log(`⚠️  ${portalName}/${pageName} already integrated, skipping...`);
            return;
        }

        // Add integration scripts
        content = this.addIntegrationScripts(content);
        
        // Add Alpine.js initialization
        content = this.addAlpineInitialization(content, pageName, portalName);
        
        // Write the updated content
        fs.writeFileSync(pagePath, content, 'utf8');
        console.log(`✅ Integrated ${portalName}/${pageName}`);
    }

    /**
     * Add integration scripts to the page
     */
    addIntegrationScripts(content) {
        // Find the head section and add scripts after existing scripts
        const headEndIndex = content.indexOf('</head>');
        if (headEndIndex === -1) {
            throw new Error('Could not find </head> tag');
        }

        const scripts = this.integrationTemplate.scripts;
        content = content.slice(0, headEndIndex) + scripts + content.slice(headEndIndex);
        
        return content;
    }

    /**
     * Add Alpine.js initialization
     */
    addAlpineInitialization(content, pageName, portalName) {
        // Find the Alpine.js function
        const functionMatch = content.match(/function\s+(\w+)\(\)\s*\{/);
        if (!functionMatch) {
            throw new Error('Could not find Alpine.js function');
        }

        const functionName = functionMatch[1];
        const functionStart = functionMatch.index;
        
        // Find the return statement
        const returnMatch = content.slice(functionStart).match(/\s*return\s*\{/);
        if (!returnMatch) {
            throw new Error('Could not find return statement in Alpine.js function');
        }

        const returnIndex = functionStart + returnMatch.index + returnMatch[0].length;
        
        // Determine if real-time updates are needed
        const enableRealTime = this.shouldEnableRealTime(pageName, portalName);
        
        // Prepare the integration code
        const pageNameClean = `${portalName}-${pageName.replace('.html', '')}`;
        const template = this.getIntegrationTemplate();
        const alpineInit = template.alpineInit(pageNameClean, enableRealTime);
        const initMethod = template.initMethod(pageNameClean, enableRealTime);
        
        // Insert the integration code
        content = content.slice(0, returnIndex) + 
                 alpineInit + 
                 content.slice(returnIndex);
        
        // Add or replace the init method
        const existingInitMatch = content.match(/init\(\)\s*\{[^}]*\}/);
        if (existingInitMatch) {
            content = content.replace(existingInitMatch[0], initMethod.trim());
        } else {
            // Add init method after the return statement
            const afterReturn = returnIndex + alpineInit.length;
            content = content.slice(0, afterReturn) + 
                     initMethod + 
                     content.slice(afterReturn);
        }
        
        return content;
    }

    /**
     * Determine if real-time updates should be enabled for a page
     */
    shouldEnableRealTime(pageName, portalName) {
        const realTimePages = [
            'home.html', 'dashboard.html', 'inventory.html', 
            'patients.html', 'prescriptions.html', 'pos.html',
            'point-of-sale.html', 'sales.html'
        ];
        
        return realTimePages.includes(pageName);
    }

    /**
     * Get page configuration for all portals
     */
    getPageConfiguration() {
        return {
            admin: [
                'account.html', 'branches.html', 'home.html', 'inventory.html',
                'patients.html', 'payments.html', 'point-of-sale.html',
                'prescriptions.html', 'reports.html', 'sales.html', 'user-management.html'
            ],
            pharmacist: [
                'account.html', 'clinical.html', 'compliance.html', 'help.html',
                'home.html', 'inventory.html', 'patients.html', 'payments.html',
                'prescriptions.html', 'reports.html', 'suppliers.html'
            ],
            cashier: [
                'account.html', 'help.html', 'home.html', 'inventory.html',
                'patients.html', 'payments.html', 'point-of-sale.html',
                'queue-management.html', 'receipt-template.html', 'reports.html',
                'sales.html', 'shift-management.html'
            ],
            operations: [
                'account.html', 'additional-users.html', 'home.html',
                'subscriptions.html', 'tenants.html', 'transactions.html', 'users.html'
            ],
            'super-admin': [
                'all-portals-test.html', 'analytics.html', 'help.html', 'home.html',
                'logs.html', 'offline-test.html', 'pharmacies.html', 'reports.html',
                'security.html', 'settings.html', 'transactions.html', 'users.html'
            ]
        };
    }

    /**
     * Run the batch integration
     */
    async run() {
        console.log('🎯 Starting backend integration batch process...\n');
        
        const pageConfig = this.getPageConfiguration();
        
        for (const [portalName, pages] of Object.entries(pageConfig)) {
            await this.processPortal(portalName, pages);
        }
        
        // Print summary
        console.log('\n📊 Integration Summary:');
        console.log(`✅ Successfully processed: ${this.processedPages} pages`);
        
        if (this.errors.length > 0) {
            console.log(`❌ Errors encountered: ${this.errors.length}`);
            this.errors.forEach(error => {
                console.log(`   - ${error.portal}/${error.page}: ${error.error}`);
            });
        }
        
        console.log('\n🎉 Batch integration complete!');
    }
}

// Run the batch integration if called directly
if (require.main === module) {
    const batch = new BackendIntegrationBatch();
    batch.run().catch(console.error);
}

module.exports = BackendIntegrationBatch;
