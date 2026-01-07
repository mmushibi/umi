# Umi Health Pharmacy Management System

A comprehensive, enterprise-grade Pharmacy Point of Sale (POS) and management system tailored to Zambian pharmacy standards. Built with microservices architecture, multi-tenancy support, and modern web technologies.

## 🏗️ Architecture Overview

### Microservices Architecture
- **API Gateway**: Central entry point with routing and load balancing
- **Identity Service**: Authentication, authorization, and user management
- **UmiHealth API**: Core business logic for pharmacy operations
- **Background Jobs**: Asynchronous processing and scheduled tasks
- **Minimal API**: Lightweight endpoints for specific operations

### Technology Stack

#### Backend
- **.NET 8.0/.NET 10.0**: Core framework for APIs
- **PostgreSQL**: Primary database with multi-tenant support
- **Redis**: Caching and session management
- **JWT**: Token-based authentication
- **Serilog**: Structured logging
- **Docker**: Containerization

#### Frontend
- **HTML5/CSS3/JavaScript**: Web-based user interfaces
- **Role-based Dashboards**: Admin, Pharmacist, Cashier, Operations portals
- **Responsive Design**: Mobile-friendly interfaces

#### DevOps & Monitoring
- **Docker Compose**: Multi-container orchestration
- **Prometheus**: Metrics collection
- **Grafana**: Monitoring dashboards
- **GitHub Actions**: CI/CD pipelines
- **Nginx**: Reverse proxy and load balancing

## 🚀 Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 8.0 SDK (for local development)
- PostgreSQL client (optional)

### Environment Setup
1. Clone the repository:
```bash
git clone https://github.com/mmushibi/umi.git
cd umi
```

2. Copy environment configuration:
```bash
cp appsettings.Security.json.example appsettings.Security.json
```

3. Update environment variables in `appsettings.Security.json`

4. Start all services:
```bash
docker-compose up -d
```

### Access Points
- **Main Application**: http://localhost:80
- **API Gateway**: http://localhost:80/api
- **Identity Service**: http://localhost:5001
- **Grafana Dashboard**: http://localhost:3000
- **Prometheus Metrics**: http://localhost:9090

## 📋 Features

### Core Pharmacy Operations
- **Patient Management**: Comprehensive patient records and history
- **Prescription Management**: Digital prescription handling and validation
- **Inventory Management**: Real-time stock tracking and alerts
- **Point of Sale**: Integrated billing and payment processing
- **Reporting & Analytics**: Business intelligence and compliance reports

### Multi-Tenancy
- **Tenant Isolation**: Complete data separation between pharmacies
- **Branch Management**: Multi-branch support within tenants
- **Role-Based Access**: Granular permissions and user management
- **Audit Logging**: Comprehensive activity tracking

### Advanced Features
- **Real-time Notifications**: SignalR-based live updates
- **Background Processing**: Automated tasks and reminders
- **Health Monitoring**: System health checks and alerts
- **API Documentation**: OpenAPI/Swagger integration

## 🔐 Security

- **JWT Authentication**: Secure token-based authentication
- **Role-Based Authorization**: Granular access control
- **Rate Limiting**: API protection against abuse
- **Data Encryption**: Sensitive data protection
- **Audit Trails**: Complete activity logging

## 📚 Documentation

- [API Documentation](docs/API_DOCUMENTATION.md)
- [Authentication & Authorization](docs/AUTHENTICATION_AUTHORIZATION.md)
- [Multi-Tenant Architecture](docs/MULTI_TENANT_ARCHITECTURE.md)
- [Implementation Guide](docs/IMPLEMENTATION_GUIDE.md)
- [Production Deployment](docs/PRODUCTION_DEPLOYMENT_GUIDE.md)
- [User Manuals](docs/user-manuals/)

## 🏢 User Portals

### Admin Portal
- System configuration and user management
- Branch management and reporting
- Access: `/portals/admin/`

### Pharmacist Portal
- Prescription management and patient care
- Inventory and clinical operations
- Access: `/portals/pharmacist/`

### Cashier Portal
- Point of sale and payment processing
- Customer management and sales reporting
- Access: `/portals/cashier/`

### Operations Portal
- Branch operations and staff management
- Compliance and administrative tasks
- Access: `/portals/operations/`

## 🧪 Testing

### API Testing
```bash
# Run Postman tests
./api-testing/scripts/run-postman-tests.ps1

# Generate API documentation
./api-testing/scripts/generate-api-docs.ps1
```

### Integration Tests
```bash
cd backend
dotnet test UmiHealth.MinimalApi.Tests/
```

## 🚀 Deployment

### Development
```bash
docker-compose -f docker-compose.yml up -d
```

### Production
```bash
docker-compose -f docker-compose.yml -f docker-compose.override.yml --profile production up -d
```

### Azure Deployment
See [Azure Deployment Guide](scripts/azure-staging-deploy.sh) for cloud deployment.

## 📊 Monitoring

### Health Checks
- API Gateway: `GET /health`
- All services include comprehensive health endpoints

### Metrics
- Prometheus metrics available at `/metrics`
- Grafana dashboards for system monitoring
- Custom business metrics and KPIs

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 📞 Support

For support and questions:
- Email: support@umihealth.com
- Documentation: [docs/](docs/)
- Issues: [GitHub Issues](https://github.com/mmushibi/umi/issues)

---

**Umi Health Information Systems** - Empowering Pharmacy Management in Zambia
