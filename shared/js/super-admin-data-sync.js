class SuperAdminDataSync {
    constructor() {
        this.baseURL = '/api/v1/superadmin';
        this.token = localStorage.getItem('authToken');
        this.refreshInterval = 30000; // 30 seconds
        this.intervals = {};
        this.cache = new Map();
        this.listeners = new Map();
        
        this.setupInterceptors();
    }

    setupInterceptors() {
        // Add request interceptor to include auth token
        const originalFetch = window.fetch;
        window.fetch = async (...args) => {
            const [url, options = {}] = args;
            
            if (url.startsWith('/api/') && this.token) {
                options.headers = {
                    ...options.headers,
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json'
                };
            }
            
            return originalFetch(url, options);
        };
    }

    async request(endpoint, options = {}) {
        try {
            const response = await fetch(`${this.baseURL}${endpoint}`, {
                ...options,
                headers: {
                    'Authorization': `Bearer ${this.token}`,
                    'Content-Type': 'application/json',
                    ...options.headers
                }
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || `HTTP ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('API request failed:', error);
            this.notifyError(error.message);
            throw error;
        }
    }

    // Dashboard
    async getDashboardSummary() {
        return this.request('/dashboard');
    }

    // Analytics
    async getAnalytics(filter = {}) {
        const params = new URLSearchParams(filter);
        return this.request(`/analytics?${params}`);
    }

    async getAnalyticsById(id) {
        return this.request(`/analytics/${id}`);
    }

    async generateAnalytics(date) {
        return this.request('/analytics/generate', {
            method: 'POST',
            body: JSON.stringify({ date })
        });
    }

    // Logs
    async getLogs(filter = {}) {
        const params = new URLSearchParams(filter);
        return this.request(`/logs?${params}`);
    }

    async getLogById(id) {
        return this.request(`/logs/${id}`);
    }

    async createLog(logData) {
        return this.request('/logs', {
            method: 'POST',
            body: JSON.stringify(logData)
        });
    }

    async clearLogs(beforeDate) {
        const params = beforeDate ? `?beforeDate=${beforeDate}` : '';
        return this.request(`/logs${params}`, {
            method: 'DELETE'
        });
    }

    // Reports
    async getReports(page = 1, pageSize = 50) {
        return this.request(`/reports?page=${page}&pageSize=${pageSize}`);
    }

    async getReportById(id) {
        return this.request(`/reports/${id}`);
    }

    async createReport(reportData) {
        return this.request('/reports', {
            method: 'POST',
            body: JSON.stringify(reportData)
        });
    }

    async updateReport(id, reportData) {
        return this.request(`/reports/${id}`, {
            method: 'PUT',
            body: JSON.stringify(reportData)
        });
    }

    async deleteReport(id) {
        return this.request(`/reports/${id}`, {
            method: 'DELETE'
        });
    }

    async downloadReport(id) {
        const response = await fetch(`${this.baseURL}/reports/${id}/download`, {
            headers: {
                'Authorization': `Bearer ${this.token}`
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `report_${id}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);
    }

    async generateReport(id) {
        return this.request(`/reports/${id}/generate`, {
            method: 'POST'
        });
    }

    // Security Events
    async getSecurityEvents(filter = {}) {
        const params = new URLSearchParams(filter);
        return this.request(`/security-events?${params}`);
    }

    async getSecurityEventById(id) {
        return this.request(`/security-events/${id}`);
    }

    async createSecurityEvent(eventData) {
        return this.request('/security-events', {
            method: 'POST',
            body: JSON.stringify(eventData)
        });
    }

    async clearSecurityEvents(beforeDate) {
        const params = beforeDate ? `?beforeDate=${beforeDate}` : '';
        return this.request(`/security-events${params}`, {
            method: 'DELETE'
        });
    }

    // System Settings
    async getSystemSettings(category) {
        const params = category ? `?category=${category}` : '';
        return this.request(`/settings${params}`);
    }

    async getSystemSettingByKey(key) {
        return this.request(`/settings/${key}`);
    }

    async updateSystemSetting(key, value) {
        return this.request(`/settings/${key}`, {
            method: 'PUT',
            body: JSON.stringify({ value })
        });
    }

    async createSystemSetting(settingData) {
        return this.request('/settings', {
            method: 'POST',
            body: JSON.stringify(settingData)
        });
    }

    async deleteSystemSetting(key) {
        return this.request(`/settings/${key}`, {
            method: 'DELETE'
        });
    }

    // Super Admin Users
    async getSuperAdminUsers(page = 1, pageSize = 50, search) {
        const params = new URLSearchParams({ page, pageSize });
        if (search) params.append('search', search);
        return this.request(`/users?${params}`);
    }

    async getSuperAdminUserById(id) {
        return this.request(`/users/${id}`);
    }

    async createSuperAdminUser(userData) {
        return this.request('/users', {
            method: 'POST',
            body: JSON.stringify(userData)
        });
    }

    async updateSuperAdminUser(id, userData) {
        return this.request(`/users/${id}`, {
            method: 'PUT',
            body: JSON.stringify(userData)
        });
    }

    async deleteSuperAdminUser(id) {
        return this.request(`/users/${id}`, {
            method: 'DELETE'
        });
    }

    async toggleSuperAdminUserStatus(id) {
        return this.request(`/users/${id}/toggle-status`, {
            method: 'POST'
        });
    }

    async resetSuperAdminUserPassword(id) {
        return this.request(`/users/${id}/reset-password`, {
            method: 'POST'
        });
    }

    async enableTwoFactor(id, secret) {
        return this.request(`/users/${id}/enable-2fa`, {
            method: 'POST',
            body: JSON.stringify({ secret })
        });
    }

    async disableTwoFactor(id) {
        return this.request(`/users/${id}/disable-2fa`, {
            method: 'POST'
        });
    }

    // System Notifications
    async getSystemNotifications(page = 1, pageSize = 50) {
        return this.request(`/notifications?page=${page}&pageSize=${pageSize}`);
    }

    async getActiveNotifications(userId, tenantId) {
        const params = new URLSearchParams();
        if (userId) params.append('userId', userId);
        if (tenantId) params.append('tenantId', tenantId);
        return this.request(`/notifications/active?${params}`);
    }

    async getSystemNotificationById(id) {
        return this.request(`/notifications/${id}`);
    }

    async createSystemNotification(notificationData) {
        return this.request('/notifications', {
            method: 'POST',
            body: JSON.stringify(notificationData)
        });
    }

    async updateSystemNotification(id, notificationData) {
        return this.request(`/notifications/${id}`, {
            method: 'PUT',
            body: JSON.stringify(notificationData)
        });
    }

    async deleteSystemNotification(id) {
        return this.request(`/notifications/${id}`, {
            method: 'DELETE'
        });
    }

    async toggleSystemNotificationStatus(id) {
        return this.request(`/notifications/${id}/toggle-status`, {
            method: 'POST'
        });
    }

    // Backup Management
    async getBackups(page = 1, pageSize = 50, tenantId) {
        const params = new URLSearchParams({ page, pageSize });
        if (tenantId) params.append('tenantId', tenantId);
        return this.request(`/backups?${params}`);
    }

    async getBackupById(id) {
        return this.request(`/backups/${id}`);
    }

    async createBackup(backupData) {
        return this.request('/backups', {
            method: 'POST',
            body: JSON.stringify(backupData)
        });
    }

    async deleteBackup(id) {
        return this.request(`/backups/${id}`, {
            method: 'DELETE'
        });
    }

    async downloadBackup(id) {
        const response = await fetch(`${this.baseURL}/backups/${id}/download`, {
            headers: {
                'Authorization': `Bearer ${this.token}`
            }
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `backup_${id}.zip`;
        a.click();
        window.URL.revokeObjectURL(url);
    }

    async restoreBackup(id) {
        return this.request(`/backups/${id}/restore`, {
            method: 'POST'
        });
    }

    async scheduleBackup(backupData, schedule) {
        return this.request('/backups/schedule', {
            method: 'POST',
            body: JSON.stringify({ createDto: backupData, schedule })
        });
    }

    // API Keys
    async getApiKeys(page = 1, pageSize = 50) {
        return this.request(`/api-keys?page=${page}&pageSize=${pageSize}`);
    }

    async getApiKeyById(id) {
        return this.request(`/api-keys/${id}`);
    }

    async createApiKey(keyData) {
        return this.request('/api-keys', {
            method: 'POST',
            body: JSON.stringify(keyData)
        });
    }

    async updateApiKey(id, keyData) {
        return this.request(`/api-keys/${id}`, {
            method: 'PUT',
            body: JSON.stringify(keyData)
        });
    }

    async deleteApiKey(id) {
        return this.request(`/api-keys/${id}`, {
            method: 'DELETE'
        });
    }

    async toggleApiKeyStatus(id) {
        return this.request(`/api-keys/${id}/toggle-status`, {
            method: 'POST'
        });
    }

    async regenerateApiKey(id) {
        return this.request(`/api-keys/${id}/regenerate`, {
            method: 'POST'
        });
    }

    // System Health
    async getSystemHealth() {
        return this.request('/health');
    }

    async getSystemMetrics() {
        return this.request('/metrics');
    }

    async getSystemWarnings() {
        return this.request('/warnings');
    }

    // Data Export/Import
    async exportSystemData(type, parameters = {}) {
        const response = await fetch(`${this.baseURL}/export`, {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${this.token}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ type, parameters })
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `export_${type}_${new Date().toISOString().split('T')[0]}.json`;
        a.click();
        window.URL.revokeObjectURL(url);
    }

    async importSystemData(type, data, parameters = {}) {
        return this.request('/import', {
            method: 'POST',
            body: JSON.stringify({ type, data, parameters })
        });
    }

    // Tenant Management
    async suspendTenant(id, reason) {
        return this.request(`/tenants/${id}/suspend`, {
            method: 'POST',
            body: JSON.stringify({ reason })
        });
    }

    async unsuspendTenant(id) {
        return this.request(`/tenants/${id}/unsuspend`, {
            method: 'POST'
        });
    }

    async deleteTenant(id) {
        return this.request(`/tenants/${id}`, {
            method: 'DELETE'
        });
    }

    async resetTenantPassword(id, email) {
        return this.request(`/tenants/${id}/reset-password`, {
            method: 'POST',
            body: JSON.stringify({ email })
        });
    }

    // User Management
    async suspendUser(id, reason) {
        return this.request(`/users-all/${id}/suspend`, {
            method: 'POST',
            body: JSON.stringify({ reason })
        });
    }

    async unsuspendUser(id) {
        return this.request(`/users-all/${id}/unsuspend`, {
            method: 'POST'
        });
    }

    async resetUserPassword(id) {
        return this.request(`/users-all/${id}/reset-password`, {
            method: 'POST'
        });
    }

    async forceLogoutUser(id) {
        return this.request(`/users-all/${id}/force-logout`, {
            method: 'POST'
        });
    }

    // Real-time sync
    startAutoRefresh(endpoint, callback, interval = this.refreshInterval) {
        const key = `refresh_${endpoint}`;
        
        // Clear existing interval for this endpoint
        if (this.intervals[key]) {
            clearInterval(this.intervals[key]);
        }

        // Set up new interval
        this.intervals[key] = setInterval(async () => {
            try {
                const data = await this.request(endpoint);
                callback(data);
            } catch (error) {
                console.error(`Auto refresh failed for ${endpoint}:`, error);
            }
        }, interval);

        // Initial load
        this.request(endpoint).then(callback).catch(console.error);
    }

    stopAutoRefresh(endpoint) {
        const key = `refresh_${endpoint}`;
        if (this.intervals[key]) {
            clearInterval(this.intervals[key]);
            delete this.intervals[key];
        }
    }

    stopAllAutoRefresh() {
        Object.keys(this.intervals).forEach(key => {
            clearInterval(this.intervals[key]);
        });
        this.intervals = {};
    }

    // Event listeners
    addEventListener(event, callback) {
        if (!this.listeners.has(event)) {
            this.listeners.set(event, []);
        }
        this.listeners.get(event).push(callback);
    }

    removeEventListener(event, callback) {
        if (this.listeners.has(event)) {
            const callbacks = this.listeners.get(event);
            const index = callbacks.indexOf(callback);
            if (index > -1) {
                callbacks.splice(index, 1);
            }
        }
    }

    notify(event, data) {
        if (this.listeners.has(event)) {
            this.listeners.get(event).forEach(callback => {
                try {
                    callback(data);
                } catch (error) {
                    console.error(`Event listener error for ${event}:`, error);
                }
            });
        }
    }

    notifyError(message) {
        this.notify('error', message);
        // Show toast notification if available
        if (window.showToast) {
            window.showToast(message, 'error');
        }
    }

    notifySuccess(message) {
        this.notify('success', message);
        // Show toast notification if available
        if (window.showToast) {
            window.showToast(message, 'success');
        }
    }

    // Cache management
    getCache(key) {
        return this.cache.get(key);
    }

    setCache(key, data, ttl = 300000) { // 5 minutes default TTL
        this.cache.set(key, {
            data,
            timestamp: Date.now(),
            ttl
        });
    }

    clearCache(key) {
        if (key) {
            this.cache.delete(key);
        } else {
            this.cache.clear();
        }
    }

    isCacheValid(key) {
        const cached = this.cache.get(key);
        if (!cached) return false;
        
        return Date.now() - cached.timestamp < cached.ttl;
    }

    // Utility methods
    formatDateTime(dateString) {
        const date = new Date(dateString);
        return date.toLocaleString();
    }

    formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

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

    // Authentication
    setToken(token) {
        this.token = token;
        localStorage.setItem('authToken', token);
    }

    clearToken() {
        this.token = null;
        localStorage.removeItem('authToken');
    }

    isAuthenticated() {
        return !!this.token;
    }
}

// Global instance
window.superAdminDataSync = new SuperAdminDataSync();

// Utility functions for common operations
window.superAdminUtils = {
    // Format status badges
    formatStatusBadge(status) {
        const statusConfig = {
            active: { class: 'bg-green-100 text-green-800', icon: 'check_circle' },
            inactive: { class: 'bg-gray-100 text-gray-800', icon: 'pause_circle' },
            suspended: { class: 'bg-red-100 text-red-800', icon: 'block' },
            pending: { class: 'bg-yellow-100 text-yellow-800', icon: 'hourglass_empty' },
            completed: { class: 'bg-blue-100 text-blue-800', icon: 'done_all' },
            failed: { class: 'bg-red-100 text-red-800', icon: 'error' },
            generating: { class: 'bg-purple-100 text-purple-800', icon: 'autorenew' }
        };

        const config = statusConfig[status] || statusConfig.active;
        return `<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.class}">
            <span class="material-symbols-rounded text-xs mr-1">${config.icon}</span>
            ${status.charAt(0).toUpperCase() + status.slice(1)}
        </span>`;
    },

    // Format severity badges
    formatSeverityBadge(severity) {
        const severityConfig = {
            low: { class: 'bg-blue-100 text-blue-800', icon: 'info' },
            medium: { class: 'bg-yellow-100 text-yellow-800', icon: 'warning' },
            high: { class: 'bg-orange-100 text-orange-800', icon: 'priority_high' },
            critical: { class: 'bg-red-100 text-red-800', icon: 'dangerous' }
        };

        const config = severityConfig[severity] || severityConfig.medium;
        return `<span class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${config.class}">
            <span class="material-symbols-rounded text-xs mr-1">${config.icon}</span>
            ${severity.charAt(0).toUpperCase() + severity.slice(1)}
        </span>`;
    },

    // Show confirmation dialog
    confirm(message, callback) {
        if (confirm(message)) {
            callback();
        }
    },

    // Show loading state
    showLoading(element) {
        if (element) {
            element.disabled = true;
            element.innerHTML = '<span class="material-symbols-rounded animate-spin">refresh</span> Loading...';
        }
    },

    // Hide loading state
    hideLoading(element, originalText) {
        if (element) {
            element.disabled = false;
            element.innerHTML = originalText || element.dataset.originalText || 'Submit';
        }
    },

    // Initialize tooltips
    initTooltips() {
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    },

    // Initialize modals
    initModals() {
        const modalElements = document.querySelectorAll('.modal');
        modalElements.forEach(modal => {
            new bootstrap.Modal(modal);
        });
    }
};

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = { SuperAdminDataSync, superAdminUtils };
}
