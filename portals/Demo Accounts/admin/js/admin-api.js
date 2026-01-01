/**
 * Admin Portal Demo API Client
 * Mock API that returns sample data for demo purposes
 */
class AdminAPI {
  constructor() {
    // Demo mode - no real API calls
    this.demoMode = true;
  }

  // Simulate API delay
  async delay(ms = 500) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  // Generate random IDs
  generateId() {
    return Math.random().toString(36).substr(2, 9);
  }

  // Mock dashboard data
  async getDashboardStats() {
    await this.delay();
    return {
      success: true,
      data: {
        totalPatients: 1247,
        activePrescriptions: 89,
        totalSales: 45678.90,
        lowStockItems: 12,
        newPatientsToday: 8,
        pendingApprovals: 3,
        monthlyRevenue: 234567.89,
        totalBranches: 4,
        activeUsers: 24
      }
    };
  }

  async getRecentActivity() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: 1, action: 'New patient registered', user: 'Bwalya Mwansa', time: '2 mins ago', type: 'info' },
        { id: 2, action: 'Prescription approved', user: 'Dr. Mutale Chanda', time: '5 mins ago', type: 'success' },
        { id: 3, action: 'Low stock alert', user: 'System', time: '10 mins ago', type: 'warning' },
        { id: 4, action: 'Payment received', user: 'Chipo Banda', time: '15 mins ago', type: 'success' },
        { id: 5, action: 'User login', user: 'Admin', time: '20 mins ago', type: 'info' }
      ]
    };
  }

  async getSystemHealth() {
    await this.delay();
    return {
      success: true,
      data: {
        api: 'healthy',
        database: 'healthy',
        redis: 'healthy',
        uptime: '15 days 4 hours',
        memoryUsage: '67%',
        cpuUsage: '23%'
      }
    };
  }

  // Mock tenant settings
  async getTenantSettings() {
    await this.delay();
    return {
      success: true,
      data: {
        pharmacyName: 'Umi Health Zambia',
        address: 'Stand 2345 Cairo Road, Lusaka',
        phone: '+260 211 234567',
        email: 'info@umizambia.com',
        timezone: 'Africa/Lusaka',
        currency: 'ZMW',
        taxRate: 16.0
      }
    };
  }

  async updateTenantSettings(settings) {
    await this.delay();
    return { success: true, message: 'Settings updated successfully' };
  }

  async getSubscriptionStatus() {
    await this.delay();
    return {
      success: true,
      data: {
        plan: 'Professional',
        status: 'active',
        expires: '2024-12-31',
        features: ['Multi-branch', 'Advanced Reports', 'API Access']
      }
    };
  }

  async upgradeSubscription(planData) {
    await this.delay();
    return { success: true, message: 'Subscription upgraded successfully' };
  }

  // Mock user management
  async getUsers(page = 1, limit = 100, filters = {}) {
    await this.delay();
    const users = Array.from({ length: 10 }, (_, i) => ({
      id: this.generateId(),
      name: `User ${i + 1}`,
      email: `user${i + 1}@demo.com`,
      role: ['Admin', 'Cashier', 'Pharmacist'][i % 3],
      status: ['active', 'inactive'][i % 2],
      created: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString()
    }));

    return {
      success: true,
      data: users,
      pagination: { page, limit, total: 45, pages: 5 }
    };
  }

  async createUser(userData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...userData, created: new Date().toISOString() }
    };
  }

  async updateUser(userId, userData) {
    await this.delay();
    return { success: true, message: 'User updated successfully' };
  }

  async deleteUser(userId) {
    await this.delay();
    return { success: true, message: 'User deleted successfully' };
  }

  async getUserPermissions(userId) {
    await this.delay();
    return {
      success: true,
      data: ['read', 'write', 'delete', 'admin']
    };
  }

  async updateUserPermissions(userId, permissions) {
    await this.delay();
    return { success: true, message: 'Permissions updated successfully' };
  }

  // Mock branch management
  async getBranches() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'Main Branch', address: '123 Main St', phone: '555-0101', status: 'active' },
        { id: '2', name: 'North Branch', address: '456 North Ave', phone: '555-0102', status: 'active' },
        { id: '3', name: 'East Branch', address: '789 East Blvd', phone: '555-0103', status: 'inactive' }
      ]
    };
  }

  async createBranch(branchData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...branchData, created: new Date().toISOString() }
    };
  }

  async updateBranch(branchId, branchData) {
    await this.delay();
    return { success: true, message: 'Branch updated successfully' };
  }

  async deleteBranch(branchId) {
    await this.delay();
    return { success: true, message: 'Branch deleted successfully' };
  }

  // Mock products
  async getProducts() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'Paracetamol', category: 'Pain Relief', price: 5.99, stock: 150 },
        { id: '2', name: 'Ibuprofen', category: 'Pain Relief', price: 7.99, stock: 89 },
        { id: '3', name: 'Amoxicillin', category: 'Antibiotics', price: 12.99, stock: 45 }
      ]
    };
  }

  // Mock inventory
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

  async addProduct(productData) {
    await this.delay();
    return { 
      success: true, 
      data: { id: this.generateId(), ...productData, created: new Date().toISOString() }
    };
  }

  async updateProduct(productId, productData) {
    await this.delay();
    return { success: true, message: 'Product updated successfully' };
  }

  async deleteProduct(productId) {
    await this.delay();
    return { success: true, message: 'Product deleted successfully' };
  }

  async getLowStockItems() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '2', name: 'Ibuprofen 400mg', currentStock: 12, minStock: 20, reorderPoint: 25 },
        { id: '5', name: 'Vitamin D', currentStock: 8, minStock: 15, reorderPoint: 20 }
      ]
    };
  }

  // Mock sales
  async getSales(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', total: 45.99, items: 3, customer: 'John Doe', date: '2024-01-15', status: 'completed' },
        { id: '2', total: 23.50, items: 2, customer: 'Jane Smith', date: '2024-01-15', status: 'completed' },
        { id: '3', total: 67.25, items: 5, customer: 'Bob Johnson', date: '2024-01-14', status: 'pending' }
      ]
    };
  }

  async getSaleDetails(saleId) {
    await this.delay();
    return {
      success: true,
      data: {
        id: saleId,
        total: 45.99,
        items: [
          { name: 'Paracetamol', qty: 2, price: 5.99 },
          { name: 'Ibuprofen', qty: 1, price: 7.99 }
        ],
        customer: 'John Doe',
        date: '2024-01-15'
      }
    };
  }

  async generateSalesReport(reportData) {
    await this.delay();
    return { success: true, message: 'Report generated successfully' };
  }

  // Mock patients
  async getPatients(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'John Doe', email: 'john@demo.com', phone: '555-0101', dob: '1980-01-15' },
        { id: '2', name: 'Jane Smith', email: 'jane@demo.com', phone: '555-0102', dob: '1985-05-20' },
        { id: '3', name: 'Bob Johnson', email: 'bob@demo.com', phone: '555-0103', dob: '1975-11-30' }
      ]
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

  async deletePatient(patientId) {
    await this.delay();
    return { success: true, message: 'Patient deleted successfully' };
  }

  // Mock prescriptions
  async getPrescriptions(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', patient: 'John Doe', medication: 'Amoxicillin', dosage: '250mg', status: 'pending' },
        { id: '2', patient: 'Jane Smith', medication: 'Paracetamol', dosage: '500mg', status: 'approved' },
        { id: '3', patient: 'Bob Johnson', medication: 'Ibuprofen', dosage: '400mg', status: 'dispensed' }
      ]
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

  // Mock system settings
  async getSystemSettings() {
    await this.delay();
    return {
      success: true,
      data: {
        systemName: 'Umi Health Demo',
        version: '1.0.0',
        maintenance: false,
        backupEnabled: true,
        logLevel: 'info'
      }
    };
  }

  async updateSystemSettings(settings) {
    await this.delay();
    return { success: true, message: 'System settings updated successfully' };
  }

  async getAuditLogs(filters = {}) {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', action: 'User login', user: 'admin', timestamp: '2024-01-15T10:30:00Z', ip: '192.168.1.1' },
        { id: '2', action: 'Patient created', user: 'cashier1', timestamp: '2024-01-15T10:25:00Z', ip: '192.168.1.2' },
        { id: '3', action: 'Sale completed', user: 'cashier1', timestamp: '2024-01-15T10:20:00Z', ip: '192.168.1.2' }
      ]
    };
  }

  // Mock reports
  async generateReport(type, filters = {}) {
    await this.delay(1000); // Reports take longer
    return { success: true, message: 'Report generated successfully', reportId: this.generateId() };
  }

  async getReports() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', name: 'Sales Report', type: 'sales', generated: '2024-01-15', status: 'completed' },
        { id: '2', name: 'Inventory Report', type: 'inventory', generated: '2024-01-14', status: 'completed' },
        { id: '3', name: 'Patient Report', type: 'patients', generated: '2024-01-13', status: 'processing' }
      ]
    };
  }

  // Mock account management
  async updateUserProfile(profileData) {
    await this.delay();
    return { success: true, message: 'Profile updated successfully' };
  }

  async getUserProfile() {
    await this.delay();
    return {
      success: true,
      data: {
        id: 'demo-admin',
        name: 'Demo Admin',
        email: 'admin@demo.com',
        role: 'Administrator',
        branch: 'Main Branch'
      }
    };
  }

  async updatePharmacySettings(settings) {
    await this.delay();
    return { success: true, message: 'Pharmacy settings updated successfully' };
  }

  async getPharmacySettings() {
    await this.delay();
    return {
      success: true,
      data: {
        name: 'Demo Pharmacy',
        address: '123 Demo Street',
        phone: '+1-555-0123',
        email: 'demo@pharmacy.com'
      }
    };
  }

  async updateNotificationSettings(settings) {
    await this.delay();
    return { success: true, message: 'Notification settings updated successfully' };
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

  async changePassword(passwordData) {
    await this.delay();
    return { success: true, message: 'Password changed successfully' };
  }

  async getActiveSessions() {
    await this.delay();
    return {
      success: true,
      data: [
        { id: '1', device: 'Chrome on Windows', ip: '192.168.1.1', lastActive: '2024-01-15T10:30:00Z' },
        { id: '2', device: 'Mobile App', ip: '192.168.1.2', lastActive: '2024-01-15T09:15:00Z' }
      ]
    };
  }

  async revokeSession(sessionId) {
    await this.delay();
    return { success: true, message: 'Session revoked successfully' };
  }

  async revokeAllSessions() {
    await this.delay();
    return { success: true, message: 'All sessions revoked successfully' };
  }

  async cancelSubscription(cancelData) {
    await this.delay();
    return { success: true, message: 'Subscription cancelled successfully' };
  }

  async sendNotification(notificationData) {
    await this.delay();
    return { success: true, message: 'Notification sent successfully' };
  }

  // Mock data sync
  async syncUserData() {
    await this.delay();
    return { success: true, message: 'User data synced successfully' };
  }

  async syncPharmacyData() {
    await this.delay();
    return { success: true, message: 'Pharmacy data synced successfully' };
  }

  async broadcastUpdate(updateData) {
    await this.delay();
    return { success: true, message: 'Update broadcasted successfully' };
  }
}

// Initialize global admin API instance
window.adminAPI = new AdminAPI();
