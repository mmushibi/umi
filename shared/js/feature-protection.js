/**
 * Feature Protection System
 * Handles read-only access to higher-tier features and upgrade prompts
 */

class FeatureProtection {
    constructor() {
        this.currentPlan = null;
        this.features = {
            // Care tier features (base)
            'inventory_management': { tier: 'Care', required: 'Care' },
            'prescription_management': { tier: 'Care', required: 'Care' },
            'pos_functionality': { tier: 'Care', required: 'Care' },
            'basic_reports': { tier: 'Care', required: 'Care' },
            'email_support': { tier: 'Care', required: 'Care' },
            
            // Care Plus tier features
            'advanced_reports': { tier: 'Care Plus', required: 'Care Plus' },
            'api_access': { tier: 'Care Plus', required: 'Care Plus' },
            'phone_support': { tier: 'Care Plus', required: 'Care Plus' },
            'multiple_branches': { tier: 'Care Plus', required: 'Care Plus' },
            'multiple_users': { tier: 'Care Plus', required: 'Care Plus' },
            
            // Care Pro tier features
            'custom_reports': { tier: 'Care Pro', required: 'Care Pro' },
            'webhooks': { tier: 'Care Pro', required: 'Care Pro' },
            'priority_support': { tier: 'Care Pro', required: 'Care Pro' },
            'dedicated_account_manager': { tier: 'Care Pro', required: 'Care Pro' },
            'unlimited_branches': { tier: 'Care Pro', required: 'Care Pro' },
            'unlimited_users': { tier: 'Care Pro', required: 'Care Pro' }
        };
        
        this.planHierarchy = ['Care', 'Care Plus', 'Care Pro'];
        this.init();
    }
    
    async init() {
        // Get current user's subscription plan
        const currentUser = JSON.parse(localStorage.getItem('umi_currentUser') || '{}');
        const subscription = JSON.parse(localStorage.getItem('umi_subscription') || '{}');
        
        this.currentPlan = subscription.planType || 'Care';
        
        // Set up feature protection
        this.setupFeatureProtection();
    }
    
    setupFeatureProtection() {
        // Add click listeners to protected features
        Object.keys(this.features).forEach(featureKey => {
            const feature = this.features[featureKey];
            const elements = document.querySelectorAll(`[data-feature="${featureKey}"]`);
            
            elements.forEach(element => {
                if (!this.canAccessFeature(featureKey)) {
                    this.makeReadOnly(element, feature);
                }
            });
        });
    }
    
    canAccessFeature(featureKey) {
        const feature = this.features[featureKey];
        if (!feature) return true;
        
        const currentPlanIndex = this.planHierarchy.indexOf(this.currentPlan);
        const requiredPlanIndex = this.planHierarchy.indexOf(feature.required);
        
        return currentPlanIndex >= requiredPlanIndex;
    }
    
    makeReadOnly(element, feature) {
        const feature = this.features[feature];
        
        // Add visual indicators
        element.classList.add('feature-locked');
        element.setAttribute('title', `This feature requires ${feature.required} plan`);
        
        // Add click listener for upgrade prompt
        element.addEventListener('click', (e) => {
            e.preventDefault();
            e.stopPropagation();
            this.showUpgradeDialog(feature);
        });
        
        // Disable form inputs
        const inputs = element.querySelectorAll('input, button, select, textarea');
        inputs.forEach(input => {
            input.disabled = true;
            input.classList.add('disabled-readonly');
        });
    }
    
    showUpgradeDialog(feature) {
        const modal = document.createElement('div');
        modal.className = 'upgrade-modal-overlay';
        modal.innerHTML = `
            <div class="upgrade-modal">
                <div class="upgrade-modal-header">
                    <h3>🔒 Feature Locked</h3>
                    <p>This feature requires <strong>${feature.required}</strong> plan</p>
                </div>
                <div class="upgrade-modal-content">
                    <div class="feature-comparison">
                        <div class="current-plan">
                            <h4>Your Plan: ${this.currentPlan}</h4>
                            <ul>
                                ${this.getCurrentPlanFeatures().map(f => `<li>✓ ${f}</li>`).join('')}
                            </ul>
                        </div>
                        <div class="required-plan">
                            <h4>Required: ${feature.required}</h4>
                            <ul>
                                ${this.getPlanFeatures(feature.required).map(f => `<li>✓ ${f}</li>`).join('')}
                            </ul>
                        </div>
                    </div>
                </div>
                <div class="upgrade-modal-actions">
                    <button class="upgrade-btn" onclick="window.featureProtection.upgradeToPlan('${feature.required}')">
                        Upgrade to ${feature.required}
                    </button>
                    <button class="cancel-btn" onclick="window.featureProtection.closeUpgradeDialog()">
                        Maybe Later
                    </button>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // Add styles if not already added
        if (!document.getElementById('feature-protection-styles')) {
            const styles = document.createElement('style');
            styles.id = 'feature-protection-styles';
            styles.textContent = `
                .upgrade-modal-overlay {
                    position: fixed;
                    top: 0;
                    left: 0;
                    width: 100%;
                    height: 100%;
                    background: rgba(0, 0, 0, 0.5);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    z-index: 9999;
                }
                
                .upgrade-modal {
                    background: white;
                    border-radius: 12px;
                    padding: 24px;
                    max-width: 600px;
                    width: 90%;
                    box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.1);
                }
                
