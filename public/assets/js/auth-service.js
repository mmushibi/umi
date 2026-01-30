// Authentication Service for Umi Health
window.AuthService = {
    isAuthenticated: false,
    user: null,
    
    login(credentials) {
        // Real authentication - validate with backend
        return new Promise((resolve, reject) => {
            // Make actual API call
            fetch('http://localhost:5001/api/v1/auth/login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    username: credentials.email,
                    password: credentials.password
                })
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    this.isAuthenticated = true;
                    this.user = {
                        id: data.data.id,
                        name: data.data.username || data.data.email,
                        email: data.data.email,
                        role: data.data.role
                    };
                    resolve(this.user);
                } else {
                    reject(new Error(data.message || 'Login failed'));
                }
            })
            .catch(error => {
                reject(error);
            });
        });
    },
    
    logout() {
        this.isAuthenticated = false;
        this.user = null;
        // Clear stored tokens
        localStorage.removeItem('umi_access_token');
        localStorage.removeItem('umi_refresh_token');
        localStorage.removeItem('umi_current_user');
    },
    
    getToken() {
        return localStorage.getItem('umi_access_token');
    }
};
