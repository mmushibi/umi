const express = require('express');
const cors = require('cors');

const app = express();
const PORT = 5001;

// Middleware
app.use(cors());
app.use(express.json());

// Mock auth endpoints
app.post('/api/v1/auth/register', (req, res) => {
    console.log('Received signup request:', JSON.stringify(req.body, null, 2));
    
    const { email, password, confirmPassword, adminFullName, firstName, lastName, phoneNumber, pharmacyName, address, province, username } = req.body;
    
    // Handle both adminFullName or firstName/lastName
    let userFirstName, userLastName;
    if (adminFullName) {
        const names = adminFullName.split(' ');
        userFirstName = names[0] || '';
        userLastName = names.slice(1).join(' ') || '';
    } else {
        userFirstName = firstName || '';
        userLastName = lastName || '';
    }
    
    // Basic validation
    if (!email || !password || !userFirstName || !userLastName || !pharmacyName || !address || !province || !username) {
        return res.status(400).json({
            success: false,
            message: 'Missing required fields'
        });
    }
    
    if (password !== confirmPassword) {
        return res.status(400).json({
            success: false,
            message: 'Passwords do not match'
        });
    }
    
    if (password.length < 6) {
        return res.status(400).json({
            success: false,
            message: 'Password must be at least 6 characters long'
        });
    }
    
    // Simulate successful registration
    res.json({
        success: true,
        message: 'Registration successful',
        redirectUrl: '/portals/admin/home.html',
        data: {
            accessToken: 'mock-jwt-token-' + Date.now(),
            refreshToken: 'mock-refresh-token-' + Date.now(),
            user: {
                id: 'user-' + Date.now(),
                email: email,
                firstName: userFirstName,
                lastName: userLastName,
                fullName: `${userFirstName} ${userLastName}`,
                phoneNumber: phoneNumber
            },
            tenant: {
                id: 'tenant-' + Date.now(),
                name: pharmacyName
            }
        }
    });
});

// Mock pharmacy name check endpoint
app.get('/api/auth/check-pharmacy-name/:pharmacyName', (req, res) => {
    const { pharmacyName } = req.params;
    
    // Simple validation - in real app this would check database
    if (!pharmacyName || pharmacyName.length < 3) {
        return res.json({
            success: false,
            message: 'Pharmacy name must be at least 3 characters long'
        });
    }
    
    // Simulate checking if name is already taken
    const takenNames = ['test pharmacy', 'demo pharmacy', 'sample pharmacy'];
    if (takenNames.includes(pharmacyName.toLowerCase())) {
        return res.json({
            success: false,
            message: 'This pharmacy name is already taken'
        });
    }
    
    res.json({
        success: true,
        message: 'Pharmacy name is available'
    });
});

app.get('/health', (req, res) => {
    res.json({ 
        status: 'healthy', 
        timestamp: new Date().toISOString(),
        port: PORT 
    });
});

app.listen(PORT, () => {
    console.log(`Mock API server running on http://localhost:${PORT}`);
    console.log('Health check: http://localhost:' + PORT + '/health');
});
