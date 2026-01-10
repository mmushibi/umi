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
      ? 'http://localhost:5001' 
      : window.location.origin;
  }

  // User profile operations
  async updateUserProfile(profile) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/profile`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(profile)
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  // Pharmacy settings operations
  async updatePharmacySettings(settings) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/settings`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(settings)
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  // Notification settings operations
  async updateNotificationSettings(settings) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/settings`, {
      method: 'PUT',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(settings)
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  // Subscription operations
  async upgradeSubscription(upgradeData) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/subscription-upgrade`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(upgradeData)
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  // Get subscription info
  async getSubscriptionInfo() {
    const response = await fetch(`${this.baseURL}/api/v1/admin/settings`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  // Get users
  async getUsers() {
    const response = await fetch(`${this.baseURL}/api/v1/admin/users`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  // Create user
  async createUser(userData) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/users`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(userData)
    });

    if (!response.ok) {
      const error = await response.json().catch(() => ({}));
      throw new Error(error.message || `HTTP error! status: ${response.status}`);
    }

    return await response.json();
  }

  // Get auth token
  getAuthToken() {
    return localStorage.getItem('umi_access_token');
  }
}

// Create global instance
window.adminAPI = new AdminAPI();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
  module.exports = AdminAPI;
}
