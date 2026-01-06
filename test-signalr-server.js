const express = require('express');
const http = require('http');
const path = require('path');

const app = express();
const server = http.createServer(app);
const PORT = 5000;

// Middleware
app.use(express.json());
app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
    res.header('Access-Control-Allow-Headers', 'Origin, X-Requested-With, Content-Type, Accept, Authorization');
    if (req.method === 'OPTIONS') {
        res.sendStatus(200);
    } else {
        next();
    }
});

// SignalR-like negotiation endpoint
app.post('/testHub/negotiate', (req, res) => {
    console.log('Negotiation request for testHub');
    res.json({
        connectionId: `conn_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
        availableTransports: [
            {
                transport: 'WebSockets',
                transferFormats: ['Text', 'Binary']
            },
            {
                transport: 'ServerSentEvents',
                transferFormats: ['Text']
            },
            {
                transport: 'LongPolling',
                transferFormats: ['Text', 'Binary']
            }
        ]
    });
});

// Simple test endpoint
app.get('/api/test', (req, res) => {
    res.json({ message: 'Backend API is running', timestamp: new Date().toISOString() });
});

// Health check
app.get('/health', (req, res) => {
    res.json({ status: 'healthy', timestamp: new Date().toISOString() });
});

// Serve static files from parent directory
app.use(express.static(path.join(__dirname, '..')));

// Start server
server.listen(PORT, () => {
    console.log(`🚀 Test API server running on http://localhost:${PORT}`);
    console.log(`📡 SignalR test hub available at /testHub`);
    console.log(`🔍 Test endpoints:`);
    console.log(`   GET  /api/test - Simple test`);
    console.log(`   GET  /health - Health check`);
    console.log(`   POST /testHub/negotiate - SignalR negotiation`);
});

// Handle WebSocket upgrade for SignalR
server.on('upgrade', (request, socket, head) => {
    console.log('WebSocket upgrade request:', request.url);
    
    if (request.url.startsWith('/testHub')) {
        // Handle WebSocket connection for SignalR
        console.log('✅ WebSocket connection established for testHub');
        
        // Simple ping-pong handler
        socket.on('data', (data) => {
            const message = data.toString();
            console.log('📨 Received:', message);
            
            // Echo back or respond to ping
            if (message.includes('ping')) {
                socket.write('{"type":1,"target":"Pong","arguments":["pong response"]}');
            } else {
                // Echo the message
                socket.write(`{"type":1,"target":"Echo","arguments":["${message}"]}`);
            }
        });
        
        socket.on('close', () => {
            console.log('❌ WebSocket connection closed');
        });
        
        socket.on('error', (error) => {
            console.error('❌ WebSocket error:', error);
        });
        
        // Send welcome message
        setTimeout(() => {
            socket.write('{"type":1,"target":"Connected","arguments":["Welcome to test hub!"]}');
        }, 100);
    }
});

module.exports = app;
