/**
 * Admin API Service
 * Production-ready implementation with configurable endpoints
 */

class AdminAPI {
  constructor() {
    this.baseURL = this.getBaseURL();
  }

  getBaseURL() {
    // Check for environment variable first
    if (typeof process !== 'undefined' && process.env?.UMI_API_BASE_URL) {
      return process.env.UMI_API_BASE_URL + '/api/v1';
    }
    
    // Check for global configuration
    if (typeof window !== 'undefined' && window.UMI_CONFIG?.apiBaseUrl) {
      return window.UMI_CONFIG.apiBaseUrl + '/api/v1';
    }
    
    // Check for localStorage configuration
    if (typeof window !== 'undefined') {
      const storedUrl = localStorage.getItem('umi_api_base_url');
      if (storedUrl) {
        return storedUrl + '/api/v1';
      }
    }
    
    // Fallback to environment-based defaults
    if (typeof window !== 'undefined') {
      const hostname = window.location.hostname;
      const port = window.location.port;
      
      if (hostname === 'localhost' || hostname === '127.0.0.1') {
        // Development environment
        return `http://localhost:${parseInt(port) + 1 || 5001}/api/v1`;
      } else if (hostname.includes('staging') || hostname.includes('dev')) {
        // Staging environment
        return `https://api-staging.umihealth.com/api/v1`;
      } else {
        // Production environment - extract subdomain for tenant-specific API calls
        const subdomain = hostname.split('.')[0];
        if (subdomain && subdomain !== 'www' && subdomain !== 'umihealth') {
          return `https://${subdomain}.umihealth.com/api/v1`;
        }
        return `https://api.umihealth.com/api/v1`;
      }
    }
    
    // Default fallback
    return 'http://localhost:5001/api/v1';
  }

  // User profile operations
  async updateUserProfile(profile) {
    const response = await fetch(`${this.baseURL}/account/profile`, {
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
    const response = await fetch(`${this.baseURL}/account/tenant-settings`, {
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
    const response = await fetch(`${this.baseURL}/account/tenant-settings`, {
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
    const response = await fetch(`${this.baseURL}/account/upgrade-subscription`, {
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
    const response = await fetch(`${this.baseURL}/account/subscription-plans`, {
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
    const response = await fetch(`${this.baseURL}/users`, {
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
    const response = await fetch(`${this.baseURL}/users`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify({
        firstName: userData.firstName || userData.name?.split(' ')[0] || '',
        lastName: userData.lastName || userData.name?.split(' ')[1] || '',
        email: userData.email,
        phoneNumber: userData.phone || '',
        role: userData.role || 'Staff',
        password: userData.password || 'TempPassword123!',
        username: userData.email?.split('@')[0] || 'user'
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to create user');
    }

    return await response.json();
  }

  // Update user
  async updateUser(userId, userData) {
    const response = await fetch(`${this.baseURL}/users/${userId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify({
        firstName: userData.firstName || userData.name?.split(' ')[0] || '',
        lastName: userData.lastName || userData.name?.split(' ')[1] || '',
        email: userData.email,
        phoneNumber: userData.phone || '',
        role: userData.role || 'Staff',
        status: userData.status || 'active'
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to update user');
    }

    return await response.json();
  }

  // Delete user
  async deleteUser(userId) {
    const response = await fetch(`${this.baseURL}/users/${userId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'Failed to delete user');
    }

    return await response.json();
  }

  // User invitation methods
  async inviteUser(userData) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/invite-user`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${this.getAuthToken()}`
      },
      body: JSON.stringify(userData)
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to send invitation');
    }

    return await response.json();
  }

  async validateInvitation(token) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/validate-invitation?token=${token}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${this.getAuthToken()}`
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to validate invitation');
    }

    return await response.json();
  }

  async acceptInvitation(token, password) {
    const response = await fetch(`${this.baseURL}/api/v1/admin/accept-invitation`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        token: token,
        password: password
      })
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error || 'Failed to accept invitation');
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
