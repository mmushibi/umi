/**
 * Data Synchronization Service
 * Handles real-time data synchronization between client and server
 */

class DataSyncService {
  constructor() {
    this.baseURL = this.getBaseURL();
    this.eventListeners = new Map();
    this.syncInterval = null;
    this.isOnline = navigator.onLine;
    this.pendingSync = [];
    
    this.setupEventListeners();
  }

  getBaseURL() {
    return window.location.hostname === 'localhost' 
      ? 'http://localhost:5000' 
      : window.location.origin;
  }

  setupEventListeners() {
    // Network status monitoring
    window.addEventListener('online', () => {
      this.isOnline = true;
      this.processPendingSync();
    });

    window.addEventListener('offline', () => {
      this.isOnline = false;
    });

    // Auto-sync every 30 seconds
    this.syncInterval = setInterval(() => {
      if (this.isOnline) {
        this.autoSync();
      }
    }, 30000);
  }

  // Get data from server
  async getData(key, defaultValue = null) {
    try {
      if (!this.isOnline) {
        return this.getFromLocalStorage(key, defaultValue);
      }

      const response = await fetch(`${this.baseURL}/api/v1/data/${key}`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const data = await response.json();
      this.saveToLocalStorage(key, data);
      this.emit('data-updated', { key, data });
      
      return data;
    } catch (error) {
      console.error(`Error fetching data for ${key}:`, error);
      return this.getFromLocalStorage(key, defaultValue);
    }
  }

  // Save data to server
  async saveData(key, data) {
    try {
      if (!this.isOnline) {
        this.pendingSync.push({ key, data, timestamp: Date.now() });
        this.saveToLocalStorage(key, data);
        return { success: true, offline: true };
      }

      const response = await fetch(`${this.baseURL}/api/v1/data/${key}`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      const result = await response.json();
      this.saveToLocalStorage(key, data);
      this.emit('data-saved', { key, data });
      
      return result;
    } catch (error) {
      console.error(`Error saving data for ${key}:`, error);
      this.pendingSync.push({ key, data, timestamp: Date.now() });
      this.saveToLocalStorage(key, data);
      return { success: false, error: error.message, offline: true };
    }
  }

  // Process pending sync when back online
  async processPendingSync() {
    if (this.pendingSync.length === 0) return;

    console.log(`Processing ${this.pendingSync.length} pending sync operations`);
    
    const syncPromises = this.pendingSync.map(async ({ key, data }) => {
      try {
        await this.saveData(key, data);
        return { key, success: true };
      } catch (error) {
        return { key, success: false, error: error.message };
      }
    });

    const results = await Promise.allSettled(syncPromises);
    
    // Remove successful syncs from pending
    this.pendingSync = this.pendingSync.filter((_, index) => 
      results[index].status === 'rejected' || !results[index].value.success
    );

    this.emit('sync-completed', { 
      processed: results.length, 
      remaining: this.pendingSync.length 
    });
  }

  // Auto-sync critical data
  async autoSync() {
    const criticalKeys = ['userProfile', 'pharmacySettings', 'notificationSettings'];
    
    for (const key of criticalKeys) {
      try {
        await this.getData(key);
      } catch (error) {
        console.warn(`Auto-sync failed for ${key}:`, error);
      }
    }
  }

  // Local storage helpers
  saveToLocalStorage(key, data) {
    try {
      localStorage.setItem(`umi_${key}`, JSON.stringify(data));
    } catch (error) {
      console.error(`Error saving to localStorage for ${key}:`, error);
    }
  }

  getFromLocalStorage(key, defaultValue = null) {
    try {
      const item = localStorage.getItem(`umi_${key}`);
      return item ? JSON.parse(item) : defaultValue;
    } catch (error) {
      console.error(`Error reading from localStorage for ${key}:`, error);
      return defaultValue;
    }
  }

  // Get auth token
  getAuthToken() {
    return localStorage.getItem('umi_access_token') || 'mock-token';
  }

  // Event system
  on(event, callback) {
    if (!this.eventListeners.has(event)) {
      this.eventListeners.set(event, []);
    }
    this.eventListeners.get(event).push(callback);
  }

  off(event, callback) {
    if (this.eventListeners.has(event)) {
      const listeners = this.eventListeners.get(event);
      const index = listeners.indexOf(callback);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    }
  }

  emit(event, data) {
    if (this.eventListeners.has(event)) {
      this.eventListeners.get(event).forEach(callback => {
        try {
          callback(data);
        } catch (error) {
          console.error(`Error in event listener for ${event}:`, error);
        }
      });
    }
  }

  // Cleanup
  destroy() {
    if (this.syncInterval) {
      clearInterval(this.syncInterval);
    }
    this.eventListeners.clear();
    this.pendingSync = [];
  }
}

// Create global instance
window.adminDataSync = new DataSyncService();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
  module.exports = DataSyncService;
}
