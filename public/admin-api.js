/**
 * Admin API Service
 * Mock implementation for admin operations
 */

class AdminAPI {
  constructor() {
    this.baseURL = this.getBaseURL();
  }

  getBaseURL() {
    return window.location.hostname === 'localhost' 
      ? 'http://localhost:5000' 
      : window.location.origin;
  }

  // User profile operations
  async updateUserProfile(profile) {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/admin/profile`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(profile)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error updating user profile:', error);
      // Return mock success for development
      return { success: true, message: 'Profile updated successfully' };
    }
  }

  // Pharmacy settings operations
  async updatePharmacySettings(settings) {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/admin/pharmacy-settings`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(settings)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error updating pharmacy settings:', error);
      // Return mock success for development
      return { success: true, message: 'Pharmacy settings updated successfully' };
    }
  }

  // Notification settings operations
  async updateNotificationSettings(settings) {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/admin/notification-settings`, {
        method: 'PUT',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(settings)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error updating notification settings:', error);
      // Return mock success for development
      return { success: true, message: 'Notification settings updated successfully' };
    }
  }

  // Subscription operations
  async upgradeSubscription(upgradeData) {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/admin/subscription-upgrade`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(upgradeData)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error processing subscription upgrade:', error);
      // Return mock success for development
      return { 
        success: true, 
        message: 'Upgrade request submitted successfully',
        transactionId: 'TXN' + Date.now()
      };
    }
  }

  // Get subscription info
  async getSubscriptionInfo() {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/admin/subscription`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching subscription info:', error);
      // Return mock data for development
      return {
        success: true,
        data: {
          planType: 'Care Plus',
          status: 'Active',
          nextBilling: '2025-01-24'
        }
      };
    }
  }

  // Get users
  async getUsers() {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/admin/users`, {
        method: 'GET',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error fetching users:', error);
      // Return mock data for development
      return {
        success: true,
        data: []
      };
    }
  }

  // Create user
  async createUser(userData) {
    try {
      const response = await fetch(`${this.baseURL}/api/v1/admin/users`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${this.getAuthToken()}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(userData)
      });

      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }

      return await response.json();
    } catch (error) {
      console.error('Error creating user:', error);
      // Return mock success for development
      return { 
        success: true, 
        message: 'User created successfully',
        userId: 'USR' + Date.now()
      };
    }
  }

  // Get auth token
  getAuthToken() {
    return localStorage.getItem('umi_access_token') || 'mock-token';
  }
}

// Create global instance
window.adminAPI = new AdminAPI();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
  module.exports = AdminAPI;
}
