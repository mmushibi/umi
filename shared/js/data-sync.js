/**
 * Shared Data Sync System
 * Centralized data management for Umi Health System
 * Handles real-time data synchronization across all portals
 */

class DataSync {
  constructor() {
    this.data = {
      // Users and authentication
      currentUser: null,
      users: [],
      
      // Pharmacy settings
      pharmacySettings: this.loadPharmacySettings(),
      
      // Core data entities
      patients: [],
      prescriptions: [],
      products: [],
      inventory: [],
      sales: [],
      payments: [],
      reports: [],
      
      // User management
      employees: [],
      leaveRequests: [],
      performanceReviews: [],
      
      // System data
      transactions: [],
      notifications: [],
      auditLogs: []
    };
    
    this.subscribers = new Map();
    this.init();
  }

  /**
   * Initialize the data sync system
   */
  init() {
    this.loadFromLocalStorage();
    this.setupEventListeners();
    this.startAutoSave();
  }

  /**
   * Load pharmacy settings from localStorage
   */
  loadPharmacySettings() {
    const stored = localStorage.getItem('pharmacySettings');
    return stored ? JSON.parse(stored) : this.getDefaultPharmacySettings();
  }

  /**
   * Get default pharmacy settings
   */
  getDefaultPharmacySettings() {
    return {
      name: 'Umi Health Pharmacy',
      phone: '+260 977 123 456',
      email: 'info@umihealth.com',
      website: 'www.umihealth.com',
      address: '123 Main Street',
      city: 'Lusaka',
      province: 'Lusaka Province',
      postalCode: '10101',
      receiptHeader: 'Umi Health Pharmacy',
      receiptFooter: 'Thank you for your business!',
      taxRate: 16,
      currency: 'ZMW',
      reportLogo: '',
      reportSignature: 'Pharmacy Manager',
      paymentTerms: 'Payment due upon receipt',
      invoicePrefix: 'INV',
      receiptPrefix: 'RCP'
    };
  }

  /**
   * Load data from localStorage
   */
  loadFromLocalStorage() {
    Object.keys(this.data).forEach(key => {
      if (key !== 'pharmacySettings') {
        const stored = localStorage.getItem(`umi_${key}`);
        if (stored) {
          try {
            this.data[key] = JSON.parse(stored);
          } catch (e) {
            console.warn(`Failed to load ${key} from localStorage:`, e);
            this.data[key] = [];
          }
        }
      }
    });
  }

  /**
   * Save data to localStorage
   */
  saveToLocalStorage(key = null) {
    if (key) {
      localStorage.setItem(`umi_${key}`, JSON.stringify(this.data[key]));
    } else {
      Object.keys(this.data).forEach(dataKey => {
        if (dataKey !== 'pharmacySettings') {
          localStorage.setItem(`umi_${dataKey}`, JSON.stringify(this.data[dataKey]));
        }
      });
    }
  }

  /**
   * Setup event listeners for cross-tab synchronization
   */
  setupEventListeners() {
    window.addEventListener('storage', (e) => {
      if (e.key && e.key.startsWith('umi_')) {
        const dataKey = e.key.replace('umi_', '');
        if (e.newValue) {
          try {
            this.data[dataKey] = JSON.parse(e.newValue);
            this.notifySubscribers(dataKey, this.data[dataKey]);
          } catch (error) {
            console.error(`Failed to parse ${dataKey} from storage event:`, error);
          }
        }
      }
    });
  }

  /**
   * Start auto-save timer
   */
  startAutoSave() {
    setInterval(() => {
      this.saveToLocalStorage();
    }, 30000); // Auto-save every 30 seconds
  }

  /**
   * Subscribe to data changes
   */
  subscribe(key, callback) {
    if (!this.subscribers.has(key)) {
      this.subscribers.set(key, new Set());
    }
    this.subscribers.get(key).add(callback);
    
    // Return unsubscribe function
    return () => {
      const callbacks = this.subscribers.get(key);
      if (callbacks) {
        callbacks.delete(callback);
        if (callbacks.size === 0) {
          this.subscribers.delete(key);
        }
      }
    };
  }

  /**
   * Notify subscribers of data changes
   */
  notifySubscribers(key, data) {
    const callbacks = this.subscribers.get(key);
    if (callbacks) {
      callbacks.forEach(callback => {
        try {
          callback(data);
        } catch (error) {
          console.error(`Error in subscriber callback for ${key}:`, error);
        }
      });
    }
  }

  /**
   * Get data by key
   */
  get(key) {
    return this.data[key];
  }

  /**
   * Set data by key
   */
  set(key, value) {
    this.data[key] = value;
    this.saveToLocalStorage(key);
    this.notifySubscribers(key, value);
  }

