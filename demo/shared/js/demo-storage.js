// Demo Storage Manager - Local Storage Implementation
class DemoStorage {
    constructor() {
        this.prefix = 'umi_demo_';
        this.initializeData();
    }

    // Initialize with Zambian sample data if empty
    initializeData() {
        if (!this.getItem('patients')) {
            this.setZambianSampleData();
        }
    }

    // Generic storage methods
    getItem(key) {
        try {
            const item = localStorage.getItem(this.prefix + key);
            return item ? JSON.parse(item) : null;
        } catch (error) {
            console.error('Error getting item:', error);
            return null;
        }
    }

    setItem(key, value) {
        try {
            localStorage.setItem(this.prefix + key, JSON.stringify(value));
            return true;
        } catch (error) {
            console.error('Error setting item:', error);
            return false;
        }
    }

    removeItem(key) {
        try {
            localStorage.removeItem(this.prefix + key);
            return true;
        } catch (error) {
            console.error('Error removing item:', error);
            return false;
        }
    }

    // Zambian Sample Data
    setZambianSampleData() {
        const zambianProvinces = ['Lusaka', 'Copperbelt', 'Northern', 'Southern', 'Eastern', 'Western', 'Central', 'Luapula', 'Muchinga', 'North-Western'];
        const zambianNames = ['Banda', 'Phiri', 'Tembo', 'Bwalya', 'Chanda', 'Kabwe', 'Mwansa', 'Sichone', 'Mumba', 'Kaluba'];
        const zambianHospitals = ['University Teaching Hospital', 'Ndola Central Hospital', 'Kitwe Central Hospital', 'Livingstone General Hospital', 'Kasama General Hospital', 'Chipata General Hospital'];

        // Sample Patients
        const patients = [
            {
                id: 1,
                firstName: 'James',
                lastName: 'Banda',
                nrc: '123456/78/9',
                phone: '+260977123456',
                email: 'james.banda@email.com',
                address: 'Chilenje, Lusaka',
                province: 'Lusaka',
                dateOfBirth: '1985-03-15',
                gender: 'Male',
                bloodGroup: 'O+',
                allergies: 'Penicillin',
                chronicConditions: 'Hypertension',
                emergencyContact: 'Mary Banda +260977123457',
                registrationDate: '2024-01-15',
                status: 'Active'
            },
            {
                id: 2,
                firstName: 'Grace',
                lastName: 'Phiri',
                nrc: '234567/89/0',
                phone: '+260976234567',
                email: 'grace.phiri@email.com',
                address: 'Kansenshi, Ndola',
                province: 'Copperbelt',
                dateOfBirth: '1992-07-22',
                gender: 'Female',
                bloodGroup: 'A+',
                allergies: 'None',
                chronicConditions: 'Asthma',
                emergencyContact: 'John Phiri +260976234568',
                registrationDate: '2024-02-20',
                status: 'Active'
            },
            {
                id: 3,
                firstName: 'Michael',
                lastName: 'Tembo',
                nrc: '345678/90/1',
                phone: '+260975345678',
                email: 'michael.tembo@email.com',
                address: 'Kansanshi, Solwezi',
                province: 'North-Western',
                dateOfBirth: '1988-11-30',
                gender: 'Male',
                bloodGroup: 'B+',
                allergies: 'Dust',
                chronicConditions: 'None',
                emergencyContact: 'Susan Tembo +260975345679',
                registrationDate: '2024-03-10',
                status: 'Active'
            }
        ];

        // Sample Medications
        const medications = [
            {
                id: 1,
                name: 'Paracetamol 500mg',
                category: 'Analgesic',
                description: 'Pain relief medication',
                unit: 'Tablet',
                stockQuantity: 1000,
                reorderLevel: 100,
                unitPrice: 2.50,
                supplier: 'Zambia Pharmaceuticals Ltd',
                expiryDate: '2025-12-31',
                batchNumber: 'P500-2024-001',
                storageConditions: 'Room temperature'
            },
            {
                id: 2,
                name: 'Amoxicillin 250mg',
                category: 'Antibiotic',
                description: 'Broad spectrum antibiotic',
                unit: 'Capsule',
                stockQuantity: 500,
                reorderLevel: 50,
                unitPrice: 8.75,
                supplier: 'MediSupply Zambia',
                expiryDate: '2025-06-30',
                batchNumber: 'AMX-2024-002',
                storageConditions: 'Cool dry place'
            },
            {
                id: 3,
                name: 'Artemether-Lumefantrine',
                category: 'Antimalarial',
                description: 'Malaria treatment',
                unit: 'Tablet',
                stockQuantity: 200,
                reorderLevel: 30,
                unitPrice: 25.00,
                supplier: 'Global Health Supplies',
                expiryDate: '2025-09-30',
                batchNumber: 'AL-2024-003',
                storageConditions: 'Room temperature'
            }
        ];

        // Sample Prescriptions
        const prescriptions = [
            {
                id: 1,
                patientId: 1,
                patientName: 'James Banda',
                doctorName: 'Dr. Sarah Mwansa',
                date: '2024-12-01',
                diagnosis: 'Hypertension',
                status: 'Active',
                medications: [
                    {
                        name: 'Lisinopril 10mg',
                        dosage: '1 tablet daily',
                        duration: '30 days',
                        instructions: 'Take with water in the morning'
                    }
                ],
                totalCost: 45.00
            },
            {
                id: 2,
                patientId: 2,
                patientName: 'Grace Phiri',
                doctorName: 'Dr. John Chanda',
                date: '2024-12-02',
                diagnosis: 'Respiratory infection',
                status: 'Completed',
                medications: [
                    {
                        name: 'Amoxicillin 250mg',
                        dosage: '1 capsule three times daily',
                        duration: '7 days',
                        instructions: 'Take after meals'
                    },
                    {
                        name: 'Paracetamol 500mg',
                        dosage: '1 tablet as needed',
                        duration: '7 days',
                        instructions: 'For pain relief'
                    }
                ],
                totalCost: 85.50
            }
        ];

        // Sample Sales
        const sales = [
            {
                id: 1,
                patientId: 1,
                patientName: 'James Banda',
                date: '2024-12-01',
                items: [
                    {
                        medicationName: 'Lisinopril 10mg',
                        quantity: 30,
                        unitPrice: 1.50,
                        totalPrice: 45.00
                    }
                ],
                totalAmount: 45.00,
                paymentMethod: 'Cash',
                paymentStatus: 'Paid',
                cashierName: 'Admin User'
            },
            {
                id: 2,
                patientId: 2,
                patientName: 'Grace Phiri',
                date: '2024-12-02',
                items: [
                    {
                        medicationName: 'Amoxicillin 250mg',
                        quantity: 21,
                        unitPrice: 3.50,
                        totalPrice: 73.50
                    },
                    {
                        medicationName: 'Paracetamol 500mg',
                        quantity: 14,
                        unitPrice: 0.86,
                        totalPrice: 12.00
                    }
                ],
                totalAmount: 85.50,
                paymentMethod: 'Mobile Money',
                paymentStatus: 'Paid',
                cashierName: 'Admin User'
            }
        ];

        // Sample Users
        const users = [
            {
                id: 1,
                username: 'admin',
                firstName: 'Admin',
                lastName: 'User',
                email: 'admin@umihealth.zm',
                role: 'Administrator',
                department: 'IT',
                phone: '+260977000001',
                status: 'Active',
                lastLogin: '2024-12-03 09:00:00',
                permissions: ['all']
            },
            {
                id: 2,
                username: 'pharmacist',
                firstName: 'Esther',
                lastName: 'Mwale',
                email: 'e.mwale@umihealth.zm',
                role: 'Pharmacist',
                department: 'Pharmacy',
                phone: '+260977000002',
                status: 'Active',
                lastLogin: '2024-12-03 08:30:00',
                permissions: ['pharmacy', 'prescriptions', 'inventory']
            },
            {
                id: 3,
                username: 'cashier',
                firstName: 'Joseph',
                lastName: 'Banda',
                email: 'j.banda@umihealth.zm',
                role: 'Cashier',
                department: 'Finance',
                phone: '+260977000003',
                status: 'Active',
                lastLogin: '2024-12-03 07:45:00',
                permissions: ['sales', 'payments', 'reports']
            }
        ];

        // Store all sample data
        this.setItem('patients', patients);
        this.setItem('medications', medications);
        this.setItem('prescriptions', prescriptions);
        this.setItem('sales', sales);
        this.setItem('users', users);
        this.setItem('settings', {
            pharmacyName: 'Umi Health Demo Pharmacy',
            address: '123 Main Street, Lusaka, Zambia',
            phone: '+260211123456',
            email: 'info@umihealth.zm',
            currency: 'ZMW',
            taxRate: 16,
            logo: '../../logo.png'
        });
    }

