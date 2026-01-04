class RealtimeSync {
    constructor(dataSync) {
        this.dataSync = dataSync;
        this.connection = null;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.reconnectDelay = 1000;
        this.isConnecting = false;
        this.subscriptions = new Map();
        
        this.init();
    }

    init() {
        // Try WebSocket first, fallback to Server-Sent Events
        this.connectWebSocket();
    }

    connectWebSocket() {
        if (this.isConnecting || this.connection) return;
        
        this.isConnecting = true;
        
        try {
            const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
            const wsUrl = `${protocol}//${window.location.host}/ws/superadmin`;
            
            this.connection = new WebSocket(wsUrl);
            
            this.connection.onopen = () => {
                console.log('WebSocket connected');
                this.isConnecting = false;
                this.reconnectAttempts = 0;
                this.dataSync.notifySuccess('Real-time sync active');
                
                // Resubscribe to all previous subscriptions
                this.resubscribeAll();
            };
            
            this.connection.onmessage = (event) => {
                try {
                    const message = JSON.parse(event.data);
                    this.handleMessage(message);
                } catch (error) {
                    console.error('Failed to parse WebSocket message:', error);
                }
            };
            
            this.connection.onclose = (event) => {
                console.log('WebSocket disconnected:', event.code, event.reason);
                this.isConnecting = false;
                this.connection = null;
                
                if (!event.wasClean && this.reconnectAttempts < this.maxReconnectAttempts) {
                    this.scheduleReconnect();
                } else {
                    this.dataSync.notifyError('Real-time sync disconnected');
                    // Fallback to polling
                    this.fallbackToPolling();
                }
            };
            
            this.connection.onerror = (error) => {
                console.error('WebSocket error:', error);
                this.isConnecting = false;
            };
            
        } catch (error) {
            console.error('Failed to create WebSocket connection:', error);
            this.isConnecting = false;
            this.fallbackToPolling();
        }
    }

    fallbackToPolling() {
        console.log('Falling back to Server-Sent Events');
        this.connectSSE();
    }

    connectSSE() {
        try {
            const eventSource = new EventSource('/api/v1/superadmin/events');
            
            eventSource.onopen = () => {
                console.log('SSE connected');
                this.dataSync.notifySuccess('Real-time sync active (SSE)');
            };
            
            eventSource.onmessage = (event) => {
                try {
                    const message = JSON.parse(event.data);
                    this.handleMessage(message);
                } catch (error) {
                    console.error('Failed to parse SSE message:', error);
                }
            };
            
            eventSource.onerror = (error) => {
                console.error('SSE error:', error);
                eventSource.close();
                
                // Fallback to polling if SSE fails
                this.fallbackToPollingInterval();
            };
            
            this.connection = eventSource;
            
        } catch (error) {
            console.error('Failed to create SSE connection:', error);
            this.fallbackToPollingInterval();
        }
    }

    fallbackToPollingInterval() {
        console.log('Falling back to polling for real-time updates');
        this.dataSync.notifyError('Using polling for real-time updates');
        
        // Set up polling for critical endpoints
        this.dataSync.startAutoRefresh('/dashboard', (data) => {
            this.dataSync.notify('dashboard-update', data);
        }, 15000); // 15 seconds polling
        
        this.dataSync.startAutoRefresh('/health', (data) => {
            this.dataSync.notify('health-update', data);
        }, 30000); // 30 seconds polling
    }

    handleMessage(message) {
        const { type, data, endpoint } = message;
        
        console.log('Received real-time message:', type, endpoint);
        
        // Update cache
        if (endpoint) {
            this.dataSync.setCache(endpoint, data);
            this.dataSync.storeOfflineData(endpoint, data);
        }
        
        // Notify listeners
        this.dataSync.notify(type, data);
        
        // Handle specific message types
        switch (type) {
            case 'dashboard-update':
                this.handleDashboardUpdate(data);
                break;
            case 'security-event':
                this.handleSecurityEvent(data);
                break;
            case 'system-alert':
                this.handleSystemAlert(data);
                break;
            case 'user-activity':
                this.handleUserActivity(data);
                break;
            default:
                console.log('Unknown message type:', type);
        }
    }

    handleDashboardUpdate(data) {
        // Update dashboard stats in real-time
        if (window.superAdmin && window.superAdmin.updateStats) {
            window.superAdmin.updateStats(data);
        }
    }

    handleSecurityEvent(data) {
        // Show security alerts immediately
        this.dataSync.notifyError(`Security Event: ${data.eventType}`);
        
        // Update security events if on that page
        if (window.location.pathname.includes('security')) {
            this.dataSync.notify('security-event-update', data);
        }
    }

    handleSystemAlert(data) {
        // Show system alerts
        const alertType = data.severity === 'critical' ? 'error' : 'warning';
        this.dataSync.notify(`system-${alertType}`, data.message);
    }

    handleUserActivity(data) {
        // Update user activity logs
        if (window.location.pathname.includes('logs')) {
            this.dataSync.notify('user-activity-update', data);
        }
    }

    subscribe(endpoint, callback) {
        if (!this.subscriptions.has(endpoint)) {
            this.subscriptions.set(endpoint, new Set());
        }
        this.subscriptions.get(endpoint).add(callback);
        
        // Send subscription message to server if WebSocket is connected
        if (this.connection && this.connection.readyState === WebSocket.OPEN) {
            this.send({
                type: 'subscribe',
                endpoint: endpoint
            });
        }
    }

    unsubscribe(endpoint, callback) {
        if (this.subscriptions.has(endpoint)) {
            this.subscriptions.get(endpoint).delete(callback);
            
            if (this.subscriptions.get(endpoint).size === 0) {
                this.subscriptions.delete(endpoint);
                
                // Send unsubscribe message to server if WebSocket is connected
                if (this.connection && this.connection.readyState === WebSocket.OPEN) {
                    this.send({
                        type: 'unsubscribe',
                        endpoint: endpoint
                    });
                }
            }
        }
    }

    resubscribeAll() {
        for (const endpoint of this.subscriptions.keys()) {
            this.send({
                type: 'subscribe',
                endpoint: endpoint
            });
        }
    }

    send(message) {
        if (this.connection && this.connection.readyState === WebSocket.OPEN) {
            this.connection.send(JSON.stringify(message));
        } else {
            console.warn('Cannot send message, WebSocket not connected');
        }
    }

    scheduleReconnect() {
        this.reconnectAttempts++;
        const delay = this.reconnectDelay * Math.pow(2, this.reconnectAttempts - 1);
        
        console.log(`Scheduling reconnect attempt ${this.reconnectAttempts} in ${delay}ms`);
        
        setTimeout(() => {
            this.connectWebSocket();
        }, delay);
    }

    disconnect() {
        if (this.connection) {
            if (this.connection.readyState === WebSocket.OPEN) {
                this.connection.close();
            } else if (this.connection.close) {
                this.connection.close();
            }
            this.connection = null;
        }
        
        this.subscriptions.clear();
        this.isConnecting = false;
        this.reconnectAttempts = 0;
    }

    getConnectionStatus() {
        if (!this.connection) {
            return 'disconnected';
        }
        
        if (this.connection.readyState === WebSocket.OPEN) {
            return 'connected';
        } else if (this.connection.readyState === WebSocket.CONNECTING) {
            return 'connecting';
        } else {
            return 'disconnected';
        }
    }

    isRealtimeActive() {
        const status = this.getConnectionStatus();
        return status === 'connected' || status === 'connecting';
    }
}

// Extend SuperAdminDataSync with real-time capabilities
if (typeof window.superAdminDataSync !== 'undefined') {
    window.superAdminDataSync.realtimeSync = new RealtimeSync(window.superAdminDataSync);
    
    // Add convenience methods
    window.superAdminDataSync.subscribe = function(endpoint, callback) {
        return this.realtimeSync.subscribe(endpoint, callback);
    };
    
    window.superAdminDataSync.unsubscribe = function(endpoint, callback) {
        return this.realtimeSync.unsubscribe(endpoint, callback);
    };
    
    window.superAdminDataSync.isRealtimeActive = function() {
        return this.realtimeSync.isRealtimeActive();
    };
    
    window.superAdminDataSync.getConnectionStatus = function() {
        return {
            ...this.realtimeSync.getConnectionStatus(),
            isOnline: this.isOnline,
            hasCachedData: this.cache.size > 0,
            hasOfflineData: Object.keys(JSON.parse(localStorage.getItem('offlineData') || '{}')).length > 0,
            queuedRequests: JSON.parse(localStorage.getItem('offlineQueue') || '[]').length,
            realtimeActive: this.realtimeSync.isRealtimeActive()
        };
    };
}

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { RealtimeSync };
}
