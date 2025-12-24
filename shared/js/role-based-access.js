/**
 * Role-Based Access Control System
 * Manages permissions and access control for different user roles
 */

class RoleBasedAccess {
  constructor() {
    this.roles = {
      admin: {
        name: 'Administrator',
        permissions: [
          'view_dashboard', 'manage_users', 'view_reports', 'manage_settings',
          'view_patients', 'manage_patients', 'view_prescriptions', 'manage_prescriptions',
          'view_inventory', 'manage_inventory', 'view_sales', 'manage_sales',
          'view_payments', 'manage_payments', 'process_sales', 'process_payments',
          'view_analytics', 'export_data', 'import_data', 'system_config'
        ],
        pages: ['home.html', 'user-management.html', 'patients.html', 'prescriptions.html',
                'inventory.html', 'point-of-sale.html', 'sales.html', 'payments.html',
                'reports.html', 'account.html']
      },
      pharmacist: {
        name: 'Pharmacist',
        permissions: [
          'view_dashboard', 'view_patients', 'manage_patients', 'view_prescriptions', 'manage_prescriptions',
          'view_inventory', 'manage_inventory', 'view_sales', 'view_reports',
          'view_analytics', 'export_data'
        ],
        pages: ['home.html', 'patients.html', 'prescriptions.html', 'inventory.html', 'reports.html', 'account.html']
      },
      cashier: {
        name: 'Cashier',
        permissions: [
          'view_dashboard', 'view_patients', 'view_sales', 'process_sales',
          'view_inventory', 'view_payments', 'process_payments', 'view_reports'
        ],
        pages: ['home.html', 'patients.html', 'point-of-sale.html', 'sales.html', 'payments.html', 'account.html']
      },
      operations: {
        name: 'Operations',
        permissions: [
          'view_dashboard', 'create_tenants', 'view_tenants', 'manage_tenants',
          'change_subscriptions', 'view_subscriptions', 'manage_subscriptions',
          'view_users', 'update_users', 'view_user_transactions', 'view_transactions',
          'view_reports', 'export_data', 'manage_account'
        ],
        pages: ['home.html', 'tenants.html', 'subscriptions.html', 'users.html', 'transactions.html', 'account.html']
      },
      super_admin: {
        name: 'Super Admin',
        permissions: [
          'view_dashboard', 'create_tenants', 'view_tenants', 'manage_tenants', 'delete_tenants',
          'change_subscriptions', 'view_subscriptions', 'manage_subscriptions',
          'view_users', 'update_users', 'delete_users', 'view_user_transactions', 'view_transactions',
          'view_reports', 'export_data', 'import_data', 'system_config', 'manage_account',
          'view_analytics', 'manage_settings', 'system_logs', 'help_center'
        ],
        pages: ['home.html', 'tenants.html', 'subscriptions.html', 'users.html', 'transactions.html', 'account.html', 'analytics.html', 'help.html']
      }
    };
  }

  /**
   * Check if user has specific permission
   */
  hasPermission(userRole, permission) {
    const role = this.roles[userRole];
    return role ? role.permissions.includes(permission) : false;
  }

  /**
   * Check if user can access specific page
   */
  canAccessPage(userRole, page) {
    const role = this.roles[userRole];
    return role ? role.pages.includes(page) : false;
  }

  /**
   * Get allowed pages for role
   */
  getAllowedPages(userRole) {
    const role = this.roles[userRole];
    return role ? role.pages : [];
  }

  /**
   * Get user navigation menu based on role
   */
  getNavigationMenu(userRole) {
    const role = this.roles[userRole];
    if (!role) return [];

    const allMenuItems = [
      { href: 'home.html', icon: 'home', label: 'Home', permission: 'view_dashboard' },
      { href: 'patients.html', icon: 'people', label: 'Patients', permission: 'view_patients' },
      { href: 'prescriptions.html', icon: 'prescriptions', label: 'Prescriptions', permission: 'view_prescriptions' },
      { href: 'inventory.html', icon: 'inventory_2', label: 'Inventory', permission: 'view_inventory' },
      { href: 'point-of-sale.html', icon: 'point_of_sale', label: 'Point of Sale', permission: 'process_sales' },
      { href: 'sales.html', icon: 'receipt_long', label: 'Sales', permission: 'view_sales' },
      { href: 'payments.html', icon: 'payments', label: 'Payments', permission: 'view_payments' },
      { href: 'reports.html', icon: 'analytics', label: 'Reports', permission: 'view_reports' },
      { href: 'user-management.html', icon: 'admin_panel_settings', label: 'User Management', permission: 'manage_users' },
      { href: 'account.html', icon: 'settings', label: 'Account', permission: null }
    ];

    return allMenuItems.filter(item => 
      !item.permission || role.permissions.includes(item.permission)
    );
  }

  /**
   * Redirect user to appropriate page based on role
   */
  redirectToRolePage(userRole) {
    const role = this.roles[userRole];
    if (!role) return 'signin.html';

    // Redirect to first allowed page
    return role.pages[0] || 'signin.html';
  }

  /**
   * Check page access and redirect if needed
   */
  checkPageAccess() {
    const currentUser = window.dataSync?.getCurrentUser();
    const currentPage = window.location.pathname.split('/').pop();

    if (!currentUser) {
      window.location.href = '../signin.html';
      return false;
    }

    if (!this.canAccessPage(currentUser.role, currentPage)) {
      const allowedPage = this.redirectToRolePage(currentUser.role);
      window.location.href = allowedPage;
      return false;
    }

    return true;
  }

  /**
   * Add role-based UI elements visibility
   */
  applyUIPermissions(userRole) {
    const role = this.roles[userRole];
    if (!role) return;

    // Hide elements based on data-permission attribute
    document.querySelectorAll('[data-permission]').forEach(element => {
      const permission = element.getAttribute('data-permission');
      if (!role.permissions.includes(permission)) {
        element.style.display = 'none';
      }
    });

    // Disable buttons based on permission
    document.querySelectorAll('[data-disable-permission]').forEach(element => {
      const permission = element.getAttribute('data-disable-permission');
      if (!role.permissions.includes(permission)) {
        element.disabled = true;
        element.title = 'You do not have permission for this action';
      }
    });
  }
}

// Create global instance
window.roleAccess = new RoleBasedAccess();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
  module.exports = RoleBasedAccess;
}
