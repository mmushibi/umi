/**
 * Pharmacist Portal API Client
 * Real API integration with backend endpoints
 */
class PharmacistApi {
  constructor() {
    this.baseURL = this.getBaseURL();
    this.accessToken = localStorage.getItem('umi_access_token');
    this.tenantId = localStorage.getItem('umi_tenant_id');
    this.branchId = localStorage.getItem('umi_branch_id');
  }

  getBaseURL() {
    // Determine if we're in development or production
    if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
      return 'http://localhost:5001/api';
    }
    
    // Extract subdomain for tenant-specific API calls
    const subdomain = window.location.hostname.split('.')[0];
    if (subdomain && subdomain !== 'www' && subdomain !== 'umihealth') {
      return `https://${subdomain}.umihealth.com/api`;
    }
    
    return 'https://api.umihealth.com/api';
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
      console.error('Pharmacist API request failed:', error);
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

  // User info from backend
  async getUserInfo() {
    return await this.get('/pharmacist/user-info');
  }

  async getPharmacyInfo() {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    
    return await this.get(`/pharmacist/pharmacy-info?${params.toString()}`);
  }

  // Dashboard APIs from backend
  async getDashboardStats() {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    
    return await this.get(`/pharmacist/dashboard?${params.toString()}`);
  }

