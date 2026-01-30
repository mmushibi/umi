/**
 * Admin Portal API Client - Updated to use new service-based controllers
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
    return await this.request('/admin/stats');
  }

  async getRecentActivity() {
    return await this.request('/admin/dashboard/activity');
  }

  async getSystemHealth() {
    return await this.request('/admin/system/health');
  }

  // Tenant Management
  async getTenantSettings() {
    return await this.request('/admin/tenant');
  }

  async updateTenantSettings(settings) {
    return await this.request('/admin/pharmacy/settings', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  async getSubscriptionStatus() {
    return await this.request('/admin/subscription/status');
  }

  async upgradeSubscription(planData) {
    return await this.request('/account/subscription/upgrade', {
      method: 'POST',
      body: JSON.stringify(planData)
    });
  }

  // User Management - Using new UserController
  async getUsers() {
    return await this.request('/user/tenant-users');
  }

  async getUserProfile() {
    return await this.request('/user/profile');
  }

  async updateUserProfile(updateData) {
    return await this.request('/user/profile', {
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

  // Patient Management - Using new PatientController
  async getPatients() {
    return await this.request('/patient');
  }

  async getPatient(patientId) {
    return await this.request(`/patient/${patientId}`);
  }

  async createPatient(patientData) {
    return await this.request('/patient', {
      method: 'POST',
      body: JSON.stringify(patientData)
    });
  }

  async updatePatient(patientId, patientData) {
    return await this.request(`/patient/${patientId}`, {
      method: 'PUT',
      body: JSON.stringify(patientData)
    });
  }

  async deletePatient(patientId) {
    return await this.request(`/patient/${patientId}`, {
      method: 'DELETE'
    });
  }

  async searchPatients(searchTerm) {
    return await this.request('/patient/search', {
      method: 'POST',
      body: JSON.stringify({ searchTerm })
    });
  }

  async addPatientAllergy(patientId, allergy) {
    return await this.request(`/patient/${patientId}/allergies`, {
      method: 'POST',
      body: JSON.stringify({ allergy })
    });
  }

  async removePatientAllergy(patientId, allergy) {
    return await this.request(`/patient/${patientId}/allergies`, {
      method: 'DELETE',
      body: JSON.stringify({ allergy })
    });
  }

  // Inventory Management - Using new InventoryController
  async getInventory() {
    return await this.request('/inventory');
  }

  async getInventoryItem(inventoryId) {
    return await this.request(`/inventory/${inventoryId}`);
  }

  async createInventoryItem(inventoryData) {
    return await this.request('/inventory', {
      method: 'POST',
      body: JSON.stringify(inventoryData)
    });
  }

  async updateInventoryItem(inventoryId, inventoryData) {
    return await this.request(`/inventory/${inventoryId}`, {
      method: 'PUT',
      body: JSON.stringify(inventoryData)
    });
  }

  async deleteInventoryItem(inventoryId) {
    return await this.request(`/inventory/${inventoryId}`, {
      method: 'DELETE'
    });
  }

  async searchInventory(searchTerm) {
    return await this.request('/inventory/search', {
      method: 'POST',
      body: JSON.stringify({ searchTerm })
    });
  }

  async getLowStockItems(threshold = 10) {
    return await this.request('/inventory/low-stock', {
      method: 'GET',
      params: { threshold }
    });
  }

  async updateStock(inventoryId, quantity, operation) {
    return await this.request(`/inventory/${inventoryId}/stock`, {
      method: 'POST',
      body: JSON.stringify({ quantity, operation })
    });
  }

  async checkExpiryDates(daysThreshold = 30) {
    return await this.request('/inventory/expiry-check', {
      method: 'GET',
      params: { daysThreshold }
    });
  }

  // Reports - Using new ReportController
  async getReports() {
    return await this.request('/report');
  }

  async getReport(reportId) {
    return await this.request(`/report/${reportId}`);
  }

  async generateSalesReport(startDate, endDate) {
    return await this.request('/report/sales', {
      method: 'POST',
      body: JSON.stringify({ startDate, endDate })
    });
  }

  async generateInventoryReport() {
    return await this.request('/report/inventory', {
      method: 'POST'
    });
  }

  async generatePatientReport() {
    return await this.request('/report/patient', {
      method: 'POST'
    });
  }

  async generateFinancialReport(startDate, endDate) {
    return await this.request('/report/financial', {
      method: 'POST',
      body: JSON.stringify({ startDate, endDate })
    });
  }

  async deleteReport(reportId) {
    return await this.request(`/report/${reportId}`, {
      method: 'DELETE'
    });
  }

  async exportReportToPdf(reportId) {
    const url = `${this.baseURL}/report/${reportId}/export/pdf`;
    const response = await fetch(url, {
      headers: this.getHeaders()
    });
    
    if (!response.ok) {
      throw new Error(`Export failed: ${response.status}`);
    }
    
    return await response.blob();
  }

  async exportReportToExcel(reportId) {
    const url = `${this.baseURL}/report/${reportId}/export/excel`;
    const response = await fetch(url, {
      headers: this.getHeaders()
    });
    
    if (!response.ok) {
      throw new Error(`Export failed: ${response.status}`);
    }
    
    return await response.blob();
  }

  // Sales Management (keeping existing endpoints for now)
  async getSales(startDate, endDate) {
    const params = {};
    if (startDate) params.startDate = startDate.toISOString();
    if (endDate) params.endDate = endDate.toISOString();
    
    return await this.request('/admin/sales', {
      method: 'GET',
      params
    });
  }

  async createSale(saleData) {
    return await this.request('/admin/sales', {
      method: 'POST',
      body: JSON.stringify(saleData)
    });
  }

  async getSale(saleId) {
    return await this.request(`/admin/sales/${saleId}`);
  }

  // Legacy endpoints for backward compatibility
  async getLegacyUsers() {
    return await this.request('/admin/users');
  }

  async getLegacyPatients() {
    return await this.request('/admin/patients');
  }

  async getLegacyInventory() {
    return await this.request('/admin/inventory');
  }
}

// Initialize global API instance
window.adminAPI = new AdminAPI();
