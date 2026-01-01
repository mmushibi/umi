/**
 * Umi Health Data Manager
 * Centralized data management and synchronization across cashier portal pages
 */
class UmiDataManager {
  constructor() {
    this.currentUser = null;
    this.currentShift = null;
    this.cache = new Map();
    this.subscribers = new Map();
    this.lastSync = new Map();
    this.syncInterval = 30000; // 30 seconds
    
    // Initialize user data
    this.initUserData();
    
    // Start periodic sync
    this.startPeriodicSync();
    
    // Listen for storage events (cross-tab synchronization)
    window.addEventListener('storage', (e) => this.handleStorageEvent(e));
  }

  /**
   * Initialize current user data
   */
  initUserData() {
    const userId = localStorage.getItem('umi_user_id') || 'cashier_001';
    const tenantId = localStorage.getItem('umi_tenant_id') || 'tenant_001';
    const branchId = localStorage.getItem('umi_branch_id') || 'branch_001';
    
    this.currentUser = {
      id: userId,
      tenantId: tenantId,
      branchId: branchId,
      name: localStorage.getItem('umi_user_name') || 'Cashier',
      role: localStorage.getItem('umi_user_role') || 'Cashier',
      shiftId: localStorage.getItem('umi_current_shift_id') || null
    };
  }

  /**
   * Get current user information
   */
  getCurrentUser() {
    return this.currentUser;
  }

  /**
   * Set current shift
   */
  setCurrentShift(shift) {
    this.currentShift = shift;
    if (shift && shift.id) {
      localStorage.setItem('umi_current_shift_id', shift.id);
      this.currentUser.shiftId = shift.id;
    }
    this.notifySubscribers('shift', shift);
  }

  /**
   * Get current shift
   */
  getCurrentShift() {
    return this.currentShift;
  }

  /**
   * Cache data with timestamp
   */
  setCache(key, data, ttl = 300000) { // 5 minutes default TTL
    this.cache.set(key, {
      data: data,
      timestamp: Date.now(),
      ttl: ttl
    });
    this.lastSync.set(key, Date.now());
  }

  /**
   * Get cached data if still valid
   */
  getCache(key) {
    const cached = this.cache.get(key);
    if (!cached) return null;
    
    if (Date.now() - cached.timestamp > cached.ttl) {
      this.cache.delete(key);
      return null;
    }
    
    return cached.data;
  }

  /**
   * Clear cache
   */
  clearCache(key = null) {
    if (key) {
      this.cache.delete(key);
      this.lastSync.delete(key);
    } else {
      this.cache.clear();
      this.lastSync.clear();
    }
  }

