/**
 * Pharmacy Receipt Generator
 * Handles receipt generation, printing, and export functionality
 */

class ReceiptGenerator {
    constructor() {
        this.currentReceipt = null;
        this.loadPharmacySettings();
        this.init();
    }

    loadPharmacySettings() {
        // Load pharmacy settings from localStorage (saved from admin portal)
        const savedSettings = localStorage.getItem('pharmacySettings');
        
        if (savedSettings) {
            const settings = JSON.parse(savedSettings);
            this.receiptSettings = {
                pharmacy: {
                    name: settings.name || 'UMI HEALTH PHARMACY',
                    address: `${settings.address || '123 Main Street'}, ${settings.city || 'Lusaka'}, ${settings.province || 'Zambia'}`,
                    phone: settings.phone || '+260 123 456 789',
                    email: settings.email || 'info@umihealth.com',
                    website: settings.website || 'www.umihealth.com',
                    license: settings.license || 'PHZ2025/1234',
                    receiptHeader: settings.receiptHeader || settings.name || 'UMI HEALTH PHARMACY',
                    receiptFooter: settings.receiptFooter || 'Thank you for choosing our pharmacy!'
                },
                currency: settings.currency || 'K',
                taxRate: (settings.taxRate || 16) / 100, // Convert percentage to decimal
                dateFormat: 'DD/MM/YYYY',
                timeFormat: 'HH:mm',
                receiptPrefix: settings.receiptPrefix || 'RCP'
            };
        } else {
            // Default settings if none are saved
            this.receiptSettings = {
                pharmacy: {
                    name: 'UMI HEALTH PHARMACY',
                    address: '123 Main Street, Lusaka, Zambia',
                    phone: '+260 123 456 789',
                    email: 'info@umihealth.com',
                    website: 'www.umihealth.com',
                    license: 'PHZ2025/1234',
                    receiptHeader: 'UMI HEALTH PHARMACY',
                    receiptFooter: 'Thank you for choosing our pharmacy!'
                },
                currency: 'K',
                taxRate: 0.16, // 16% VAT for Zambia
                dateFormat: 'DD/MM/YYYY',
                timeFormat: 'HH:mm',
                receiptPrefix: 'RCP'
            };
        }
    }

    init() {
        this.loadReceiptData();
        this.setupEventListeners();
    }

    loadReceiptData() {
        // Load receipt data from URL parameters or localStorage
        const urlParams = new URLSearchParams(window.location.search);
        const receiptData = urlParams.get('data');
        
        if (receiptData) {
            try {
                this.currentReceipt = JSON.parse(atob(receiptData));
                this.populateReceipt(this.currentReceipt);
            } catch (error) {
                console.error('Error parsing receipt data:', error);
                this.loadSampleData();
            }
        } else {
            this.loadSampleData();
        }
    }

    loadSampleData() {
        // Sample receipt data for demonstration
        this.currentReceipt = {
            transactionId: 'RCP' + Date.now(),
            date: new Date().toISOString(),
            patient: {
                name: 'John Mushibi',
                id: 'P001234',
                prescriptionNumber: 'RX123456',
                insurance: 'MediCare Zambia'
            },
            items: [
                {
                    type: 'prescription',
                    name: 'Amoxicillin',
                    strength: '500mg',
                    dosageForm: 'capsules',
                    quantity: 30,
                    prescriptionNumber: 'RX123456',
                    unitPrice: 10.00,
                    totalPrice: 10.00,
                    copay: 10.00,
                    insuranceCoverage: 0
                },
                {
                    type: 'otc',
                    name: 'Vitamin C',
                    strength: '1000mg',
                    dosageForm: 'tablets',
                    quantity: 1,
                    unitPrice: 8.99,
                    totalPrice: 8.99
                }
            ],
            payment: {
                method: 'debit',
                amount: 19.71,
                cardLastFour: '1234',
                authorizationCode: 'ABC123'
            },
            pharmacist: {
                name: 'Dr. Sarah Mwewa',
                license: 'PHZ7890'
            },
            totals: {
                subtotal: 18.99,
                discount: 0,
                insuranceCoverage: 0,
                tax: 0.72,
                total: 19.71
            }
        };
        
        this.populateReceipt(this.currentReceipt);
    }

