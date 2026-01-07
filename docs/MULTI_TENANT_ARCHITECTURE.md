# Umi Health Multi-Tenant Architecture Design

## Overview

This document outlines the comprehensive multi-tenant architecture for Umi Health Pharmacy Management System, designed to support multiple pharmacy chains with branch-level isolation, scalability, and security using modern .NET technologies and Docker containerization.

## 1. Architecture Overview

```text
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            Frontend Applications                                │
├─────────────────────────────────────────────────────────────────────────────────┤
│  Admin Portal │ Pharmacist Portal │ Cashier Portal │ Operations Portal │ Demo   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              API Gateway (.NET 8.0)                             │
│  (Authentication, Rate Limiting, Routing, Load Balancing, CORS, Caching)      │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          Microservices Layer                                    │
├─────────────────────────────────────────────────────────────────────────────────┤
│ Identity Service │ UmiHealth API │ Background Jobs │ Minimal API (.NET 10.0)   │
│ Tenant Management │ User Management │ Branch Management │ Pharmacy Operations  │
│ Inventory API │ POS API │ Reports API │ Payment Service │ Notification API   │
└─────────────────────────────────────────────────────────────────────────────────┘
                                      │
┌─────────────────────────────────────────────────────────────────────────────────┐
│                               Data Layer                                        │
├─────────────────────────────────────────────────────────────────────────────────┤
│  PostgreSQL 15 (Multi-Tenant) │ Redis 7 Cache │ File Storage │ Monitoring      │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## 2. Technology Stack

### 2.1 Backend Services
- **.NET 8.0**: Main API and Identity services
- **.NET 10.0**: Minimal API for lightweight operations
- **PostgreSQL 15**: Multi-tenant database with row-level security
- **Redis 7**: Caching and session management
- **Docker**: Containerization and orchestration

### 2.2 Frontend Portals
- **HTML5/CSS3/JavaScript**: Modern web standards
- **Responsive Design**: Mobile-friendly interfaces
- **JWT Authentication**: Secure token-based access
- **SignalR**: Real-time notifications

### 2.3 DevOps & Monitoring
- **Docker Compose**: Multi-container deployment
- **Prometheus**: Metrics collection and monitoring
- **Grafana**: Visualization and alerting
- **Serilog**: Structured logging

## 3. Database Schema Design

### 3.1 Shared Schema (System-Wide)

```sql
-- Tenant Management
CREATE SCHEMA shared;

