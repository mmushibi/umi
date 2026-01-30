/**
 * Backend Integration Helper (Portals Version)
 * Provides standardized functions for integrating frontend pages with the Umi Health API
 * This is a symlink/redirect to the main shared helper for portal compatibility
 */

// Import the main backend integration helper
// Note: This file serves as a compatibility layer for portals that expect this file in portals/shared/js/

// Import the actual helper from the shared directory
// In a real deployment, this would be a proper import or the file would be symlinked
// For now, we'll create a reference to the main helper

// Check if the main helper is already loaded
if (typeof window.backendHelper === 'undefined') {
    // Try to load the main helper
    const script = document.createElement('script');
    script.src = '../../shared/js/backend-integration-helper.js';
    script.onload = () => {
        console.log('✅ Backend integration helper loaded from shared directory');
    };
    script.onerror = () => {
        console.error('❌ Failed to load backend integration helper from shared directory');
    };
    document.head.appendChild(script);
} else {
    console.log('✅ Backend integration helper already available');
}

// Export compatibility
window.backendIntegrationHelper = window.backendHelper;
