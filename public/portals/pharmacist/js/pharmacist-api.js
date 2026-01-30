/**
 * Pharmacist Portal API Service - Updated to use new service-based controllers
 * Handles all API communications for the pharmacist portal
 * Integrates with backend endpoints and data synchronization
 */

class PharmacistApi {
    constructor() {
        this.baseUrl = 'http://localhost:5000/api/v1';
        this.tenantId = localStorage.getItem('umi_tenant_id');
        this.userId = null;
        this.token = localStorage.getItem('umi_access_token');
        this.init();
    }

    /**
     * Clear authentication information
     */
    clearAuth() {
        this.token = null;
        this.tenantId = null;
        this.userId = null;
        
        localStorage.removeItem('umi_access_token');
        localStorage.removeItem('umi_tenant_id');
        localStorage.removeItem('auth_tokens');
    }

    /**
     * Initialize API service
     */
    init() {
        this.loadAuthInfo();
        this.setupInterceptors();
    }

    /**
     * Load authentication information
     */
    loadAuthInfo() {
        try {
            const token = localStorage.getItem('umi_access_token');
            const tenantId = localStorage.getItem('umi_tenant_id');
            
            if (token) {
                this.token = token;
            }
            
            if (tenantId) {
                this.tenantId = tenantId;
            }
        } catch (error) {
            console.error('Failed to load auth info:', error);
        }
    }

    /**
     * Setup request interceptors
     */
    setupInterceptors() {
        // This would typically be handled by fetch interceptors in a real app
        // For now, we'll handle headers in each request
    }

    /**
     * Get request headers
     */
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

        return headers;
    }

    /**
     * Make API request
     */
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        
        const config = {
            headers: this.getHeaders(),
            ...options
        };

        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                const error = await response.json().catch(() => ({}));
                throw new Error(error.message || `HTTP ${response.status}`);
            }
            
            return await response.json();
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

    // ==================== INVENTORY MANAGEMENT ====================
    // Using new InventoryController

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

    // ==================== PRESCRIPTION MANAGEMENT ====================
    // Using existing endpoints (will be updated when PrescriptionController is created)

    async getPrescriptions() {
        return await this.request('/admin/prescriptions');
    }

    async getPrescription(prescriptionId) {
        return await this.request(`/admin/prescriptions/${prescriptionId}`);
    }

    async createPrescription(prescriptionData) {
        return await this.request('/admin/prescriptions', {
            method: 'POST',
            body: JSON.stringify(prescriptionData)
        });
    }

    async updatePrescription(prescriptionId, prescriptionData) {
        return await this.request(`/admin/prescriptions/${prescriptionId}`, {
            method: 'PUT',
            body: JSON.stringify(prescriptionData)
        });
    }

    async deletePrescription(prescriptionId) {
        return await this.request(`/admin/prescriptions/${prescriptionId}`, {
            method: 'DELETE'
        });
    }

    async approvePrescription(prescriptionId) {
        return await this.request(`/admin/prescriptions/${prescriptionId}/approve`, {
            method: 'POST'
        });
    }

    async dispensePrescription(prescriptionId, dispenseData) {
        return await this.request(`/admin/prescriptions/${prescriptionId}/dispense`, {
            method: 'POST',
            body: JSON.stringify(dispenseData)
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
        const url = `${this.baseUrl}/report/${reportId}/export/pdf`;
        const response = await fetch(url, {
            headers: this.getHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`Export failed: ${response.status}`);
        }
        
        return await response.blob();
    }

    async exportReportToExcel(reportId) {
        const url = `${this.baseUrl}/report/${reportId}/export/excel`;
        const response = await fetch(url, {
            headers: this.getHeaders()
        });
        
        if (!response.ok) {
            throw new Error(`Export failed: ${response.status}`);
        }
        
        return await response.blob();
    }

    // ==================== CLINICAL FEATURES ====================

    async getDrugInteractions(medications) {
        return await this.request('/clinical/drug-interactions', {
            method: 'POST',
            body: JSON.stringify({ medications })
        });
    }

    async getDrugInformation(drugId) {
        return await this.request(`/clinical/drugs/${drugId}`);
    }

    async getDosageRecommendations(patientId, drugId) {
        return await this.request(`/clinical/dosage-recommendations`, {
            method: 'POST',
            body: JSON.stringify({ patientId, drugId })
        });
    }

    async validatePrescription(prescriptionData) {
        return await this.request('/clinical/validate-prescription', {
            method: 'POST',
            body: JSON.stringify(prescriptionData)
        });
    }

    // ==================== COMPLIANCE ====================

    async getComplianceStatus() {
        return await this.request('/compliance/status');
    }

    async getAuditLogs(filters = {}) {
        return await this.request('/compliance/audit-logs', {
            method: 'GET',
            params: filters
        });
    }

    async generateComplianceReport(reportType) {
        return await this.request('/compliance/reports', {
            method: 'POST',
            body: JSON.stringify({ reportType })
        });
    }

    // ==================== UTILITIES ====================

    async getSystemHealth() {
        return await this.request('/health');
    }

    async getNotifications() {
        return await this.request('/notifications');
    }

    async markNotificationRead(notificationId) {
        return await this.request(`/notifications/${notificationId}/read`, {
            method: 'POST'
        });
    }
}

// Initialize global API instance
window.pharmacistApi = new PharmacistApi();
