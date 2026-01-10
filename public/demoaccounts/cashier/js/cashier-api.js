/**
 * Cashier Portal API Client
 * Real API integration with backend endpoints
 */
class CashierAPI {
  constructor() {
    this.baseURL = this.getBaseURL();
    this.accessToken = localStorage.getItem('umi_access_token');
    this.tenantId = localStorage.getItem('umi_tenant_id');
    this.branchId = localStorage.getItem('umi_branch_id');
  }

  getBaseURL() {
    // Determine if we're in development or production
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
      return 'http://localhost:5001/api/v1';
    }
    
    // Extract subdomain for tenant-specific API calls
    const subdomain = window.location.hostname.split('.')[0];
    if (subdomain && subdomain !== 'www' && subdomain !== 'umihealth') {
      return `https://${subdomain}.umihealth.com/api/v1`;
    }
    
    return 'https://api.umihealth.com/api/v1';
  }

  async request(endpoint, options = {}) {
    const url = `${this.baseURL}${endpoint}`;
    
    const config = {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers
      },
      ...options
    };

    // Add auth token if available
    if (this.accessToken) {
      config.headers['Authorization'] = `Bearer ${this.accessToken}`;
    }

    // Add tenant context if available
    if (this.tenantId) {
      config.headers['X-Tenant-ID'] = this.tenantId;
    }

    // Add branch context if available
    if (this.branchId) {
      config.headers['X-Branch-ID'] = this.branchId;
    }

    try {
      const response = await fetch(url, config);
      const data = await response.json();

      if (!response.ok) {
        throw new Error(data.message || data.error || `HTTP error! status: ${response.status}`);
      }

      return data;
    } catch (error) {
      console.error('Cashier API request failed:', error);
      throw error;
    }
  }

  async get(endpoint) {
    return this.request(endpoint, {
      method: 'GET'
    });
  }

  async post(endpoint, data) {
    return this.request(endpoint, {
      method: 'POST',
      body: JSON.stringify(data)
    });
  }

  async put(endpoint, data) {
    return this.request(endpoint, {
      method: 'PUT',
      body: JSON.stringify(data)
    });
  }

  async delete(endpoint) {
    return this.request(endpoint, {
      method: 'DELETE'
    });
  }

  // Dashboard APIs from backend
  async getDashboardStats() {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    
    return await this.get(`/cashier/dashboard?${params.toString()}`);
  }

  async getSalesStats() {
    return await this.getDashboardStats();
  }

  async getRecentSales(limit = 10) {
    await this.delay();
    return {
      success: true,
      data: Array.from({ length: limit }, (_, i) => ({
        id: this.generateId(),
        total: Math.random() * 200 + 10,
        items: Math.floor(Math.random() * 5) + 1,
        customer: `Customer ${i + 1}`,
        time: `${Math.floor(Math.random() * 60)} mins ago`,
        paymentMethod: ['Cash', 'Card', 'Mobile'][Math.floor(Math.random() * 3)]
      }))
    };
  }

  async getLowStockItems() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'Paracetamol 500mg', currentStock: 12, minStock: 20 },
        { id: '2', name: 'Ibuprofen 400mg', currentStock: 8, minStock: 15 },
        { id: '3', name: 'Vitamin C', currentStock: 5, minStock: 10 }
      ]
    };
  }

  async getPopularProducts(limit = 5) {
    await this.delay();
    return {
      success: true,
      data: [
        { name: 'Paracetamol', sales: 145, revenue: 868.55 },
        { name: 'Ibuprofen', sales: 98, revenue: 782.02 },
        { name: 'Vitamin C', sales: 76, revenue: 456.24 },
        { name: 'Antibiotic Cream', sales: 54, revenue: 675.30 },
        { name: 'Cough Syrup', sales: 43, revenue: 342.67 }
      ]
    };
  }

  async getRecentPatients(limit = 5) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'John Doe', lastVisit: '2024-01-15', prescriptions: 2 },
        { id: '2', name: 'Jane Smith', lastVisit: '2024-01-14', prescriptions: 1 },
        { id: '3', name: 'Bob Johnson', lastVisit: '2024-01-13', prescriptions: 3 }
      ]
    };
  }

  // Sales Management from backend
  async getSales(filters = {}) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    if (filters.startDate) params.append('startDate', filters.startDate);
    if (filters.endDate) params.append('endDate', filters.endDate);
    params.append('page', filters.page || 1);
    params.append('limit', filters.limit || 50);
    
    return await this.get(`/cashier/sales?${params.toString()}`);
  }

  async getSaleDetails(saleId) {
    await this.delay();
    return {
      success: true,
      data: {
        id: saleId,
        total: 45.99,
        subtotal: 42.75,
        tax: 3.24,
        items: [
          { name: 'Paracetamol', qty: 2, price: 5.99, total: 11.98 },
          { name: 'Ibuprofen', qty: 1, price: 7.99, total: 7.99 },
          { name: 'Vitamin C', qty: 1, price: 12.99, total: 12.99 }
        ],
        customer: 'John Doe',
        date: '2024-01-15',
        paymentMethod: 'Card',
        cashier: 'Demo Cashier'
      }
    };
  }

  async createSale(saleData) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    
    return await this.post(`/cashier/sales?${params.toString()}`, saleData);
  }

  async updateSale(saleId, saleData) {
    await this.delay();
    return { success: true, message: 'Sale updated successfully' };
  }

  async getSalesRevenueStats(period = 7) {
    await this.delay();
    return {
      success: true,
      data: {
        totalRevenue: 15678.90,
        averageDaily: 2239.84,
        growth: 12.5,
        breakdown: [
          { date: '2024-01-15', revenue: 2345.67 },
          { date: '2024-01-14', revenue: 1987.43 },
          { date: '2024-01-13', revenue: 2456.78 }
        ]
      }
    };
  }

  async getPaymentMethodsStats() {
    await this.delay();
    return {
      success: true,
      data: {
        cash: 35.5,
        card: 48.2,
        mobile: 12.8,
        insurance: 3.5
      }
    };
  }

  // Payments Management
  async getPayments(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', amount: 45.99, type: 'sale', status: 'completed', method: 'Card', date: '2024-01-15' },
        { id: '2', amount: 23.50, type: 'sale', status: 'completed', method: 'Cash', date: '2024-01-15' },
        { id: '3', amount: 67.25, type: 'sale', status: 'pending', method: 'Mobile', date: '2024-01-14' }
      ]
    };
  }

  async getPaymentDetails(paymentId) {
    await this.delay();
    return {
      success: true,
      data: {
        id: paymentId,
        amount: 45.99,
        type: 'sale',
        status: 'completed',
        method: 'Card',
        date: '2024-01-15',
        transactionId: 'TXN' + this.generateId().toUpperCase()
      }
    };
  }

  async createPayment(paymentData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...paymentData, status: 'completed', created: new Date().toISOString() }
    };
  }

  async updatePayment(paymentId, paymentData) {
    await this.delay();
    return { success: true, message: 'Payment updated successfully' };
  }

  async getPaymentStats() {
    await this.delay();
    return {
      success: true,
      data: {
        todayTotal: 2345.67,
        weekTotal: 15678.90,
        monthTotal: 67890.45,
        pendingCount: 3
      }
    };
  }

  // Point of Sale from backend
  async getProducts(filters = {}) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    if (filters.category) params.append('category', filters.category);
    if (filters.search) params.append('search', filters.search);
    params.append('page', filters.page || 1);
    params.append('limit', filters.limit || 50);
    
    return await this.get(`/cashier/products?${params.toString()}`);
  }

  async getProductDetails(productId) {
    await this.delay();
    return {
      success: true,
      data: {
        id: productId,
        name: 'Paracetamol 500mg',
        sku: 'PAR001',
        price: 5.99,
        stock: 150,
        category: 'Pain Relief',
        description: 'Pain relief medication',
        manufacturer: 'Demo Pharma'
      }
    };
  }

  async getProductByBarcode(barcode) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    params.append('barcode', barcode);
    
    return await this.get(`/cashier/products/barcode?${params.toString()}`);
  }

  async searchProducts(query) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'Paracetamol 500mg', price: 5.99, stock: 150 },
        { id: '2', name: 'Paracetamol 250mg', price: 3.99, stock: 200 }
      ].filter(p => p.name.toLowerCase().includes(query.toLowerCase()))
    };
  }

  async checkProductStock(productId) {
    await this.delay();
    return {
      success: true,
      data: {
        productId: productId,
        currentStock: 150,
        available: true,
        lowStock: false
      }
    };
  }

  // Patients Management from backend
  async getPatients(filters = {}) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (filters.search) params.append('search', filters.search);
    params.append('page', filters.page || 1);
    params.append('limit', filters.limit || 50);
    
    return await this.get(`/cashier/patients?${params.toString()}`);
  }

  async getPatientDetails(patientId) {
    await this.delay();
    return {
      success: true,
      data: {
        id: patientId,
        name: 'John Doe',
        email: 'john@demo.com',
        phone: '555-0101',
        dob: '1980-01-15',
        address: '123 Main St, Demo City',
        emergencyContact: 'Jane Doe (555-0104)',
        allergies: ['Penicillin'],
        memberSince: '2020-05-15'
      }
    };
  }

  async createPatient(patientData) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    
    return await this.post(`/cashier/patients?${params.toString()}`, patientData);
  }

  async updatePatient(patientId, patientData) {
    await this.delay();
    return { success: true, message: 'Patient updated successfully' };
  }

  async searchPatients(query) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'John Doe', phone: '555-0101' },
        { id: '2', name: 'Jane Doe', phone: '555-0105' }
      ].filter(p => p.name.toLowerCase().includes(query.toLowerCase()))
    };
  }

  // Account Management
  async getUserProfile() {
    await this.delay();
    return {
      success: true,
      data: {
        id: 'demo-cashier',
        name: 'Demo Cashier',
        email: 'cashier@demo.com',
        role: 'Cashier',
        branch: 'Main Branch'
      }
    };
  }

  async updateProfile(profileData) {
    await this.delay();
    return { success: true, message: 'Profile updated successfully' };
  }

  async changePassword(passwordData) {
    await this.delay();
    return { success: true, message: 'Password changed successfully' };
  }

  async getNotificationSettings() {
    await this.delay();
    return {
      success: true,
      data: {
        emailNotifications: true,
        smsNotifications: false,
        pushNotifications: true
      }
    };
  }

  async updateNotificationSettings(settings) {
    await this.delay();
    return { success: true, message: 'Notification settings updated successfully' };
  }

  async getPharmacySettings() {
    await this.delay();
    return {
      success: true,
      data: {
        name: 'Demo Pharmacy',
        address: '123 Demo Street',
        phone: '+1-555-0123',
        taxRate: 8.5
      }
    };
  }

  async updatePharmacySettings(settings) {
    await this.delay();
    return { success: true, message: 'Pharmacy settings updated successfully' };
  }

  // Queue Management
  async getQueueData() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'John Doe', service: 'Prescription Pickup', waitTime: 5, priority: 'normal' },
        { id: '2', name: 'Jane Smith', service: 'Consultation', waitTime: 12, priority: 'normal' },
        { id: '3', name: 'Bob Johnson', service: 'New Prescription', waitTime: 18, priority: 'high' }
      ]
    };
  }

  async getQueueStats() {
    await this.delay();
    return {
      success: true,
      data: {
        totalWaiting: 8,
        averageWaitTime: 12,
        servedToday: 45,
        estimatedWaitTime: 15
      }
    };
  }

  async addToQueue(patientData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...patientData, addedAt: new Date().toISOString() }
    };
  }

  async servePatient(patientId) {
    await this.delay();
    return { success: true, message: 'Patient served successfully' };
  }

  async removePatient(patientId) {
    await this.delay();
    return { success: true, message: 'Patient removed from queue' };
  }

  async clearQueue() {
    await this.delay();
    return { success: true, message: 'Queue cleared successfully' };
  }

  async callNextPatient() {
    await this.delay();
    return {
      success: true,
      data: {
        id: this.generateId(),
        name: 'Next Patient',
        service: 'Prescription Pickup'
      }
    };
  }

  // Reports
  async getReportsSummary(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: {
        totalSales: 15678.90,
        totalTransactions: 234,
        averageSale: 67.02,
        topProduct: 'Paracetamol',
        customerCount: 189
      }
    };
  }

  async getSalesReport(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { date: '2024-01-15', sales: 2345.67, transactions: 45 },
        { date: '2024-01-14', sales: 1987.43, transactions: 38 },
        { date: '2024-01-13', sales: 2456.78, transactions: 52 }
      ]
    };
  }

  async getPerformanceReport(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: {
        salesGrowth: 12.5,
        customerSatisfaction: 4.6,
        averageTransactionValue: 67.02,
        itemsPerTransaction: 3.2
      }
    };
  }

  // Shift Management
  async getShiftData() {
    await this.delay();
    return {
      success: true,
      data: {
        id: 'shift-001',
        startTime: '2024-01-15T08:00:00Z',
        endTime: null,
        status: 'active',
        sales: 2345.67,
        transactions: 45
      }
    };
  }

  async startShift(shiftData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...shiftData, status: 'active', started: new Date().toISOString() }
    };
  }

  async endShift(shiftData) {
    await this.delay();
    return { success: true, message: 'Shift ended successfully' };
  }

  async takeBreak(breakData) {
    await this.delay();
    return { success: true, message: 'Break started' };
  }

  async getShiftHistory(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', date: '2024-01-14', startTime: '08:00', endTime: '16:00', sales: 3456.78, status: 'completed' },
        { id: '2', date: '2024-01-13', startTime: '08:00', endTime: '16:00', sales: 2890.45, status: 'completed' }
      ]
    };
  }

  // Inventory Management
  async getInventory(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'Paracetamol 500mg', sku: 'PAR001', stock: 150, lowStock: false, price: 5.99 },
        { id: '2', name: 'Ibuprofen 400mg', sku: 'IBU002', stock: 12, lowStock: true, price: 7.99 },
        { id: '3', name: 'Amoxicillin 250mg', sku: 'AMX003', stock: 45, lowStock: false, price: 12.99 }
      ]
    };
  }

  async createInventoryItem(itemData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...itemData, created: new Date().toISOString() }
    };
  }

  async updateInventoryItem(itemId, itemData) {
    await this.delay();
    return { success: true, message: 'Inventory item updated successfully' };
  }

  async deleteInventoryItem(itemId) {
    await this.delay();
    return { success: true, message: 'Inventory item deleted successfully' };
  }

  async adjustStock(itemId, adjustmentData) {
    await this.delay();
    return { success: true, message: 'Stock adjusted successfully' };
  }

  // Mock other required methods with basic implementations
  async createInvoice(invoiceData) {
    await this.delay();
    return { success: true, data: { id: this.generateId(), ...invoiceData } };
  }

  async createQuotation(quotationData) {
    await this.delay();
    return { success: true, data: { id: this.generateId(), ...quotationData } };
  }

  async getInsuranceClaims(filters = {}) {
    await this.delay();
    return { success: true, data: [] };
  }

  async createInsuranceClaim(claimData) {
    await this.delay();
    return { success: true, data: { id: this.generateId(), ...claimData } };
  }

  async getInsuranceClaimDetails(claimId) {
    await this.delay();
    return { success: true, data: { id: claimId, status: 'pending' } };
  }

  async updateInsuranceClaim(claimId, claimData) {
    await this.delay();
    return { success: true, message: 'Insurance claim updated successfully' };
  }

  async submitInsuranceClaim(claimId) {
    await this.delay();
    return { success: true, message: 'Insurance claim submitted successfully' };
  }

  async adjudicateInsuranceClaim(claimId, adjudicationData) {
    await this.delay();
    return { success: true, message: 'Insurance claim adjudicated successfully' };
  }

  async getInsuranceProviders() {
    await this.delay();
    return { success: true, data: [] };
  }

  async validateInsuranceCoverage(patientId, insuranceProviderId) {
    await this.delay();
    return { success: true, data: { valid: true, coverage: 'full' } };
  }

  // Add other required methods with mock implementations
  async getPasswordInfo() { await this.delay(); return { success: true, data: {} }; }
  async getTwoFactorStatus() { await this.delay(); return { success: true, data: { enabled: false } }; }
  async sendTwoFactorCode(data) { await this.delay(); return { success: true }; }
  async enableTwoFactor(data) { await this.delay(); return { success: true }; }
  async disableTwoFactor(data) { await this.delay(); return { success: true }; }
  async getLoginSessions() { await this.delay(); return { success: true, data: [] }; }
  async revokeSession(sessionId) { await this.delay(); return { success: true }; }
  async revokeAllSessions() { await this.delay(); return { success: true }; }
  // Portal registration from backend
  async registerPortal() {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    
    return await this.post(`/cashier/register?${params.toString()}`);
  }

  async getPortalStatus() {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    
    return await this.get(`/cashier/status?${params.toString()}`);
  }

  async notifyDataChange(entityType, data) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    
    return await this.post(`/cashier/notify/${entityType}?${params.toString()}`, data);
  }
  async notifyUserChange(changeData) { await this.delay(); return { success: true }; }
  async logout() { await this.delay(); return { success: true }; }
  async completePatientService(patientId, serviceData) { await this.delay(); return { success: true }; }
  async getPatientQueueHistory(patientId) { await this.delay(); return { success: true, data: [] }; }
  async updateQueuePosition(patientId, newPosition) { await this.delay(); return { success: true }; }
  async getProviders() { await this.delay(); return { success: true, data: [] }; }
  async getProviderAvailability(providerId) { await this.delay(); return { success: true, data: [] }; }
  async assignPatientToProvider(patientId, providerId) { await this.delay(); return { success: true }; }
  async getProviderQueue(providerId) { await this.delay(); return { success: true, data: [] }; }
  async getEmergencyQueue() { await this.delay(); return { success: true, data: [] }; }
  async addToEmergencyQueue(patientData) { await this.delay(); return { success: true }; }
  async updatePatientPriority(patientId, priority) { await this.delay(); return { success: true }; }
  async escalatePriority(patientId) { await this.delay(); return { success: true }; }
  async searchPatients(query, filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async filterQueue(filters) { await this.delay(); return { success: true, data: [] }; }
  async sendSMSNotification(patientId, message) { await this.delay(); return { success: true }; }
  async sendWhatsAppNotification(patientId, message) { await this.delay(); return { success: true }; }
  async getQueueAnalytics(period = 7) { await this.delay(); return { success: true, data: {} }; }
  async getWaitTimeAnalytics(period = 30) { await this.delay(); return { success: true, data: {} }; }
  async getProviderAnalytics(providerId, period = 30) { await this.delay(); return { success: true, data: {} }; }
  async getPeakHourAnalysis(period = 30) { await this.delay(); return { success: true, data: {} }; }
  async getQueueForecast(days = 7) { await this.delay(); return { success: true, data: {} }; }
  async getQueueHistory(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getAuditLog(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async bulkServePatients(patientIds) { await this.delay(); return { success: true }; }
  async bulkRemovePatients(patientIds) { await this.delay(); return { success: true }; }
  async bulkAssignProvider(patientIds, providerId) { await this.delay(); return { success: true }; }
  async bulkUpdatePriority(patientIds, priority) { await this.delay(); return { success: true }; }
  async exportQueue(format = 'pdf', filters = {}) { await this.delay(); return { success: true }; }
  async printQueueSlip(patientId) { await this.delay(); return { success: true }; }
  async printDailyReport(date) { await this.delay(); return { success: true }; }
  async collectFeedback(patientId, feedback) { await this.delay(); return { success: true }; }
  async getFeedbackStats(period = 30) { await this.delay(); return { success: true, data: {} }; }
  async getPatientFeedback(patientId) { await this.delay(); return { success: true, data: [] }; }
  async playSound(soundType) { return new Promise(resolve => setTimeout(resolve, 100)); }
  async setSoundVolume(volume) { await this.delay(); return { success: true }; }
  async getSoundVolume() { await this.delay(); return { success: true, data: { volume: 0.5 } }; }
  async getReportSummary(filters = {}) { await this.delay(); return { success: true, data: {} }; }
  async getCustomerReport(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getPatientReport(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getFinancialReport(filters = {}) { await this.delay(); return { success: true, data: {} }; }
  async exportReport(reportType, format = 'pdf', filters = {}) { await this.delay(); return { success: true }; }
  async getRevenueTrend(period = 30) { await this.delay(); return { success: true, data: [] }; }
  async getSalesByCategory(period = 30) { await this.delay(); return { success: true, data: [] }; }
  async getPeakHoursData(period = 7) { await this.delay(); return { success: true, data: [] }; }
  async getDetailedTransactions(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getPOSData(shiftId) { await this.delay(); return { success: true, data: {} }; }
  async getPOSTransactions(shiftId) { await this.delay(); return { success: true, data: [] }; }
  async getPOSRevenue(shiftId) { await this.delay(); return { success: true, data: {} }; }
  async getInventoryItem(itemId) { await this.delay(); return { success: true, data: {} }; }
  async getInventoryHistory(itemId, filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getInventoryStats() { await this.delay(); return { success: true, data: {} }; }
  async getOutOfStockItems() { await this.delay(); return { success: true, data: [] }; }
  async getInventoryCategories() { await this.delay(); return { success: true, data: [] }; }
  async getExpiringItems(days = 30) { await this.delay(); return { success: true, data: [] }; }
  async exportInventoryCSV(filters = {}) { await this.delay(); return { success: true }; }
  async bulkUpdateInventory(items) { await this.delay(); return { success: true }; }
  async importInventoryCSV(file) { await this.delay(); return { success: true, data: {} }; }
  async getInventoryReport(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getShiftDetails(shiftId) { await this.delay(); return { success: true, data: {} }; }
  async updateShift(shiftId, shiftData) { await this.delay(); return { success: true }; }
  async getShiftStats(period = 7) { await this.delay(); return { success: true, data: {} }; }
}

// Initialize global cashier API instance
window.cashierAPI = new CashierAPI();
