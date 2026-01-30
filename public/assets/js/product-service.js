// Product Service for Umi Health System
window.ProductService = {
    products: [
        { id: 1, name: 'Paracetamol 500mg', price: 2.50, stock: 100 },
        { id: 2, name: 'Amoxicillin 250mg', price: 15.00, stock: 50 },
        { id: 3, name: 'Ibuprofen 400mg', price: 3.75, stock: 75 }
    ],
    
    getAll() {
        return Promise.resolve(this.products);
    },
    
    getById(id) {
        return Promise.resolve(this.products.find(p => p.id === parseInt(id)));
    },
    
    search(query) {
        return Promise.resolve(
            this.products.filter(p => 
                p.name.toLowerCase().includes(query.toLowerCase())
            )
        );
    }
};
