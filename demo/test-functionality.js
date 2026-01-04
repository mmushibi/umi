// Demo Portal Functionality Test Script
// This script tests all the core functionality of the demo portals

console.log('🧪 Starting Umi Health Demo Portal Functionality Tests...\n');

// Test 1: Check if demo storage is available and working
function testDemoStorage() {
    console.log('📦 Testing Demo Storage...');
    
    try {
        // Check if demoStorage is available
        if (typeof window.demoStorage === 'undefined') {
            console.log('❌ Demo storage not available');
            return false;
        }
        
        // Test basic operations
        const testPatient = {
            firstName: 'Test',
            lastName: 'User',
            nrc: '123456/78/9',
            phone: '+260977123456',
            email: 'test@example.com'
        };
        
        // Test add patient
        const addedPatient = window.demoStorage.addPatient(testPatient);
        if (!addedPatient || !addedPatient.id) {
            console.log('❌ Failed to add patient');
            return false;
        }
        
        // Test get patient
        const retrievedPatient = window.demoStorage.getItem('patients').find(p => p.id === addedPatient.id);
        if (!retrievedPatient || retrievedPatient.firstName !== 'Test') {
            console.log('❌ Failed to retrieve patient');
            return false;
        }
        
        // Test update patient
        const updatedPatient = window.demoStorage.updatePatient(addedPatient.id, { firstName: 'Updated' });
        if (!updatedPatient || updatedPatient.firstName !== 'Updated') {
            console.log('❌ Failed to update patient');
            return false;
        }
        
        // Test delete patient
        const deleteResult = window.demoStorage.deletePatient(addedPatient.id);
        if (!deleteResult) {
            console.log('❌ Failed to delete patient');
            return false;
        }
        
        console.log('✅ Demo storage tests passed');
        return true;
        
    } catch (error) {
        console.log('❌ Demo storage test failed:', error.message);
        return false;
    }
}

// Test 2: Check if sample data is loaded
function testSampleData() {
    console.log('📊 Testing Sample Data...');
    
    try {
        const patients = window.demoStorage.getItem('patients') || [];
        const medications = window.demoStorage.getItem('medications') || [];
        const prescriptions = window.demoStorage.getItem('prescriptions') || [];
        const sales = window.demoStorage.getItem('sales') || [];
        
        if (patients.length === 0) {
            console.log('❌ No patient sample data found');
            return false;
        }
        
        if (medications.length === 0) {
            console.log('❌ No medication sample data found');
            return false;
        }
        
        if (prescriptions.length === 0) {
            console.log('❌ No prescription sample data found');
            return false;
        }
        
        if (sales.length === 0) {
            console.log('❌ No sales sample data found');
            return false;
        }
        
        // Check for Zambian context
        const zambianNames = ['Banda', 'Phiri', 'Tembo', 'Bwalya', 'Chanda'];
        const hasZambianNames = patients.some(p => 
            zambianNames.some(name => 
                p.firstName.includes(name) || p.lastName.includes(name)
            )
        );
        
        if (!hasZambianNames) {
            console.log('❌ Zambian sample data not found');
            return false;
        }
        
        console.log(`✅ Sample data loaded: ${patients.length} patients, ${medications.length} medications, ${prescriptions.length} prescriptions, ${sales.length} sales`);
        return true;
        
    } catch (error) {
        console.log('❌ Sample data test failed:', error.message);
        return false;
    }
}

// Test 3: Check dashboard stats
function testDashboardStats() {
    console.log('📈 Testing Dashboard Stats...');
    
    try {
        const stats = window.demoStorage.getDashboardStats();
        
        if (!stats || typeof stats.totalPatients !== 'number') {
            console.log('❌ Dashboard stats not working');
            return false;
        }
        
        console.log('✅ Dashboard stats working:', stats);
        return true;
        
    } catch (error) {
        console.log('❌ Dashboard stats test failed:', error.message);
        return false;
    }
}

// Test 4: Check search functionality
function testSearchFunctionality() {
    console.log('🔍 Testing Search Functionality...');
    
    try {
        // Test patient search
        const searchResults = window.demoStorage.searchPatients('Banda');
        if (!Array.isArray(searchResults)) {
            console.log('❌ Patient search not working');
            return false;
        }
        
        // Test medication search
        const medResults = window.demoStorage.searchMedications('Paracetamol');
        if (!Array.isArray(medResults)) {
            console.log('❌ Medication search not working');
            return false;
        }
        
        console.log(`✅ Search working: ${searchResults.length} patients found, ${medResults.length} medications found`);
        return true;
        
    } catch (error) {
        console.log('❌ Search functionality test failed:', error.message);
        return false;
    }
}

