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
    
    const { email, password, confirmPassword, firstName, lastName, phoneNumber, pharmacyName, address, province, username } = req.body;
    
    // Basic validation
    if (!email || !password || !firstName || !lastName || !pharmacyName || !address || !province || !username) {
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
                firstName: firstName,
                lastName: lastName,
                fullName: `${firstName} ${lastName}`,
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
