```text
api-testing/
├── postman/
│   ├── collections/           # Postman test collections
│   │   ├── Authentication.postman_collection.json
│   │   ├── TenantManagement.postman_collection.json
│   │   ├── PharmacyOperations.postman_collection.json
│   │   ├── PointOfSale.postman_collection.json
│   │   └── PatientManagement.postman_collection.json
│   └── environments/          # Environment configurations
│       ├── Development.postman_environment.json
│       ├── Staging.postman_environment.json
│       └── Production.postman_environment.json
├── scripts/
│   ├── generate-api-docs.ps1  # API documentation generator
│   └── run-api-tests.ps1      # Test runner script
├── docs/                      # Generated documentation (auto-created)
└── test-results/              # Test results (auto-created)
```

## 🚀 Quick Start

### Prerequisites

- PowerShell 5.1+ or PowerShell Core
- Node.js and npm (for Newman)
- Access to Umi Health API endpoints

### Installation

**Install Newman (Postman CLI)**:

```powershell
npm install -g newman
npm install -g newman-reporter-html
npm install -g newman-reporter-junit
```

**Set up Environment**:

```powershell
# Copy and configure environment files
cp postman/environments/Development.postman_environment.json postman/environments/Local.postman_environment.json
# Edit file with your local API URL and credentials
```

## 🧪 Running Tests

### Using PowerShell Script

Run all test suites:

```powershell
.\scripts\run-api-tests.ps1 -Environment development -Collection all -GenerateReports
```

Run specific collection:

```powershell
.\scripts\run-api-tests.ps1 -Environment development -Collection authentication -GenerateReports
```

Run tests in parallel:

```powershell
.\scripts\run-api-tests.ps1 -Environment development -Collection all -Parallel -GenerateReports
```

### Using Newman Directly

```powershell
newman run postman/collections/Authentication.postman_collection.json -e postman/environments/Development.postman_environment.json --reporters cli,html
```

## 📚 Test Collections

### 1. Authentication Tests

- **User Registration**: Test new user creation with validation
- **Login/Logout**: Test authentication flow and session management
- **Token Refresh**: Test JWT token renewal mechanism
- **Permission Validation**: Test role-based access control

### 2. Tenant Management Tests

- **Tenant Creation**: Test multi-tenant setup
- **Branch Management**: Test branch operations
- **User Assignment**: Test user-tenant relationships
- **Settings Configuration**: Test tenant-specific settings

### 3. Pharmacy Operations Tests

- **Product Management**: Test medication/product CRUD operations
- **Inventory Updates**: Test stock level management
- **Stock Transfers**: Test inter-branch inventory transfers
- **Supplier Management**: Test supplier relationships

### 4. Point of Sale Tests

- **Sale Processing**: Test transaction workflows
- **Payment Handling**: Test multiple payment methods
- **Receipt Generation**: Test receipt creation and delivery
- **Return Processing**: Test product return workflows

### 5. Patient Management Tests

- **Patient Registration**: Test patient record creation
- **Prescription Creation**: Test prescription workflows
- **Dispensing**: Test medication dispensing process
- **History Tracking**: Test patient medical history

## 🌍 Environment Configuration

### Development Environment

- **Base URL**: `https://localhost:7123`
- **Tenant Code**: `DEV001`
- **Debug Mode**: Enabled
- **Request Timeout**: 30 seconds

### Staging Environment

- **Base URL**: `https://staging-api.umihealth.com`
- **Tenant Code**: `STAGE001`
- **Debug Mode**: Disabled
- **Request Timeout**: 30 seconds

### Production Environment

- **Base URL**: `https://api.umihealth.com`
- **Tenant Code**: (Set per test run)
- **Debug Mode**: Disabled
- **Request Timeout**: 60 seconds

## 📖 Documentation Generation

### Generate API Documentation

Generate documentation for all environments:

```powershell
.\scripts\generate-api-docs.ps1 -Environment development -IncludePostman -OpenInBrowser
```

Generate for specific environment:

```powershell
.\scripts\generate-api-docs.ps1 -Environment staging -OutputPath "./docs/staging"
```

### Documentation Formats

The script generates documentation in multiple formats:

- **OpenAPI Specification**: Raw JSON specification
- **Markdown**: GitHub-compatible documentation
- **HTML**: Interactive web documentation
- **Postman Collection**: Auto-generated from OpenAPI

