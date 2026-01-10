/**
 * Development API Proxy Server
 * Forwards requests to the real backend API during development
 */

const express = require('express');
const { createProxyMiddleware } = require('http-proxy-middleware');
const cors = require('cors');

const app = express();
const PORT = 5001;
const BACKEND_URL = process.env.BACKEND_URL || 'http://localhost:5000';

// Middleware
app.use(cors());
app.use(express.json());

// Logging middleware
app.use((req, res, next) => {
    console.log(`${req.method} ${req.path} -> ${BACKEND_URL}${req.path}`);
    next();
});

// Health check
app.get('/health', (req, res) => {
    res.json({ 
        status: 'healthy', 
        timestamp: new Date().toISOString(),
        port: PORT,
        backend: BACKEND_URL,
        mode: 'proxy'
    });
});

// Proxy all API requests to the backend
const apiProxy = createProxyMiddleware({
    target: BACKEND_URL,
    changeOrigin: true,
    pathRewrite: {
        '^/api': '/api', // Keep the same path
    },
    onError: (err, req, res) => {
        console.error('Proxy error:', err.message);
        res.status(500).json({
            success: false,
            message: 'Backend service unavailable',
            error: err.message
        });
    },
    onProxyReq: (proxyReq, req, res) => {
        // Forward authorization header if present
        if (req.headers.authorization) {
            proxyReq.setHeader('Authorization', req.headers.authorization);
        }
        console.log(`Forwarding to backend: ${req.method} ${proxyReq.path}`);
    },
    onProxyRes: (proxyRes, req, res) => {
        console.log(`Backend response: ${proxyRes.statusCode} for ${req.method} ${req.path}`);
    }
});

// Apply proxy to all API routes
app.use('/api', apiProxy);

// Serve static files from parent directory for development
app.use(express.static(require('path').join(__dirname)));

// Fallback route for SPA support
app.get('*', (req, res) => {
    res.sendFile(require('path').join(__dirname, 'index.html'));
});

app.listen(PORT, () => {
    console.log(`🚀 Development API Proxy running on http://localhost:${PORT}`);
    console.log(`📡 Proxying backend requests to: ${BACKEND_URL}`);
    console.log(`🔍 Health check: http://localhost:${PORT}/health`);
    console.log(`📝 All /api/* requests will be forwarded to the backend`);
    console.log(`⚠️  Make sure the backend is running on ${BACKEND_URL}`);
});
