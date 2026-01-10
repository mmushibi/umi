/**
 * SignalR Client Configuration
 * Handles real-time connections to Umi Health backend hubs
 */

class SignalRClient {
    constructor() {
        this.connections = new Map();
        this.hubs = {
            notifications: '/notificationHub',
            inventory: '/inventoryHub', 
            prescriptions: '/pharmacyHub',
            sales: '/salesHub',
            patients: '/pharmacyHub',
            test: '/testHub'
        };
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 3000;
    }

    /**
     * Initialize SignalR connection
     */
    async init(hubName, options = {}) {
        if (this.connections.has(hubName)) {
            return this.connections.get(hubName);
        }

        const hubUrl = this.hubs[hubName];
        if (!hubUrl) {
            throw new Error(`Unknown hub: ${hubName}. Available hubs: ${Object.keys(this.hubs).join(', ')}`);
        }

        // Validate SignalR library
        if (typeof signalR === 'undefined') {
            throw new Error('SignalR library not loaded. Please include @microsoft/signalR');
        }

        // Check authentication
        const token = this.getAccessToken();
        if (!token) {
            console.warn(`⚠️ No authentication token available for ${hubName} hub`);
        }

        try {
            const connection = this.createConnection(hubUrl, options);
            this.connections.set(hubName, connection);
            
            await connection.start();
            console.log(`✅ SignalR connected to ${hubName} hub at ${this.getHubUrl(hubUrl)}`);
            
            return connection;
        } catch (error) {
            console.error(`❌ Failed to connect to ${hubName} hub:`, error);
            this.connections.delete(hubName);
            throw error;
        }
    }

    /**
     * Create SignalR connection with configuration
     */
    createConnection(hubUrl, options) {
        if (typeof signalR === 'undefined') {
            throw new Error('SignalR library not loaded. Please include @microsoft/signalr');
        }

        const connection = new signalR.HubConnectionBuilder()
            .withUrl(this.getHubUrl(hubUrl), {
                accessTokenFactory: () => this.getAccessToken(),
                ...options.transport
            })
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
                        return this.reconnectDelay * Math.pow(2, retryContext.previousRetryCount);
                    }
                    return null; // Stop retrying
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Setup connection events
        this.setupConnectionEvents(connection, hubUrl);