    // Data manipulation methods
    addPatient(patient) {
        const patients = this.getItem('patients') || [];
        patient.id = Date.now();
        patient.registrationDate = new Date().toISOString().split('T')[0];
        patient.status = 'Active';
        patients.push(patient);
        this.setItem('patients', patients);
        return patient;
    }

    updatePatient(id, updates) {
        const patients = this.getItem('patients') || [];
        const index = patients.findIndex(p => p.id === parseInt(id));
        if (index !== -1) {
            patients[index] = { ...patients[index], ...updates };
            this.setItem('patients', patients);
            return patients[index];
        }
        return null;
    }

    deletePatient(id) {
        const patients = this.getItem('patients') || [];
        const filtered = patients.filter(p => p.id !== parseInt(id));
        this.setItem('patients', filtered);
        return true;
    }

    addMedication(medication) {
        const medications = this.getItem('medications') || [];
        medication.id = Date.now();
        medications.push(medication);
        this.setItem('medications', medications);
        return medication;
    }

    updateMedication(id, updates) {
        const medications = this.getItem('medications') || [];
        const index = medications.findIndex(m => m.id === parseInt(id));
        if (index !== -1) {
            medications[index] = { ...medications[index], ...updates };
            this.setItem('medications', medications);
            return medications[index];
        }
        return null;
    }

