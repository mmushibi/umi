// API Configuration for Umi Health Demo System
window.ApiConfig = {
    baseUrl: window.location.origin,
    endpoints: {
        auth: '/api/auth',
        users: '/api/users',
        products: '/api/products',
        customers: '/api/customers',
        sales: '/api/sales',
        inventory: '/api/inventory',
        reports: '/api/reports'
    },
    timeout: 10000,
    retries: 3
};
