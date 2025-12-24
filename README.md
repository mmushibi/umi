# Umi Health Pharmacy Management System

A comprehensive multi-tenant pharmacy management system with branch support, built with modern web technologies.

## 🚀 Quick Start

### Prerequisites
- Node.js 16+ and npm
- .NET 8.0 SDK
- PostgreSQL 14+
- Git

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd Umi_Health
```

2. **Install dependencies**
```bash
# Frontend dependencies
npm install

# Backend dependencies
cd backend
dotnet restore
```

3. **Set up Tailwind CSS**
```bash
# Build CSS for development
npm run build-css

# Build CSS for production
npm run build-css-prod
```

4. **Configure environment**
```bash
# Copy and configure settings
cp backend/src/UmiHealth.Api/appsettings.Development.json.example backend/src/UmiHealth.Api/appsettings.Development.json
```

5. **Run the application**
```bash
# Start frontend development server
npm run dev

# Start backend API server
cd backend
dotnet run --project UmiHealth.Api
```

## 🏗️ Architecture

### Multi-Tenant System
- **Tenant Isolation**: Separate database schemas per tenant
- **Branch Support**: Multiple branches per tenant with role-based access
- **Subscription Management**: 14-day trial with upgrade system
- **JWT Authentication**: Secure token-based authentication with auto-refresh

### Frontend Structure
```
public/                 # Public pages (signin, signup)
├── account/           # Account setup and management
├── js/               # Shared JavaScript utilities
└── assets/            # CSS and static assets

portals/               # Role-based dashboards
├── admin/             # Administrative interface
├── pharmacist/         # Pharmacy management
├── cashier/            # Point of sale
└── operations/         # Operations management

shared/                # Shared resources
├── css/               # Common styles
└── js/                # Shared utilities
```

### Backend Structure
```
backend/src/
├── UmiHealth.Api/          # Web API controllers and middleware
├── UmiHealth.Application/    # Business logic and services
├── UmiHealth.Domain/         # Domain entities and interfaces
└── UmiHealth.Infrastructure/ # Data access and external services
```

## 🔧 Configuration

### Tailwind CSS
Custom configuration with Umi Health branding:
- **Primary Colors**: Blue (#2563EB) and Teal (#14B8A6)
- **Custom Components**: Auth cards, trial banners, form inputs
- **Responsive Design**: Mobile-first approach
- **Custom Animations**: Fade-in, slide-up, pulse effects

### Database
- **Shared Schema**: Tenants, users, subscriptions
- **Tenant Schemas**: Patient data, inventory, prescriptions, sales
- **Dynamic Connections**: Per-tenant database connections

## 🔐 Authentication & Security

### Features
- **Multi-identifier Login**: Username, email, or phone number
- **JWT Tokens**: Access and refresh tokens with auto-renewal
- **Role-based Access**: Admin, pharmacist, cashier, operations roles
- **Branch Permissions**: Granular access control per branch

### Trial System
- **14-Day Free Trial**: Automatic trial creation for new tenants
- **Upgrade Reminders**: Banner notifications in last 7 days
- **Subscription Plans**: Basic, Professional, Enterprise tiers
- **Graceful Expiration**: Service interruption handling

## 📱 Frontend Features

### Authentication Pages
- **Signup**: Multi-step pharmacy registration with trial creation
- **Signin**: Multi-identifier login with remember me
- **Setup**: Complete pharmacy configuration and subscription upgrade

### Dashboard Portals
- **Admin Portal**: Tenant management, user administration, reporting
- **Pharmacist Portal**: Patient management, prescriptions, inventory
- **Cashier Portal**: Point of sale, receipt generation
- **Operations Portal**: Analytics, compliance, system monitoring

## 🛠️ Development

### Available Scripts
```bash
npm run build-css      # Watch and build CSS
npm run build-css-prod # Minified CSS build
npm run dev            # Development server with live reload
npm run install-deps   # Install all dependencies
```

### Custom Components
- **Auth API**: Unified authentication and tenant management
- **Data Sync**: Centralized data management with localStorage
- **Role-based Access**: Permission system and navigation control

## 🚀 Deployment

### Production Build
```bash
# Build optimized CSS
npm run build-css-prod

# Build backend for production
cd backend
dotnet publish -c Release -o ./publish
```

### Environment Variables
- **JWT Configuration**: Secret keys and token lifetimes
- **Database**: Connection strings and pool settings
- **API**: Base URLs and CORS configuration

## 📊 Monitoring & Analytics

### Built-in Features
- **Audit Logging**: Comprehensive activity tracking
- **Performance Metrics**: Request timing and error rates
- **Health Checks**: Service availability monitoring
- **Compliance Reporting**: Regulatory compliance tracking

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📄 License

MIT License - see LICENSE file for details

## 📞 Support

For support and documentation, visit the project repository or contact the development team.

---

**Built with ❤️ for Zambian pharmacies**
