// Authentication Service for Umi Health Demo System
window.AuthService = {
    isAuthenticated: false,
    user: null,
    
    login(credentials) {
        // Demo authentication - always succeeds for demo
        return new Promise((resolve) => {
            setTimeout(() => {
                this.isAuthenticated = true;
                this.user = {
                    id: 1,
                    name: 'Demo User',
                    email: credentials.email,
                    role: 'demo'
                };
                resolve(this.user);
            }, 500);
        });
    },
    
    logout() {
        this.isAuthenticated = false;
        this.user = null;
    },
    
    getToken() {
        return 'demo-token-' + Date.now();
    }
};
