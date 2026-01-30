/**
 * Cashier Portal API Client - Updated to use new service-based controllers
 * Handles all backend API calls for cashier functionality
 */
class CashierAPI {
  constructor() {
    this.baseURL = 'http://localhost:5000/api/v1';
    this.token = localStorage.getItem('umi_access_token');
    this.tenantId = localStorage.getItem('umi_tenant_id');
    this.branchId = localStorage.getItem('umi_branch_id');
  }

  // Set authentication token
  setToken(token) {
    this.token = token;
    localStorage.setItem('umi_access_token', token);
  }

  // Get auth headers
  getHeaders() {
    const headers = {
      'Content-Type': 'application/json'
    };

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`;
    }

    if (this.tenantId) {
      headers['X-Tenant-ID'] = this.tenantId;
    }

    if (this.branchId) {
      headers['X-Branch-ID'] = this.branchId;
    }

    return headers;
  }

  // Generic API request method
  async request(endpoint, options = {}) {
    const url = `${this.baseURL}${endpoint}`;
    
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
        // If 404, return empty data instead of throwing error
        if (response.status === 404) {
          console.warn(`API endpoint not found: ${endpoint}, returning empty data`);
          return { data: [] };
        }
        const error = await response.json().catch(() => ({ message: `HTTP ${response.status}` }));
        throw new Error(error.message || `HTTP ${response.status}`);
      }
      
      // Handle empty responses
      const text = await response.text();
      return text ? JSON.parse(text) : { data: [] };
    } catch (error) {
      console.error(`API Error (${endpoint}):`, error);
      throw error;
    }
  }

  // ==================== USER MANAGEMENT ====================
  // Using new UserController

  async getUserProfile() {
    return await this.request('/user/profile');
  }

  async updateUserProfile(updateData) {
    return await this.request('/user/profile', {
      method: 'PUT',
      body: JSON.stringify(updateData)
    });
  }

  async changePassword(currentPassword, newPassword) {
    return await this.request('/user/change-password', {
      method: 'POST',
      body: JSON.stringify({ currentPassword, newPassword })
    });
  }

  // ==================== INVENTORY MANAGEMENT ====================
  // Using new InventoryController

  async getInventory() {
    return await this.request('/inventory');
  }

  async getInventoryItem(inventoryId) {
    return await this.request(`/inventory/${inventoryId}`);
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

  // ==================== PATIENT MANAGEMENT ====================
  // Using new PatientController

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

  async searchPatients(searchTerm) {
    return await this.request('/patient/search', {
      method: 'POST',
      body: JSON.stringify({ searchTerm })
    });
  }

  // ==================== SALES MANAGEMENT ====================
  // Using existing endpoints (will be updated when SalesController is created)

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

  async updateSale(saleId, saleData) {
    return await this.request(`/admin/sales/${saleId}`, {
      method: 'PUT',
      body: JSON.stringify(saleData)
    });
  }

  async deleteSale(saleId) {
    return await this.request(`/admin/sales/${saleId}`, {
      method: 'DELETE'
    });
  }

  async refundSale(saleId, refundData) {
    return await this.request(`/admin/sales/${saleId}/refund`, {
      method: 'POST',
      body: JSON.stringify(refundData)
    });
  }

  // ==================== PAYMENT PROCESSING ====================

  async processPayment(paymentData) {
    return await this.request('/payments/process', {
      method: 'POST',
      body: JSON.stringify(paymentData)
    });
  }

  async getPaymentMethods() {
    return await this.request('/payments/methods');
  }

  async validatePayment(paymentData) {
    return await this.request('/payments/validate', {
      method: 'POST',
      body: JSON.stringify(paymentData)
    });
  }

  async refundPayment(paymentId, refundData) {
    return await this.request(`/payments/${paymentId}/refund`, {
      method: 'POST',
      body: JSON.stringify(refundData)
    });
  }

  // ==================== SHIFT MANAGEMENT ====================

  async getCurrentShift() {
    return await this.request('/shifts/current');
  }

  async startShift(shiftData) {
    return await this.request('/shifts/start', {
      method: 'POST',
      body: JSON.stringify(shiftData)
    });
  }

  async endShift(shiftId, shiftData) {
    return await this.request(`/shifts/${shiftId}/end`, {
      method: 'POST',
      body: JSON.stringify(shiftData)
    });
  }

  async getShiftHistory(filters = {}) {
    return await this.request('/shifts/history', {
      method: 'GET',
      params: filters
    });
  }

  // ==================== QUEUE MANAGEMENT ====================

  async getQueueStatus() {
    return await this.request('/queue/status');
  }

  async addToQueue(queueData) {
    return await this.request('/queue/add', {
      method: 'POST',
      body: JSON.stringify(queueData)
    });
  }

  async removeFromQueue(queueId) {
    return await this.request(`/queue/${queueId}/remove`, {
      method: 'DELETE'
    });
  }

  async updateQueuePosition(queueId, position) {
    return await this.request(`/queue/${queueId}/position`, {
      method: 'PUT',
      body: JSON.stringify({ position })
    });
  }

  // ==================== REPORTS ====================
  // Using new ReportController

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

  async generateFinancialReport(startDate, endDate) {
    return await this.request('/report/financial', {
      method: 'POST',
      body: JSON.stringify({ startDate, endDate })
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

  // ==================== RECEIPTS ====================

  async generateReceipt(saleId) {
    return await this.request(`/receipts/${saleId}/generate`);
  }

  async emailReceipt(saleId, emailData) {
    return await this.request(`/receipts/${saleId}/email`, {
      method: 'POST',
      body: JSON.stringify(emailData)
    });
  }

  async printReceipt(saleId) {
    return await this.request(`/receipts/${saleId}/print`);
  }

  // ==================== NOTIFICATIONS ====================

  async getNotifications() {
    return await this.request('/notifications');
  }

  async markNotificationRead(notificationId) {
    return await this.request(`/notifications/${notificationId}/read`, {
      method: 'POST'
    });
  }

  // ==================== UTILITIES ====================

  async getSystemHealth() {
    return await this.request('/health');
  }

  async getDashboardStats() {
    return await this.request('/admin/stats');
  }

  async getTenantSettings() {
    return await this.request('/admin/tenant');
  }
}

// Initialize global API instance
window.cashierAPI = new CashierAPI();