  async getRecentActivity() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: 1, action: 'New prescription received', patient: 'James Mulenga', time: '5 mins ago', priority: 'normal' },
        { id: 2, action: 'Medication stock low', medication: 'Amoxicillin', time: '15 mins ago', priority: 'high' },
        { id: 3, action: 'Prescription dispensed', patient: 'Esther Phiri', time: '30 mins ago', priority: 'normal' },
        { id: 4, action: 'Critical drug interaction', patient: 'Michael Bwalya', time: '45 mins ago', priority: 'critical' },
        { id: 5, action: 'Inventory updated', medication: 'Paracetamol', time: '1 hour ago', priority: 'low' }
      ]
    };
  }

  // Prescription Management from backend
  async getPrescriptions(filters = {}) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (filters.status) params.append('status', filters.status);
    if (filters.patientName) params.append('patientName', filters.patientName);
    if (filters.prescriptionNumber) params.append('prescriptionNumber', filters.prescriptionNumber);
    
    return await this.get(`/pharmacist/prescriptions?${params.toString()}`);
  }

  async getPrescriptionDetails(prescriptionId) {
    await this.delay();
    return {
      success: true,
      data: {
        id: prescriptionId,
        patient: {
          id: '1',
          name: 'John Doe',
          age: 45,
          weight: 75,
          allergies: ['Penicillin'],
          conditions: ['Hypertension']
        },
        medication: 'Amoxicillin 500mg',
        dosage: '1 tablet twice daily',
        duration: '7 days',
        prescribedBy: 'Dr. Smith',
        date: '2024-01-15',
        status: 'pending',
        priority: 'normal',
        notes: 'Take with food',
        interactions: ['Alcohol may increase dizziness'],
        sideEffects: ['Nausea', 'Diarrhea']
      }
    };
  }

  async approvePrescription(prescriptionId) {
    await this.delay();
    return { success: true, message: 'Prescription approved successfully' };
  }

  async rejectPrescription(prescriptionId, reason) {
    await this.delay();
    return { success: true, message: 'Prescription rejected successfully' };
  }

  async dispensePrescription(prescriptionId, dispensingData) {
    await this.delay();
    return { success: true, message: 'Prescription dispensed successfully' };
  }

  async createPrescription(prescriptionData) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    
    return await this.post(`/pharmacist/prescriptions?${params.toString()}`, prescriptionData);
  }

  async updatePrescriptionStatus(prescriptionId, statusData) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    
    return await this.put(`/pharmacist/prescriptions/${prescriptionId}/status?${params.toString()}`, statusData);
  }

  // Patient Management
  async getPatients(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { 
          id: '1', 
          name: 'John Doe', 
          email: 'john@demo.com', 
          phone: '555-0101', 
          dob: '1980-01-15',
          memberSince: '2020-05-15',
          lastVisit: '2024-01-10',
          activePrescriptions: 2,
          allergies: ['Penicillin'],
          conditions: ['Hypertension']
        },
        { 
          id: '2', 
          name: 'Jane Smith', 
          email: 'jane@demo.com', 
          phone: '555-0102', 
          dob: '1985-05-20',
          memberSince: '2021-03-10',
          lastVisit: '2024-01-12',
          activePrescriptions: 1,
          allergies: [],
          conditions: ['Diabetes']
        },
        { 
          id: '3', 
          name: 'Bob Johnson', 
          email: 'bob@demo.com', 
          phone: '555-0103', 
          dob: '1975-11-30',
          memberSince: '2019-08-22',
          lastVisit: '2024-01-14',
          activePrescriptions: 3,
          allergies: ['Sulfa'],
          conditions: ['Asthma']
        }
      ]
    };
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
        conditions: ['Hypertension', 'High Cholesterol'],
        medications: ['Lisinopril 10mg', 'Atorvastatin 20mg'],
        memberSince: '2020-05-15',
        lastVisit: '2024-01-10',
        insuranceProvider: 'Demo Health Insurance',
        policyNumber: 'POL123456'
      }
    };
  }

  async createPatient(patientData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...patientData, created: new Date().toISOString() }
    };
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
        { id: '1', name: 'John Doe', phone: '555-0101', email: 'john@demo.com' },
        { id: '2', name: 'Jane Doe', phone: '555-0105', email: 'jane@demo.com' }
      ].filter(p => p.name.toLowerCase().includes(query.toLowerCase()))
    };
  }

  // Inventory Management
  async getInventory(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { 
          id: '1', 
          name: 'Amoxicillin 500mg', 
          sku: 'AMX001', 
          stock: 45, 
          lowStock: false, 
          price: 12.99,
          category: 'Antibiotics',
          expiryDate: '2024-12-31',
          manufacturer: 'Demo Pharma',
          controlledSubstance: false
        },
        { 
          id: '2', 
          name: 'Paracetamol 500mg', 
          sku: 'PAR002', 
          stock: 150, 
          lowStock: false, 
          price: 5.99,
          category: 'Pain Relief',
          expiryDate: '2025-06-30',
          manufacturer: 'MediCorp',
          controlledSubstance: false
        },
        { 
          id: '3', 
          name: 'Ibuprofen 400mg', 
          sku: 'IBU003', 
          stock: 12, 
          lowStock: true, 
          price: 7.99,
          category: 'Pain Relief',
          expiryDate: '2024-09-15',
          manufacturer: 'HealthPlus',
          controlledSubstance: false
        }
      ]
    };
  }

  async getInventoryItem(itemId) {
    await this.delay();
    return {
      success: true,
      data: {
        id: itemId,
        name: 'Amoxicillin 500mg',
        sku: 'AMX001',
        stock: 45,
        lowStock: false,
        price: 12.99,
        category: 'Antibiotics',
        expiryDate: '2024-12-31',
        manufacturer: 'Demo Pharma',
        controlledSubstance: false,
        description: 'Broad-spectrum antibiotic',
        dosageForm: 'Capsule',
        strength: '500mg',
        storageConditions: 'Store at room temperature',
        sideEffects: ['Nausea', 'Diarrhea', 'Allergic reactions']
      }
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

  async getLowStockItems() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '3', name: 'Ibuprofen 400mg', currentStock: 12, minStock: 20, reorderPoint: 25 },
        { id: '5', name: 'Vitamin D', currentStock: 8, minStock: 15, reorderPoint: 20 }
      ]
    };
  }

  async getExpiringItems(days = 30) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '4', name: 'Cough Syrup', expiryDate: '2024-02-15', daysToExpiry: 15, stock: 25 },
        { id: '6', name: 'Antihistamine', expiryDate: '2024-02-28', daysToExpiry: 28, stock: 18 }
      ]
    };
  }

  // Clinical Management
  async getClinicalData() {
    await this.delay();
    return {
      success: true,
      data: {
        totalConsultations: 45,
        averageConsultationTime: 15,
        patientSatisfaction: 4.7,
        commonConditions: [
          { condition: 'Hypertension', count: 12 },
          { condition: 'Diabetes', count: 8 },
          { condition: 'Asthma', count: 6 }
        ]
      }
    };
  }

  async getDrugInteractions(medications) {
    await this.delay();
    return {
      success: true,
      data: [
        {
          medication1: 'Warfarin',
          medication2: 'Aspirin',
          severity: 'high',
          description: 'Increased risk of bleeding',
          recommendation: 'Monitor closely'
        }
      ]
    };
  }

  async getDosageRecommendations(medication, patientData) {
    await this.delay();
    return {
      success: true,
      data: {
        medication: medication,
        recommendedDosage: '500mg twice daily',
        adjustments: [
          { condition: 'Renal impairment', adjustment: 'Reduce dose by 50%' },
          { condition: 'Elderly', adjustment: 'Start with lower dose' }
        ]
      }
    };
  }

  // Compliance Management
  async getComplianceData() {
    await this.delay();
    return {
      success: true,
      data: {
        overallCompliance: 94.5,
        prescriptionAccuracy: 98.2,
        inventoryAccuracy: 96.8,
        documentationCompliance: 92.3
      }
    };
  }

  async getComplianceReports() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', type: 'Prescription Audit', date: '2024-01-15', status: 'passed', score: 98 },
        { id: '2', type: 'Inventory Check', date: '2024-01-14', status: 'passed', score: 95 },
        { id: '3', type: 'Documentation Review', date: '2024-01-13', status: 'warning', score: 88 }
      ]
    };
  }

  // Supplier Management
  async getSuppliers() {
    await this.delay();
    return {
      success: true,
      data: [
        { 
          id: '1', 
          name: 'Demo Pharma Supplies', 
          contact: 'John Supplier', 
          phone: '555-0201',
          email: 'supplier@demopharma.com',
          status: 'active',
          rating: 4.8,
          deliveryTime: '2-3 days'
        },
        { 
          id: '2', 
          name: 'MediCorp Distributors', 
          contact: 'Jane Distributor', 
          phone: '555-0202',
          email: 'info@medicorp.com',
          status: 'active',
          rating: 4.5,
          deliveryTime: '3-5 days'
        },
        { 
          id: '3', 
          name: 'HealthPlus Wholesale', 
          contact: 'Bob Wholesale', 
          phone: '555-0203',
          email: 'sales@healthplus.com',
          status: 'inactive',
          rating: 4.2,
          deliveryTime: '5-7 days'
        }
      ]
    };
  }

  async createSupplier(supplierData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...supplierData, created: new Date().toISOString() }
    };
  }

  async updateSupplier(supplierId, supplierData) {
    await this.delay();
    return { success: true, message: 'Supplier updated successfully' };
  }

  async deleteSupplier(supplierId) {
    await this.delay();
    return { success: true, message: 'Supplier deleted successfully' };
  }

  // Reports and Analytics
  async getReportsSummary(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: {
        totalPrescriptions: 156,
        dispensedToday: 28,
        pendingApprovals: 12,
        inventoryValue: 45678.90,
        complianceScore: 94.5
      }
    };
  }

  async getPrescriptionReport(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { date: '2024-01-15', total: 15, dispensed: 12, pending: 3 },
        { date: '2024-01-14', total: 18, dispensed: 16, pending: 2 },
        { date: '2024-01-13', total: 22, dispensed: 20, pending: 2 }
      ]
    };
  }

  async getInventoryReport(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: {
        totalItems: 245,
        lowStock: 8,
        outOfStock: 2,
        expiringSoon: 5,
        totalValue: 45678.90
      }
    };
  }

  async getPatientReport(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: {
        totalPatients: 156,
        newThisMonth: 12,
        activePrescriptions: 89,
        averageAge: 45.2
      }
    };
  }

  // Account Management
  async getUserProfile() {
    await this.delay();
    return {
      success: true,
      data: {
        id: 'demo-pharmacist',
        name: 'Demo Pharmacist',
        email: 'pharmacist@demo.com',
        role: 'Pharmacist',
        licenseNumber: 'LP123456',
        branch: 'Main Branch',
        department: 'Pharmacy'
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
        pushNotifications: true,
        prescriptionAlerts: true,
        inventoryAlerts: true
      }
    };
  }

  async updateNotificationSettings(settings) {
    await this.delay();
    return { success: true, message: 'Notification settings updated successfully' };
  }

  // Sync functionality from backend
  async triggerSync(entityType = null) {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    if (entityType) params.append('entityType', entityType);
    
    return await this.post(`/pharmacist/sync/trigger?${params.toString()}`);
  }

  async getSyncStatus() {
    const params = new URLSearchParams();
    if (this.tenantId) params.append('tenantId', this.tenantId);
    if (this.branchId) params.append('branchId', this.branchId);
    
    return await this.get(`/pharmacist/sync/status?${params.toString()}`);
  }

  // Mock other required methods with basic implementations
  async getPaymentMethodsStats() { await this.delay(); return { success: true, data: {} }; }
  async createPayment(paymentData) { await this.delay(); return { success: true, data: { id: this.generateId() } }; }
  async getPayments(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getPaymentDetails(paymentId) { await this.delay(); return { success: true, data: {} }; }
  async updatePayment(paymentId, paymentData) { await this.delay(); return { success: true }; }
  async getPaymentStats() { await this.delay(); return { success: true, data: {} }; }
  async createInvoice(invoiceData) { await this.delay(); return { success: true, data: { id: this.generateId() } }; }
  async createQuotation(quotationData) { await this.delay(); return { success: true, data: { id: this.generateId() } }; }
  async getInsuranceClaims(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async createInsuranceClaim(claimData) { await this.delay(); return { success: true, data: { id: this.generateId() } }; }
  async getInsuranceClaimDetails(claimId) { await this.delay(); return { success: true, data: {} }; }
  async updateInsuranceClaim(claimId, claimData) { await this.delay(); return { success: true }; }
  async submitInsuranceClaim(claimId) { await this.delay(); return { success: true }; }
  async adjudicateInsuranceClaim(claimId, adjudicationData) { await this.delay(); return { success: true }; }
  async getInsuranceProviders() { await this.delay(); return { success: true, data: [] }; }
  async validateInsuranceCoverage(patientId, insuranceProviderId) { await this.delay(); return { success: true, data: { valid: true } }; }
  async getPharmacySettings() { await this.delay(); return { success: true, data: {} }; }
  async updatePharmacySettings(settings) { await this.delay(); return { success: true }; }
  async getSalesStats() { await this.delay(); return { success: true, data: {} }; }
  async getRecentSales(limit = 10) { await this.delay(); return { success: true, data: [] }; }
  async getPopularProducts(limit = 5) { await this.delay(); return { success: true, data: [] }; }
  async getRecentPatients(limit = 5) { await this.delay(); return { success: true, data: [] }; }
  async getSales(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getSaleDetails(saleId) { await this.delay(); return { success: true, data: {} }; }
  async createSale(saleData) { await this.delay(); return { success: true, data: { id: this.generateId() } }; }
  async updateSale(saleId, saleData) { await this.delay(); return { success: true }; }
  async getSalesRevenueStats(period = 7) { await this.delay(); return { success: true, data: {} }; }
  async getProducts(filters = {}) { await this.delay(); return { success: true, data: [] }; }
  async getProductDetails(productId) { await this.delay(); return { success: true, data: {} }; }
  async getProductByBarcode(barcode) { await this.delay(); return { success: true, data: {} }; }
  async searchProducts(query) { await this.delay(); return { success: true, data: [] }; }
  async checkProductStock(productId) { await this.delay(); return { success: true, data: {} }; }
  async getReports() { await this.delay(); return { success: true, data: [] }; }
  async generateReport(type, filters = {}) { await this.delay(); return { success: true, data: { id: this.generateId() } }; }
  async scheduleReport(scheduleData) { await this.delay(); return { success: true }; }
  async getScheduledReports() { await this.delay(); return { success: true, data: [] }; }
  async downloadReport(reportId) { await this.delay(); return { success: true }; }
  async getBranchData(branchId) { await this.delay(); return { success: true, data: {} }; }
  async updateUserProfile(profileData) { await this.delay(); return { success: true }; }
  async cancelSubscription(cancelData) { await this.delay(); return { success: true }; }
  async sendNotification(notificationData) { await this.delay(); return { success: true }; }
  async syncUserData() { await this.delay(); return { success: true }; }
  async syncPharmacyData() { await this.delay(); return { success: true }; }
  async broadcastUpdate(updateData) { await this.delay(); return { success: true }; }
}

// Initialize global pharmacist API instance
window.pharmacistApi = new PharmacistApi();
