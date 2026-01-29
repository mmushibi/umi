class PharmacistService {
    constructor() {
        this.baseUrl = window.location.origin + '/api';
        this.connection = null;
    }

    // Get auth data from auth service
    getAuthData() {
        return {
            tenantId: window.authService.getTenantId(),
            branchId: window.authService.getBranchId(),
            token: window.authService.getAuthHeader()
        };
    }

    // Initialize SignalR connection
    async initializeSignalR() {
        if (this.connection) return this.connection;

        const authData = this.getAuthData();

        try {
            const connection = new signalR.HubConnectionBuilder()
                .withUrl(`${window.location.origin}/pharmacyHub?branchId=${authData.branchId}`, {
                    accessTokenFactory: () => authData.token.replace('Bearer ', '')
                })
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            connection.onreconnected(() => {
                console.log('SignalR reconnected');
                this.joinBranchGroup();
            });

            connection.onclose(() => {
                console.log('SignalR connection closed');
            });

            connection.on('SyncTriggered', (data) => {
                console.log('Sync triggered:', data);
                // Trigger dashboard refresh
                window.dispatchEvent(new CustomEvent('dataSynced', { detail: data }));
            });

            connection.on('DashboardUpdated', (data) => {
                console.log('Dashboard updated:', data);
                // Update dashboard in real-time
                window.dispatchEvent(new CustomEvent('dashboardUpdated', { detail: data }));
            });

            await connection.start();
            this.connection = connection;
            
            // Join the appropriate group
            await this.joinBranchGroup();
            
            console.log('SignalR connected successfully');
            return connection;
        } catch (error) {
            console.error('Error connecting to SignalR:', error);
            throw error;
        }
    }

    async joinBranchGroup() {
        const authData = this.getAuthData();
        if (this.connection && authData.tenantId && authData.branchId) {
            try {
                await this.connection.invoke('JoinBranchGroup', authData.tenantId, authData.branchId);
                console.log(`Joined branch group: ${authData.tenantId}_${authData.branchId}`);
            } catch (error) {
                console.error('Error joining branch group:', error);
            }
        }
    }

    // API request helper
    async makeRequest(endpoint, options = {}) {
        const authData = this.getAuthData();
        const url = `${this.baseUrl}${endpoint}`;
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': authData.token
            }
        };

        const response = await fetch(url, { ...defaultOptions, ...options });
        
        if (!response.ok) {
            const error = await response.json().catch(() => ({ error: 'Unknown error' }));
            throw new Error(error.error || `HTTP ${response.status}: ${response.statusText}`);
        }

        return response.json();
    }

    // Dashboard data
    async getDashboardData() {
        const authData = this.getAuthData();
        if (!authData.tenantId || !authData.branchId) {
            throw new Error('Tenant ID and Branch ID are required');
        }

        return this.makeRequest(`/pharmacist/dashboard?tenantId=${authData.tenantId}&branchId=${authData.branchId}`);
    }

    // User information
    async getUserInfo() {
        return this.makeRequest('/pharmacist/user-info');
    }

    // Pharmacy information
    async getPharmacyInfo() {
        const authData = this.getAuthData();
        if (!authData.tenantId) {
            throw new Error('Tenant ID is required');
        }

        return this.makeRequest(`/pharmacist/pharmacy-info?tenantId=${authData.tenantId}`);
    }

    // Sync operations
    async triggerSync(entityType = null) {
        const authData = this.getAuthData();
        if (!authData.tenantId || !authData.branchId) {
            throw new Error('Tenant ID and Branch ID are required');
        }

        const endpoint = `/pharmacist/sync/trigger?tenantId=${authData.tenantId}&branchId=${authData.branchId}`;
        const url = entityType ? `${endpoint}&entityType=${entityType}` : endpoint;
        
        return this.makeRequest(url, { method: 'POST' });
    }

    async getSyncStatus() {
        const authData = this.getAuthData();
        if (!authData.tenantId || !authData.branchId) {
            throw new Error('Tenant ID and Branch ID are required');
        }

        return this.makeRequest(`/pharmacist/sync/status?tenantId=${authData.tenantId}&branchId=${authData.branchId}`);
    }

    // Authentication helpers (delegated to auth service)
    setAuthData(token, tenantId, branchId) {
        // This is now handled by auth service
        console.warn('setAuthData is deprecated. Use authService.setAuthData instead.');
    }

    clearAuthData() {
        // This is now handled by auth service
        console.warn('clearAuthData is deprecated. Use authService.clearAuthData instead.');
        window.authService.clearAuthData();
    }

    // Check if user is authenticated
    isAuthenticated() {
        return window.authService.isAuthenticated();
    }

    // Get current user initials
    getUserInitials(name) {
        if (!name) return 'U';
        return name.split(' ')
            .map(word => word.charAt(0).toUpperCase())
            .join('')
            .substring(0, 2);
    }

    // Format currency
    formatCurrency(amount, currency = 'ZMW') {
        return new Intl.NumberFormat('en-ZM', {
            style: 'currency',
            currency: currency,
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        }).format(amount);
    }

    // Format relative time
    formatRelativeTime(date) {
        const now = new Date();
        const targetDate = new Date(date);
        const diffMs = now - targetDate;
        const diffMins = Math.floor(diffMs / 60000);
        const diffHours = Math.floor(diffMs / 3600000);
        const diffDays = Math.floor(diffMs / 86400000);

        if (diffMins < 1) return 'Just now';
        if (diffMins < 60) return `${diffMins} minute${diffMins > 1 ? 's' : ''} ago`;
        if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
        if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
        
        return targetDate.toLocaleDateString();
    }

    // Disconnect SignalR
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
        }
    }
}

// Export singleton instance
window.pharmacistService = new PharmacistService();
