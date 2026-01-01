/**
 * Demo Data for Umi Health Portals - Zambia
 * Provides sample data for all portal types with Zambian context
 */

// Demo Users
const demoUsers = {
  admin: {
    id: 'demo-admin',
    name: 'Bwalya Mwansa',
    email: 'b.mwansa@umizambia.com',
    role: 'Administrator',
    branch: 'Lusaka Main Branch',
    permissions: ['read', 'write', 'delete', 'admin']
  },
  cashier: {
    id: 'demo-cashier',
    name: 'Chipo Banda',
    email: 'c.banda@umizambia.com',
    role: 'Cashier',
    branch: 'Lusaka Main Branch',
    permissions: ['read', 'write']
  },
  pharmacist: {
    id: 'demo-pharmacist',
    name: 'Dr. Mutale Chanda',
    email: 'm.chanda@umizambia.com',
    role: 'Pharmacist',
    licenseNumber: 'ZMP2024001',
    branch: 'Lusaka Main Branch',
    permissions: ['read', 'write', 'clinical']
  }
};

// Demo Patients
const demoPatients = [
  {
    id: '1',
    name: 'James Mulenga',
    email: 'j.mulenga@email.com',
    phone: '+260 97 1234567',
    dob: '1985-03-15',
    address: 'Plot 1234 Kabulonga Road, Lusaka',
    emergencyContact: 'Mary Mulenga (+260 97 1234568)',
    allergies: ['Penicillin'],
    conditions: ['Hypertension'],
    memberSince: '2022-05-15',
    lastVisit: '2026-01-10',
    insuranceProvider: 'Zamcare Medical Scheme',
    policyNumber: 'ZMS2024001234',
    nrc: '123456/78/9'
  },
  {
    id: '2',
    name: 'Esther Phiri',
    email: 'e.phiri@email.com',
    phone: '+260 96 9876543',
    dob: '1990-07-20',
    address: 'House 56 Rhodes Park, Lusaka',
    emergencyContact: 'Joseph Phiri (+260 96 9876544)',
    allergies: [],
    conditions: ['Diabetes Type 2'],
    memberSince: '2023-03-10',
    lastVisit: '2026-01-12',
    insuranceProvider: 'Madison General Insurance',
    policyNumber: 'MGI2024005678',
    nrc: '234567/89/0'
  },
  {
    id: '3',
    name: 'Michael Bwalya',
    email: 'm.bwalya@email.com',
    phone: '+260 95 4567890',
    dob: '1978-11-30',
    address: 'Stand 234 Chilenje, Lusaka',
    emergencyContact: 'Grace Bwalya (+260 95 4567891)',
    allergies: ['Sulfa drugs'],
    conditions: ['Asthma'],
    memberSince: '2021-08-22',
    lastVisit: '2026-01-14',
    insuranceProvider: 'Zamcare Medical Scheme',
    policyNumber: 'ZMS2024009012',
    nrc: '345678/90/1'
  },
  {
    id: '4',
    name: 'Agnes Tembo',
    email: 'a.tembo@email.com',
    phone: '+260 97 2345678',
    dob: '1992-02-28',
    address: 'Flat 12 Manda Hill, Lusaka',
    emergencyContact: 'Peter Tembo (+260 97 2345679)',
    allergies: ['Aspirin'],
    conditions: ['Migraine'],
    memberSince: '2024-01-15',
    lastVisit: '2026-01-15',
    insuranceProvider: 'Prudential Life Zambia',
    policyNumber: 'PLZ2024003456',
    nrc: '456789/01/2'
  },
  {
    id: '5',
    name: 'Kennedy Mwila',
    email: 'k.mwila@email.com',
    phone: '+260 96 7890123',
    dob: '1988-09-12',
    address: 'Plot 567 Kalingalinga, Lusaka',
    emergencyContact: 'Susan Mwila (+260 96 7890124)',
    allergies: [],
    conditions: ['Allergic Rhinitis'],
    memberSince: '2023-06-20',
    lastVisit: '2026-01-13',
    insuranceProvider: 'Madison General Insurance',
    policyNumber: 'MGI2024007890',
    nrc: '567890/12/3'
  }
];

