/**
 * Demo Verification Script
 * Ensures all demo portals work without backend dependencies
 */

// Demo verification utility
window.DemoVerification = {
  // Check if all required demo files are loaded
  checkDependencies() {
    const checks = {
      adminAPI: typeof window.adminAPI !== 'undefined',
      cashierAPI: typeof window.cashierAPI !== 'undefined',
      pharmacistApi: typeof window.pharmacistApi !== 'undefined',
      demoData: typeof window.demoData !== 'undefined',
      alpine: typeof window.Alpine !== 'undefined'
    };

    console.log('🔍 Demo Dependencies Check:', checks);
    
    const allLoaded = Object.values(checks).every(check => check === true);
    if (allLoaded) {
      console.log('✅ All demo dependencies loaded successfully!');
    } else {
      console.warn('⚠️ Some dependencies missing:', checks);
    }

    return allLoaded;
  },

  // Test API functionality
  testAPIs() {
    console.log('🧪 Testing Demo APIs...');
    
    // Test Admin API
    if (window.adminAPI) {
      window.adminAPI.getDashboardStats().then(result => {
        console.log('✅ Admin API Test:', result.success ? 'PASS' : 'FAIL');
      }).catch(err => {
        console.log('❌ Admin API Test: FAIL', err);
      });
    }

    // Test Cashier API
    if (window.cashierAPI) {
      window.cashierAPI.getDashboardStats().then(result => {
        console.log('✅ Cashier API Test:', result.success ? 'PASS' : 'FAIL');
      }).catch(err => {
        console.log('❌ Cashier API Test: FAIL', err);
      });
    }

    // Test Pharmacist API
    if (window.pharmacistApi) {
      window.pharmacistApi.getDashboardStats().then(result => {
        console.log('✅ Pharmacist API Test:', result.success ? 'PASS' : 'FAIL');
      }).catch(err => {
        console.log('❌ Pharmacist API Test: FAIL', err);
      });
    }
  },

  // Verify demo data structure
  verifyDemoData() {
    if (!window.demoData) {
      console.warn('⚠️ Demo data not loaded');
      return false;
    }

    const requiredData = ['users', 'patients', 'products', 'prescriptions', 'sales', 'branches', 'suppliers', 'queue', 'settings'];
    const hasAllData = requiredData.every(key => window.demoData[key]);
    
    console.log('📊 Demo Data Verification:', hasAllData ? 'PASS' : 'FAIL');
    
    if (hasAllData) {
      console.log('🇿🇲 Zambian Demo Data Loaded:');
      console.log('- Currency:', window.demoData.settings.pharmacy.currency);
      console.log('- Users:', Object.keys(window.demoData.users).length);
      console.log('- Patients:', window.demoData.patients.length);
      console.log('- Products:', window.demoData.products.length);
    }

    return hasAllData;
  },

  // Run full verification
  runFullCheck() {
    console.log('🚀 Starting Demo Verification...');
    
    const depsOk = this.checkDependencies();
    const dataOk = this.verifyDemoData();
    
    if (depsOk && dataOk) {
      setTimeout(() => this.testAPIs(), 1000);
    }

    return depsOk && dataOk;
  }
};

// Auto-run verification when page loads
document.addEventListener('DOMContentLoaded', () => {
  setTimeout(() => {
    window.DemoVerification.runFullCheck();
  }, 2000);
});
