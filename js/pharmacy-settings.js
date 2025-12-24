/**
 * Pharmacy Settings Utility
 * Global pharmacy configuration manager for Umi Health System
 * Provides centralized access to pharmacy settings across all modules
 */

class PharmacySettings {
  constructor() {
    this.settings = null;
    this.loadSettings();
  }

  /**
   * Load pharmacy settings from localStorage
   */
  loadSettings() {
    const stored = localStorage.getItem('pharmacySettings');
    if (stored) {
      this.settings = JSON.parse(stored);
    } else {
      // Default settings if none saved
      this.settings = this.getDefaultSettings();
    }
  }

  /**
   * Get default pharmacy settings
   */
  getDefaultSettings() {
    return {
      name: 'Umi Health Pharmacy',
      phone: '+260 977 123 456',
      email: 'info@umihealth.com',
      website: 'www.umihealth.com',
      address: '123 Main Street',
      city: 'Lusaka',
      province: 'Lusaka Province',
      postalCode: '10101',
      // Receipt settings
      receiptHeader: 'Umi Health Pharmacy',
      receiptFooter: 'Thank you for your business!',
      taxRate: 16,
      currency: 'ZMW',
      // Report settings
      reportLogo: '',
      reportSignature: 'Pharmacy Manager',
      // Payment settings
      paymentTerms: 'Payment due upon receipt',
      invoicePrefix: 'INV',
      receiptPrefix: 'RCP'
    };
  }

  /**
   * Get all pharmacy settings
   */
  getSettings() {
    return this.settings || this.getDefaultSettings();
  }

  /**
   * Get specific setting value
   */
  get(key) {
    const settings = this.getSettings();
    return settings[key];
  }

  /**
   * Update pharmacy settings
   */
  update(newSettings) {
    this.settings = { ...this.settings, ...newSettings };
    localStorage.setItem('pharmacySettings', JSON.stringify(this.settings));
  }

  /**
   * Get formatted pharmacy address
   */
  getFormattedAddress() {
    const settings = this.getSettings();
    return `${settings.address}, ${settings.city}, ${settings.province} ${settings.postalCode}`;
  }

  /**
   * Get formatted currency amount
   */
  formatCurrency(amount) {
    const currency = this.get('currency') || 'ZMW';
    return `${currency} ${parseFloat(amount).toFixed(2)}`;
  }

  /**
   * Calculate tax amount
   */
  calculateTax(amount) {
    const taxRate = this.get('taxRate') || 16;
    return (amount * taxRate) / 100;
  }

  /**
   * Calculate total with tax
   */
  calculateTotalWithTax(amount) {
    const tax = this.calculateTax(amount);
    return amount + tax;
  }

  /**
   * Generate receipt number
   */
  generateReceiptNumber() {
    const prefix = this.get('receiptPrefix') || 'RCP';
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000).toString().padStart(3, '0');
    return `${prefix}-${timestamp}-${random}`;
  }

  /**
   * Generate invoice number
   */
  generateInvoiceNumber() {
    const prefix = this.get('invoicePrefix') || 'INV';
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000).toString().padStart(3, '0');
    return `${prefix}-${timestamp}-${random}`;
  }

  /**
   * Get receipt header HTML
   */
  getReceiptHeader() {
    const settings = this.getSettings();
    return `
      <div style="text-align: center; margin-bottom: 20px;">
        <h2 style="margin: 0; color: #000;">${settings.receiptHeader}</h2>
        <p style="margin: 5px 0;">${settings.address}</p>
        <p style="margin: 5px 0;">${settings.city}, ${settings.province}</p>
        <p style="margin: 5px 0;">Phone: ${settings.phone}</p>
        <p style="margin: 5px 0;">Email: ${settings.email}</p>
      </div>
    `;
  }

  /**
   * Get receipt footer HTML
   */
  getReceiptFooter() {
    const settings = this.getSettings();
    return `
      <div style="text-align: center; margin-top: 20px; border-top: 1px solid #ccc; padding-top: 10px;">
        <p style="margin: 5px 0;">${settings.receiptFooter}</p>
        <p style="margin: 5px 0; font-size: 12px;">Tax Rate: ${settings.taxRate}%</p>
      </div>
    `;
  }

  /**
   * Get report header HTML
   */
  getReportHeader() {
    const settings = this.getSettings();
    return `
      <div style="text-align: center; margin-bottom: 30px;">
        <h1 style="margin: 0; color: #000;">${settings.name}</h1>
        <p style="margin: 5px 0;">${this.getFormattedAddress()}</p>
        <p style="margin: 5px 0;">Phone: ${settings.phone} | Email: ${settings.email}</p>
        <p style="margin: 5px 0;">Website: ${settings.website}</p>
      </div>
    `;
  }

  /**
   * Get report footer HTML
   */
  getReportFooter() {
    const settings = this.getSettings();
    const date = new Date().toLocaleDateString();
    return `
      <div style="margin-top: 30px;">
        <p style="margin: 5px 0;">Generated on: ${date}</p>
        <p style="margin: 15px 0;">_____________________________</p>
        <p style="margin: 0;">${settings.reportSignature}</p>
      </div>
    `;
  }
}

// Create global instance
window.pharmacySettings = new PharmacySettings();

// Export for module usage
if (typeof module !== 'undefined' && module.exports) {
  module.exports = PharmacySettings;
}
