/**
 * Cashier Portal API Client
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
    return {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${this.token}`,
      'X-Tenant-ID': this.tenantId
    };
  }

  // Generic API request method
  async request(endpoint, options = {}) {
    const url = `${this.baseURL}${endpoint}`;
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
    return await this.request('/sales/stats');
  }

  async getRecentSales(limit = 10) {
    return await this.request(`/sales?pageSize=${limit}`);
  }

  async getLowStockItems() {
    return await this.request('/inventory/low-stock');
  }

  async getPopularProducts(limit = 5) {
    return await this.request(`/products/popular?limit=${limit}`);
  }

  async getRecentPatients(limit = 5) {
    return await this.request(`/patients?pageSize=${limit}`);
  }

  // Sales Management
  async getSales(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      search: filters.search || '',
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      status: filters.status || ''
    });
    return await this.request(`/sales?${params}`);
  }

  async getSaleDetails(saleId) {
    return await this.request(`/sales/${saleId}`);
  }

  async createSale(saleData) {
    return await this.request('/sales', {
      method: 'POST',
      body: JSON.stringify(saleData)
    });
  }

  async updateSale(saleId, saleData) {
    return await this.request(`/sales/${saleId}`, {
      method: 'PUT',
      body: JSON.stringify(saleData)
    });
  }

  async getSalesRevenueStats(period = 7) {
    const endDate = new Date();
    const startDate = new Date();
    startDate.setDate(endDate.getDate() - period);
    
    return await this.request(`/sales/stats?startDate=${startDate.toISOString()}&endDate=${endDate.toISOString()}`);
  }

  async getPaymentMethodsStats() {
    return await this.request('/sales/payment-methods-stats');
  }

  // Payments Management
  async getPayments(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      search: filters.search || '',
      status: filters.status || '',
      type: filters.type || ''
    });
    return await this.request(`/payments?${params}`);
  }

  async getPaymentDetails(paymentId) {
    return await this.request(`/payments/${paymentId}`);
  }

  async createPayment(paymentData) {
    return await this.request('/payments', {
      method: 'POST',
      body: JSON.stringify(paymentData)
    });
  }

  async updatePayment(paymentId, paymentData) {
    return await this.request(`/payments/${paymentId}`, {
      method: 'PUT',
      body: JSON.stringify(paymentData)
    });
  }

  async getPaymentStats() {
    return await this.request('/payments/stats');
  }

  async createInvoice(invoiceData) {
    return await this.request('/payments/invoices', {
      method: 'POST',
      body: JSON.stringify(invoiceData)
    });
  }

  async createQuotation(quotationData) {
    return await this.request('/payments/quotations', {
      method: 'POST',
      body: JSON.stringify(quotationData)
    });
  }

  // Point of Sale
  async getProducts(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 100,
      search: filters.search || '',
      category: filters.category || '',
      inStock: filters.inStock !== false
    });
    return await this.request(`/products?${params}`);
  }

  async getProductDetails(productId) {
    return await this.request(`/products/${productId}`);
  }

  async searchProducts(query) {
    return await this.request(`/products/search?q=${encodeURIComponent(query)}`);
  }

  async checkProductStock(productId) {
    return await this.request(`/products/${productId}/stock`);
  }

  // Patients Management
  async getPatients(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      search: filters.search || ''
    });
    return await this.request(`/patients?${params}`);
  }

  async getPatientDetails(patientId) {
    return await this.request(`/patients/${patientId}`);
  }

  async createPatient(patientData) {
    return await this.request('/patients', {
      method: 'POST',
      body: JSON.stringify(patientData)
    });
  }

  async updatePatient(patientId, patientData) {
    return await this.request(`/patients/${patientId}`, {
      method: 'PUT',
      body: JSON.stringify(patientData)
    });
  }

  async searchPatients(query) {
    return await this.request(`/patients/search?q=${encodeURIComponent(query)}`);
  }

  // Account Management
  async getUserProfile() {
    return await this.request('/account/profile');
  }

  async updateProfile(profileData) {
    return await this.request('/account/profile', {
      method: 'PUT',
      body: JSON.stringify(profileData)
    });
  }

  async changePassword(passwordData) {
    return await this.request('/account/change-password', {
      method: 'POST',
      body: JSON.stringify(passwordData)
    });
  }

  async getNotificationSettings() {
    return await this.request('/account/notifications');
  }

  async updateNotificationSettings(settings) {
    return await this.request('/account/notifications', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  async getPharmacySettings() {
    return await this.request('/account/pharmacy-settings');
  }

  async updatePharmacySettings(settings) {
    return await this.request('/account/pharmacy-settings', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  // Cashier Portal Integration
  async registerPortal() {
    return await this.request('/cashier/register', {
      method: 'POST'
    });
  }

  async getPortalStatus() {
    return await this.request('/cashier/status');
  }

  async notifyDataChange(entityType, data) {
    return await this.request(`/cashier/notify/${entityType}`, {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  // Utility Methods
  formatCurrency(amount) {
    return new Intl.NumberFormat('en-ZM', {
      style: 'currency',
      currency: 'ZMW'
    }).format(amount);
  }

  formatDate(date) {
    return new Date(date).toLocaleDateString('en-ZM');
  }

  formatDateTime(date) {
    return new Date(date).toLocaleString('en-ZM');
  }

  // Error handling
  handleError(error, context = '') {
    console.error(`Cashier API Error ${context}:`, error);
    
    // You can implement custom error handling here
    // For example, show toast notifications, redirect to login, etc.
    
    if (error.message.includes('401') || error.message.includes('Unauthorized')) {
      // Token expired or invalid - redirect to login
      localStorage.removeItem('umi_access_token');
      window.location.href = '../index.html';
    }
  }
}

// Initialize global cashier API instance
window.cashierAPI = new CashierAPI();
