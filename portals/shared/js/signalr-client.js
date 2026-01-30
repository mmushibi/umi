/**
 * SignalR Client (Portals Version)
 * Handles real-time communication with the UmiHealth hub
 * This is a compatibility layer for portals that expect this file in portals/shared/js/
 */

// Import the main SignalR client from public directory
// Note: This file serves as a compatibility layer for portal pages

// Check if SignalR client is already loaded
if (typeof window.umiSignalR === 'undefined') {
    // Try to load the main SignalR client
    const script = document.createElement('script');
    script.src = '../../public/js/signalr-client.js';
    script.onload = () => {
        console.log('✅ SignalR client loaded from public directory');
    };
    script.onerror = () => {
        console.error('❌ Failed to load SignalR client from public directory');
    };
    document.head.appendChild(script);
} else {
    console.log('✅ SignalR client already available');
}

// Export compatibility
window.signalRClient = window.umiSignalR;