    populateReceipt(receipt) {
        // Update pharmacy header with settings
        document.querySelector('.pharmacy-name').textContent = this.receiptSettings.pharmacy.receiptHeader;
        document.querySelector('.pharmacy-address').textContent = this.receiptSettings.pharmacy.address;
        document.querySelector('.pharmacy-contact').textContent = this.receiptSettings.pharmacy.phone;
        document.querySelector('.license-number').textContent = `License #: ${this.receiptSettings.pharmacy.license}`;
        
        // Set transaction info
        const date = new Date(receipt.date);
        document.getElementById('transactionDate').textContent = this.formatDate(date);
        document.getElementById('transactionTime').textContent = this.formatTime(date);
        document.getElementById('receiptNumber').textContent = receipt.transactionId;

        // Set patient info
        document.getElementById('patientName').textContent = receipt.patient.name;
        document.getElementById('patientId').textContent = receipt.patient.id;
        document.getElementById('prescriptionNumber').textContent = receipt.patient.prescriptionNumber;
        document.getElementById('insuranceName').textContent = receipt.patient.insurance;

        // Populate items
        this.populateItems(receipt.items);

        // Set totals
        document.getElementById('subtotal').textContent = this.formatCurrency(receipt.totals.subtotal);
        document.getElementById('discount').textContent = '-' + this.formatCurrency(receipt.totals.discount);
        document.getElementById('insuranceCoverage').textContent = '-' + this.formatCurrency(receipt.totals.insuranceCoverage);
        document.getElementById('tax').textContent = this.formatCurrency(receipt.totals.tax);
        document.getElementById('totalAmount').textContent = this.formatCurrency(receipt.totals.total);

        // Set payment info
        const paymentMethodMap = {
            'cash': 'Cash',
            'debit': 'Debit Card',
            'credit': 'Credit Card',
            'mobile': 'Mobile Money',
            'insurance': 'Insurance'
        };
        
        document.getElementById('paymentMethod').textContent = paymentMethodMap[receipt.payment.method] || receipt.payment.method;
        document.getElementById('amountPaid').textContent = this.formatCurrency(receipt.payment.amount);

        // Show payment method specific details
        if (receipt.payment.method === 'cash') {
            document.getElementById('changeInfo').style.display = 'block';
            document.getElementById('changeGiven').textContent = this.formatCurrency(receipt.payment.change || 0);
        } else if (receipt.payment.method === 'debit' || receipt.payment.method === 'credit') {
            document.getElementById('cardInfo').style.display = 'block';
            document.getElementById('cardLastFour').textContent = receipt.payment.cardLastFour;
            document.getElementById('authCode').style.display = 'block';
            document.getElementById('authorizationCode').textContent = receipt.payment.authorizationCode;
        }

        // Set pharmacist info
        document.getElementById('pharmacistName').textContent = receipt.pharmacist.name + ' (' + receipt.pharmacist.license + ')';
        
        // Update footer with pharmacy settings
        document.querySelector('.thank-you').textContent = this.receiptSettings.pharmacy.receiptFooter;
        document.querySelector('.hours-info').innerHTML = `Hours: Mon-Fri 8AM-8PM, Sat 9AM-6PM, Sun 10AM-4PM<br>${this.receiptSettings.pharmacy.website}`;
    }

    populateItems(items) {
        const itemsContainer = document.getElementById('receiptItems');
        itemsContainer.innerHTML = '';

        items.forEach(item => {
            const itemElement = document.createElement('div');
            itemElement.className = `receipt-item ${item.type}-item`;
            
            let itemMeta = [];
            if (item.strength) itemMeta.push(item.strength);
            if (item.dosageForm) itemMeta.push(item.dosageForm);
            if (item.quantity) itemMeta.push(`Qty: ${item.quantity}`);
            
            if (item.type === 'prescription') {
                if (item.prescriptionNumber) itemMeta.push(`Rx #${item.prescriptionNumber}`);
                if (item.copay !== undefined) itemMeta.push(`Copay: ${this.formatCurrency(item.copay)}`);
            }

            itemElement.innerHTML = `
                <div class="item-details">
                    <div class="item-name">${item.name}</div>
                    <div class="item-meta">${itemMeta.join(' | ')}</div>
                </div>
                <div class="item-price">
                    ${this.formatCurrency(item.totalPrice)}
                </div>
            `;

            itemsContainer.appendChild(itemElement);
        });
    }

    formatDate(date) {
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        return `${day}/${month}/${year}`;
    }

