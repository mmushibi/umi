// Customer Service for Umi Health Demo System
window.CustomerService = {
    customers: [
        { id: 1, name: 'John Doe', email: 'john@example.com', phone: '+260123456789' },
        { id: 2, name: 'Jane Smith', email: 'jane@example.com', phone: '+260987654321' }
    ],
    
    getAll() {
        return Promise.resolve(this.customers);
    },
    
    getById(id) {
        return Promise.resolve(this.customers.find(c => c.id === parseInt(id)));
    },
    
    create(customer) {
        const newCustomer = { id: this.customers.length + 1, ...customer };
        this.customers.push(newCustomer);
        return Promise.resolve(newCustomer);
    }
};
