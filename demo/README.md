# Umi Health Demo Portal

A fully functional demonstration of the Umi Health pharmacy management system with Zambian healthcare context. This demo runs entirely in the browser using local storage - no backend required!

## 🚀 Quick Start

1. **Start the demo server:**
   ```bash
   cd demo
   python -m http.server 8080
   ```

2. **Open in browser:**
   Navigate to `http://localhost:8080`

3. **Choose your portal:**
   - **Admin Portal** - Complete system administration
   - **Pharmacist Portal** - Pharmacy operations and patient care
   - **Cashier Portal** - Point of sale and payment processing

## 🌟 Features

### ✅ **Fully Functional**
- Complete CRUD operations for all entities
- Real-time data persistence using local storage
- Interactive charts and dashboards
- Search and filtering capabilities
- Data export/import functionality
- No authentication required - open access demo

### 🇿🇲 **Zambian Context**
- Zambian patient names and demographics
- Zambian provinces and locations
- Local medication names and suppliers
- Zambian Kwacha (ZMW) currency
- Realistic healthcare scenarios

### 📱 **Responsive Design**
- Works on desktop, tablet, and mobile
- Modern UI with Tailwind CSS
- Smooth animations and transitions
- Professional pharmacy management interface

## 📊 Portal Capabilities

### **Admin Portal**
- Patient management with full CRUD
- Inventory management with stock tracking
- Prescription oversight and management
- Sales analytics and reporting
- User management simulation
- System configuration and settings
- Export data functionality

### **Pharmacist Portal**
- Prescription processing and management
- Inventory control with alerts
- Patient records and clinical data
- Medication dispensing workflow
- Compliance and regulatory tools
- Supplier management
- Clinical decision support

### **Cashier Portal**
- Point of sale with cart functionality
- Customer management and search
- Payment processing (Cash, Mobile Money, Card, Insurance)
- Sales history and reporting
- Queue management
- Receipt printing simulation
- Shift management

## 💾 Data Management

### **Sample Data Included**
- **Patients:** 3+ Zambian patients with realistic demographics
- **Medications:** 10+ common medications with pricing
- **Prescriptions:** Sample prescriptions with medications
- **Sales:** Transaction history with payment methods
- **Users:** Demo user accounts for each role

### **Data Persistence**
- All data saved to browser local storage
- Automatic data initialization on first load
- Export functionality to download data as JSON
- Reset option to restore sample data
- No server connection required

## 🧪 Testing

Run the functionality test script:
```javascript
// Load test-functionality.js in browser console
// Or open http://localhost:8080/test-functionality.js
```

The test script verifies:
- ✅ Demo storage operations
- ✅ Sample data loading
- ✅ Dashboard statistics
- ✅ Search functionality
- ✅ Data export/import
- ✅ CRUD operations

## 🔧 Technical Details

### **Frontend Stack**
- **HTML5** with semantic markup
- **Tailwind CSS** for styling
- **Alpine.js** for reactivity
- **Chart.js** for data visualization
- **Vanilla JavaScript** for functionality

### **Storage**
- **Browser Local Storage** for data persistence
- **JSON format** for data serialization
- **Automatic backup** and recovery
- **No external dependencies** for storage

### **Architecture**
- **Component-based** design
- **Modular JavaScript** functions
- **Reusable CSS** components
- **Responsive grid** layouts
- **Accessible markup**

## 📁 File Structure

```
demo/
├── index.html                 # Main landing page
├── admin/                     # Admin portal pages
│   ├── home.html
│   ├── patients.html
│   ├── prescriptions.html
│   ├── inventory.html
│   ├── sales.html
│   └── reports.html
├── pharmacist/                # Pharmacist portal pages
│   ├── home.html
│   ├── prescriptions.html
│   ├── inventory.html
│   └── patients.html
├── cashier/                   # Cashier portal pages
│   ├── home.html
│   ├── point-of-sale.html
│   ├── sales.html
│   ├── payments.html
│   └── patients.html
└── shared/                    # Shared resources
    ├── css/
    │   └── shared-styles.css
    └── js/
        └── demo-storage.js
```

## 🎯 Use Cases

### **Demonstration**
- Show pharmacy management capabilities
- Demonstrate Zambian healthcare context
- Present system features to stakeholders
- Training and education purposes

### **Testing**
- UI/UX testing without backend
- Feature validation
- Performance testing
- User acceptance testing

### **Development**
- Frontend development reference
- Component testing
- Integration testing
- API contract validation

## 🔒 Security & Privacy

- **No data transmission** - everything stays local
- **No server connections** - completely offline
- **No personal data collection** - demo data only
- **Secure sandbox** environment
- **GDPR compliant** by design

## 🚀 Deployment

### **Local Development**
```bash
cd demo
python -m http.server 8080
# Visit http://localhost:8080
```

### **Static Hosting**
- Deploy to any static hosting service
- Netlify, Vercel, GitHub Pages, etc.
- No server configuration required
- Instant deployment ready

### **Enterprise**
- Can be packaged as desktop app
- Works in kiosk mode
- Suitable for training environments
- Demo for sales presentations

## 📞 Support

This is a demonstration system. For production deployment or customization:

1. **Review the code** - Fully documented and commented
2. **Test functionality** - Use the built-in test script
3. **Customize data** - Modify sample data in `demo-storage.js`
4. **Extend features** - Add new pages and functionality
5. **Contact development** - For enterprise implementations

## 🎉 Conclusion

The Umi Health Demo Portal provides a complete, realistic pharmacy management experience with Zambian healthcare context. It demonstrates modern web development capabilities while maintaining simplicity and accessibility. Perfect for demonstrations, testing, and training without any infrastructure requirements.

**Key Benefits:**
- ✅ Zero setup required
- ✅ Fully functional
- ✅ Zambian context
- ✅ Professional UI/UX
- ✅ Comprehensive features
- ✅ No backend needed
- ✅ Mobile responsive
- ✅ Export capabilities

Enjoy exploring the demo! 🚀