// Demo Products/Medications
const demoProducts = [
  {
    id: '1',
    name: 'Paracetamol 500mg',
    sku: 'PAR001',
    price: 45.50,
    stock: 150,
    lowStock: false,
    category: 'Pain Relief',
    expiryDate: '2026-12-31',
    manufacturer: 'Pharma Ltd Zambia',
    controlledSubstance: false,
    description: 'Pain relief medication',
    dosageForm: 'Tablet'
  },
  {
    id: '2',
    name: 'Ibuprofen 400mg',
    sku: 'IBU002',
    price: 68.75,
    stock: 12,
    lowStock: true,
    category: 'Pain Relief',
    expiryDate: '2026-09-15',
    manufacturer: 'MediSource Zambia',
    controlledSubstance: false,
    description: 'Anti-inflammatory medication',
    dosageForm: 'Tablet'
  },
  {
    id: '3',
    name: 'Amoxicillin 250mg',
    sku: 'AMX003',
    price: 125.00,
    stock: 45,
    lowStock: false,
    category: 'Antibiotics',
    expiryDate: '2026-08-31',
    manufacturer: 'ZamPharma Industries',
    controlledSubstance: false,
    description: 'Broad-spectrum antibiotic',
    dosageForm: 'Capsule'
  },
  {
    id: '4',
    name: 'Vitamin C 500mg',
    sku: 'VIT004',
    price: 89.90,
    stock: 120,
    lowStock: false,
    category: 'Vitamins',
    expiryDate: '2027-03-31',
    manufacturer: 'NutriHealth Zambia',
    controlledSubstance: false,
    description: 'Vitamin C supplement',
    dosageForm: 'Tablet'
  },
  {
    id: '5',
    name: 'Cough Syrup',
    sku: 'COU005',
    price: 56.25,
    stock: 67,
    lowStock: false,
    category: 'Cold & Flu',
    expiryDate: '2026-07-15',
    manufacturer: 'RemedyCo Zambia',
    controlledSubstance: false,
    description: 'Cough and cold relief',
    dosageForm: 'Syrup'
  },
  {
    id: '6',
    name: 'Artemether/Lumefantrine',
    sku: 'MAL006',
    price: 245.00,
    stock: 30,
    lowStock: false,
    category: 'Antimalarial',
    expiryDate: '2026-11-30',
    manufacturer: 'ZamPharma Industries',
    controlledSubstance: false,
    description: 'Malaria treatment',
    dosageForm: 'Tablet'
  },
  {
    id: '7',
    name: 'ORS Solution',
    sku: 'ORS007',
    price: 12.50,
    stock: 200,
    lowStock: false,
    category: 'Rehydration',
    expiryDate: '2027-01-31',
    manufacturer: 'MediSource Zambia',
    controlledSubstance: false,
    description: 'Oral rehydration salts',
    dosageForm: 'Powder'
  }
];

// Demo Prescriptions
const demoPrescriptions = [
  {
    id: '1',
    patientId: '1',
    patientName: 'James Mulenga',
    medication: 'Amoxicillin 500mg',
    dosage: '1 capsule twice daily',
    duration: '7 days',
    prescribedBy: 'Dr. Mutale Chanda',
    date: '2026-01-15',
    status: 'pending',
    priority: 'normal',
    notes: 'Take after meals'
  },
  {
    id: '2',
    patientId: '2',
    patientName: 'Esther Phiri',
    medication: 'Paracetamol 500mg',
    dosage: '1 tablet as needed for pain',
    duration: '30 days',
    prescribedBy: 'Dr. Bwalya Mwansa',
    date: '2026-01-15',
    status: 'approved',
    priority: 'normal',
    notes: 'Maximum 4 tablets per day'
  },
  {
    id: '3',
    patientId: '3',
    patientName: 'Michael Bwalya',
    medication: 'Ibuprofen 400mg',
    dosage: '1 tablet three times daily',
    duration: '5 days',
    prescribedBy: 'Dr. Chipo Banda',
    date: '2026-01-14',
    status: 'dispensed',
    priority: 'high',
    notes: 'Take with food'
  },
  {
    id: '4',
    patientId: '4',
    patientName: 'Agnes Tembo',
    medication: 'Artemether/Lumefantrine',
    dosage: '4 tablets twice daily for 3 days',
    duration: '3 days',
    prescribedBy: 'Dr. Mutale Chanda',
    date: '2026-01-16',
    status: 'pending',
    priority: 'high',
    notes: 'Complete full course even if feeling better'
  },
  {
    id: '5',
    patientId: '5',
    patientName: 'Kennedy Mwila',
    medication: 'ORS Solution',
    dosage: '1 sachet in 1 liter water',
    duration: 'Until diarrhea stops',
    prescribedBy: 'Dr. Bwalya Mwansa',
    date: '2026-01-16',
    status: 'approved',
    priority: 'normal',
    notes: 'Use clean water'
  }
];

