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
        const user = JSON.parse(localStorage.getItem('umi_currentUser') || '{}');
        this.tenantId = user.tenantId;
        this.userId = user.id;
        this.token = localStorage.getItem('authToken');
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
        localStorage.removeItem('authToken');
        localStorage.removeItem('umi_currentUser');
        window.location.href = '/public/admin-signin.html';
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
}

// Create global instance
window.pharmacistApi = new PharmacistApi();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
    module.exports = PharmacistApi;
}
