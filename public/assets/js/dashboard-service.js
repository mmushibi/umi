// Dashboard Service for Umi Health Demo System
window.DashboardService = {
    stats: {
        totalSales: 12500.00,
        todaySales: 850.00,
        totalCustomers: 245,
        lowStockItems: 3
    },
    
    getStats() {
        return Promise.resolve(this.stats);
    },
    
    getRecentSales() {
        return Promise.resolve([
            { id: 1, customer: 'John Doe', amount: 45.50, time: '10:30 AM' },
            { id: 2, customer: 'Jane Smith', amount: 23.75, time: '11:15 AM' }
        ]);
    },
    
    getLowStockProducts() {
        return Promise.resolve([
            { id: 1, name: 'Paracetamol 500mg', stock: 5 },
            { id: 2, name: 'Amoxicillin 250mg', stock: 3 }
        ]);
    }
};