// Demo Sales
const demoSales = [
  {
    id: '1',
    total: 567.25,
    subtotal: 520.23,
    tax: 47.02,
    items: [
      { name: 'Paracetamol', qty: 2, price: 45.50, total: 91.00 },
      { name: 'Ibuprofen', qty: 1, price: 68.75, total: 68.75 },
      { name: 'Vitamin C', qty: 1, price: 89.90, total: 89.90 },
      { name: 'ORS Solution', qty: 3, price: 12.50, total: 37.50 },
      { name: 'Cough Syrup', qty: 2, price: 56.25, total: 112.50 }
    ],
    customer: 'James Mulenga',
    date: '2026-01-15',
    status: 'completed',
    paymentMethod: 'Mobile Money',
    cashier: 'Chipo Banda'
  },
  {
    id: '2',
    total: 345.00,
    subtotal: 316.51,
    tax: 28.49,
    items: [
      { name: 'Paracetamol', qty: 2, price: 45.50, total: 91.00 },
      { name: 'Cough Syrup', qty: 1, price: 56.25, total: 56.25 },
      { name: 'ORS Solution', qty: 4, price: 12.50, total: 50.00 },
      { name: 'Vitamin C', qty: 1, price: 89.90, total: 89.90 }
    ],
    customer: 'Esther Phiri',
    date: '2026-01-15',
    status: 'completed',
    paymentMethod: 'Cash',
    cashier: 'Chipo Banda'
  },
  {
    id: '3',
    total: 892.50,
    subtotal: 818.35,
    tax: 74.15,
    items: [
      { name: 'Amoxicillin', qty: 3, price: 125.00, total: 375.00 },
      { name: 'Artemether/Lumefantrine', qty: 2, price: 245.00, total: 490.00 },
      { name: 'ORS Solution', qty: 1, price: 12.50, total: 12.50 }
    ],
    customer: 'Michael Bwalya',
    date: '2026-01-14',
    status: 'completed',
    paymentMethod: 'Bank Transfer',
    cashier: 'Chipo Banda'
  }
];

// Demo Branches
const demoBranches = [
  {
    id: '1',
    name: 'Lusaka Main Branch',
    address: 'Stand 2345 Cairo Road, Lusaka',
    phone: '+260 211 234567',
    email: 'lusaka@umizambia.com',
    manager: 'Bwalya Mwansa',
    status: 'active',
    openTime: '08:00',
    closeTime: '20:00'
  },
  {
    id: '2',
    name: 'Kitwe Branch',
    address: 'Plot 5678 Obote Avenue, Kitwe',
    phone: '+260 212 345678',
    email: 'kitwe@umizambia.com',
    manager: 'Grace Mwape',
    status: 'active',
    openTime: '08:00',
    closeTime: '20:00'
  },
  {
    id: '3',
    name: 'Livingstone Branch',
    address: 'House 123 Mosi-oa-Tunya Road, Livingstone',
    phone: '+260 213 456789',
    email: 'livingstone@umizambia.com',
    manager: 'John Silavwe',
    status: 'active',
    openTime: '08:00',
    closeTime: '18:00'
  },
  {
    id: '4',
    name: 'Ndola Branch',
    address: 'Shop 456 Main Street, Ndola',
    phone: '+260 212 567890',
    email: 'ndola@umizambia.com',
    manager: 'Mary Banda',
    status: 'inactive',
    openTime: '08:00',
    closeTime: '20:00'
  }
];

