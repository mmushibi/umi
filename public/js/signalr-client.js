/**
 * UmiHealth SignalR Client
 * Handles real-time communication with the UmiHealth hub
 */

class UmiHealthSignalR {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 3000;
        this.eventHandlers = new Map();
    }

    async start() {
        try {
            // Get the base URL from the existing auth API
            const authAPI = window.authAPI || new AuthAPI();
            const hubUrl = `${authAPI.baseURL.replace('/api/v1', '')}/umiHealthHub`;

            // Build connection with authentication
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl(hubUrl, {
                    accessTokenFactory: () => localStorage.getItem('umi_access_token'),
                    skipNegotiation: true,
                    transport: signalR.HttpTransportType.WebSockets
                })
                .withAutomaticReconnect({
                    nextRetryDelayInMilliseconds: retryContext => {
                        if (retryContext.previousRetryCount < this.maxReconnectAttempts) {
                            return this.reconnectDelay;
                        }
                        return null;
                    }
                })
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Set up event handlers
            this.setupEventHandlers();

            // Start the connection
            await this.connection.start();
            this.isConnected = true;
            this.reconnectAttempts = 0;

            console.log('SignalR connected successfully');
            this.emit('connected');

        } catch (error) {
            console.error('SignalR connection failed:', error);
            this.isConnected = false;
            this.emit('error', error);
            
            // Attempt to reconnect after delay
            setTimeout(() => this.attemptReconnect(), this.reconnectDelay);
        }
    }

    setupEventHandlers() {
        // Server-to-client events
        this.connection.on('ReceiveNotification', (data) => {
            console.log('Received notification:', data);
            this.emit('notification', data);
            
            // Show toast notification if AuthManager is available
            if (window.authManager) {
                window.authManager.showToast(data.message, data.type || 'info');
            }
        });

        this.connection.on('InventoryUpdated', (data) => {
            console.log('Inventory updated:', data);
            this.emit('inventoryUpdated', data);
        });

        this.connection.on('SaleCreated', (data) => {
            console.log('Sale created:', data);
            this.emit('saleCreated', data);
        });

        this.connection.on('PrescriptionCreated', (data) => {
            console.log('Prescription created:', data);
            this.emit('prescriptionCreated', data);
        });

        this.connection.on('UserUpdated', (data) => {
            console.log('User updated:', data);
            this.emit('userUpdated', data);
        });

        // Connection lifecycle events
        this.connection.onreconnected(() => {
            console.log('SignalR reconnected');
            this.isConnected = true;
            this.reconnectAttempts = 0;
            this.emit('reconnected');
        });

        this.connection.onclose(() => {
            console.log('SignalR connection closed');
            this.isConnected = false;
            this.emit('disconnected');
        });

        this.connection.onreconnecting(() => {
            console.log('SignalR reconnecting...');
            this.emit('reconnecting');
        });
    }

    async attemptReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            console.log('Max reconnect attempts reached');
            this.emit('reconnectFailed');
            return;
        }

        this.reconnectAttempts++;
        console.log(`Attempting reconnect ${this.reconnectAttempts}/${this.maxReconnectAttempts}`);
        
        try {
            await this.start();
        } catch (error) {
            console.error('Reconnect attempt failed:', error);
        }
    }

    // Client-to-server methods
    async sendToTenant(tenantId, message, type = 'info') {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }
        return await this.connection.invoke('SendToTenant', tenantId, message, type);
    }

    async sendToRole(role, message, type = 'info') {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }
        return await this.connection.invoke('SendToRole', role, message, type);
    }

    async broadcast(message, type = 'info') {
        if (!this.isConnected) {
            throw new Error('SignalR not connected');
        }
        return await this.connection.invoke('Broadcast', message, type);
    }

    // Event handling
    on(event, handler) {
        if (!this.eventHandlers.has(event)) {
            this.eventHandlers.set(event, []);
        }
        this.eventHandlers.get(event).push(handler);
    }

    off(event, handler) {
        if (this.eventHandlers.has(event)) {
            const handlers = this.eventHandlers.get(event);
            const index = handlers.indexOf(handler);
            if (index > -1) {
                handlers.splice(index, 1);
            }
        }
    }

    emit(event, data) {
        if (this.eventHandlers.has(event)) {
            this.eventHandlers.get(event).forEach(handler => {
                try {
                    handler(data);
                } catch (error) {
                    console.error(`Error in event handler for ${event}:`, error);
                }
            });
        }
    }

    async stop() {
        if (this.connection) {
            await this.connection.stop();
            this.isConnected = false;
            console.log('SignalR disconnected');
        }
    }

    getConnectionState() {
        if (!this.connection) return 'disconnected';
        return this.connection.state;
    }
}

// Create global instance
window.umiSignalR = new UmiHealthSignalR();

// Auto-start when page loads and user is authenticated
document.addEventListener('DOMContentLoaded', () => {
    // Check if user is authenticated before starting SignalR
    const token = localStorage.getItem('umi_access_token');
    if (token) {
        // Load SignalR library if not already loaded
        if (typeof signalR === 'undefined') {
            const script = document.createElement('script');
            script.src = 'https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js';
            script.onload = () => {
                window.umiSignalR.start();
            };
            document.head.appendChild(script);
        } else {
            window.umiSignalR.start();
        }
    }
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = UmiHealthSignalR;
}
