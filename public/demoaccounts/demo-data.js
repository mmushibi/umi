/**
 * Production Data Configuration for Umi Health Portals
 * Provides data structure definitions and validation helpers
 */

// Data structure definitions for frontend validation
const dataSchemas = {
  user: {
    required: ['id', 'name', 'email', 'role', 'permissions'],
    properties: {
      id: 'string',
      name: 'string', 
      email: 'email',
      role: 'enum:admin,cashier,pharmacist',
      permissions: 'array',
      branch: 'string',
      status: 'enum:active,inactive'
    }
  },
  patient: {
    required: ['id', 'name', 'email', 'phone'],
    properties: {
      id: 'string',
      name: 'string',
      email: 'email',
      phone: 'phone',
      dob: 'date',
      address: 'string',
      emergencyContact: 'string',
      allergies: 'array',
      conditions: 'array',
      memberSince: 'date',
      lastVisit: 'date',
      insuranceProvider: 'string',
      policyNumber: 'string',
      nrc: 'string'
    }
  },
  product: {
    required: ['id', 'name', 'sku', 'price', 'stock', 'category'],
    properties: {
      id: 'string',
      name: 'string',
      sku: 'string',
      price: 'number',
      stock: 'number',
      lowStock: 'boolean',
      category: 'string',
      expiryDate: 'date',
      manufacturer: 'string',
      controlledSubstance: 'boolean',
      description: 'string',
      dosageForm: 'string'
    }
  },
  prescription: {
    required: ['id', 'patientId', 'patientName', 'medication', 'dosage', 'prescribedBy', 'date'],
    properties: {
      id: 'string',
      patientId: 'string',
      patientName: 'string',
      medication: 'string',
      dosage: 'string',
      duration: 'string',
      prescribedBy: 'string',
      date: 'date',
      status: 'enum:pending,approved,dispensed,completed',
      priority: 'enum:normal,high,urgent',
      notes: 'string'
    }
  },
  sale: {
    required: ['id', 'total', 'items', 'customer', 'date', 'status'],
    properties: {
      id: 'string',
      total: 'number',
      subtotal: 'number',
      tax: 'number',
      items: 'array',
      customer: 'string',
      date: 'date',
      status: 'enum:completed,pending,cancelled',
      paymentMethod: 'enum:cash,mobile_money,bank_transfer,card',
      cashier: 'string'
    }
  },
  branch: {
    required: ['id', 'name', 'address', 'phone'],
    properties: {
      id: 'string',
      name: 'string',
      address: 'string',
      phone: 'phone',
      email: 'email',
      manager: 'string',
      status: 'enum:active,inactive',
      openTime: 'time',
      closeTime: 'time'
    }
  },
  supplier: {
    required: ['id', 'name', 'contact', 'phone'],
    properties: {
      id: 'string',
      name: 'string',
      contact: 'string',
      phone: 'phone',
      email: 'email',
      status: 'enum:active,inactive',
      rating: 'number',
      deliveryTime: 'string',
      address: 'string'
    }
  }
};

// Zambian business configuration
const zambiaConfig = {
  country: 'Zambia',
  currency: 'ZMW',
  timezone: 'Africa/Lusaka',
  phoneCode: '+260',
  nrcPattern: /^\d{6}\/\d{2}\/\d{1}$/,
  provinces: [
    'Central', 'Copperbelt', 'Eastern', 'Luapula', 'Lusaka', 
    'Muchinga', 'Northern', 'North-Western', 'Southern', 'Western'
  ],
  commonSurnames: [
    'Banda', 'Phiri', 'Tembo', 'Bwalya', 'Chanda', 'Mwansa', 
    'Mulenga', 'Mwila', 'Kunda', 'Mwape', 'Chileshe', 'Musonda'
  ],
  regulatoryBody: 'Pharmacy and Poisons Board',
  taxRate: 0.16
};

// System configuration
const systemConfig = {
  pharmacy: {
    name: 'Umi Health Zambia',
    address: 'Stand 2345 Cairo Road, Lusaka, Zambia',
    phone: '+260 211 234567',
    email: 'info@umizambia.com',
    registrationNumber: 'ZMP/2026/001234'
  },
  system: {
    systemName: 'Umi Health Zambia',
    version: '2.0.0',
    maintenance: false,
    backupEnabled: true,
    logLevel: 'info'
  },
  notifications: {
    emailNotifications: true,
    smsNotifications: true,
    pushNotifications: true,
    prescriptionAlerts: true,
    inventoryAlerts: true,
    lowStockAlerts: true
  }
};

// Validation helpers
const DataValidator = {
  validateEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  },

  validatePhone(phone) {
    // Zambian phone validation
    const phoneRegex = /^\+260\s?[97]\d\s?\d{3}\s?\d{4}$/;
    return phoneRegex.test(phone.replace(/\s/g, ''));
  },

  validateNRC(nrc) {
    return zambiaConfig.nrcPattern.test(nrc);
  },

  validateSchema(data, schema) {
    const errors = [];
    
    // Check required fields
    schema.required.forEach(field => {
      if (!data[field]) {
        errors.push(`Missing required field: ${field}`);
      }
    });

    // Check field types
    Object.entries(schema.properties).forEach(([field, type]) => {
      if (data[field] !== undefined) {
        if (type.startsWith('enum:')) {
          const validValues = type.substring(5).split(',');
          if (!validValues.includes(data[field])) {
            errors.push(`Invalid ${field}: must be one of ${validValues.join(', ')}`);
          }
        } else if (type === 'email' && !this.validateEmail(data[field])) {
          errors.push(`Invalid email format for ${field}`);
        } else if (type === 'phone' && !this.validatePhone(data[field])) {
          errors.push(`Invalid phone format for ${field}`);
        } else if (type === 'number' && isNaN(data[field])) {
          errors.push(`${field} must be a number`);
        }
      }
    });

    return errors;
  }
};

// Export configuration and helpers
window.productionConfig = {
  dataSchemas,
  zambiaConfig,
  systemConfig,
  DataValidator
};

// Helper functions for data operations
window.dataHelpers = {
  // Format currency for Zambia
  formatCurrency(amount) {
    return new Intl.NumberFormat('en-ZM', {
      style: 'currency',
      currency: 'ZMW'
    }).format(amount);
  },

  // Format date for Zambia
  formatDate(date) {
    return new Intl.DateTimeFormat('en-ZM', {
      timeZone: zambiaConfig.timezone,
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    }).format(new Date(date));
  },

  // Generate unique ID
  generateId() {
    return Date.now().toString(36) + Math.random().toString(36).substr(2);
  },

  // Validate patient data
  validatePatient(patient) {
    return DataValidator.validateSchema(patient, dataSchemas.patient);
  },

  // Validate product data
  validateProduct(product) {
    return DataValidator.validateSchema(product, dataSchemas.product);
  },

  // Get system settings
  getSystemSettings() {
    return systemConfig;
  },

  // Get Zambian configuration
  getZambiaConfig() {
    return zambiaConfig;
  }
};

console.log('🇿🇲 Umi Health Production Configuration Loaded');
console.log('📊 Data schemas and validation helpers available');
console.log('⚙️ System configuration loaded for Zambia');