// Demo Suppliers
const demoSuppliers = [
  {
    id: '1',
    name: 'ZamPharma Industries Ltd',
    contact: 'Mr. Bwalya Kunda',
    phone: '+260 211 123456',
    email: 'info@zampharma.co.zm',
    status: 'active',
    rating: 4.8,
    deliveryTime: '2-3 days',
    address: 'Industrial Area, Lusaka'
  },
  {
    id: '2',
    name: 'MediSource Zambia',
    contact: 'Ms. Chileshe Mulenga',
    phone: '+260 977 234567',
    email: 'orders@medisource.co.zm',
    status: 'active',
    rating: 4.5,
    deliveryTime: '3-5 days',
    address: 'Kafue Road, Lusaka'
  },
  {
    id: '3',
    name: 'HealthPlus Distributors',
    contact: 'Mr. Joseph Phiri',
    phone: '+260 966 345678',
    email: 'sales@healthplus.co.zm',
    status: 'active',
    rating: 4.2,
    deliveryTime: '5-7 days',
    address: 'Chelston, Lusaka'
  }
];

// Demo Queue Data
const demoQueue = [
  {
    id: '1',
    patientId: '1',
    patientName: 'James Mulenga',
    service: 'Prescription Pickup',
    waitTime: 5,
    priority: 'normal',
    addedAt: '2026-01-16T10:30:00Z'
  },
  {
    id: '2',
    patientId: '2',
    patientName: 'Esther Phiri',
    service: 'Consultation',
    waitTime: 12,
    priority: 'normal',
    addedAt: '2026-01-16T10:25:00Z'
  },
  {
    id: '3',
    patientId: '3',
    patientName: 'Michael Bwalya',
    service: 'New Prescription',
    waitTime: 18,
    priority: 'high',
    addedAt: '2026-01-16T10:20:00Z'
  },
  {
    id: '4',
    patientId: '4',
    patientName: 'Agnes Tembo',
    service: 'Medication Review',
    waitTime: 8,
    priority: 'normal',
    addedAt: '2026-01-16T10:35:00Z'
  }
];

// Demo Settings
const demoSettings = {
  pharmacy: {
    name: 'Umi Health Zambia',
    address: 'Stand 2345 Cairo Road, Lusaka, Zambia',
    phone: '+260 211 234567',
    email: 'info@umizambia.com',
    timezone: 'Africa/Lusaka',
    currency: 'ZMW',
    taxRate: 16.0,
    registrationNumber: 'ZMP/2024/001234'
  },
  system: {
    systemName: 'Umi Health Zambia',
    version: '2.0.0',
    maintenance: false,
    backupEnabled: true,
    logLevel: 'info',
    country: 'Zambia',
    regulatoryBody: 'Pharmacy and Poisons Board'
  },
  notifications: {
    emailNotifications: true,
    smsNotifications: true,
    pushNotifications: true,
    prescriptionAlerts: true,
    inventoryAlerts: true,
    lowStockAlerts: true
  }
};

// Export demo data
window.demoData = {
  users: demoUsers,
  patients: demoPatients,
  products: demoProducts,
  prescriptions: demoPrescriptions,
  sales: demoSales,
  branches: demoBranches,
  suppliers: demoSuppliers,
  queue: demoQueue,
  settings: demoSettings
};

// Helper functions
window.demoHelpers = {
  getRandomId: () => Math.random().toString(36).substr(2, 9),
  getCurrentUser: (role) => demoUsers[role] || demoUsers.admin,
  getPatients: () => demoPatients,
  getProducts: () => demoProducts,
  getPrescriptions: () => demoPrescriptions,
  getSales: () => demoSales,
  getBranches: () => demoBranches,
  getSuppliers: () => demoSuppliers,
  getQueue: () => demoQueue,
  getSettings: () => demoSettings
};