        return connection;
    }

    /**
     * Get full hub URL with configurable endpoint
     */
    getHubUrl(hubUrl) {
        // Check for environment variable first
        if (typeof process !== 'undefined' && process.env?.UMI_API_BASE_URL) {
            return process.env.UMI_API_BASE_URL + hubUrl;
        }
        
        // Check for global configuration
        if (typeof window !== 'undefined' && window.UMI_CONFIG?.apiBaseUrl) {
            return window.UMI_CONFIG.apiBaseUrl + hubUrl;
        }
        
        // Check for localStorage configuration
        if (typeof window !== 'undefined') {
            const storedUrl = localStorage.getItem('umi_api_base_url');
            if (storedUrl) {
                return storedUrl + hubUrl;
            }
        }
        
        // Fallback to environment-based defaults
        if (typeof window !== 'undefined') {
            const hostname = window.location.hostname;
            const port = window.location.port;
            
            if (hostname === 'localhost' || hostname === '127.0.0.1') {
                // Development environment
                return `http://localhost:${parseInt(port) + 1 || 5001}${hubUrl}`;
            } else if (hostname.includes('staging') || hostname.includes('dev')) {
                // Staging environment
                return `https://api-staging.umihealth.com${hubUrl}`;
            } else {
                // Production environment
                return `https://api.umihealth.com${hubUrl}`;
            }
        }
        
        // Default fallback
        return 'http://localhost:5001' + hubUrl;
    }

    /**
     * Get access token for authentication
     */
    getAccessToken() {
        // Try auth manager first
        if (window.authManager?.isAuthenticated()) {
            const token = window.authManager.currentUser?.token;
            return token?.replace('Bearer ', '') || token;
        }
        
        // Try auth API
        if (window.authAPI?.isAuthenticated()) {
            return window.authAPI.accessToken;
        }
        
        // Try standardized auth_tokens storage
        try {
            const storedTokens = localStorage.getItem('auth_tokens');
            if (storedTokens) {
                const { accessToken } = JSON.parse(storedTokens);
                return accessToken?.replace('Bearer ', '') || accessToken;
            }
        } catch (error) {
            console.error('Error reading auth_tokens:', error);
        }
        
        // Fallback to legacy storage
        const token = localStorage.getItem('umi_access_token') || 
                     sessionStorage.getItem('umi_access_token') ||
                     localStorage.getItem('umi_auth_token') || 
                     sessionStorage.getItem('umi_auth_token');
        return token?.replace('Bearer ', '') || token;
    }

    /**
     * Setup connection event handlers
     */
    setupConnectionEvents(connection, hubUrl) {
        connection.onreconnecting(error => {
            console.warn(`⚠️ SignalR reconnecting to ${hubUrl}:`, error);
            this.notifyConnectionStatus(hubUrl, 'reconnecting', error);
        });

        connection.onreconnected(connectionId => {
            console.log(`✅ SignalR reconnected to ${hubUrl}: ${connectionId}`);
            this.notifyConnectionStatus(hubUrl, 'connected');
            this.resubscribeEvents(hubUrl);
        });

        connection.onclose(error => {
            console.error(`❌ SignalR connection closed to ${hubUrl}:`, error);
            this.notifyConnectionStatus(hubUrl, 'disconnected', error);
        });
    }

    /**
     * Subscribe to hub events
     */
    subscribe(hubName, eventName, callback) {
        const connection = this.connections.get(hubName);
        if (!connection) {
            throw new Error(`Not connected to ${hubName} hub`);
        }

        connection.on(eventName, callback);
        
        // Track subscription for reconnection
        if (!this.subscriptions) {
            this.subscriptions = new Map();
        }
        if (!this.subscriptions.has(hubName)) {
            this.subscriptions.set(hubName, new Map());
        }
        this.subscriptions.get(hubName).set(eventName, callback);
    }

    /**
     * Unsubscribe from hub events
     */
    unsubscribe(hubName, eventName) {
        const connection = this.connections.get(hubName);
        if (!connection) return;

        connection.off(eventName);
        
        if (this.subscriptions?.has(hubName)) {
            this.subscriptions.get(hubName).delete(eventName);
        }
    }

    /**
     * Resubscribe to events after reconnection
     */
    resubscribeEvents(hubName) {
        if (!this.subscriptions?.has(hubName)) return;

        const connection = this.connections.get(hubName);
        const hubSubscriptions = this.subscriptions.get(hubName);

        hubSubscriptions.forEach((callback, eventName) => {
            connection.on(eventName, callback);
        });
    }

    /**
     * Send message to hub
     */
    async send(hubName, methodName, ...args) {
        const connection = this.connections.get(hubName);
        if (!connection) {
            throw new Error(`Not connected to ${hubName} hub`);
        }

        try {
            await connection.send(methodName, ...args);
        } catch (error) {
            console.error(`Failed to send ${methodName} to ${hubName}:`, error);
            throw error;
        }
    }

    /**
     * Invoke hub method and get response
     */
    async invoke(hubName, methodName, ...args) {
        const connection = this.connections.get(hubName);
        if (!connection) {
            throw new Error(`Not connected to ${hubName} hub`);
        }

        try {
            return await connection.invoke(methodName, ...args);
        } catch (error) {
            console.error(`Failed to invoke ${methodName} on ${hubName}:`, error);
            throw error;
        }
    }

    /**
     * Get connection status
     */
    getConnectionStatus(hubName) {
        const connection = this.connections.get(hubName);
        if (!connection) {
            return { state: 'disconnected', hubName };
        }

        return {
            state: connection.state,
            hubName,
            connectionId: connection.connectionId
        };
    }

    /**
     * Notify connection status changes
     */
    notifyConnectionStatus(hubName, status, error = null) {
        const event = new CustomEvent('signalr-connection-status', {
            detail: { hubName, status, error }
        });
        document.dispatchEvent(event);
    }

    /**
     * Disconnect from hub
     */
    async disconnect(hubName) {
        const connection = this.connections.get(hubName);
        if (!connection) return;

        try {
            await connection.stop();
            this.connections.delete(hubName);
            console.log(`🔌 Disconnected from ${hubName} hub`);
        } catch (error) {
            console.error(`Failed to disconnect from ${hubName}:`, error);
        }
    }

    /**
     * Disconnect from all hubs
     */
    async disconnectAll() {
        const disconnectPromises = Array.from(this.connections.keys())
            .map(hubName => this.disconnect(hubName));
        
        await Promise.all(disconnectPromises);
        console.log('🔌 Disconnected from all SignalR hubs');
    }

    /**
     * Test connection to a hub
     */
    async testConnection(hubName) {
        try {
            const status = this.getConnectionStatus(hubName);
            console.log(`🔍 Testing ${hubName} hub connection:`, status);
            
            if (status.state === 'connected') {
                // Try to ping the server
                await this.invoke(hubName, 'Ping', 'test');
                console.log(`✅ ${hubName} hub connection test successful`);
                return true;
            } else {
                console.warn(`⚠️ ${hubName} hub not connected, state: ${status.state}`);
                return false;
            }
        } catch (error) {
            console.error(`❌ ${hubName} hub connection test failed:`, error);
            return false;
        }
    }

    /**
     * Initialize all hubs for current user role
     */
    async initializeForRole(userRole) {
        const hubConfigs = this.getHubsForRole(userRole);
        const connections = {};
        const errors = [];

        console.log(`🚀 Initializing SignalR hubs for role: ${userRole}`);

        for (const [hubName, config] of Object.entries(hubConfigs)) {
            try {
                connections[hubName] = await this.init(hubName, config);
                console.log(`✅ ${hubName} hub initialized for ${userRole}`);
            } catch (error) {
                console.warn(`⚠️ Failed to initialize ${hubName} hub:`, error);
                errors.push({ hubName, error });
            }
        }

        // Test all connections
        const testResults = {};
        for (const hubName of Object.keys(connections)) {
            testResults[hubName] = await this.testConnection(hubName);
        }

        return {
            connections,
            errors,
            testResults,
            summary: {
                total: Object.keys(hubConfigs).length,
                connected: Object.keys(connections).length,
                failed: errors.length
            }
        };
    }

    /**
     * Get hubs configuration based on user role
     */
    getHubsForRole(userRole) {
        const baseConfig = {
            transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        };

        switch (userRole) {
            case 'Admin':
                return {
                    notifications: { ...baseConfig },
                    inventory: { ...baseConfig },
                    sales: { ...baseConfig },
                    patients: { ...baseConfig }
                };
                
            case 'Pharmacist':
                return {
                    notifications: { ...baseConfig },
                    prescriptions: { ...baseConfig },
                    inventory: { ...baseConfig }
                };
                
            case 'Cashier':
                return {
                    notifications: { ...baseConfig },
                    sales: { ...baseConfig }
                };
                
            case 'SuperAdmin':
                return {
                    notifications: { ...baseConfig },
                    inventory: { ...baseConfig },
                    sales: { ...baseConfig },
                    prescriptions: { ...baseConfig },
                    patients: { ...baseConfig }
                };
                
            default:
                return {
                    notifications: { ...baseConfig }
                };
        }
    }
}

// Create global instance
window.signalRClient = new SignalRClient();

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = SignalRClient;
}
