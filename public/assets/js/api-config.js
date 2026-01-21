// API Configuration for Umi Health Demo System
window.ApiConfig = {
    baseUrl: 'http://localhost:5001',
    endpoints: {
        auth: '/api/v1/auth',
        users: '/api/v1/users',
        products: '/api/v1/products',
        customers: '/api/v1/customers',
        sales: '/api/v1/sales',
        inventory: '/api/v1/inventory',
        reports: '/api/v1/reports'
    },
    timeout: 10000,
    retries: 3
};
