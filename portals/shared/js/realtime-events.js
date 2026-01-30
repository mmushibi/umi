/**
 * Real-time Events (Portals Version)
 * Handles real-time event management for Umi Health portals
 * This is a compatibility layer for portals that expect this file in portals/shared/js/
 */

// Import the main real-time events from shared directory
// Note: This file serves as a compatibility layer for portal pages

// Check if real-time events are already loaded
if (typeof window.realtimeEvents === 'undefined') {
    // Try to load the main real-time events
    const script = document.createElement('script');
    script.src = '../../shared/js/realtime-events.js';
    script.onload = () => {
        console.log('✅ Real-time events loaded from shared directory');
    };
    script.onerror = () => {
        console.error('❌ Failed to load real-time events from shared directory');
    };
    document.head.appendChild(script);
} else {
    console.log('✅ Real-time events already available');
}

// Export compatibility
window.realtimeEvents = window.realtimeEvents;