                .upgrade-modal-header {
                    text-align: center;
                    margin-bottom: 20px;
                }
                
                .upgrade-modal-header h3 {
                    margin: 0 0 8px 0;
                    color: #dc2626;
                }
                
                .feature-comparison {
                    display: grid;
                    grid-template-columns: 1fr 1fr;
                    gap: 20px;
                    margin-bottom: 24px;
                }
                
                .current-plan, .required-plan {
                    padding: 16px;
                    border: 1px solid #e5e7eb;
                    border-radius: 8px;
                }
                
                .required-plan {
                    border-color: #2563eb;
                    background: #eff6ff;
                }
                
                .upgrade-modal-actions {
                    display: flex;
                    gap: 12px;
                    justify-content: center;
                }
                
                .upgrade-btn {
                    background: #2563eb;
                    color: white;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 8px;
                    cursor: pointer;
                    font-weight: 600;
                }
                
                .cancel-btn {
                    background: #f3f4f6;
                    color: #374151;
                    border: none;
                    padding: 12px 24px;
                    border-radius: 8px;
                    cursor: pointer;
                }
                
                .feature-locked {
                    opacity: 0.6;
                    cursor: not-allowed !important;
                    position: relative;
                }
                
                .feature-locked::after {
                    content: '🔒';
                    position: absolute;
                    top: 8px;
                    right: 8px;
                    font-size: 16px;
                }
                
                .disabled-readonly {
                    background: #f9fafb !important;
                    border-color: #e5e7eb !important;
                    cursor: not-allowed !important;
                }
            `;
            document.head.appendChild(styles);
        }
    }
    
    getCurrentPlanFeatures() {
        const planFeatures = {
            'Care': ['Inventory Management', 'Prescription Management', 'POS Functionality', 'Basic Reports', 'Email Support'],
            'Care Plus': ['All Care features', 'Advanced Reports', 'API Access', 'Phone Support', 'Multiple Branches', 'Multiple Users'],
            'Care Pro': ['All Care Plus features', 'Custom Reports', 'Webhooks', 'Priority Support', 'Dedicated Account Manager', 'Unlimited Branches/Users']
        };
        return planFeatures[this.currentPlan] || [];
    }
    
    getPlanFeatures(plan) {
        const planFeatures = {
            'Care': ['Inventory Management', 'Prescription Management', 'POS Functionality', 'Basic Reports', 'Email Support'],
            'Care Plus': ['All Care features', 'Advanced Reports', 'API Access', 'Phone Support', 'Multiple Branches', 'Multiple Users'],
            'Care Pro': ['All Care Plus features', 'Custom Reports', 'Webhooks', 'Priority Support', 'Dedicated Account Manager', 'Unlimited Branches/Users']
        };
        return planFeatures[plan] || [];
    }
    
    upgradeToPlan(planName) {
        // Redirect to account upgrade page
        window.location.href = '/account/upgrade.html?plan=' + encodeURIComponent(planName);
    }
    
    closeUpgradeDialog() {
        const modal = document.querySelector('.upgrade-modal-overlay');
        if (modal) {
            modal.remove();
        }
    }
}

// Public API
window.featureProtection = {
    canAccessFeature: (feature) => new FeatureProtection().canAccessFeature(feature),
    upgradeToPlan: (plan) => new FeatureProtection().upgradeToPlan(plan),
    closeUpgradeDialog: () => new FeatureProtection().closeUpgradeDialog()
};

// Initialize feature protection
document.addEventListener('DOMContentLoaded', () => {
    new FeatureProtection();
});

// Branch Management Helper Functions
window.branchManagement = {
    canManageBranches() {
        const subscription = JSON.parse(localStorage.getItem('umi_subscription') || '{}');
        const planType = subscription.planType || 'Care';
        
        const tierConfig = {
            'Care': { maxBranches: 1 },
            'Care Plus': { maxBranches: 3 },
            'Care Pro': { maxBranches: 10 }
        };
        
        return tierConfig[planType] ? tierConfig[planType].maxBranches > 1 : false;
    },
    
    getMaxBranches() {
        const subscription = JSON.parse(localStorage.getItem('umi_subscription') || '{}');
        const planType = subscription.planType || 'Care';
        
        const tierConfig = {
            'Care': { maxBranches: 1 },
            'Care Plus': { maxBranches: 3 },
            'Care Pro': { maxBranches: 10 }
        };
        
        return tierConfig[planType] ? tierConfig[planType].maxBranches : 1;
    },
    
    getCurrentBranchCount() {
        const branches = JSON.parse(localStorage.getItem('umi_branches') || '[]');
        return branches.length;
    },
    
    canAddBranch() {
        return this.getCurrentBranchCount() < this.getMaxBranches();
    },
    
    canDeleteBranch() {
        return this.getCurrentBranchCount() > 1; // Can't delete the last branch
    },
    
    getNextTier() {
        const tiers = [
            { name: 'Care', maxBranches: 1 },
            { name: 'Care Plus', maxBranches: 3 },
            { name: 'Care Pro', maxBranches: 10 }
        ];
        
        const currentMax = this.getMaxBranches();
        const currentIndex = tiers.findIndex(t => t.maxBranches === currentMax);
        return currentIndex < tiers.length - 1 ? tiers[currentIndex + 1] : tiers[currentIndex];
    }
};