-- Tenants Table
CREATE TABLE shared.tenants (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name VARCHAR(255) NOT NULL,
    subdomain VARCHAR(100) UNIQUE NOT NULL,
    database_name VARCHAR(100) UNIQUE NOT NULL,
    status VARCHAR(50) DEFAULT 'active',
    subscription_plan VARCHAR(50) DEFAULT 'basic',
    max_branches INTEGER DEFAULT 1,
    max_users INTEGER DEFAULT 10,
    settings JSONB DEFAULT '{}',
    billing_info JSONB DEFAULT '{}',
    compliance_settings JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Branch Management
CREATE TABLE shared.branches (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES shared.tenants(id),
    name VARCHAR(255) NOT NULL,
    code VARCHAR(50) NOT NULL,
    address TEXT,
    phone VARCHAR(50),
    email VARCHAR(100),
    license_number VARCHAR(100),
    operating_hours JSONB DEFAULT '{}',
    settings JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    UNIQUE(tenant_id, code)
);

-- User Management
CREATE TABLE shared.users (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES shared.tenants(id),
    branch_id UUID REFERENCES shared.branches(id),
    email VARCHAR(255) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    role VARCHAR(50) NOT NULL,
    branch_access UUID[] DEFAULT '{}', -- Array of branch IDs user can access
    permissions JSONB DEFAULT '{}',
    is_active BOOLEAN DEFAULT true,
    last_login TIMESTAMP,
    email_verified BOOLEAN DEFAULT false,
    phone_verified BOOLEAN DEFAULT false,
    two_factor_enabled BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP,
    UNIQUE(tenant_id, email)
);

-- Subscription Management
CREATE TABLE shared.subscriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    tenant_id UUID REFERENCES shared.tenants(id),
    plan_type VARCHAR(50) NOT NULL,
    status VARCHAR(50) DEFAULT 'active',
    billing_cycle VARCHAR(20) DEFAULT 'monthly',
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'ZMW',
    features JSONB DEFAULT '{}',
    limits JSONB DEFAULT '{}',
    start_date DATE NOT NULL,
    end_date DATE,
    auto_renew BOOLEAN DEFAULT true,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- System Configuration
CREATE TABLE shared.system_settings (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    key VARCHAR(100) UNIQUE NOT NULL,
    value JSONB NOT NULL,
    description TEXT,
    is_public BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### 2.2 Tenant-Specific Schema

```sql
-- Create dynamic schema for each tenant
-- Example: tenant_demo, tenant_test, etc.

-- Patient Management
CREATE TABLE tenant_patients (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID NOT NULL,
    patient_number VARCHAR(50) UNIQUE NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    date_of_birth DATE,
    gender VARCHAR(20),
    phone VARCHAR(50),
    email VARCHAR(100),
    address TEXT,
    emergency_contact JSONB,
    medical_history JSONB DEFAULT '{}',
    allergies JSONB DEFAULT '{}',
    insurance_info JSONB DEFAULT '{}',
    status VARCHAR(20) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Product Management
CREATE TABLE tenant_products (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID NOT NULL,
    sku VARCHAR(100) UNIQUE NOT NULL,
    name VARCHAR(255) NOT NULL,
    generic_name VARCHAR(255),
    category VARCHAR(100),
    description TEXT,
    manufacturer VARCHAR(100),
    strength VARCHAR(50),
    form VARCHAR(50),
    requires_prescription BOOLEAN DEFAULT false,
    controlled_substance BOOLEAN DEFAULT false,
    storage_requirements JSONB,
    pricing JSONB DEFAULT '{}',
    barcode VARCHAR(100),
    images JSONB DEFAULT '[]',
    status VARCHAR(20) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    deleted_at TIMESTAMP
);

-- Inventory Management
CREATE TABLE tenant_inventory (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID NOT NULL,
    product_id UUID NOT NULL,
    batch_number VARCHAR(100),
    expiry_date DATE,
    quantity_on_hand INTEGER DEFAULT 0,
    quantity_reserved INTEGER DEFAULT 0,
    reorder_level INTEGER DEFAULT 0,
    reorder_quantity INTEGER DEFAULT 0,
    cost_price DECIMAL(10,2),
    selling_price DECIMAL(10,2),
    supplier_id UUID,
    location VARCHAR(100),
    last_counted DATE,
    status VARCHAR(20) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Prescription Management
CREATE TABLE tenant_prescriptions (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID NOT NULL,
    patient_id UUID NOT NULL,
    prescriber_id UUID NOT NULL,
    prescription_number VARCHAR(50) UNIQUE NOT NULL,
    date_prescribed DATE NOT NULL,
    status VARCHAR(20) DEFAULT 'pending',
    notes TEXT,
    diagnosis TEXT,
    items JSONB NOT NULL, -- Array of prescription items
    dispensed_items JSONB DEFAULT '[]',
    pharmacist_id UUID,
    dispensed_date TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Sales Management
CREATE TABLE tenant_sales (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID NOT NULL,
    sale_number VARCHAR(50) UNIQUE NOT NULL,
    patient_id UUID,
    cashier_id UUID NOT NULL,
    subtotal DECIMAL(10,2) NOT NULL,
    tax_amount DECIMAL(10,2) DEFAULT 0,
    discount_amount DECIMAL(10,2) DEFAULT 0,
    total_amount DECIMAL(10,2) NOT NULL,
    payment_method VARCHAR(50),
    payment_status VARCHAR(20) DEFAULT 'pending',
    items JSONB NOT NULL, -- Array of sale items
    prescriptions UUID[] DEFAULT '{}', -- Linked prescriptions
    status VARCHAR(20) DEFAULT 'active',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Payment Management
CREATE TABLE tenant_payments (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID NOT NULL,
    sale_id UUID REFERENCES tenant_sales(id),
    payment_method VARCHAR(50) NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'ZMW',
    transaction_reference VARCHAR(100),
    payment_gateway VARCHAR(50),
    gateway_response JSONB,
    status VARCHAR(20) DEFAULT 'pending',
    processed_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## 3. API Gateway Design

### 3.1 Gateway Configuration

```yaml
# API Gateway Configuration
gateway:
  port: 8080
  timeout: 30000
  rate_limiting:
    requests_per_minute: 100
    burst_size: 20
  
  authentication:
    jwt_secret: "${JWT_SECRET}"
    token_expiry: 3600
    refresh_token_expiry: 86400
  
  cors:
    allowed_origins: ["https://*.umihealth.com", "http://localhost:3000"]
    allowed_methods: ["GET", "POST", "PUT", "DELETE", "PATCH"]
    allowed_headers: ["Authorization", "Content-Type", "X-Tenant-ID", "X-Branch-ID"]
  
  routes:
    - path: "/api/v1/auth/**"
      service: "user-service"
      auth_required: false
    - path: "/api/v1/tenants/**"
      service: "tenant-service"
      auth_required: true
      roles: ["admin", "super_admin"]
    - path: "/api/v1/pharmacy/**"
      service: "pharmacy-service"
      auth_required: true
      tenant_isolation: true
    - path: "/api/v1/inventory/**"
      service: "inventory-service"
      auth_required: true
      tenant_isolation: true
      branch_isolation: true
```

### 3.2 Middleware Stack

```typescript
// API Gateway Middleware
interface MiddlewareContext {
  request: Request;
  response: Response;
  user?: User;
  tenant?: Tenant;
  branch?: Branch;
}

// Middleware Pipeline
const middleware = [
  requestLogger,
  rateLimiter,
  corsHandler,
  tenantResolver,      // Extract tenant from subdomain/header
  authentication,      // JWT validation
  authorization,       // Role and permission check
  branchResolver,      // Extract branch context
  requestValidator,
  auditLogger,
  errorHandler
];
```

## 4. Microservices Architecture

### 4.1 Service Definitions

#### Tenant Service

```yaml
service: tenant-service
port: 3001
database: shared_schema
responsibilities:
  - Tenant management
  - Subscription management
  - Branch management
  - Tenant-level settings
  - Billing and payments

endpoints:
  - GET /tenants
  - POST /tenants
  - PUT /tenants/{id}
  - DELETE /tenants/{id}
  - GET /tenants/{id}/branches
  - POST /tenants/{id}/branches
  - GET /tenants/{id}/subscriptions
  - POST /tenants/{id}/subscriptions
```

#### User Service

```yaml
service: user-service
port: 3002
database: shared_schema
responsibilities:
  - User authentication
  - User management
  - Role and permission management
  - Branch access control
  - Password management

endpoints:
  - POST /auth/login
  - POST /auth/logout
  - POST /auth/refresh
  - GET /users
  - POST /users
  - PUT /users/{id}
  - GET /users/{id}/permissions
```

#### Pharmacy Service

```yaml
service: pharmacy-service
port: 3003
database: tenant_schema
responsibilities:
  - Patient management
  - Prescription management
  - Clinical workflows
  - Drug interactions
  - Compliance checking

endpoints:
  - GET /patients
  - POST /patients
  - PUT /patients/{id}
  - GET /prescriptions
  - POST /prescriptions
  - PUT /prescriptions/{id}/dispense
```

#### Inventory Service

```yaml
service: inventory-service
port: 3004
database: tenant_schema
responsibilities:
  - Product management
  - Stock management
  - Purchase orders
  - Supplier management
  - Stock movements

endpoints:
  - GET /products
  - POST /products
  - PUT /products/{id}
  - GET /inventory
  - PUT /inventory/{id}/adjust
  - GET /stock-movements
```

#### POS Service

```yaml
service: pos-service
port: 3005
database: tenant_schema
responsibilities:
  - Sales processing
  - Payment processing
  - Receipt generation
  - Discount management
  - Till management

endpoints:
  - POST /sales
  - GET /sales/{id}
  - POST /payments
  - GET /receipts/{id}
  - GET /tills
```

### 4.2 Inter-Service Communication

```typescript
// Service Communication Pattern
interface ServiceMessage {
  eventId: string;
  eventType: string;
  serviceName: string;
  timestamp: string;
  data: any;
  metadata: {
    tenantId: string;
    branchId?: string;
    userId: string;
    correlationId: string;
  };
}

// Event Bus Configuration
const eventBus = {
  provider: 'Redis' | 'RabbitMQ' | 'Apache Kafka',
  topics: {
    'user.created': 'user-service',
    'sale.completed': 'pos-service',
    'inventory.updated': 'inventory-service',
    'prescription.dispensed': 'pharmacy-service'
  }
};
```

## 5. Tenant Isolation Strategy

### 5.1 Data Isolation Levels

1. **Database-Level Isolation**
   - Separate schemas per tenant
   - Row-level security for additional protection
   - Tenant-specific connection pooling

2. **Application-Level Isolation**
   - Tenant context in all requests
   - Automatic tenant filtering in queries
   - Branch-level data segregation

3. **Infrastructure Isolation**
   - Separate containers for high-security tenants
   - Resource quotas per tenant
   - Network segmentation

### 5.2 Tenant Resolution

```typescript
class TenantResolver {
  async resolveTenant(request: Request): Promise<Tenant> {
    // Method 1: Subdomain-based
    const subdomain = this.extractSubdomain(request.hostname);
    
    // Method 2: Header-based
    const tenantId = request.headers['x-tenant-id'];
    
    // Method 3: Custom domain mapping
    const customDomain = request.hostname;
    
    return await this.tenantService.findByIdentifier({
      subdomain,
      id: tenantId,
      customDomain
    });
  }
}
```

## 6. Branch-Level Access Control

### 6.1 Permission Matrix

```typescript
interface BranchPermission {
  userId: string;
  tenantId: string;
  branchIds: string[];
  permissions: {
    canViewAllBranches: boolean;
    canManageAllBranches: boolean;
    canTransferStock: boolean;
    canViewReports: string[]; // Array of branch IDs
    canProcessSales: string[]; // Array of branch IDs
  };
}

// Branch Access Control
class BranchAccessControl {
  async checkAccess(userId: string, branchId: string, action: string): Promise<boolean> {
    const permissions = await this.getUserPermissions(userId);
    
    if (permissions.canManageAllBranches) {
      return true;
    }
    
    return permissions.branchIds.includes(branchId) && 
           this.hasPermissionForAction(permissions, action);
  }
}
```

### 6.2 Data Filtering

```typescript
// Automatic branch filtering in repositories
class BaseRepository {
  async findMany(criteria: any, userContext: UserContext): Promise<any[]> {
    const query = this.buildQuery(criteria);
    
    // Apply tenant filter
    query.where.tenantId = userContext.tenantId;
    
    // Apply branch filter if not admin
    if (!userContext.canViewAllBranches) {
      query.where.branchId = { in: userContext.branchIds };
    }
    
    return await this.database.query(query);
  }
}
```

## 7. Caching Strategy

### 7.1 Multi-Level Caching

```typescript
interface CacheStrategy {
  // L1: In-memory cache (per service instance)
  l1Cache: {
    ttl: 300; // 5 minutes
    maxSize: 1000;
    strategy: 'LRU';
  };
  
  // L2: Redis cache (shared across services)
  l2Cache: {
    ttl: 3600; // 1 hour
    cluster: true;
    persistence: true;
  };
  
  // L3: Database query cache
  l3Cache: {
    ttl: 86400; // 24 hours
    invalidateOnWrite: true;
  };
}

// Cache Key Pattern
const cacheKeys = {
  tenant: (tenantId: string) => `tenant:${tenantId}`,
  user: (userId: string) => `user:${userId}`,
  branch: (branchId: string) => `branch:${branchId}`,
  product: (tenantId: string, branchId: string, sku: string) => 
    `product:${tenantId}:${branchId}:${sku}`,
  inventory: (tenantId: string, branchId: string) => 
    `inventory:${tenantId}:${branchId}`
};
```

### 7.2 Cache Invalidation

```typescript
class CacheInvalidation {
  async onProductUpdate(event: ProductUpdatedEvent): Promise<void> {
    const keys = [
      `product:${event.tenantId}:${event.branchId}:${event.sku}`,
      `inventory:${event.tenantId}:${event.branchId}`,
      `products:${event.tenantId}:${event.branchId}`
    ];
    
    await Promise.all(keys.map(key => this.cache.delete(key)));
  }
}
```

## 8. Audit Logging & Compliance

### 8.1 Audit Trail Design

```sql
-- Audit Log Table (Tenant-Specific)
CREATE TABLE tenant_audit_logs (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID,
    user_id UUID NOT NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id UUID,
    old_values JSONB,
    new_values JSONB,
    ip_address INET,
    user_agent TEXT,
    session_id VARCHAR(255),
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    metadata JSONB DEFAULT '{}'
);

-- Compliance Events
CREATE TABLE tenant_compliance_events (
    id UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    branch_id UUID,
    event_type VARCHAR(100) NOT NULL,
    severity VARCHAR(20) DEFAULT 'info',
    description TEXT,
    data JSONB DEFAULT '{}',
    reported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMP,
    resolution_notes TEXT
);
```

### 8.2 Compliance Features

```typescript
class ComplianceService {
  // Prescription monitoring
  async trackControlledSubstance(prescription: Prescription): Promise<void> {
    if (prescription.containsControlledSubstance) {
      await this.createComplianceEvent({
        type: 'CONTROLLED_SUBSTANCE_DISPENSED',
        severity: 'high',
        data: {
          prescriptionId: prescription.id,
          patientId: prescription.patientId,
          pharmacistId: prescription.pharmacistId,
          items: prescription.items.filter(item => item.isControlled)
        }
      });
    }
  }
  
  // Data retention policies
  async enforceDataRetention(tenantId: string): Promise<void> {
    const retentionPeriods = await this.getTenantRetentionSettings(tenantId);
    
    await Promise.all([
      this.archiveAuditLogs(tenantId, retentionPeriods.auditLogs),
      this.anonymizePatientData(tenantId, retentionPeriods.patientData),
      this.archiveSalesData(tenantId, retentionPeriods.salesData)
    ]);
  }
}
```

## 9. Deployment Architecture

### 9.1 Container Strategy

```yaml
# Docker Compose for Multi-Service Deployment
version: '3.8'

services:
  api-gateway:
    image: umi-health/api-gateway:latest
    ports: ["8080:8080"]
    environment:
      - NODE_ENV=production
      - REDIS_URL=redis://redis:6379
    depends_on: [redis]

  tenant-service:
    image: umi-health/tenant-service:latest
    ports: ["3001:3001"]
    environment:
      - DATABASE_URL=postgresql://user:pass@postgres:5432/umi_shared
    depends_on: [postgres]

  pharmacy-service:
    image: umi-health/pharmacy-service:latest
    ports: ["3003:3003"]
    environment:
      - DATABASE_URL=postgresql://user:pass@postgres:5432/umi_tenants
    depends_on: [postgres]

  postgres:
    image: postgres:15
    environment:
      - POSTGRES_DB=umi_health
      - POSTGRES_USER=umi_admin
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]
    volumes:
      - redis_data:/data

volumes:
  postgres_data:
  redis_data:
```

### 9.2 Kubernetes Deployment

```yaml
# Kubernetes Deployment Example
apiVersion: apps/v1
kind: Deployment
metadata:
  name: pharmacy-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: pharmacy-service
  template:
    metadata:
      labels:
        app: pharmacy-service
    spec:
      containers:
      - name: pharmacy-service
        image: umi-health/pharmacy-service:latest
        ports:
        - containerPort: 3003
        env:
        - name: DATABASE_URL
          valueFrom:
            secretKeyRef:
              name: database-secrets
              key: url
        - name: REDIS_URL
          value: "redis://redis-service:6379"
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

## 10. Security Considerations

### 10.1 Data Encryption

```typescript
// Encryption at Rest
const encryptionConfig = {
  database: {
    transparentDataEncryption: true,
    columnEncryption: ['ssn', 'medical_history', 'contact_info']
  },
  storage: {
    serverSideEncryption: 'AES-256',
    keyRotation: '90 days'
  },
  transmission: {
    tlsVersion: '1.3',
    certificateValidation: 'strict'
  }
};

// Encryption in Transit
class EncryptionService {
  encryptSensitiveData(data: any, tenantId: string): string {
    const key = this.getTenantEncryptionKey(tenantId);
    return AES.encrypt(JSON.stringify(data), key).toString();
  }
  
  decryptSensitiveData(encryptedData: string, tenantId: string): any {
    const key = this.getTenantEncryptionKey(tenantId);
    return JSON.parse(AES.decrypt(encryptedData, key).toString());
  }
}
```

### 10.2 Network Security

```yaml
# Network Policies
networkPolicies:
  - name: tenant-isolation
    selector:
      app: pharmacy-service
    ingress:
    - from:
      - podSelector:
          matchLabels:
            app: api-gateway
    egress:
    - to:
      - podSelector:
          matchLabels:
            app: postgres
      ports:
      - protocol: TCP
        port: 5432
```

## 11. Monitoring & Observability

### 11.1 Metrics Collection

```typescript
// Custom Metrics
const metrics = {
  // Business Metrics
  tenantsActive: new Gauge('tenants_active_total', 'Number of active tenants'),
  branchesActive: new Gauge('branches_active_total', 'Number of active branches'),
  prescriptionsProcessed: new Counter('prescriptions_processed_total', 'Total prescriptions processed'),
  salesCompleted: new Counter('sales_completed_total', 'Total sales completed'),
  
  // Technical Metrics
  apiRequestDuration: new Histogram('api_request_duration_seconds', 'API request duration'),
  databaseConnections: new Gauge('database_connections_active', 'Active database connections'),
  cacheHitRate: new Gauge('cache_hit_rate', 'Cache hit rate percentage'),
  
  // Security Metrics
  authenticationFailures: new Counter('auth_failures_total', 'Authentication failures'),
  authorizationDenials: new Counter('auth_denials_total', 'Authorization denials')
};
```

### 11.2 Distributed Tracing

```typescript
// Jaeger Tracing Configuration
const tracingConfig = {
  serviceName: 'pharmacy-service',
  agentHost: 'jaeger-agent',
  agentPort: 6831,
  samplingRate: 0.1 // 10% sampling
};

class TracingMiddleware {
  async traceRequest(req: Request, res: Response, next: NextFunction): Promise<void> {
    const span = this.tracer.startSpan(`${req.method} ${req.path}`);
    
    span.setTag('user.id', req.user?.id);
    span.setTag('tenant.id', req.tenant?.id);
    span.setTag('branch.id', req.branch?.id);
    
    res.on('finish', () => {
      span.setTag('http.status_code', res.statusCode);
      span.finish();
    });
    
    next();
  }
}
```

## 12. Disaster Recovery & Backup

### 12.1 Backup Strategy

```yaml
backupStrategy:
  # Database Backups
  database:
    frequency: 'daily'
    retention: '90 days'
    encryption: true
    regions: ['primary', 'secondary']
    
  # File Storage Backups
  files:
    frequency: 'hourly'
    retention: '30 days'
    compression: true
    
  # Configuration Backups
  config:
    frequency: 'on-change'
    retention: '365 days'
    versioning: true
```

### 12.2 High Availability

```yaml
highAvailability:
  database:
    primary: 'us-east-1'
    replicas: ['us-west-1', 'eu-west-1']
    failoverTime: '< 30 seconds'
    
  services:
    minReplicas: 2
    maxReplicas: 10
    targetCpuUtilization: 70
    
  cache:
    clusterMode: true
    nodeCount: 6
    failover: 'automatic'
```

## 13. Performance Optimization

### 13.1 Database Optimization

```sql
-- Partitioning Strategy
CREATE TABLE tenant_sales (
    id UUID,
    branch_id UUID,
    created_at TIMESTAMP,
    -- other columns
) PARTITION BY RANGE (created_at);

-- Monthly Partitions
CREATE TABLE tenant_sales_2024_01 PARTITION OF tenant_sales
    FOR VALUES FROM ('2024-01-01') TO ('2024-02-01');

-- Indexing Strategy
CREATE INDEX CONCURRENTLY idx_tenant_sales_branch_date 
ON tenant_sales (branch_id, created_at DESC);

CREATE INDEX CONCURRENTLY idx_tenant_inventory_product_branch 
ON tenant_inventory (product_id, branch_id);
```

### 13.2 Query Optimization

```typescript
class OptimizedQueries {
  // Use CTEs for complex queries
  async getInventoryReport(tenantId: string, branchId: string): Promise<any> {
    const query = `
      WITH inventory_summary AS (
        SELECT 
          p.id,
          p.name,
          p.sku,
          COALESCE(SUM(i.quantity_on_hand), 0) as total_stock,
          COALESCE(SUM(i.quantity_reserved), 0) as reserved_stock
        FROM tenant_products p
        LEFT JOIN tenant_inventory i ON p.id = i.product_id
        WHERE p.tenant_id = $1 AND i.branch_id = $2
        GROUP BY p.id, p.name, p.sku
      )
      SELECT * FROM inventory_summary
      WHERE total_stock > 0
      ORDER BY p.name;
    `;
    
    return await this.database.query(query, [tenantId, branchId]);
  }
}
```

## 14. Implementation Roadmap

### Phase 1: Foundation (Months 1-2)

- [ ] Set up shared database schema
- [ ] Implement tenant and user services
- [ ] Create API gateway with authentication
- [ ] Set up basic monitoring and logging

### Phase 2: Core Services (Months 3-4)

- [ ] Implement pharmacy service
- [ ] Implement inventory service
- [ ] Implement POS service
- [ ] Set up caching layer

### Phase 3: Advanced Features (Months 5-6)

- [ ] Add branch-level access control
- [ ] Implement audit logging
- [ ] Add compliance features
- [ ] Set up distributed tracing

### Phase 4: Production Ready (Months 7-8)

- [ ] Performance optimization
- [ ] Security hardening
- [ ] Disaster recovery setup
- [ ] Load testing and optimization

This architecture provides a comprehensive foundation for a scalable, secure, and compliant multi-tenant pharmacy management system with full branch support.