    updatePrescription(id, updates) {
        const prescriptions = this.getItem('prescriptions') || [];
        const index = prescriptions.findIndex(p => p.id === parseInt(id));
        if (index !== -1) {
            prescriptions[index] = { ...prescriptions[index], ...updates };
            this.setItem('prescriptions', prescriptions);
            return prescriptions[index];
        }
        return null;
    }

    addSale(sale) {
        const sales = this.getItem('sales') || [];
        sale.id = Date.now();
        sale.date = new Date().toISOString().split('T')[0];
        sale.paymentStatus = 'Paid';
        sales.push(sale);
        this.setItem('sales', sales);
        return sale;
    }

    updateSale(id, updates) {
        const sales = this.getItem('sales') || [];
        const index = sales.findIndex(s => s.id === parseInt(id));
        if (index !== -1) {
            sales[index] = { ...sales[index], ...updates };
            this.setItem('sales', sales);
            return sales[index];
        }
        return null;
    }

    // Search and filter methods
    searchPatients(query) {
        const patients = this.getItem('patients') || [];
        return patients.filter(p => 
            p.firstName.toLowerCase().includes(query.toLowerCase()) ||
            p.lastName.toLowerCase().includes(query.toLowerCase()) ||
            p.nrc.includes(query) ||
            p.phone.includes(query)
        );
    }

    searchMedications(query) {
        const medications = this.getItem('medications') || [];
        return medications.filter(m => 
            m.name.toLowerCase().includes(query.toLowerCase()) ||
            m.category.toLowerCase().includes(query.toLowerCase())
        );
    }

    // Statistics methods
    getDashboardStats() {
        const patients = this.getItem('patients') || [];
        const medications = this.getItem('medications') || [];
        const prescriptions = this.getItem('prescriptions') || [];
        const sales = this.getItem('sales') || [];

        return {
            totalPatients: patients.length,
            activePatients: patients.filter(p => p.status === 'Active').length,
            totalMedications: medications.length,
            lowStockItems: medications.filter(m => m.stockQuantity <= m.reorderLevel).length,
            totalPrescriptions: prescriptions.length,
            activePrescriptions: prescriptions.filter(p => p.status === 'Active').length,
            totalSales: sales.length,
            totalRevenue: sales.reduce((sum, sale) => sum + sale.totalAmount, 0),
            todaySales: sales.filter(s => s.date === new Date().toISOString().split('T')[0]).length
        };
    }

    // Export/Import methods
    exportData() {
        const data = {
            patients: this.getItem('patients'),
            medications: this.getItem('medications'),
            prescriptions: this.getItem('prescriptions'),
            sales: this.getItem('sales'),
            users: this.getItem('users'),
            settings: this.getItem('settings'),
            exportDate: new Date().toISOString()
        };
        return data;
    }

    importData(data) {
        if (data.patients) this.setItem('patients', data.patients);
        if (data.medications) this.setItem('medications', data.medications);
        if (data.prescriptions) this.setItem('prescriptions', data.prescriptions);
        if (data.sales) this.setItem('sales', data.sales);
        if (data.users) this.setItem('users', data.users);
        if (data.settings) this.setItem('settings', data.settings);
        return true;
    }

    // Clear all data
    clearAllData() {
        const keys = ['patients', 'medications', 'prescriptions', 'sales', 'users', 'settings'];
        keys.forEach(key => this.removeItem(key));
        this.initializeData();
    }
}

// Initialize global storage instance
window.demoStorage = new DemoStorage();
