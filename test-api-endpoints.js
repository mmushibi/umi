// Test script for Umi Health API endpoints
const fetch = require('node-fetch').default || require('node-fetch');

const API_BASE = 'http://localhost:5001';

// Test data
const testUser = {
    username: 'testuser',
    email: 'test@example.com',
    password: 'Test123!',
    firstName: 'Test',
    lastName: 'User',
    phoneNumber: '+1234567890',
    role: 'admin'
};

const testTenant = {
    pharmacyName: 'Test Pharmacy ' + Date.now(),
    email: `pharmacy${Date.now()}@example.com`,
    password: 'Test123!',
    adminFullName: 'Test Admin',
    phoneNumber: '+1234567890'
};

async function testEndpoint(method, endpoint, data = null, token = null) {
    const options = {
        method,
        headers: {
            'Content-Type': 'application/json',
        }
    };

    if (token) {
        options.headers.Authorization = `Bearer ${token}`;
    }

    if (data) {
        options.body = JSON.stringify(data);
    }

    try {
        const response = await fetch(`${API_BASE}${endpoint}`, options);
        const result = await response.json();
        console.log(`\n${method} ${endpoint}`);
        console.log(`Status: ${response.status}`);
        console.log('Response:', JSON.stringify(result, null, 2));
        return result;
    } catch (error) {
        console.error(`Error testing ${endpoint}:`, error.message);
        return null;
    }
}

async function runTests() {
    console.log('🧪 Testing Umi Health API Endpoints...\n');

    // Test health endpoint
    await testEndpoint('GET', '/health');

    // Test registration
    console.log('\n📝 Testing Registration...');
    const regResult = await testEndpoint('POST', '/api/v1/auth/register', testTenant);
    
    if (regResult?.success) {
        const token = regResult.accessToken;
        console.log('\n✅ Registration successful! Token obtained.');

        // Test authenticated endpoints
        console.log('\n🔐 Testing Authenticated Endpoints...');
        
        // Test dashboard
        await testEndpoint('GET', '/api/v1/dashboard/summary', null, token);
        
        // Test users endpoint
        await testEndpoint('GET', '/api/v1/users', null, token);
        
        // Test creating a user
        const newUser = {
            ...testUser,
            username: 'newuser',
            email: 'newuser@example.com'
        };
        await testEndpoint('POST', '/api/v1/users', newUser, token);
        
        // Test patients endpoint
        await testEndpoint('GET', '/api/v1/patients', null, token);
        
        // Test creating a patient
        const testPatient = {
            firstName: 'John',
            lastName: 'Doe',
            email: 'john.doe@example.com',
            phoneNumber: '+1234567890',
            dateOfBirth: '1990-01-01',
            gender: 'Male',
            address: '123 Main St',
            emergencyContact: 'Jane Doe',
            emergencyPhone: '+1234567891',
            bloodType: 'O+',
            allergies: 'None',
            medicalHistory: 'No significant medical history'
        };
        await testEndpoint('POST', '/api/v1/patients', testPatient, token);
        
        // Test inventory endpoint
        await testEndpoint('GET', '/api/v1/inventory', null, token);
        
        // Test creating inventory item
        const testInventory = {
            productName: 'Test Medicine',
            genericName: 'Test Generic',
            category: 'Antibiotics',
            productCode: 'MED001',
            barcode: '1234567890',
            currentStock: 100,
            minStockLevel: 10,
            maxStockLevel: 200,
            unitPrice: 10.50,
            sellingPrice: 15.75,
            unit: 'tablets',
            manufacturer: 'Test Pharma',
            supplier: 'Test Supplier',
            expiryDate: '2025-12-31',
            description: 'Test medication for testing purposes'
        };
        await testEndpoint('POST', '/api/v1/inventory', testInventory, token);
        
        // Test search endpoints
        await testEndpoint('GET', '/api/v1/search/patients?query=John', null, token);
        await testEndpoint('GET', '/api/v1/search/inventory?query=Test', null, token);
        
        console.log('\n✅ All tests completed!');
    } else {
        console.log('\n❌ Registration failed. Cannot test authenticated endpoints.');
    }
}

// Run the tests
runTests().catch(console.error);
