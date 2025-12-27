/**
 * Umi Health Data Manager
 * Shared data persistence and synchronization system
 * Works across all portals with localStorage fallback and API integration
 */
class UmiDataManager {
  constructor(portalType, apiEndpoint) {
    this.portalType = portalType; // 'admin', 'pharmacist', 'cashier'
    this.apiEndpoint = apiEndpoint;
    this.storageKey = `umi_${portalType}_data`;
    this.autoSaveInterval = 30000; // 30 seconds
    this.lastSaveTime = null;
    this.isOnline = navigator.onLine;
    this.syncQueue = [];
    
    // Setup online/offline detection
    this.setupConnectivityListeners();
  }

  // Setup connectivity listeners
  setupConnectivityListeners() {
    window.addEventListener('online', () => {
      this.isOnline = true;
      console.log('Back online - processing sync queue');
      this.processSyncQueue();
    });
    
    window.addEventListener('offline', () => {
      this.isOnline = false;
      console.log('Gone offline - using localStorage only');
    });
  }

  // Save data with fallback strategy
  async save(data, action = 'create') {
    try {
      // Add metadata
      const dataWithMeta = {
        ...data,
        portalType: this.portalType,
        action: action,
        timestamp: new Date().toISOString(),
        deviceId: this.getDeviceId()
      };

      if (this.isOnline) {
        try {
          // Try API first
          const result = await this.saveToAPI(dataWithMeta);
          this.showSaveIndicator('Saved to server', 'success');
          return result;
        } catch (apiError) {
          console.warn('API save failed, using localStorage:', apiError);
          // Add to sync queue for later
          this.syncQueue.push(dataWithMeta);
          return this.saveToLocalStorage(dataWithMeta);
        }
      } else {
        // Offline - save to localStorage and queue for sync
        this.syncQueue.push(dataWithMeta);
        return this.saveToLocalStorage(dataWithMeta);
      }
    } catch (error) {
      console.error('Save failed:', error);
      this.showSaveIndicator('Save failed', 'error');
      throw error;
    }
  }

  // Load data with smart caching
  async load(forceRefresh = false) {
    try {
      if (this.isOnline && !forceRefresh) {
        // Try API first with cache fallback
        try {
          const serverData = await this.loadFromAPI();
          // Cache the server data
          this.saveToLocalStorage(serverData);
          return serverData;
        } catch (apiError) {
          console.warn('API load failed, using cache:', apiError);
          return this.loadFromLocalStorage();
        }
      } else {
        // Force refresh or offline - use cache
        return this.loadFromLocalStorage();
      }
    } catch (error) {
      console.error('Load failed:', error);
      return [];
    }
  }

  // API methods
  async saveToAPI(data) {
    const response = await fetch(this.apiEndpoint, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'X-Portal-Type': this.portalType
      },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  async loadFromAPI() {
    const response = await fetch(`${this.apiEndpoint}?portal=${this.portalType}&limit=100`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'X-Portal-Type': this.portalType
      }
    });

    if (!response.ok) {
      throw new Error(`HTTP error! status: ${response.status}`);
    }