  /**
   * Add item to array data
   */
  add(key, item) {
    if (!Array.isArray(this.data[key])) {
      this.data[key] = [];
    }
    
    // Add ID and timestamp if not present
    if (!item.id) {
      item.id = this.generateId(key);
    }
    if (!item.createdAt) {
      item.createdAt = new Date().toISOString();
    }
    if (!item.updatedAt) {
      item.updatedAt = new Date().toISOString();
    }
    
    this.data[key].push(item);
    this.saveToLocalStorage(key);
    this.notifySubscribers(key, this.data[key]);
    
    return item;
  }

  /**
   * Update item in array data
   */
  update(key, id, updates) {
    if (!Array.isArray(this.data[key])) {
      return null;
    }
    
    const index = this.data[key].findIndex(item => item.id === id);
    if (index !== -1) {
      this.data[key][index] = {
        ...this.data[key][index],
        ...updates,
        updatedAt: new Date().toISOString()
      };
      this.saveToLocalStorage(key);
      this.notifySubscribers(key, this.data[key]);
      return this.data[key][index];
    }
    
    return null;
  }

  /**
   * Delete item from array data
   */
  delete(key, id) {
    if (!Array.isArray(this.data[key])) {
      return false;
    }
    
    const index = this.data[key].findIndex(item => item.id === id);
    if (index !== -1) {
      this.data[key].splice(index, 1);
      this.saveToLocalStorage(key);
      this.notifySubscribers(key, this.data[key]);
      return true;
    }
    
    return false;
  }

  /**
   * Find item by ID
   */
  find(key, id) {
    if (!Array.isArray(this.data[key])) {
      return null;
    }
    
    return this.data[key].find(item => item.id === id) || null;
  }

  /**
   * Filter items by criteria
   */
  filter(key, criteria) {
    if (!Array.isArray(this.data[key])) {
      return [];
    }
    
    return this.data[key].filter(item => {
      return Object.keys(criteria).every(key => {
        if (typeof criteria[key] === 'function') {
          return criteria[key](item[key]);
        }
        return item[key] === criteria[key];
      });
    });
  }

  /**
   * Generate unique ID
   */
  generateId(prefix) {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 10000).toString().padStart(4, '0');
    return `${prefix}_${timestamp}_${random}`;
  }

  /**
   * Set current user
   */
  setCurrentUser(user) {
    this.data.currentUser = user;
    localStorage.setItem('umi_currentUser', JSON.stringify(user));
    this.notifySubscribers('currentUser', user);
  }

  /**
   * Get current user
   */
  getCurrentUser() {
    return this.data.currentUser;
  }

  /**
   * Login user
   */
  login(email, password) {
    // This would typically make an API call
    // For now, we'll simulate with stored users
    const user = this.data.users.find(u => u.email === email && u.password === password);
    if (user) {
      this.setCurrentUser({
        ...user,
        lastLogin: new Date().toISOString()
      });
      return user;
    }
    return null;
  }

  /**
   * Logout user
   */
  logout() {
    this.data.currentUser = null;
    localStorage.removeItem('umi_currentUser');
    this.notifySubscribers('currentUser', null);
  }

  /**
   * Check user permissions
   */
  hasPermission(permission) {
    const user = this.getCurrentUser();
    if (!user) return false;
    
    // Super Admin, Operations, and Tenant Admin have all permissions within their scope
    if (user.role === 'super_admin' || user.role === 'operations' || user.role === 'tenant_admin') return true;
    
    // Check specific permissions based on role
    const rolePermissions = {
      pharmacist: ['view_patients', 'manage_prescriptions', 'view_inventory', 'view_reports'],
      cashier: ['view_patients', 'process_sales', 'view_inventory', 'process_payments']
    };
    
    return rolePermissions[user.role]?.includes(permission) || false;
  }

  /**
   * Add audit log entry
   */
  addAuditLog(action, details) {
    const user = this.getCurrentUser();
    this.add('auditLogs', {
      action,
      details,
      userId: user?.id,
      userName: user?.name,
      timestamp: new Date().toISOString()
    });
  }

  /**
   * Export data for backup
   */
  exportData() {
    return {
      ...this.data,
      exportDate: new Date().toISOString(),
      version: '1.0'
    };
  }

  /**
   * Import data from backup
   */
  importData(backupData) {
    Object.keys(backupData).forEach(key => {
      if (key !== 'exportDate' && key !== 'version') {
        this.data[key] = backupData[key];
        this.saveToLocalStorage(key);
        this.notifySubscribers(key, this.data[key]);
      }
    });
  }
}

// Create global instance
window.dataSync = new DataSync();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
  module.exports = DataSync;
}
