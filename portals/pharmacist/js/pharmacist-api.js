/**
 * Pharmacist Portal API Service
 * Handles all API communications for the pharmacist portal
 * Integrates with backend endpoints and data synchronization
 */

class PharmacistApi {
    constructor() {
        this.baseUrl = '/api/v1';
        this.tenantId = null;
        this.userId = null;
        this.token = localStorage.getItem('authToken');
        this.init();
    }

    /**
     * Clear authentication information
     */
    clearAuth() {
        this.token = null;
        this.tenantId = null;
        this.userId = null;
        
        // Clear all possible auth keys
        localStorage.removeItem('authToken');
        localStorage.removeItem('umi_currentUser');
        localStorage.removeItem('umi_access_token');
        localStorage.removeItem('umi_current_user');
        localStorage.removeItem('umi_current_tenant');
        localStorage.removeItem('umi_tenant_id');
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
            // Try to get auth token (new format) or access token (legacy format)
            const token = localStorage.getItem('authToken') || localStorage.getItem('umi_access_token');
            
            // Try to get user info (new format) or current user (legacy format)
            const userStr = localStorage.getItem('umi_currentUser') || localStorage.getItem('umi_current_user');
            
            if (!userStr || userStr === 'undefined' || userStr === 'null') {
                console.warn('No user data found in localStorage');
                this.tenantId = null;
                this.userId = null;
                this.token = null;
                return;
            }
            
            const user = JSON.parse(userStr);
            if (!user || typeof user !== 'object') {
                console.warn('Invalid user data format');
                this.tenantId = null;
                this.userId = null;
                this.token = null;
                return;
            }
            
            this.tenantId = user.tenantId || null;
            this.userId = user.id || null;
            this.token = token || null;
            
            // Log for debugging
            console.log('Auth info loaded:', {
                tenantId: this.tenantId,
                userId: this.userId,
                hasToken: !!this.token
            });
        } catch (error) {
            console.error('Failed to load auth info:', error);
            this.tenantId = null;
            this.userId = null;
            this.token = null;
        }
    }

    /**
     * Setup request interceptors
     */
    setupInterceptors() {
        // This would be used with a proper HTTP client like axios
        // For now, we'll handle auth in each request
    }

    /**
     * Make authenticated API request
     */
    async request(endpoint, options = {}) {
        const url = `${this.baseUrl}${endpoint}`;
        const config = {
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${this.token}`,
                ...options.headers
            },
            ...options
        };

        try {
            const response = await fetch(url, config);
            
            if (!response.ok) {
                if (response.status === 401) {
                    this.handleUnauthorized();
                }
                throw new Error(`HTTP ${response.status}: ${response.statusText}`);
            }

            const data = await response.json();
            return data;
        } catch (error) {
            console.error(`API Error [${endpoint}]:`, error);
            throw error;
        }
    }

    /**
     * Handle unauthorized requests
     */
    handleUnauthorized() {
        this.clearAuth();
        window.location.href = '../../public/signin.html';
    }

    /**
     * Inventory API methods
     */
    inventory = {
        getProducts: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/inventory?${queryString}`);
        },

        getProduct: async (id) => {
            return await this.request(`/inventory/${id}`);
        },

        createProduct: async (product) => {
            return await this.request('/inventory', {
                method: 'POST',
                body: JSON.stringify(product)
            });
        },

        updateProduct: async (id, product) => {
            return await this.request(`/inventory/${id}`, {
                method: 'PUT',
                body: JSON.stringify(product)
            });
        },

        deleteProduct: async (id) => {
            return await this.request(`/inventory/${id}`, {
                method: 'DELETE'
            });
        },

        bulkUpload: async (products) => {
            return await this.request('/inventory/bulk-upload', {
                method: 'POST',
                body: JSON.stringify({ products })
            });
        },

        getLowStock: async () => {
            return await this.request('/inventory/low-stock');
        },

        getExpiring: async (days = 30) => {
            return await this.request(`/inventory/expiring?days=${days}`);
        },

        adjustStock: async (id, adjustment) => {
            return await this.request(`/inventory/${id}/adjust-stock`, {
                method: 'POST',
                body: JSON.stringify(adjustment)
            });
        },

        getCategories: async () => {
            return await this.request('/inventory/categories');
        },

        getSuppliers: async () => {
            return await this.request('/inventory/suppliers');
        }
    };

    /**
     * Prescriptions API methods
     */
    prescriptions = {
        getPrescriptions: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/prescriptions?${queryString}`);
        },

        getPrescription: async (id) => {
            return await this.request(`/prescriptions/${id}`);
        },

        createPrescription: async (prescription) => {
            return await this.request('/prescriptions', {
                method: 'POST',
                body: JSON.stringify(prescription)
            });
        },

        updatePrescription: async (id, prescription) => {
            return await this.request(`/prescriptions/${id}`, {
                method: 'PUT',
                body: JSON.stringify(prescription)
            });
        },

        deletePrescription: async (id) => {
            return await this.request(`/prescriptions/${id}`, {
                method: 'DELETE'
            });
        },

        dispensePrescription: async (id, dispenseInfo) => {
            return await this.request(`/prescriptions/${id}/dispense`, {
                method: 'POST',
                body: JSON.stringify(dispenseInfo)
            });
        },

        verifyPrescription: async (id, verification) => {
            return await this.request(`/prescriptions/${id}/verify`, {
                method: 'POST',
                body: JSON.stringify(verification)
            });
        },

        getPending: async () => {
            return await this.request('/prescriptions/pending');
        },

        getExpired: async () => {
            return await this.request('/prescriptions/expired');
        },

        getByPatient: async (patientId) => {
            return await this.request(`/prescriptions/patient/${patientId}`);
        }
    };

    /**
     * Patients API methods
     */
    patients = {
        getPatients: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/patients?${queryString}`);
        },

        getPatient: async (id) => {
            return await this.request(`/patients/${id}`);
        },

        createPatient: async (patient) => {
            return await this.request('/patients', {
                method: 'POST',
                body: JSON.stringify(patient)
            });
        },

        updatePatient: async (id, patient) => {
            return await this.request(`/patients/${id}`, {
                method: 'PUT',
                body: JSON.stringify(patient)
            });
        },

        deletePatient: async (id) => {
            return await this.request(`/patients/${id}`, {
                method: 'DELETE'
            });
        },

        bulkImport: async (patients) => {
            return await this.request('/patients/bulk-import', {
                method: 'POST',
                body: JSON.stringify({ patients })
            });
        },

        searchPatients: async (query, limit = 10) => {
            return await this.request(`/patients/search?query=${encodeURIComponent(query)}&limit=${limit}`);
        },

        getMedicalHistory: async (id) => {
            return await this.request(`/patients/${id}/medical-history`);
        },

        getPrescriptions: async (id) => {
            return await this.request(`/patients/${id}/prescriptions`);
        },

        getAllergies: async (id) => {
            return await this.request(`/patients/${id}/allergies`);
        },

        addAllergy: async (id, allergy) => {
            return await this.request(`/patients/${id}/allergies`, {
                method: 'POST',
                body: JSON.stringify(allergy)
            });
        },

        getMedications: async (id) => {
            return await this.request(`/patients/${id}/medications`);
        },

        addMedicalRecord: async (id, record) => {
            return await this.request(`/patients/${id}/medical-records`, {
                method: 'POST',
                body: JSON.stringify(record)
            });
        }
    };

    /**
     * Payments API methods
     */
    payments = {
        getPayments: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/payments?${queryString}`);
        },

        getPayment: async (id) => {
            return await this.request(`/payments/${id}`);
        },

        createPayment: async (payment) => {
            return await this.request('/payments', {
                method: 'POST',
                body: JSON.stringify(payment)
            });
        },

        updatePayment: async (id, payment) => {
            return await this.request(`/payments/${id}`, {
                method: 'PUT',
                body: JSON.stringify(payment)
            });
        },

        deletePayment: async (id) => {
            return await this.request(`/payments/${id}`, {
                method: 'DELETE'
            });
        },

        processRefund: async (id, refund) => {
            return await this.request(`/payments/${id}/refund`, {
                method: 'POST',
                body: JSON.stringify(refund)
            });
        },

        getSummary: async (startDate, endDate) => {
            const params = new URLSearchParams();
            if (startDate) params.append('startDate', startDate);
            if (endDate) params.append('endDate', endDate);
            return await this.request(`/payments/summary?${params}`);
        },

        getMethods: async () => {
            return await this.request('/payments/methods');
        },

        addMethod: async (method) => {
            return await this.request('/payments/methods', {
                method: 'POST',
                body: JSON.stringify(method)
            });
        },

        getPatientPayments: async (patientId) => {
            return await this.request(`/payments/patient/${patientId}`);
        },

        reconcile: async (request) => {
            return await this.request('/payments/reconcile', {
                method: 'POST',
                body: JSON.stringify(request)
            });
        }
    };

    /**
     * Account API methods
     */
    account = {
        getProfile: async () => {
            return await this.request('/account/profile');
        },

        updateProfile: async (profile) => {
            return await this.request('/account/profile', {
                method: 'PUT',
                body: JSON.stringify(profile)
            });
        },

        getTenantSettings: async () => {
            return await this.request('/account/tenant-settings');
        },

        updateTenantSettings: async (settings) => {
            return await this.request('/account/tenant-settings', {
                method: 'PUT',
                body: JSON.stringify(settings)
            });
        },

        getSubscriptionPlans: async () => {
            return await this.request('/account/subscription-plans');
        },

        upgradeSubscription: async (upgrade) => {
            return await this.request('/account/upgrade-subscription', {
                method: 'POST',
                body: JSON.stringify(upgrade)
            });
        }
    };

    /**
     * Dashboard API methods
     */
    dashboard = {
        getStats: async () => {
            return await this.request('/dashboard/stats');
        },

        getRecentActivity: async () => {
            return await this.request('/dashboard/recent-activity');
        },

        getAlerts: async () => {
            return await this.request('/dashboard/alerts');
        },

        getCharts: async (type, period) => {
            return await this.request(`/dashboard/charts/${type}?period=${period}`);
        }
    };

    /**
     * Cross-page data fetching methods
     */
    crossPageData = {
        // Fetch data from inventory page
        getInventoryData: async () => {
            try {
                const productsResponse = await this.inventory.getProducts();
                const lowStockResponse = await this.inventory.getLowStock();
                const categoriesResponse = await this.inventory.getCategories();
                
                return {
                    products: productsResponse.data || [],
                    lowStock: lowStockResponse.data || [],
                    categories: categoriesResponse.data || []
                };
            } catch (error) {
                console.error('Failed to fetch inventory data:', error);
                throw error;
            }
        },

        // Fetch data from prescriptions page
        getPrescriptionsData: async () => {
            try {
                const prescriptionsResponse = await this.prescriptions.getPrescriptions();
                const pendingResponse = await this.prescriptions.getPending();
                const expiredResponse = await this.prescriptions.getExpired();
                
                return {
                    prescriptions: prescriptionsResponse.data || [],
                    pending: pendingResponse.data || [],
                    expired: expiredResponse.data || []
                };
            } catch (error) {
                console.error('Failed to fetch prescriptions data:', error);
                throw error;
            }
        },

        // Fetch data from patients page
        getPatientsData: async () => {
            try {
                const patientsResponse = await this.patients.getPatients();
                const recentPatients = await this.patients.getPatients({ limit: 10, sortBy: 'createdAt' });
                
                return {
                    patients: patientsResponse.data || [],
                    recentPatients: recentPatients.data || []
                };
            } catch (error) {
                console.error('Failed to fetch patients data:', error);
                throw error;
            }
        },

        // Fetch data from payments page
        getPaymentsData: async () => {
            try {
                const paymentsResponse = await this.payments.getPayments();
                const summaryResponse = await this.payments.getSummary();
                const methodsResponse = await this.payments.getMethods();
                
                return {
                    payments: paymentsResponse.data || [],
                    summary: summaryResponse.data || {},
                    methods: methodsResponse.data || []
                };
            } catch (error) {
                console.error('Failed to fetch payments data:', error);
                throw error;
            }
        },

        // Fetch data from suppliers page
        getSuppliersData: async () => {
            try {
                const suppliersResponse = await this.suppliers.getSuppliers();
                const statsResponse = await this.suppliers.getSupplierStats();
                
                return {
                    suppliers: suppliersResponse.data || [],
                    stats: statsResponse.data || {}
                };
            } catch (error) {
                console.error('Failed to fetch suppliers data:', error);
                throw error;
            }
        },

        // Get comprehensive data for reports
        getComprehensiveData: async () => {
            try {
                const [inventoryData, prescriptionsData, patientsData, paymentsData, suppliersData] = await Promise.all([
                    this.getInventoryData(),
                    this.getPrescriptionsData(),
                    this.getPatientsData(),
                    this.getPaymentsData(),
                    this.getSuppliersData()
                ]);
                
                return {
                    inventory: inventoryData,
                    prescriptions: prescriptionsData,
                    patients: patientsData,
                    payments: paymentsData,
                    suppliers: suppliersData,
                    timestamp: new Date().toISOString()
                };
            } catch (error) {
                console.error('Failed to fetch comprehensive data:', error);
                throw error;
            }
        }
    };

    /**
     * Compliance API methods
     */
    compliance = {
        getComplianceOverview: async () => {
            return await this.request('/compliance/overview');
        },

        getLicenses: async () => {
            return await this.request('/compliance/licenses');
        },

        updateLicense: async (id, licenseData) => {
            return await this.request(`/compliance/licenses/${id}`, {
                method: 'PUT',
                body: JSON.stringify(licenseData)
            });
        },

        getAuditTrail: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/compliance/audit-trail?${queryString}`);
        },

        createAuditLog: async (auditData) => {
            return await this.request('/compliance/audit-trail', {
                method: 'POST',
                body: JSON.stringify(auditData)
            });
        },

        getTrainingRecords: async () => {
            return await this.request('/compliance/training');
        },

        updateTrainingRecord: async (id, trainingData) => {
            return await this.request(`/compliance/training/${id}`, {
                method: 'PUT',
                body: JSON.stringify(trainingData)
            });
        },

        getDocumentation: async () => {
            return await this.request('/compliance/documentation');
        },

        uploadDocumentation: async (docData) => {
            return await this.request('/compliance/documentation', {
                method: 'POST',
                body: JSON.stringify(docData)
            });
        },

        getComplianceAlerts: async () => {
            return await this.request('/compliance/alerts');
        },

        dismissAlert: async (id) => {
            return await this.request(`/compliance/alerts/${id}/dismiss`, {
                method: 'POST'
            });
        },

        generateComplianceReport: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/compliance/reports/generate?${queryString}`);
        },

        exportAuditTrail: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/compliance/audit-trail/export?${queryString}`);
        },

        getComplianceStats: async () => {
            return await this.request('/compliance/stats');
        },

        runComplianceCheck: async () => {
            return await this.request('/compliance/check', {
                method: 'POST'
            });
        }
    };

    /**
     * Reports API methods
     */
    reports = {
        getReports: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports?${queryString}`);
        },

        getReport: async (id) => {
            return await this.request(`/reports/${id}`);
        },

        generateReport: async (reportData) => {
            return await this.request('/reports/generate', {
                method: 'POST',
                body: JSON.stringify(reportData)
            });
        },

        downloadReport: async (id, format = 'pdf') => {
            return await this.request(`/reports/${id}/download?format=${format}`);
        },

        shareReport: async (id, shareData) => {
            return await this.request(`/reports/${id}/share`, {
                method: 'POST',
                body: JSON.stringify(shareData)
            });
        },

        deleteReport: async (id) => {
            return await this.request(`/reports/${id}`, {
                method: 'DELETE'
            });
        },

        scheduleReport: async (scheduleData) => {
            return await this.request('/reports/schedule', {
                method: 'POST',
                body: JSON.stringify(scheduleData)
            });

        },

        getScheduledReports: async () => {
            return await this.request('/reports/scheduled');
        },

        cancelScheduledReport: async (id) => {
            return await this.request(`/reports/scheduled/${id}`, {
                method: 'DELETE'
            });
        },

        // Revenue and analytics endpoints
        getRevenueAnalytics: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports/analytics/revenue?${queryString}`);
        },

        getSalesByCategory: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports/analytics/sales-by-category?${queryString}`);
        },

        getTopProducts: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports/analytics/top-products?${queryString}`);
        },

        getInventoryMetrics: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports/analytics/inventory-metrics?${queryString}`);
        },

        getPatientAnalytics: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports/analytics/patient-analytics?${queryString}`);
        },

        getPaymentAnalytics: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports/analytics/payment-analytics?${queryString}`);
        },

        getPatientVisitsAnalytics: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/reports/analytics/patient-visits?${queryString}`);
        },

        exportAnalytics: async (type, format, params = {}) => {
            const queryString = new URLSearchParams({ ...params, format }).toString();
            return await this.request(`/reports/export/${type}?${queryString}`);
        }
    };

    /**
     * Suppliers API methods
     */
    suppliers = {
        getSuppliers: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/suppliers?${queryString}`);
        },

        getSupplier: async (id) => {
            return await this.request(`/suppliers/${id}`);
        },

        createSupplier: async (supplier) => {
            return await this.request('/suppliers', {
                method: 'POST',
                body: JSON.stringify(supplier)
            });
        },

        updateSupplier: async (id, supplier) => {
            return await this.request(`/suppliers/${id}`, {
                method: 'PUT',
                body: JSON.stringify(supplier)
            });
        },

        deleteSupplier: async (id) => {
            return await this.request(`/suppliers/${id}`, {
                method: 'DELETE'
            });
        },

        getSupplierStats: async () => {
            return await this.request('/suppliers/stats');
        },

        getSupplierProducts: async (id) => {
            return await this.request(`/suppliers/${id}/products`);
        },

        getSupplierOrders: async (id, params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/suppliers/${id}/orders?${queryString}`);
        },

        searchSuppliers: async (query, limit = 10) => {
            return await this.request(`/suppliers/search?query=${encodeURIComponent(query)}&limit=${limit}`);
        },

        bulkImportSuppliers: async (suppliers) => {
            return await this.request('/suppliers/bulk-import', {
                method: 'POST',
                body: JSON.stringify({ suppliers })
            });
        },

        exportSuppliers: async (format = 'csv', params = {}) => {
            const queryString = new URLSearchParams({ ...params, format }).toString();
            return await this.request(`/suppliers/export?${queryString}`);
        },

        updateSupplierStatus: async (id, status) => {
            return await this.request(`/suppliers/${id}/status`, {
                method: 'PATCH',
                body: JSON.stringify({ status })
            });
        },

        getSupplierDocuments: async (id) => {
            return await this.request(`/suppliers/${id}/documents`);
        },

        uploadSupplierDocument: async (id, document) => {
            const formData = new FormData();
            formData.append('name', document.name);
            formData.append('type', document.type);
            if (document.file) {
                formData.append('file', document.file);
            }
            
            return await this.request(`/suppliers/${id}/documents`, {
                method: 'POST',
                body: formData,
                headers: {} // Let browser set Content-Type for FormData
            });
        },

        deleteSupplierDocument: async (documentId) => {
            return await this.request(`/suppliers/documents/${documentId}`, {
                method: 'DELETE'
            });
        },

        downloadSupplierDocument: async (documentId) => {
            const response = await this.request(`/suppliers/documents/${documentId}/download`);
            return response;
        },

        bulkDeleteSuppliers: async (supplierIds) => {
            return await this.request('/suppliers/bulk-delete', {
                method: 'POST',
                body: JSON.stringify({ supplierIds })
            });
        },

        bulkUpdateSupplierStatus: async (supplierIds, status) => {
            return await this.request('/suppliers/bulk-status-update', {
                method: 'POST',
                body: JSON.stringify({ supplierIds, status })
            });
        }
    };

    /**
     * Clinical Decision Support API methods
     */
    clinical = {
        // Drug Interaction Checker
        checkDrugInteractions: async (drug1, drug2) => {
            return await this.request('/clinical/drug-interactions', {
                method: 'POST',
                body: JSON.stringify({ drug1, drug2 })
            });
        },

        // Dosage Calculator
        calculateDosage: async (medication, weight, age, renalFunction = null) => {
            return await this.request('/clinical/dosage-calculator', {
                method: 'POST',
                body: JSON.stringify({ medication, weight, age, renalFunction })
            });
        },

        // Allergy Checker
        checkAllergies: async (patientId, medication) => {
            return await this.request('/clinical/allergy-check', {
                method: 'POST',
                body: JSON.stringify({ patientId, medication })
            });
        },

        // Alternative Medications Finder
        findAlternatives: async (medication, reason, patientId = null) => {
            return await this.request('/clinical/alternatives', {
                method: 'POST',
                body: JSON.stringify({ medication, reason, patientId })
            });
        },

        // Get available medications from inventory
        getMedications: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/clinical/medications?${queryString}`);
        },

        // Get patient allergies and medical history
        getPatientProfile: async (patientId) => {
            return await this.request(`/clinical/patient-profile/${patientId}`);
        },

        // Save clinical decision
        saveClinicalDecision: async (decisionData) => {
            return await this.request('/clinical/decisions', {
                method: 'POST',
                body: JSON.stringify(decisionData)
            });
        },

        // Get clinical decisions history
        getDecisionsHistory: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/clinical/decisions?${queryString}`);
        },

        // Get drug database
        getDrugDatabase: async (params = {}) => {
            const queryString = new URLSearchParams(params).toString();
            return await this.request(`/clinical/drug-database?${queryString}`);
        },

        // Get therapeutic guidelines
        getTherapeuticGuidelines: async (condition) => {
            return await this.request(`/clinical/guidelines/${condition}`);
        },

        // Check contraindications
        checkContraindications: async (medication, patientId) => {
            return await this.request('/clinical/contraindications', {
                method: 'POST',
                body: JSON.stringify({ medication, patientId })
            });
        },

        // Get dosage guidelines
        getDosageGuidelines: async (medication, patientAge = null) => {
            const params = new URLSearchParams();
            if (patientAge) params.append('patientAge', patientAge);
            return await this.request(`/clinical/dosage-guidelines/${medication}?${params}`);
        },

        // Patient search and selection
        searchPatients: async (query, limit = 10) => {
            return await this.request(`/clinical/patients/search?query=${encodeURIComponent(query)}&limit=${limit}`);
        },

        getPatientMedications: async (patientId) => {
            return await this.request(`/clinical/patients/${patientId}/medications`);
        },

        // Clinical history and decisions
        getPatientClinicalHistory: async (patientId, limit = 20) => {
            return await this.request(`/clinical/patients/${patientId}/history?limit=${limit}`);
        },

        getRecentDecisions: async (limit = 10) => {
            return await this.request(`/clinical/decisions/recent?limit=${limit}`);
        },

        // Drug monographs and information
        getDrugMonograph: async (drugId) => {
            return await this.request(`/clinical/drugs/${drugId}/monograph`);
        },

        getDrugInteractionsDetailed: async (drugId) => {
            return await this.request(`/clinical/drugs/${drugId}/interactions`);
        },

        // Organ function considerations
        calculateRenalDose: async (medication, creatinine, weight, age, gender) => {
            return await this.request('/clinical/renal-dose-calculator', {
                method: 'POST',
                body: JSON.stringify({ medication, creatinine, weight, age, gender })
            });
        },

        calculateHepaticDose: async (medication, childPughScore) => {
            return await this.request('/clinical/hepatic-dose-calculator', {
                method: 'POST',
                body: JSON.stringify({ medication, childPughScore })
            });
        },

        // Pregnancy and lactation safety
        checkPregnancySafety: async (medication, trimester = null) => {
            const params = new URLSearchParams();
            if (trimester) params.append('trimester', trimester);
            return await this.request(`/clinical/pregnancy-safety/${medication}?${params}`);
        },

        checkLactationSafety: async (medication) => {
            return await this.request(`/clinical/lactation-safety/${medication}`);
        },

        // Therapeutic duplication checker
        checkTherapeuticDuplication: async (patientId, newMedication) => {
            return await this.request('/clinical/therapeutic-duplication', {
                method: 'POST',
                body: JSON.stringify({ patientId, newMedication })
            });
        },

        // Elderly considerations
        checkElderlyAppropriateness: async (medication, patientAge) => {
            return await this.request('/clinical/elderly-appropriateness', {
                method: 'POST',
                body: JSON.stringify({ medication, patientAge })
            });
        },

        getBeersCriteria: async () => {
            return await this.request('/clinical/beers-criteria');
        },

        // Clinical alerts and notifications
        getClinicalAlerts: async (patientId = null) => {
            const params = new URLSearchParams();
            if (patientId) params.append('patientId', patientId);
            return await this.request(`/clinical/alerts?${params}`);
        },

        dismissAlert: async (alertId) => {
            return await this.request(`/clinical/alerts/${alertId}/dismiss`, {
                method: 'POST'
            });
        },

        // Export and reporting
        generateClinicalReport: async (reportData) => {
            return await this.request('/clinical/reports/generate', {
                method: 'POST',
                body: JSON.stringify(reportData)
            });
        },

        exportClinicalReport: async (reportId, format = 'pdf') => {
            return await this.request(`/clinical/reports/${reportId}/export?format=${format}`);
        },

        // Severity scoring
        getInteractionSeverity: async (drug1, drug2) => {
            return await this.request('/clinical/interaction-severity', {
                method: 'POST',
                body: JSON.stringify({ drug1, drug2 })
            });
        },

        // Batch operations
        batchDrugCheck: async (medications, patientId = null) => {
            return await this.request('/clinical/batch-check', {
                method: 'POST',
                body: JSON.stringify({ medications, patientId })
            });
        }
    };
}

// Create global instance
window.pharmacistApi = new PharmacistApi();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PharmacistApi;
}