    formatTime(date) {
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');
        return `${hours}:${minutes}`;
    }

    formatCurrency(amount) {
        return `${this.receiptSettings.currency}${amount.toFixed(2)}`;
    }

    calculateTotals(items) {
        let subtotal = 0;
        let taxableAmount = 0;

        items.forEach(item => {
            subtotal += item.totalPrice || 0;
            if (item.type === 'otc') {
                taxableAmount += item.totalPrice || 0;
            }
        });

        const tax = taxableAmount * this.receiptSettings.taxRate;
        const total = subtotal + tax;

        return {
            subtotal,
            tax,
            total
        };
    }

    generateReceiptId() {
        const timestamp = Date.now();
        const random = Math.floor(Math.random() * 1000);
        return `${this.receiptSettings.receiptPrefix}${timestamp}${random}`;
    }

    // Export functions
    printReceipt() {
        window.print();
    }

    downloadReceipt() {
        this.downloadAsPDF();
    }

    downloadAsPDF() {
        // Simple HTML to PDF conversion using browser print to PDF
        // In production, you might want to use a library like jsPDF or html2canvas
        const originalTitle = document.title;
        document.title = `Receipt_${this.currentReceipt.transactionId}`;
        
        window.print();
        
        setTimeout(() => {
            document.title = originalTitle;
        }, 100);
    }

    emailReceipt() {
        const emailSubject = encodeURIComponent(`Receipt ${this.currentReceipt.transactionId} - Umi Health Pharmacy`);
        const emailBody = encodeURIComponent(this.generateEmailBody());
        
        window.location.href = `mailto:?subject=${emailSubject}&body=${emailBody}`;
    }

    generateEmailBody() {
        const receipt = this.currentReceipt;
        let body = `Dear ${receipt.patient.name},\n\n`;
        body += `Thank you for your purchase at Umi Health Pharmacy.\n\n`;
        body += `Receipt Number: ${receipt.transactionId}\n`;
        body += `Date: ${this.formatDate(new Date(receipt.date))}\n`;
        body += `Time: ${this.formatTime(new Date(receipt.date))}\n\n`;
        
        body += `Items Purchased:\n`;
        receipt.items.forEach(item => {
            body += `- ${item.name}`;
            if (item.strength) body += ` ${item.strength}`;
            if (item.quantity) body += ` (Qty: ${item.quantity})`;
            body += ` - ${this.formatCurrency(item.totalPrice)}\n`;
        });
        
        body += `\nTotal Amount: ${this.formatCurrency(receipt.totals.total)}\n`;
        body += `Payment Method: ${receipt.payment.method}\n\n`;
        
        body += `Please keep this receipt for your records.\n\n`;
        body += `If you have any questions about your prescription, please consult your pharmacist.\n\n`;
        body += `Thank you for choosing Umi Health Pharmacy!\n`;
        body += `${this.receiptSettings.pharmacy.name}\n`;
        body += `${this.receiptSettings.pharmacy.address}\n`;
        body += `${this.receiptSettings.pharmacy.phone}`;
        
        return body;
    }

    // Static method to create receipt from transaction data
    static createFromTransaction(transactionData) {
        const generator = new ReceiptGenerator();
        
        const receipt = {
            transactionId: generator.generateReceiptId(),
            date: new Date().toISOString(),
            patient: transactionData.patient || {},
            items: transactionData.items || [],
            payment: transactionData.payment || {},
            pharmacist: transactionData.pharmacist || {
                name: 'Available Pharmacist',
                license: 'PHZ7890'
            },
            totals: generator.calculateTotals(transactionData.items || [])
        };

        return receipt;
    }

    // Static method to open receipt in new window
    static openReceipt(receiptData) {
        const encodedData = btoa(JSON.stringify(receiptData));
        const receiptUrl = `receipt.html?data=${encodedData}`;
        window.open(receiptUrl, '_blank', 'width=500,height=800,scrollbars=yes');
    }
}

// Global functions for button clicks
function printReceipt() {
    if (window.receiptGenerator) {
        window.receiptGenerator.printReceipt();
    }
}

function downloadReceipt() {
    if (window.receiptGenerator) {
        window.receiptGenerator.downloadReceipt();
    }
}

function emailReceipt() {
    if (window.receiptGenerator) {
        window.receiptGenerator.emailReceipt();
    }
}

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', function() {
    window.receiptGenerator = new ReceiptGenerator();
});
