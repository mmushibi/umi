/**
 * Admin Portal API Client
 * Handles all backend API calls for admin functionality
 */
class AdminAPI {
  constructor() {
    this.baseURL = 'http://localhost:5000/api/v1';
    this.token = localStorage.getItem('umi_access_token');
    this.tenantId = localStorage.getItem('umi_tenant_id');
  }

  // Set authentication token
  setToken(token) {
    this.token = token;
    localStorage.setItem('umi_access_token', token);
  }

  // Get auth headers
  getHeaders() {
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.token}`,
      'X-Tenant-ID': this.tenantId
    };
  }

  // Generic API request method
  async request(endpoint, options = {}) {
    let url = `${this.baseURL}${endpoint}`;
    
    // Handle query parameters for GET requests
    if (options.params && options.method === 'GET') {
      const params = new URLSearchParams(options.params);
      url += `?${params.toString()}`;
      delete options.params;
    }
    
    const config = {
      headers: this.getHeaders(),
      ...options
    };

    try {
      const response = await fetch(url, config);
      
      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || `HTTP ${response.status}`);
      }
      
      return await response.json();
    } catch (error) {
      console.error(`API Error (${endpoint}):`, error);
      throw error;
    }
  }

  // Dashboard APIs
  async getDashboardStats() {
    return await this.request('/admin/dashboard/stats');
  }

  async getRecentActivity() {
    return await this.request('/admin/dashboard/activity');
  }

  async getSystemHealth() {
    return await this.request('/admin/system/health');
  }

  // Tenant Management
  async getTenantSettings() {
    return await this.request('/account/tenant/settings');
  }

  async updateTenantSettings(settings) {
    return await this.request('/account/tenant/settings', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  async getSubscriptionStatus() {
    return await this.request('/auth/subscription/status');
  }

  async upgradeSubscription(planData) {
    return await this.request('/account/subscription/upgrade', {
      method: 'POST',
      body: JSON.stringify(planData)
    });
  }

  // User Management - Using new UserController
  async getUsers(page = 1, limit = 100, filters = {}) {
    return await this.request('/user/tenant-users', {
      method: 'GET',
      params: { page, limit, ...filters }
    });
  }

  async getUserProfile() {
    return await this.request('/user/profile');
  }

  async updateUserProfile(userId, updateData) {
    return await this.request(`/user/profile`, {
      method: 'PUT',
      body: JSON.stringify(updateData)
    });
  }

  async createUser(userData) {
    return await this.request('/user/create', {
      method: 'POST',
      body: JSON.stringify(userData)
    });
  }

  async deleteUser(userId) {
    return await this.request(`/user/${userId}`, {
      method: 'DELETE'
    });
  }

  async changePassword(currentPassword, newPassword) {
    return await this.request('/user/change-password', {
      method: 'POST',
      body: JSON.stringify({ currentPassword, newPassword })
    });
  }

  async resetPassword(email) {
    return await this.request('/user/reset-password', {
      method: 'POST',
      body: JSON.stringify({ email })
    });
  }
    
    return await this.request('/admin/users', {
      method: 'GET',
      params
    });
  }

  async createUser(userData) {
    return await this.request('/admin/users', {
      method: 'POST',
      body: JSON.stringify(userData)
    });
  }

  async updateUser(userId, userData) {
    return await this.request(`/admin/users/${userId}`, {
      method: 'PUT',
      body: JSON.stringify(userData)
    });
  }

  async deleteUser(userId) {
    return await this.request(`/admin/users/${userId}`, {
      method: 'DELETE'
    });
  }

  async getUserPermissions(userId) {
    return await this.request(`/admin/users/${userId}/permissions`);
  }

  async updateUserPermissions(userId, permissions) {
    return await this.request(`/admin/users/${userId}/permissions`, {
      method: 'PUT',
      body: JSON.stringify(permissions)
    });
  }

  // Branch Management
  async getBranches() {
    return await this.request('/admin/branches');
  }

  async createBranch(branchData) {
    return await this.request('/admin/branches', {
      method: 'POST',
      body: JSON.stringify(branchData)
    });
  }

  async updateBranch(branchId, branchData) {
    return await this.request(`/admin/branches/${branchId}`, {
      method: 'PUT',
      body: JSON.stringify(branchData)
    });
  }

  async deleteBranch(branchId) {
    return await this.request(`/admin/branches/${branchId}`, {
      method: 'DELETE'
    });
  }

  // Product Management
  async getProducts() {
    return await this.request('/admin/products');
  }

  // Inventory Management
  async getInventory(filters = {}) {
    const params = new URLSearchParams(filters);
    return await this.request(`/admin/inventory?${params}`);
  }

  async addProduct(productData) {
    return await this.request('/admin/inventory/products', {
      method: 'POST',
      body: JSON.stringify(productData)
    });
  }

  async updateProduct(productId, productData) {
    return await this.request(`/admin/inventory/products/${productId}`, {
      method: 'PUT',
      body: JSON.stringify(productData)
    });
  }

  async deleteProduct(productId) {
    return await this.request(`/admin/inventory/products/${productId}`, {
      method: 'DELETE'
    });
  }

  async getLowStockItems() {
    return await this.request('/admin/inventory/low-stock');
  }

  // Sales Management
  async getSales(filters = {}) {
    const params = new URLSearchParams(filters);
    return await this.request(`/admin/sales?${params}`);
  }

  async getSaleDetails(saleId) {
    return await this.request(`/admin/sales/${saleId}`);
  }

  async generateSalesReport(reportData) {
    return await this.request('/admin/reports/sales', {
      method: 'POST',
      body: JSON.stringify(reportData)
    });
  }

  // Patient Management
  async getPatients(filters = {}) {
    const params = new URLSearchParams(filters);
    return await this.request(`/admin/patients?${params}`);
  }

  async createPatient(patientData) {
    return await this.request('/admin/patients', {
      method: 'POST',
      body: JSON.stringify(patientData)
    });
  }

  async updatePatient(patientId, patientData) {
    return await this.request(`/admin/patients/${patientId}`, {
      method: 'PUT',
      body: JSON.stringify(patientData)
    });
  }

  async deletePatient(patientId) {
    return await this.request(`/admin/patients/${patientId}`, {
      method: 'DELETE'
    });
  }

  // Prescription Management
  async getPrescriptions(filters = {}) {
    const params = new URLSearchParams(filters);
    return await this.request(`/admin/prescriptions?${params}`);
  }

  async approvePrescription(prescriptionId) {
    return await this.request(`/admin/prescriptions/${prescriptionId}/approve`, {
      method: 'POST'
    });
  }

  async rejectPrescription(prescriptionId, reason) {
    return await this.request(`/admin/prescriptions/${prescriptionId}/reject`, {
      method: 'POST',
      body: JSON.stringify({ reason })
    });
  }

  // System Settings
  async getSystemSettings() {
    return await this.request('/admin/settings/system');
  }

  async updateSystemSettings(settings) {
    return await this.request('/admin/settings/system', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  async getAuditLogs(filters = {}) {
    const params = new URLSearchParams(filters);
    return await this.request(`/admin/audit/logs?${params}`);
  }

  // Reports
  async generateReport(type, filters = {}) {
    return await this.request(`/admin/reports/${type}`, {
      method: 'POST',
      body: JSON.stringify(filters)
    });
  }

  async getReports() {
    return await this.request('/admin/reports');
  }

  async scheduleReport(scheduleData) {
    return await this.request('/admin/reports/schedule', {
      method: 'POST',
      body: JSON.stringify(scheduleData)
    });
  }

  async getScheduledReports() {
    return await this.request('/admin/reports/scheduled');
  }

  async getBranchData(branchId) {
    return await this.request(`/admin/branches/${branchId}/data`);
  }

  async downloadReport(reportId) {
    const response = await fetch(`${this.baseURL}/admin/reports/${reportId}/download`, {
      headers: this.getHeaders()
    });
    
    if (!response.ok) {
      throw new Error('Failed to download report');
    }
    
    return await response.blob();
  }

  // Account Management APIs
  async updateUserProfile(profileData) {
    return await this.request('/account/profile', {
      method: 'PUT',
      body: JSON.stringify(profileData)
    });
  }

  async getUserProfile() {
    return await this.request('/account/profile');
  }

  async updatePharmacySettings(settings) {
    return await this.request('/account/pharmacy/settings', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  async getPharmacySettings() {
    return await this.request('/account/pharmacy/settings');
  }

  async updateNotificationSettings(settings) {
    return await this.request('/account/notifications/settings', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  async getNotificationSettings() {
    return await this.request('/account/notifications/settings');
  }

  async changePassword(passwordData) {
    return await this.request('/account/password/change', {
      method: 'POST',
      body: JSON.stringify(passwordData)
    });
  }

  async getActiveSessions() {
    return await this.request('/account/sessions');
  }

  async revokeSession(sessionId) {
    return await this.request(`/account/sessions/${sessionId}/revoke`, {
      method: 'POST'
    });
  }

  async revokeAllSessions() {
    return await this.request('/account/sessions/revoke-all', {
      method: 'POST'
    });
  }

  async cancelSubscription(cancelData) {
    return await this.request('/account/subscription/cancel', {
      method: 'POST',
      body: JSON.stringify(cancelData)
    });
  }

  async sendNotification(notificationData) {
    return await this.request('/notifications/send', {
      method: 'POST',
      body: JSON.stringify(notificationData)
    });
  }

  // Data Sync APIs
  async syncUserData() {
    return await this.request('/sync/user');
  }

  async syncPharmacyData() {
    return await this.request('/sync/pharmacy');
  }

  async broadcastUpdate(updateData) {
    return await this.request('/sync/broadcast', {
      method: 'POST',
      body: JSON.stringify(updateData)
    });
  }
}

// Initialize global admin API instance
window.adminAPI = new AdminAPI();
