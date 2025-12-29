/**
 * Cashier Portal API Client
 * Handles all backend API calls for cashier functionality
 */
class CashierAPI {
  constructor() {
    this.baseURL = 'http://localhost:5000/api/v1'; // Updated to use v1 API
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
      if (!text) {
        return { data: [] };
      }
      
      return JSON.parse(text);
    } catch (error) {
      console.error(`API Error (${endpoint}):`, error);
      // Return empty data for network errors to prevent UI crashes
      if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
        return { data: [] };
      }
      throw error;
    }
  }

  // Dashboard APIs
  async getDashboardStats() {
    return await this.request('/sales/stats');
  }

  async getSalesStats() {
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
    const startDate = new Date(endDate.getTime() - (period * 24 * 60 * 60 * 1000));
    
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

  // Insurance Claims Management
  async getInsuranceClaims(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      status: filters.status || '',
      patientId: filters.patientId || ''
    });
    return await this.request(`/insurance-claims?${params}`);
  }

  async createInsuranceClaim(claimData) {
    return await this.request('/insurance-claims', {
      method: 'POST',
      body: JSON.stringify(claimData)
    });
  }

  async getInsuranceClaimDetails(claimId) {
    return await this.request(`/insurance-claims/${claimId}`);
  }

  async updateInsuranceClaim(claimId, claimData) {
    return await this.request(`/insurance-claims/${claimId}`, {
      method: 'PUT',
      body: JSON.stringify(claimData)
    });
  }

  async submitInsuranceClaim(claimId) {
    return await this.request(`/insurance-claims/${claimId}/submit`, {
      method: 'POST'
    });
  }

  async adjudicateInsuranceClaim(claimId, adjudicationData) {
    return await this.request(`/insurance-claims/${claimId}/adjudicate`, {
      method: 'POST',
      body: JSON.stringify(adjudicationData)
    });
  }

  async getInsuranceProviders() {
    return await this.request('/insurance-providers');
  }

  async validateInsuranceCoverage(patientId, insuranceProviderId) {
    return await this.request(`/insurance/validate-coverage`, {
      method: 'POST',
      body: JSON.stringify({
        patientId: patientId,
        insuranceProviderId: insuranceProviderId
      })
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

  async getProductByBarcode(barcode) {
    return await this.request(`/products/barcode/${barcode}`);
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

  // Security Management
  async getPasswordInfo() {
    return await this.request('/account/password-info');
  }

  async getTwoFactorStatus() {
    return await this.request('/account/2fa-status');
  }

  async sendTwoFactorCode(data) {
    return await this.request('/account/2fa/send-code', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  async enableTwoFactor(data) {
    return await this.request('/account/2fa/enable', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  async disableTwoFactor(data) {
    return await this.request('/account/2fa/disable', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  async getLoginSessions() {
    return await this.request('/account/sessions');
  }

  async revokeSession(sessionId) {
    return await this.request(`/account/sessions/${sessionId}`, {
      method: 'DELETE'
    });
  }

  async revokeAllSessions() {
    return await this.request('/account/sessions/revoke-all', {
      method: 'POST'
    });
  }

  // User Management
  getUserId() {
    // Extract user ID from JWT token or return from storage
    const token = this.token;
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        return payload.userId || payload.sub;
      } catch (e) {
        return localStorage.getItem('umi_user_id');
      }
    }
    return localStorage.getItem('umi_user_id');
  }

  getTenantId() {
    return this.tenantId || localStorage.getItem('umi_tenant_id');
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

  async notifyUserChange(changeData) {
    return await this.request('/account/notify-change', {
      method: 'POST',
      body: JSON.stringify(changeData)
    });
  }

  async logout() {
    return await this.request('/auth/logout', {
      method: 'POST'
    });
  }

  // Queue Management APIs
  async getQueueData() {
    return await this.request('/queue/current');
  }

  async getQueueStats() {
    return await this.request('/queue/stats');
  }

  async addToQueue(patientData) {
    return await this.request('/queue/add', {
      method: 'POST',
      body: JSON.stringify(patientData)
    });
  }

  async servePatient(patientId) {
    return await this.request(`/queue/${patientId}/serve`, {
      method: 'POST'
    });
  }

  async removePatient(patientId) {
    return await this.request(`/queue/${patientId}/remove`, {
      method: 'DELETE'
    });
  }

  async clearQueue() {
    return await this.request('/queue/clear', {
      method: 'POST'
    });
  }

  async callNextPatient() {
    return await this.request('/queue/next', {
      method: 'POST'
    });
  }

  async completePatientService(patientId, serviceData) {
    return await this.request(`/queue/${patientId}/complete`, {
      method: 'POST',
      body: JSON.stringify(serviceData)
    });
  }

  async getPatientQueueHistory(patientId) {
    return await this.request(`/queue/patient/${patientId}/history`);
  }

  async updateQueuePosition(patientId, newPosition) {
    return await this.request(`/queue/${patientId}/position`, {
      method: 'PUT',
      body: JSON.stringify({ position: newPosition })
    });
  }

  // Doctor/Provider Management
  async getProviders() {
    return await this.request('/providers');
  }

  async getProviderAvailability(providerId) {
    return await this.request(`/providers/${providerId}/availability`);
  }

  async assignPatientToProvider(patientId, providerId) {
    return await this.request(`/queue/${patientId}/assign`, {
      method: 'POST',
      body: JSON.stringify({ providerId })
    });
  }

  async getProviderQueue(providerId) {
    return await this.request(`/providers/${providerId}/queue`);
  }

  // Emergency/Priority Management
  async getEmergencyQueue() {
    return await this.request('/queue/emergency');
  }

  async addToEmergencyQueue(patientData) {
    return await this.request('/queue/emergency/add', {
      method: 'POST',
      body: JSON.stringify(patientData)
    });
  }

  async updatePatientPriority(patientId, priority) {
    return await this.request(`/queue/${patientId}/priority`, {
      method: 'PUT',
      body: JSON.stringify({ priority })
    });
  }

  async escalatePriority(patientId) {
    return await this.request(`/queue/${patientId}/escalate`, {
      method: 'POST'
    });
  }

  // Search and Filter
  async searchPatients(query, filters = {}) {
    const params = new URLSearchParams({
      q: query,
      status: filters.status || '',
      priority: filters.priority || '',
      provider: filters.provider || '',
      dateFrom: filters.dateFrom || '',
      dateTo: filters.dateTo || ''
    });
    return await this.request(`/queue/search?${params}`);
  }

  async filterQueue(filters) {
    const params = new URLSearchParams(filters);
    return await this.request(`/queue/filter?${params}`);
  }

  // Notifications
  async sendSMSNotification(patientId, message) {
    return await this.request(`/notifications/sms/${patientId}`, {
      method: 'POST',
      body: JSON.stringify({ message })
    });
  }

  async sendWhatsAppNotification(patientId, message) {
    return await this.request(`/notifications/whatsapp/${patientId}`, {
      method: 'POST',
      body: JSON.stringify({ message })
    });
  }

  async getNotificationSettings() {
    return await this.request('/notifications/settings');
  }

  async updateNotificationSettings(settings) {
    return await this.request('/notifications/settings', {
      method: 'PUT',
      body: JSON.stringify(settings)
    });
  }

  // Analytics and Reporting
  async getQueueAnalytics(period = 7) {
    return await this.request(`/analytics/queue?period=${period}`);
  }

  async getWaitTimeAnalytics(period = 30) {
    return await this.request(`/analytics/wait-times?period=${period}`);
  }

  async getProviderAnalytics(providerId, period = 30) {
    return await this.request(`/analytics/provider/${providerId}?period=${period}`);
  }

  async getPeakHourAnalysis(period = 30) {
    return await this.request(`/analytics/peak-hours?period=${period}`);
  }

  async getQueueForecast(days = 7) {
    return await this.request(`/analytics/forecast?days=${days}`);
  }

  // History and Audit Trail
  async getQueueHistory(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      dateFrom: filters.dateFrom || '',
      dateTo: filters.dateTo || '',
      action: filters.action || '',
      userId: filters.userId || ''
    });
    return await this.request(`/queue/history?${params}`);
  }

  async getPatientQueueHistory(patientId) {
    return await this.request(`/queue/patient/${patientId}/history`);
  }

  async getAuditLog(filters = {}) {
    const params = new URLSearchParams(filters);
    return await this.request(`/audit/log?${params}`);
  }

  // Bulk Operations
  async bulkServePatients(patientIds) {
    return await this.request('/queue/bulk/serve', {
      method: 'POST',
      body: JSON.stringify({ patientIds })
    });
  }

  async bulkRemovePatients(patientIds) {
    return await this.request('/queue/bulk/remove', {
      method: 'POST',
      body: JSON.stringify({ patientIds })
    });
  }

  async bulkAssignProvider(patientIds, providerId) {
    return await this.request('/queue/bulk/assign', {
      method: 'POST',
      body: JSON.stringify({ patientIds, providerId })
    });
  }

  async bulkUpdatePriority(patientIds, priority) {
    return await this.request('/queue/bulk/priority', {
      method: 'POST',
      body: JSON.stringify({ patientIds, priority })
    });
  }

  // Export and Print
  async exportQueue(format = 'pdf', filters = {}) {
    const params = new URLSearchParams({
      format,
      ...filters
    });
    
    const url = `${this.baseURL}/queue/export?${params}`;
    const response = await fetch(url, {
      headers: this.getHeaders()
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || `HTTP ${response.status}`);
    }

    return response.blob();
  }

  async printQueueSlip(patientId) {
    return await this.request(`/queue/${patientId}/print-slip`, {
      method: 'POST'
    });
  }

  async printDailyReport(date) {
    const params = date ? `?date=${date}` : '';
    return await this.request(`/queue/print/daily-report${params}`, {
      method: 'POST'
    });
  }

  // Patient Feedback
  async collectFeedback(patientId, feedback) {
    return await this.request(`/feedback/${patientId}`, {
      method: 'POST',
      body: JSON.stringify(feedback)
    });
  }

  async getFeedbackStats(period = 30) {
    return await this.request(`/feedback/stats?period=${period}`);
  }

  async getPatientFeedback(patientId) {
    return await this.request(`/feedback/patient/${patientId}`);
  }

  // Sound Management
  async playSound(soundType) {
    // Client-side sound playing
    return new Promise((resolve) => {
      const audio = new Audio();
      switch(soundType) {
        case 'add':
          audio.src = 'data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBSuBzvLZiTYIG2m98OScTgwOUarm7blmGgU7k9n1unEiBC13yO/eizEIHWq+8+OWT';
          break;
        case 'serve':
          audio.src = 'data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBSuBzvLZiTYIG2m98OScTgwOUarm7blmGgU7k9n1unEiBC13yO/eizEIHWq+8+OWT';
          break;
        case 'complete':
          audio.src = 'data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBSuBzvLZiTYIG2m98OScTgwOUarm7blmGgU7k9n1unEiBC13yO/eizEIHWq+8+OWT';
          break;
        case 'emergency':
          audio.src = 'data:audio/wav;base64,UklGRnoGAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQoGAACBhYqFbF1fdJivrJBhNjVgodDbq2EcBj+a2/LDciUFLIHO8tiJNwgZaLvt559NEAxQp+PwtmMcBjiR1/LMeSwFJHfH8N2QQAoUXrTp66hVFApGn+DyvmwhBSuBzvLZiTYIG2m98OScTgwOUarm7blmGgU7k9n1unEiBC13yO/eizEIHWq+8+OWT';
          break;
        default:
          resolve();
          return;
      }
      
      audio.volume = localStorage.getItem('umi_sound_volume') || 0.5;
      audio.play().then(() => {
        setTimeout(resolve, 500);
      }).catch(() => {
        resolve();
      });
    });
  }

  async setSoundVolume(volume) {
    localStorage.setItem('umi_sound_volume', volume);
    return { success: true };
  }

  async getSoundVolume() {
    return { volume: localStorage.getItem('umi_sound_volume') || 0.5 };
  }

  // Reports and Analytics
  async getReportsSummary(filters = {}) {
    const params = new URLSearchParams({
      reportType: filters.reportType || 'summary',
      dateFrom: filters.dateFrom || '',
      dateTo: filters.dateTo || '',
      shift: filters.shift || 'all'
    });
    return await this.request(`/reports/summary?${params}`);
  }

  async getReportSummary(filters = {}) {
    const params = new URLSearchParams({
      reportType: filters.reportType || 'sales',
      dateRange: filters.dateRange || 'today',
      startDate: filters.startDate || '',
      endDate: filters.endDate || ''
    });
    return await this.request(`/reports/summary?${params}`);
  }

  async getSalesReport(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      search: filters.search || '',
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      status: filters.status || '',
      exportFormat: filters.exportFormat || 'json'
    });
    return await this.request(`/reports/sales?${params}`);
  }

  async getPerformanceReport(filters = {}) {
    const params = new URLSearchParams({
      period: filters.period || 'month',
      startDate: filters.startDate || '',
      endDate: filters.endDate || ''
    });
    return await this.request(`/reports/performance?${params}`);
  }

  async getInventoryReport(filters = {}) {
    const params = new URLSearchParams({
      category: filters.category || '',
      lowStock: filters.lowStock || false,
      startDate: filters.startDate || '',
      endDate: filters.endDate || ''
    });
    return await this.request(`/reports/inventory?${params}`);
  }

  async getCustomerReport(filters = {}) {
    const params = new URLSearchParams({
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      customerType: filters.customerType || ''
    });
    return await this.request(`/reports/customers?${params}`);
  }

  async getPatientReport(filters = {}) {
    const params = new URLSearchParams({
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      customerType: 'patient'
    });
    return await this.request(`/reports/customers?${params}`);
  }

  async getFinancialReport(filters = {}) {
    const params = new URLSearchParams({
      period: filters.period || 'month',
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      includeTax: filters.includeTax !== false
    });
    return await this.request(`/reports/financial?${params}`);
  }

  async exportReport(reportType, format = 'pdf', filters = {}) {
    const params = new URLSearchParams({
      reportType: reportType,
      format: format,
      ...filters
    });
    
    const url = `${this.baseURL}/reports/export?${params}`;
    const response = await fetch(url, {
      headers: this.getHeaders()
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || `HTTP ${response.status}`);
    }

    return response.blob();
  }

  async getRevenueTrend(period = 30) {
    return await this.request(`/reports/revenue-trend?period=${period}`);
  }

  async getSalesByCategory(period = 30) {
    return await this.request(`/reports/sales-by-category?period=${period}`);
  }

  async getPeakHoursData(period = 7) {
    return await this.request(`/reports/peak-hours?period=${period}`);
  }

  async getPaymentMethodsStats(period = 30) {
    return await this.request(`/reports/payment-methods?period=${period}`);
  }

  async getDetailedTransactions(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      search: filters.search || '',
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      status: filters.status || '',
      paymentMethod: filters.paymentMethod || ''
    });
    return await this.request(`/reports/transactions?${params}`);
  }

  // Shift Management
  async getShiftData() {
    return await this.request('/shifts/current');
  }

  async startShift(shiftData) {
    return await this.request('/shifts/start', {
      method: 'POST',
      body: JSON.stringify(shiftData)
    });
  }

  async endShift(shiftData) {
    return await this.request('/shifts/end', {
      method: 'POST',
      body: JSON.stringify(shiftData)
    });
  }

  async takeBreak(breakData) {
    return await this.request('/shifts/break', {
      method: 'POST',
      body: JSON.stringify(breakData)
    });
  }

  async getShiftHistory(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      status: filters.status || ''
    });
    return await this.request(`/shifts/history?${params}`);
  }

  async getShiftDetails(shiftId) {
    return await this.request(`/shifts/${shiftId}`);
  }

  async updateShift(shiftId, shiftData) {
    return await this.request(`/shifts/${shiftId}`, {
      method: 'PUT',
      body: JSON.stringify(shiftData)
    });
  }

  async getShiftStats(period = 7) {
    return await this.request(`/shifts/stats?period=${period}`);
  }

  // Point of Sale Data Integration
  async getPOSData(shiftId) {
    return await this.request(`/shifts/${shiftId}/pos-data`);
  }

  async getPOSTransactions(shiftId) {
    return await this.request(`/shifts/${shiftId}/transactions`);
  }

  async getPOSRevenue(shiftId) {
    return await this.request(`/shifts/${shiftId}/revenue`);
  }

  // Inventory Management APIs
  async getInventory(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 100,
      search: filters.search || '',
      category: filters.category || '',
      lowStock: filters.lowStock || false,
      outOfStock: filters.outOfStock || false,
      branchId: this.branchId
    });
    return await this.request(`/inventory?${params}`);
  }

  async getInventoryItem(itemId) {
    return await this.request(`/inventory/${itemId}`);
  }

  async createInventoryItem(itemData) {
    return await this.request('/inventory', {
      method: 'POST',
      body: JSON.stringify(itemData)
    });
  }

  async updateInventoryItem(itemId, itemData) {
    return await this.request(`/inventory/${itemId}`, {
      method: 'PUT',
      body: JSON.stringify(itemData)
    });
  }

  async deleteInventoryItem(itemId) {
    return await this.request(`/inventory/${itemId}`, {
      method: 'DELETE'
    });
  }

  async adjustStock(itemId, adjustmentData) {
    return await this.request(`/inventory/${itemId}/adjust-stock`, {
      method: 'POST',
      body: JSON.stringify(adjustmentData)
    });
  }

  async getInventoryHistory(itemId, filters = {}) {
    // This would need to be implemented in the backend
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      action: filters.action || ''
    });
    return await this.request(`/audit/history/inventory/${itemId}?${params}`);
  }

  async getInventoryStats() {
    // This would need to be implemented in the backend
    return await this.request('/analytics/inventory/stats');
  }

  async getLowStockItems() {
    return await this.request('/inventory/low-stock');
  }

  async getOutOfStockItems() {
    // This would need to be implemented in the backend
    return await this.request('/inventory/out-of-stock');
  }

  async getInventoryCategories() {
    return await this.request('/inventory/categories');
  }

  async getExpiringItems(days = 30) {
    return await this.request(`/inventory/expiring?days=${days}`);
  }

  async exportInventoryCSV(filters = {}) {
    const params = new URLSearchParams({
      ...filters,
      format: 'csv'
    });
    
    const url = `${this.baseURL}/reports/inventory/export?${params}`;
    const response = await fetch(url, {
      headers: this.getHeaders()
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || `HTTP ${response.status}`);
    }

    return response.blob();
  }

  async bulkUpdateInventory(items) {
    return await this.request('/inventory/bulk-upload', {
      method: 'POST',
      body: JSON.stringify({ products: items })
    });
  }

  async importInventoryCSV(file) {
    const formData = new FormData();
    formData.append('file', file);
    
    const response = await fetch(`${this.baseURL}/inventory/import`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.token}`,
        'X-Tenant-ID': this.tenantId
      },
      body: formData
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || `HTTP ${response.status}`);
    }

    return response.json();
  }

  // Reports and Analytics APIs
  async getInventoryReport(filters = {}) {
    const params = new URLSearchParams({
      period: filters.period || '30',
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      tenantId: this.tenantId,
      branchId: this.branchId
    });
    return await this.request(`/reports/inventory?${params}`);
  }

  async getAuditTrail(itemId, params) {
    return await this.request(`/audit/trail/${itemId}?${params}`);
  }

  async getSuppliers() {
    return await this.request(`/suppliers?tenantId=${this.tenantId}`);
  }

  async createSupplier(supplierData) {
    return await this.request('/suppliers', {
      method: 'POST',
      body: JSON.stringify(supplierData)
    });
  }

  async getInventoryForecast(period = 30) {
    return await this.request(`/analytics/inventory/forecast?period=${period}&tenantId=${this.tenantId}`);
  }

  async getSalesAnalytics(period = 30) {
    return await this.request(`/analytics/sales?period=${period}&tenantId=${this.tenantId}`);
  }

  async getInventoryAnalytics(period = 30) {
    return await this.request(`/analytics/inventory?period=${period}&tenantId=${this.tenantId}`);
  }

  async getSupplierPerformance(supplierId, period = 30) {
    return await this.request(`/analytics/suppliers/${supplierId}?period=${period}`);
  }

  async getExpiryAnalytics(period = 90) {
    return await this.request(`/analytics/expiry?period=${period}&tenantId=${this.tenantId}`);
  }

  async getStockMovementAnalytics(period = 30) {
    return await this.request(`/analytics/stock-movement?period=${period}&tenantId=${this.tenantId}`);
  }

  async getDemandForecast(productId, period = 30) {
    return await this.request(`/analytics/demand/${productId}?period=${period}`);
  }

  async getInventoryTurnover(period = 90) {
    return await this.request(`/analytics/turnover?period=${period}&tenantId=${this.tenantId}`);
  }

  // Enhanced Audit Trail APIs
  async getComplianceReport(filters = {}) {
    const params = new URLSearchParams({
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      reportType: filters.reportType || 'full',
      tenantId: this.tenantId
    });
    return await this.request(`/audit/compliance?${params}`);
  }

  async getActivityLog(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      userId: filters.userId || '',
      action: filters.action || '',
      startDate: filters.startDate || '',
      endDate: filters.endDate || '',
      tenantId: this.tenantId
    });
    return await this.request(`/audit/activity?${params}`);
  }

  async getChangeHistory(entityType, entityId) {
    return await this.request(`/audit/history/${entityType}/${entityId}?tenantId=${this.tenantId}`);
  }

  // Supplier Management APIs
  async getSupplierDetails(supplierId) {
    return await this.request(`/suppliers/${supplierId}`);
  }

  async updateSupplier(supplierId, supplierData) {
    return await this.request(`/suppliers/${supplierId}`, {
      method: 'PUT',
      body: JSON.stringify(supplierData)
    });
  }

  async getSupplierOrders(supplierId, filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      status: filters.status || '',
      startDate: filters.startDate || '',
      endDate: filters.endDate || ''
    });
    return await this.request(`/suppliers/${supplierId}/orders?${params}`);
  }

  async getSupplierPerformanceMetrics(supplierId, period = 30) {
    return await this.request(`/suppliers/${supplierId}/performance?period=${period}`);
  }

  // Real-time Updates APIs
  async subscribeToInventoryUpdates(callback) {
    if (this.websocket) {
      this.websocket.addEventListener('message', callback);
    }
  }

  async unsubscribeFromInventoryUpdates(callback) {
    if (this.websocket) {
      this.websocket.removeEventListener('message', callback);
    }
  }

  // Mobile Optimization APIs
  async getMobileOptimizedInventory(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 20,
      search: filters.search || '',
      category: filters.category || '',
      lowStock: filters.lowStock || false,
      outOfStock: filters.outOfStock || false,
      mobile: true,
      tenantId: this.tenantId,
      branchId: this.branchId
    });
    return await this.request(`/inventory/mobile?${params}`);
  }

  async getMobileDashboardData() {
    return await this.request(`/dashboard/mobile?tenantId=${this.tenantId}&branchId=${this.branchId}`);
  }

  async createRefund(refundData) {
    return await this.request('/refunds', {
      method: 'POST',
      body: JSON.stringify(refundData)
    });
  }

  async getRefunds(filters = {}) {
    const params = new URLSearchParams({
      page: filters.page || 1,
      pageSize: filters.pageSize || 50,
      saleId: filters.saleId || '',
      startDate: filters.startDate || '',
      endDate: filters.endDate || ''
    });
    return await this.request(`/refunds?${params}`);
  }

  async getRefundDetails(refundId) {
    return await this.request(`/refunds/${refundId}`);
  }

  // Utility Methods
  formatCurrency(amount) {
    return new Intl.NumberFormat('en-ZM', {
      style: 'currency',
      currency: 'ZMW'
    }).format(amount).replace('ZMW', 'K');
  }

  formatDate(date) {
    return new Date(date).toLocaleDateString('en-ZM');
  }

  formatDateTime(date) {
    return new Date(date).toLocaleString('en-ZM');
  }

  // Error handling
  handleError(error, context = '') {
    console.error(`Error in ${context}:`, error);
    
    // Show user-friendly error message
    const errorMessage = error.message || 'An unexpected error occurred';
    
    // You could integrate with a notification system here
    if (window.paymentSystem && window.paymentSystem.showNotification) {
      window.paymentSystem.showNotification(errorMessage, 'error');
    } else {
      alert(errorMessage);
    }
  }
}

// Initialize global cashier API instance
window.cashierAPI = new CashierAPI();