    const result = await response.json();
    return result.data || result;
  }

  async updateItem(id, updates) {
    const updateData = {
      id,
      ...updates,
      portalType: this.portalType,
      action: 'update',
      timestamp: new Date().toISOString()
    };

    if (this.isOnline) {
      try {
        const response = await fetch(`${this.apiEndpoint}/${id}`, {
          method: 'PUT',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${this.getAuthToken()}`,
            'X-Portal-Type': this.portalType
          },
          body: JSON.stringify(updateData)
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        return await response.json();
      } catch (error) {
        this.syncQueue.push(updateData);
        throw error;
      }
    } else {
      this.syncQueue.push(updateData);
      return this.saveToLocalStorage(updateData);
    }
  }

  async deleteItem(id) {
    const deleteData = {
      id,
      portalType: this.portalType,
      action: 'delete',
      timestamp: new Date().toISOString()
    };

    if (this.isOnline) {
      try {
        const response = await fetch(`${this.apiEndpoint}/${id}`, {
          method: 'DELETE',
          headers: {
            'Authorization': `Bearer ${this.getAuthToken()}`,
            'X-Portal-Type': this.portalType
          }
        });

        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        return true;
      } catch (error) {
        this.syncQueue.push(deleteData);
        throw error;
      }
    } else {
      this.syncQueue.push(deleteData);
      return true;
    }
  }

  // LocalStorage methods
  saveToLocalStorage(data) {
    try {
      const currentData = this.loadFromLocalStorage();
      const updatedData = Array.isArray(data) ? data : [...currentData, data];
      
      localStorage.setItem(this.storageKey, JSON.stringify({
        data: updatedData,
        lastSaved: new Date().toISOString(),
        version: '1.0',
        portalType: this.portalType
      }));
      
      this.lastSaveTime = new Date();
      this.showSaveIndicator('Saved locally', 'info');
      return data;
    } catch (error) {
      console.error('localStorage save failed:', error);
      throw error;
    }
  }

  loadFromLocalStorage() {
    try {
      const saved = localStorage.getItem(this.storageKey);
      if (saved) {
        const parsed = JSON.parse(saved);
        console.log(`Loaded ${parsed.data?.length || 0} items from localStorage for ${this.portalType}`);
        return parsed.data || [];
      }
    } catch (error) {
      console.error('localStorage load failed:', error);
    }
    return [];
  }

  // Sync queue management
  async processSyncQueue() {
    if (this.syncQueue.length === 0 || !this.isOnline) return;

    console.log(`Processing ${this.syncQueue.length} items in sync queue`);
    
    const failedItems = [];
    
    for (const item of this.syncQueue) {
      try {
        await this.saveToAPI(item);
      } catch (error) {
        console.error('Sync failed for item:', item, error);
        failedItems.push(item);
      }
    }
    
    // Update sync queue with only failed items
    this.syncQueue = failedItems;
    
    if (failedItems.length === 0) {
      this.showSaveIndicator('All items synced', 'success');
    } else {
      this.showSaveIndicator(`${failedItems.length} items failed to sync`, 'warning');
    }
  }

  // Auto-save setup
  setupAutoSave(dataGetter) {
    setInterval(() => {
      const currentData = dataGetter();
      if (currentData && currentData.length > 0) {
        this.saveToLocalStorage(currentData);
        this.showSaveIndicator('Auto-saved', 'info');
      }
    }, this.autoSaveInterval);
  }

  // Manual sync
  async syncWithServer() {
    try {
      this.showSaveIndicator('Syncing...', 'info');
      
      // Process any pending sync queue items
      await this.processSyncQueue();
      
      // Get fresh data from server
      const serverData = await this.loadFromAPI();
      
      // Save fresh data to localStorage
      this.saveToLocalStorage(serverData);
      
      this.showSaveIndicator('Sync complete', 'success');
      return serverData;
    } catch (error) {
      console.error('Sync failed:', error);
      this.showSaveIndicator('Sync failed', 'error');
      throw error;
    }
  }

  // Export data
  exportData(format = 'json') {
    const data = this.loadFromLocalStorage();
    
    if (format === 'csv') {
      return this.convertToCSV(data);
    } else if (format === 'json') {
      return JSON.stringify(data, null, 2);
    }
    
    return data;
  }

  convertToCSV(data) {
    if (!Array.isArray(data) || data.length === 0) return '';
    
    const headers = Object.keys(data[0]);
    const csvRows = [headers.join(',')];
    
    for (const row of data) {
      const values = headers.map(header => {
        const value = row[header];
        return typeof value === 'string' ? `"${value}"` : value;
      });
      csvRows.push(values.join(','));
    }
    
    return csvRows.join('\n');
  }

  // Utility methods
  getAuthToken() {
    return localStorage.getItem('umi_auth_token') || 
           sessionStorage.getItem('umi_auth_token') || 
           '';
  }

  getDeviceId() {
    let deviceId = localStorage.getItem('umi_device_id');
    if (!deviceId) {
      deviceId = 'device_' + Math.random().toString(36).substr(2, 9);
      localStorage.setItem('umi_device_id', deviceId);
    }
    return deviceId;
  }

  showSaveIndicator(message, type = 'info') {
    // Remove existing indicators
    const existing = document.querySelector('.save-indicator');
    if (existing) {
      existing.remove();
    }

    const indicator = document.createElement('div');
    indicator.className = 'save-indicator';
    indicator.textContent = message;
    
    const colors = {
      success: 'var(--color-success)',
      error: 'var(--color-warning)',
      info: 'var(--color-accent)',
      warning: '#f59e0b'
    };
    
    indicator.style.cssText = `
      position: fixed;
      bottom: 20px;
      right: 20px;
      background: ${colors[type]};
      color: white;
      padding: 0.75rem 1.25rem;
      border-radius: 8px;
      font-size: 0.875rem;
      font-weight: 500;
      z-index: 10000;
      opacity: 0;
      transform: translateY(20px);
      transition: all 0.3s ease;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
    `;
    
    document.body.appendChild(indicator);
    
    // Animate in
    setTimeout(() => {
      indicator.style.opacity = '1';
      indicator.style.transform = 'translateY(0)';
    }, 100);
    
    // Remove after 3 seconds
    setTimeout(() => {
      indicator.style.opacity = '0';
      indicator.style.transform = 'translateY(20px)';
      setTimeout(() => {
        if (indicator.parentNode) {
          document.body.removeChild(indicator);
        }
      }, 300);
    }, 3000);
  }

  // Get sync status
  getSyncStatus() {
    return {
      isOnline: this.isOnline,
      lastSaveTime: this.lastSaveTime,
      pendingSyncItems: this.syncQueue.length,
      storageKey: this.storageKey
    };
  }
}

// Export for use in other files
if (typeof module !== 'undefined' && module.exports) {
  module.exports = UmiDataManager;
}