// Test 5: Check data export/import
function testDataExportImport() {
    console.log('💾 Testing Data Export/Import...');
    
    try {
        const exportData = window.demoStorage.exportData();
        
        if (!exportData || !exportData.patients || !exportData.medications) {
            console.log('❌ Data export not working');
            return false;
        }
        
        console.log('✅ Data export working');
        return true;
        
    } catch (error) {
        console.log('❌ Data export/import test failed:', error.message);
        return false;
    }
}

// Test 6: Check CRUD operations for all entities
function testCRUDOperations() {
    console.log('🔄 Testing CRUD Operations...');
    
    try {
        // Test medication CRUD
        const testMed = {
            name: 'Test Medication',
            category: 'Analgesic',
            stockQuantity: 100,
            unitPrice: 10.50
        };
        
        const addedMed = window.demoStorage.addMedication(testMed);
        const updatedMed = window.demoStorage.updateMedication(addedMed.id, { stockQuantity: 150 });
        
        if (!updatedMed || updatedMed.stockQuantity !== 150) {
            console.log('❌ Medication CRUD failed');
            return false;
        }
        
        // Test prescription CRUD
        const testPrescription = {
            patientId: 1,
            patientName: 'Test Patient',
            doctorName: 'Dr. Test',
            diagnosis: 'Test Condition',
            medications: [{ name: 'Test Med', dosage: '1 daily' }],
            totalCost: 25.00
        };
        
        const addedPrescription = window.demoStorage.addPrescription(testPrescription);
        const updatedPrescription = window.demoStorage.updatePrescription(addedPrescription.id, { status: 'Completed' });
        
        if (!updatedPrescription || updatedPrescription.status !== 'Completed') {
            console.log('❌ Prescription CRUD failed');
            return false;
        }
        
        // Test sale CRUD
        const testSale = {
            patientId: 1,
            patientName: 'Test Patient',
            items: [{ medicationName: 'Test Med', quantity: 1, unitPrice: 10.50, totalPrice: 10.50 }],
            totalAmount: 10.50,
            paymentMethod: 'Cash'
        };
        
        const addedSale = window.demoStorage.addSale(testSale);
        const updatedSale = window.demoStorage.updateSale(addedSale.id, { paymentStatus: 'Pending' });
        
        if (!updatedSale || updatedSale.paymentStatus !== 'Pending') {
            console.log('❌ Sale CRUD failed');
            return false;
        }
        
        console.log('✅ CRUD operations working');
        return true;
        
    } catch (error) {
        console.log('❌ CRUD operations test failed:', error.message);
        return false;
    }
}

// Run all tests
function runAllTests() {
    const tests = [
        { name: 'Demo Storage', fn: testDemoStorage },
        { name: 'Sample Data', fn: testSampleData },
        { name: 'Dashboard Stats', fn: testDashboardStats },
        { name: 'Search Functionality', fn: testSearchFunctionality },
        { name: 'Data Export/Import', fn: testDataExportImport },
        { name: 'CRUD Operations', fn: testCRUDOperations }
    ];
    
    let passed = 0;
    let failed = 0;
    
    tests.forEach(test => {
        const result = test.fn();
        if (result) {
            passed++;
        } else {
            failed++;
        }
    });
    
    console.log('\n📋 Test Results:');
    console.log(`✅ Passed: ${passed}`);
    console.log(`❌ Failed: ${failed}`);
    console.log(`📊 Success Rate: ${Math.round((passed / tests.length) * 100)}%`);
    
    if (failed === 0) {
        console.log('\n🎉 All tests passed! The demo portal is fully functional.');
    } else {
        console.log('\n⚠️ Some tests failed. Please check the issues above.');
    }
    
    return failed === 0;
}

// Auto-run tests when script is loaded
if (typeof window !== 'undefined') {
    // Wait for demo storage to be available
    setTimeout(() => {
        runAllTests();
    }, 1000);
} else {
    // Node.js environment
    module.exports = { runAllTests };
}