### Accessing Documentation

- **Interactive Swagger UI**: `https://localhost:7123/swagger`
- **Generated HTML**: Open `docs/api/index.html` in browser
- **Markdown**: View `docs/api/markdown/api-documentation-{env}.md`

## 🔧 Configuration

### Environment Variables

Key environment variables to configure:

| Variable      | Description           | Example                    |
|---------------|-----------------------|----------------------------|
| `base_url`    | API base URL          | `https://localhost:7123`    |
| `tenant_code`  | Tenant identifier      | `DEV001`                   |
| `access_token` | JWT access token      | (Set after login)          |
| `refresh_token`| JWT refresh token     | (Set after login)          |
| `user_id`      | Current user ID       | (Set after login)          |
| `branch_id`    | Current branch ID     | (Set after creation)       |

### Test Data Management

The test collections automatically generate unique test data:

- **Timestamps**: Used for unique identifiers
- **Random Values**: Generated for emails, names, codes
- **Environment Storage**: Variables stored between test runs
- **Cleanup**: Tests include cleanup procedures where applicable

## 📊 Test Reports

### Report Types

- **Console Output**: Real-time test execution feedback
- **HTML Reports**: Interactive test result visualization
- **JUnit XML**: CI/CD integration format
- **JSON Summary**: Machine-readable test results

### Report Locations

- **HTML Reports**: `test-results/{collection-name}-{timestamp}.html`
- **JUnit XML**: `test-results/{collection-name}-{timestamp}.xml`
- **JSON Summary**: `test-results/test-summary-{timestamp}.json`

## 🔄 CI/CD Integration

### GitHub Actions Example

```yaml
name: API Tests
on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v2
    - name: Setup Node.js
      uses: actions/setup-node@v2
      with:
        node-version: '16'
    - name: Install Newman
      run: npm install -g newman newman-reporter-html
    - name: Run API Tests
      run: ./api-testing/scripts/run-api-tests.ps1 -Environment staging -Collection all -GenerateReports
    - name: Upload Test Results
      uses: actions/upload-artifact@v2
      with:
        name: test-results
        path: test-results/
```

## 🐛 Troubleshooting

### Common Issues

- **Newman Not Found**:

   ```powershell
   npm install -g newman
   ```

- **SSL Certificate Errors**:

   ```powershell
   # For development only
   newman run collection.json --insecure
   ```

- **Timeout Issues**:

  - Increase `request_timeout` in environment
  - Check API server availability
  - Verify network connectivity

- **Authentication Failures**:

  - Verify tenant code is correct
  - Check user credentials
  - Ensure user has required permissions

### Debug Mode

Enable debug mode in environment files:

```json
{
  "key": "debug_mode",
  "value": "true",
  "type": "boolean"
}
```

This enables detailed logging for request/response inspection.

## 📝 Best Practices

### Test Development

- **Use Environment Variables**: Avoid hardcoded values
- **Generate Unique Data**: Prevent test conflicts
- **Clean Up Resources**: Remove test data after completion
- **Validate Responses**: Check both status codes and response bodies
- **Handle Timeouts**: Set appropriate timeouts for each test

### Environment Management

- **Separate Credentials**: Never commit actual passwords/tokens
- **Use Different Tenants**: Test with different tenant configurations
- **Isolate Test Data**: Use dedicated test data sets
- **Regular Cleanup**: Remove obsolete test data

### Continuous Integration

- **Run Tests Automatically**: On every commit/PR
- **Generate Reports**: Store test results as artifacts
- **Fail Fast**: Stop pipeline on critical test failures
- **Monitor Performance**: Track test execution times

## 🤝 Contributing

### Adding New Tests

- Create new collection in `postman/collections/`
- Follow naming convention: `ModuleName.postman_collection.json`
- Include comprehensive test scripts
- Add environment variables as needed
- Update documentation

### Test Standards

- **Response Validation**: Always validate status codes and response structure
- **Error Handling**: Test both success and failure scenarios
- **Data Validation**: Verify data integrity and relationships
- **Performance**: Include response time assertions
- **Security**: Test authentication and authorization

## 📞 Support

For issues with:

- **API Functionality**: Contact development team
- **Test Framework**: Create GitHub issue
- **Documentation**: Submit documentation PR
- **Environment Access**: Contact DevOps team

---

**Last Updated**: December 24, 2024
**Version**: 1.0.0