  /**
   * Subscribe to data changes
   */
  subscribe(key, callback) {
    if (!this.subscribers.has(key)) {
      this.subscribers.set(key, []);
    }
    this.subscribers.get(key).push(callback);
    
    // Return unsubscribe function
    return () => {
      const callbacks = this.subscribers.get(key);
      if (callbacks) {
        const index = callbacks.indexOf(callback);
        if (index > -1) {
          callbacks.splice(index, 1);
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
      callbacks.forEach(callback => callback(data));
    }
  }

  /**
   * Handle cross-tab storage events
   */
  handleStorageEvent(event) {
    if (event.key && event.key.startsWith('umi_data_')) {
      const key = event.key.replace('umi_data_', '');
      if (event.newValue) {
        try {
          const data = JSON.parse(event.newValue);
          this.setCache(key, data);
          this.notifySubscribers(key, data);
        } catch (e) {
          console.error('Error parsing storage event data:', e);
        }
      }
    }
  }

  /**
   * Save data to localStorage for cross-tab sync
   */
  saveToStorage(key, data) {
    try {
      localStorage.setItem(`umi_data_${key}`, JSON.stringify({
        data: data,
        timestamp: Date.now(),
        userId: this.currentUser.id
      }));
    } catch (e) {
      console.error('Error saving to storage:', e);
    }
  }

  /**
   * Get sales data from various sources
   */
  async getSalesData(filters = {}) {
    const cacheKey = `sales_${JSON.stringify(filters)}`;
    const cached = this.getCache(cacheKey);
    
    if (cached) {
      return cached;
    }

    try {
      // Fetch from API
      const salesData = await window.cashierAPI.getSalesReport({
        startDate: filters.startDate || '',
        endDate: filters.endDate || '',
        shift: filters.shift || '',
        userId: this.currentUser.id,
        tenantId: this.currentUser.tenantId,
        branchId: this.currentUser.branchId
      });

      // Add local sales from localStorage (from other pages)
      const localSales = this.getLocalSales();
      const combinedData = this.combineSalesData(salesData, localSales);

      this.setCache(cacheKey, combinedData);
      this.saveToStorage(cacheKey, combinedData);
      this.notifySubscribers('sales', combinedData);

      return combinedData;
    } catch (error) {
      console.error('Error fetching sales data:', error);
      // Return local data as fallback
      return this.getLocalSales();
    }
  }

  /**
   * Get inventory data
   */
  async getInventoryData(filters = {}) {
    const cacheKey = `inventory_${JSON.stringify(filters)}`;
    const cached = this.getCache(cacheKey);
    
    if (cached) {
      return cached;
    }

    try {
      const inventoryData = await window.cashierAPI.getInventoryReport({
        startDate: filters.startDate || '',
        endDate: filters.endDate || '',
        tenantId: this.currentUser.tenantId,
        branchId: this.currentUser.branchId
      });

      // Add local inventory updates
      const localInventory = this.getLocalInventory();
      const combinedData = this.combineInventoryData(inventoryData, localInventory);

      this.setCache(cacheKey, combinedData);
      this.saveToStorage(cacheKey, combinedData);
      this.notifySubscribers('inventory', combinedData);

      return combinedData;
    } catch (error) {
      console.error('Error fetching inventory data:', error);
      return this.getLocalInventory();
    }
  }

  /**
   * Get patient data
   */
  async getPatientData(filters = {}) {
    const cacheKey = `patients_${JSON.stringify(filters)}`;
    const cached = this.getCache(cacheKey);
    
    if (cached) {
      return cached;
    }

    try {
      const patientData = await window.cashierAPI.getCustomerReport({
        startDate: filters.startDate || '',
        endDate: filters.endDate || '',
        customerType: 'patient',
        tenantId: this.currentUser.tenantId,
        branchId: this.currentUser.branchId
      });

      // Add local patient data
      const localPatients = this.getLocalPatients();
      const combinedData = this.combinePatientData(patientData, localPatients);

      this.setCache(cacheKey, combinedData);
      this.saveToStorage(cacheKey, combinedData);
      this.notifySubscribers('patients', combinedData);

      return combinedData;
    } catch (error) {
      console.error('Error fetching patient data:', error);
      return this.getLocalPatients();
    }
  }

  /**
   * Get local sales data from localStorage (from other pages)
   */
  getLocalSales() {
    try {
      const salesData = localStorage.getItem('umi_local_sales');
      return salesData ? JSON.parse(salesData) : [];
    } catch (e) {
      return [];
    }
  }

  /**
   * Get local inventory data
   */
  getLocalInventory() {
    try {
      const inventoryData = localStorage.getItem('umi_local_inventory');
      return inventoryData ? JSON.parse(inventoryData) : [];
    } catch (e) {
      return [];
    }
  }

  /**
   * Get local patient data
   */
  getLocalPatients() {
    try {
      const patientData = localStorage.getItem('umi_local_patients');
      return patientData ? JSON.parse(patientData) : [];
    } catch (e) {
      return [];
    }
  }

  /**
   * Combine API data with local data
   */
  combineSalesData(apiData, localData) {
    const combined = [...(apiData.data || apiData || [])];
    
    // Add local data that's not already in API data
    localData.forEach(localSale => {
      if (!combined.find(sale => sale.id === localSale.id)) {
        combined.push(localSale);
      }
    });

    return combined;
  }

  /**
   * Combine inventory data
   */
  combineInventoryData(apiData, localData) {
    const combined = [...(apiData.data || apiData || [])];
    
    // Update inventory with local changes
    localData.forEach(localItem => {
      const existingIndex = combined.findIndex(item => item.id === localItem.id);
      if (existingIndex > -1) {
        combined[existingIndex] = { ...combined[existingIndex], ...localItem };
      } else {
        combined.push(localItem);
      }
    });

    return combined;
  }

  /**
   * Combine patient data
   */
  combinePatientData(apiData, localData) {
    const combined = [...(apiData.data || apiData || [])];
    
    // Add local patients that aren't in API data
    localData.forEach(localPatient => {
      if (!combined.find(patient => patient.id === localPatient.id)) {
        combined.push(localPatient);
      }
    });

    return combined;
  }

  /**
   * Add local sale (from other pages)
   */
  addLocalSale(sale) {
    const localSales = this.getLocalSales();
    sale.userId = this.currentUser.id;
    sale.timestamp = Date.now();
    localSales.push(sale);
    localStorage.setItem('umi_local_sales', JSON.stringify(localSales));
    
    // Clear cache to force refresh
    this.clearCache('sales');
    this.notifySubscribers('sales', this.getLocalSales());
  }

  /**
   * Update local inventory
   */
  updateLocalInventory(item) {
    const localInventory = this.getLocalInventory();
    const existingIndex = localInventory.findIndex(inv => inv.id === item.id);
    
    if (existingIndex > -1) {
      localInventory[existingIndex] = { ...localInventory[existingIndex], ...item };
    } else {
      localInventory.push(item);
    }
    
    localStorage.setItem('umi_local_inventory', JSON.stringify(localInventory));
    this.clearCache('inventory');
    this.notifySubscribers('inventory', localInventory);
  }

  /**
   * Add local patient
   */
  addLocalPatient(patient) {
    const localPatients = this.getLocalPatients();
    patient.userId = this.currentUser.id;
    patient.timestamp = Date.now();
    localPatients.push(patient);
    localStorage.setItem('umi_local_patients', JSON.stringify(localPatients));
    
    this.clearCache('patients');
    this.notifySubscribers('patients', localPatients);
  }

  /**
   * Get summary data for reports
   */
  async getSummaryData(filters = {}) {
    try {
      const [salesData, inventoryData, patientData] = await Promise.all([
        this.getSalesData(filters),
        this.getInventoryData(filters),
        this.getPatientData(filters)
      ]);

      // Calculate summary metrics
      const totalSales = salesData.reduce((sum, sale) => sum + (sale.totalAmount || 0), 0);
      const totalTransactions = salesData.length;
      const itemsSold = salesData.reduce((sum, sale) => sum + (sale.items || 0), 0);
      const averageTransaction = totalTransactions > 0 ? totalSales / totalTransactions : 0;

      return {
        totalSales,
        totalTransactions,
        itemsSold,
        averageTransaction,
        salesGrowth: 0, // Would be calculated based on historical data
        transactionsGrowth: 0,
        itemsGrowth: 0,
        averageGrowth: 0
      };
    } catch (error) {
      console.error('Error calculating summary data:', error);
      return {
        totalSales: 0,
        totalTransactions: 0,
        itemsSold: 0,
        averageTransaction: 0,
        salesGrowth: 0,
        transactionsGrowth: 0,
        itemsGrowth: 0,
        averageGrowth: 0
      };
    }
  }

  /**
   * Start periodic data synchronization
   */
  startPeriodicSync() {
    setInterval(() => {
      this.syncAllData();
    }, this.syncInterval);
  }

  /**
   * Sync all data
   */
  async syncAllData() {
    try {
      await Promise.all([
        this.getSalesData(),
        this.getInventoryData(),
        this.getPatientData()
      ]);
    } catch (error) {
      console.error('Error during periodic sync:', error);
    }
  }

  /**
   * Force refresh all data
   */
  async refreshAll() {
    this.clearCache();
    return await this.syncAllData();
  }
}

// Initialize global data manager
window.umiDataManager = new UmiDataManager();
