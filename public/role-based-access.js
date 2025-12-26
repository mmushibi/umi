/**
 * Role-Based Access Control
 * Manages user permissions and access control
 */

class RoleBasedAccess {
  constructor() {
    this.currentUser = null;
    this.permissions = new Map();
    this.roles = new Map();
    
    this.initializeRoles();
    this.loadCurrentUser();
  }

  initializeRoles() {
    // Define role permissions
    this.roles.set('admin', {
      name: 'Administrator',
      permissions: [
        'users.create', 'users.read', 'users.update', 'users.delete',
        'pharmacy.create', 'pharmacy.read', 'pharmacy.update', 'pharmacy.delete',
        'inventory.create', 'inventory.read', 'inventory.update', 'inventory.delete',
        'sales.create', 'sales.read', 'sales.update', 'sales.delete',
        'reports.read', 'reports.export',
        'settings.update', 'billing.manage'
      ]
    });

    this.roles.set('pharmacist', {
      name: 'Pharmacist',
      permissions: [
        'inventory.read', 'inventory.update',
        'sales.create', 'sales.read',
        'prescriptions.create', 'prescriptions.read', 'prescriptions.update',
        'reports.read'
      ]
    });

    this.roles.set('cashier', {
      name: 'Cashier',
      permissions: [
        'inventory.read',
        'sales.create', 'sales.read',
        'customers.create', 'customers.read'
      ]
    });

    this.roles.set('operations', {
      name: 'Operations Manager',
      permissions: [
        'inventory.read', 'inventory.update',
        'sales.read', 'sales.update',
        'reports.read', 'reports.export',
        'pharmacy.read', 'pharmacy.update'
      ]
    });
  }

  loadCurrentUser() {
    try {
      const userData = localStorage.getItem('umi_user_data');
      if (userData) {
        this.currentUser = JSON.parse(userData);
        this.cachePermissions();
      }
    } catch (error) {
      console.error('Error loading current user:', error);
    }
  }

  cachePermissions() {
    if (!this.currentUser || !this.currentUser.role) return;

    const roleData = this.roles.get(this.currentUser.role);
    if (roleData) {
      roleData.permissions.forEach(permission => {
        this.permissions.set(permission, true);
      });
    }
  }

  // Check if user has specific permission
  hasPermission(permission) {
    return this.permissions.has(permission);
  }

  // Check if user has any of the specified permissions
  hasAnyPermission(permissions) {
    return permissions.some(permission => this.permissions.has(permission));
  }

  // Check if user has all specified permissions
  hasAllPermissions(permissions) {
    return permissions.every(permission => this.permissions.has(permission));
  }

  // Check if user has specific role
  hasRole(role) {
    return this.currentUser && this.currentUser.role === role;
  }

  // Get current user info
  getCurrentUser() {
    return this.currentUser;
  }

  // Get user role name
  getRoleName() {
    if (!this.currentUser || !this.currentUser.role) return 'Unknown';
    
    const roleData = this.roles.get(this.currentUser.role);
    return roleData ? roleData.name : this.currentUser.role;
  }

  // Update current user
  updateUser(userData) {
    this.currentUser = userData;
    localStorage.setItem('umi_user_data', JSON.stringify(userData));
    this.cachePermissions();
  }

  // Check page access
  canAccessPage(page) {
    const pagePermissions = {
      'dashboard': ['dashboard.read'],
      'users': ['users.read'],
      'inventory': ['inventory.read'],
      'sales': ['sales.read'],
      'reports': ['reports.read'],
      'settings': ['settings.update'],
      'billing': ['billing.manage'],
      'pharmacy': ['pharmacy.read']
    };

    const requiredPermissions = pagePermissions[page];
    if (!requiredPermissions) return true; // No specific permissions required

    return this.hasAnyPermission(requiredPermissions);
  }

  // Check action access
  canPerformAction(action) {
    return this.hasPermission(action);
  }

  // Filter menu items based on permissions
  filterMenuItems(menuItems) {
    return menuItems.filter(item => {
      if (!item.permission) return true;
      return this.hasPermission(item.permission);
    });
  }

  // Show/hide elements based on permissions
  applyPermissions() {
    // Hide elements with data-permission attribute
    document.querySelectorAll('[data-permission]').forEach(element => {
      const permission = element.getAttribute('data-permission');
      if (!this.hasPermission(permission)) {
        element.style.display = 'none';
      }
    });

    // Hide elements with data-role attribute
    document.querySelectorAll('[data-role]').forEach(element => {
      const role = element.getAttribute('data-role');
      if (!this.hasRole(role)) {
        element.style.display = 'none';
      }
    });

    // Disable elements with data-permission-disable attribute
    document.querySelectorAll('[data-permission-disable]').forEach(element => {
      const permission = element.getAttribute('data-permission-disable');
      if (!this.hasPermission(permission)) {
        element.disabled = true;
        element.setAttribute('title', 'You do not have permission to perform this action');
      }
    });
  }

  // Permission check for API calls
  checkApiPermission(endpoint, method) {
    const apiPermissions = {
      'GET:/api/v1/users': 'users.read',
      'POST:/api/v1/users': 'users.create',
      'PUT:/api/v1/users': 'users.update',
      'DELETE:/api/v1/users': 'users.delete',
      'GET:/api/v1/inventory': 'inventory.read',
      'POST:/api/v1/inventory': 'inventory.create',
      'PUT:/api/v1/inventory': 'inventory.update',
      'DELETE:/api/v1/inventory': 'inventory.delete',
      'GET:/api/v1/sales': 'sales.read',
      'POST:/api/v1/sales': 'sales.create',
      'GET:/api/v1/reports': 'reports.read',
      'POST:/api/v1/settings': 'settings.update'
    };

    const key = `${method}:${endpoint}`;
    const requiredPermission = apiPermissions[key];

    if (!requiredPermission) return true; // No specific permission required
    return this.hasPermission(requiredPermission);
  }

  // Get user permissions list
  getUserPermissions() {
    return Array.from(this.permissions.keys());
  }

  // Check if user is admin
  isAdmin() {
    return this.hasRole('admin');
  }

  // Check if user can manage other users
  canManageUsers() {
    return this.hasAnyPermission(['users.create', 'users.update', 'users.delete']);
  }

  // Check if user can access financial data
  canAccessFinancialData() {
    return this.hasAnyPermission(['sales.read', 'reports.read', 'billing.manage']);
  }
}

// Create global instance
window.roleBasedAccess = new RoleBasedAccess();

// Apply permissions when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
  window.roleBasedAccess.applyPermissions();
});

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
  module.exports = RoleBasedAccess;
}
