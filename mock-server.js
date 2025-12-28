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
        data: {
            token: 'mock-jwt-token-' + Date.now(),
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
