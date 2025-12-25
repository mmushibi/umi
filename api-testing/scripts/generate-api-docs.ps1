# Umi Health API Documentation Generation Script
# This script generates comprehensive API documentation from Swagger/OpenAPI specifications

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("development", "staging", "production")]
    [string]$Environment = "development",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "./docs/api",
    
    [Parameter(Mandatory=$false)]
    [switch]$IncludePostman,
    
    [Parameter(Mandatory=$false)]
    [switch]$OpenInBrowser
)

# Import required modules
Import-Module WebAdministration -ErrorAction SilentlyContinue

# Configuration
$ApiBaseUrl = switch ($Environment) {
    "development" { "https://localhost:7123" }
    "staging" { "https://staging-api.umihealth.com" }
    "production" { "https://api.umihealth.com" }
}

$SwaggerUrl = "$ApiBaseUrl/swagger/v1/swagger.json"
$OutputPath = Resolve-Path $OutputPath -ErrorAction SilentlyContinue
if (-not $OutputPath) {
    $OutputPath = New-Item -Path "./docs/api" -ItemType Directory -Force
}

Write-Host "🚀 Starting API Documentation Generation" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Cyan
Write-Host "API Base URL: $ApiBaseUrl" -ForegroundColor Cyan
Write-Host "Output Path: $OutputPath" -ForegroundColor Cyan
Write-Host ""

# Create output directories
$directories = @(
    "$OutputPath/openapi",
    "$OutputPath/postman",
    "$OutputPath/markdown",
    "$OutputPath/html"
)

foreach ($dir in $directories) {
    if (-not (Test-Path $dir)) {
        New-Item -Path $dir -ItemType Directory -Force | Out-Null
        Write-Host "📁 Created directory: $dir" -ForegroundColor Yellow
    }
}

# Function to download Swagger JSON
function Get-SwaggerSpec {
    param($Url)
    
    try {
        Write-Host "📥 Downloading Swagger specification from $Url..." -ForegroundColor Blue
        $response = Invoke-RestMethod -Uri $Url -Method Get -TimeoutSec 30
        return $response
    }
    catch {
        Write-Host "❌ Failed to download Swagger specification: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Function to generate OpenAPI JSON file
function Export-OpenApiSpec {
    param($Spec, $OutputFile)
    
    try {
        $spec | ConvertTo-Json -Depth 100 | Out-File -FilePath $OutputFile -Encoding UTF8
        Write-Host "✅ OpenAPI specification saved to: $OutputFile" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to save OpenAPI specification: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Function to generate Markdown documentation
function ConvertTo-MarkdownDoc {
    param($Spec, $OutputFile)
    
    try {
        $markdown = @()
        $markdown += "# Umi Health API Documentation"
        $markdown += ""
        $markdown += "**Environment:** $Environment"
        $markdown += "**API Version:** $($Spec.info.version)"
        $markdown += "**Generated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')"
        $markdown += ""
        $markdown += "---"
        $markdown += ""
        
        # API Information
        $markdown += "## API Information"
        $markdown += ""
        $markdown += "| Property | Value |"
        $markdown += "|----------|-------|"
        $markdown += "| Title | $($Spec.info.title) |"
        $markdown += "| Version | $($Spec.info.version) |"
        $markdown += "| Description | $($Spec.info.description) |"
        if ($Spec.info.contact) {
            $markdown += "| Contact | $($Spec.info.contact.name) <$($Spec.info.contact.email)> |"
        }
        if ($Spec.info.license) {
            $markdown += "| License | $($Spec.info.license.name) |"
        }
        $markdown += ""
        
        # Base URL
        $markdown += "## Base URL"
        $markdown += ""
        $markdown += "````"
        $markdown += $ApiBaseUrl
        $markdown += "````"
        $markdown += ""
        
        # Authentication
        if ($Spec.components.securitySchemes) {
            $markdown += "## Authentication"
            $markdown += ""
            foreach ($scheme in $Spec.components.securitySchemes.PSObject.Properties) {
                $markdown += "### $($scheme.Name)"
                $markdown += ""
                $markdown += "**Type:** $($scheme.Value.type)"
                if ($scheme.Value.description) {
                    $markdown += "**Description:** $($scheme.Value.description)"
                }
                if ($scheme.Value.bearerFormat) {
                    $markdown += "**Bearer Format:** $($scheme.Value.bearerFormat)"
                }
                $markdown += ""
            }
        }
        
        # API Endpoints
        $markdown += "## API Endpoints"
        $markdown += ""
        
        foreach ($path in $Spec.paths.PSObject.Properties | Sort-Object Name) {
            $markdown += "### $($path.Name)"
            $markdown += ""
            
            foreach ($method in $path.Value.PSObject.Properties) {
                $methodUpper = $method.Name.ToUpper()
                $operation = $method.Value
                
                $markdown += "#### $methodUpper $($path.Name)"
                $markdown += ""
                
                if ($operation.summary) {
                    $markdown += "**Summary:** $($operation.summary)"
                    $markdown += ""
                }
                
                if ($operation.description) {
                    $markdown += "**Description:** $($operation.description)"
                    $markdown += ""
                }
                
                if ($operation.tags) {
                    $markdown += "**Tags:** $($operation.tags -join ', ')"
                    $markdown += ""
                }
                
                # Parameters
                if ($operation.parameters) {
                    $markdown += "##### Parameters"
                    $markdown += ""
                    $markdown += "| Name | Location | Type | Required | Description |"
                    $markdown += "|------|----------|------|----------|-------------|"
                    
                    foreach ($param in $operation.parameters) {
                        $name = $param.name
                        $location = $param.in
                        $type = if ($param.schema) { $param.schema.type } else { $param.type }
                        $required = if ($param.required) { "Yes" } else { "No" }
                        $description = if ($param.description) { $param.description } else { "-" }
                        
                        $markdown += "| $name | $location | $type | $required | $description |"
                    }
                    $markdown += ""
                }
                
                # Request Body
                if ($operation.requestBody) {
                    $markdown += "##### Request Body"
                    $markdown += ""
                    if ($operation.requestBody.description) {
                        $markdown += "**Description:** $($operation.requestBody.description)"
                        $markdown += ""
                    }
                    
                    foreach ($contentType in $operation.requestBody.content.PSObject.Properties) {
                        $markdown += "**Content-Type:** $($contentType.Name)"
                        $markdown += ""
                        if ($contentType.Value.schema) {
                            $markdown += "```json"
                            $schemaExample = Get-SchemaExample -Schema $contentType.Value.schema
                            $markdown += $schemaExample
                            $markdown += "```"
                            $markdown += ""
                        }
                    }
                }
                
                # Responses
                if ($operation.responses) {
                    $markdown += "##### Responses"
                    $markdown += ""
                    
                    foreach ($response in $operation.responses.PSObject.Properties) {
                        $statusCode = $response.Name
                        $responseObj = $response.Value
                        
                        $markdown += "**$statusCode**"
                        if ($responseObj.description) {
                            $markdown += " - $($responseObj.description)"
                        }
                        $markdown += ""
                        
                        if ($responseObj.content) {
                            foreach ($contentType in $responseObj.content.PSObject.Properties) {
                                $markdown += "- **Content-Type:** $($contentType.Name)"
                                if ($contentType.Value.schema) {
                                    $markdown += "  ```json"
                                    $schemaExample = Get-SchemaExample -Schema $contentType.Value.schema
                                    $markdown += $schemaExample
                                    $markdown += "  ```"
                                }
                            }
                        }
                        $markdown += ""
                    }
                }
                
                $markdown += "---"
                $markdown += ""
            }
        }
        
        # Data Models
        if ($Spec.components.schemas) {
            $markdown += "## Data Models"
            $markdown += ""
            
            foreach ($schema in $Spec.components.schemas.PSObject.Properties | Sort-Object Name) {
                $markdown += "### $($schema.Name)"
                $markdown += ""
                
                if ($schema.Value.description) {
                    $markdown += "**Description:** $($schema.Value.description)"
                    $markdown += ""
                }
                
                if ($schema.Value.type -eq "object" -and $schema.Value.properties) {
                    $markdown += "| Property | Type | Required | Description |"
                    $markdown += "|----------|------|----------|-------------|"
                    
                    $requiredProps = if ($schema.Value.required) { $schema.Value.required } else { @() }
                    
                    foreach ($prop in $schema.Value.properties.PSObject.Properties) {
                        $name = $prop.Name
                        $type = Get-PropertyType -Property $prop.Value
                        $required = if ($name -in $requiredProps) { "Yes" } else { "No" }
                        $description = if ($prop.Value.description) { $prop.Value.description } else { "-" }
                        
                        $markdown += "| $name | $type | $required | $description |"
                    }
                    $markdown += ""
                }
            }
        }
        
        $markdown | Out-File -FilePath $OutputFile -Encoding UTF8
        Write-Host "✅ Markdown documentation saved to: $OutputFile" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to generate Markdown documentation: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Function to get schema example
function Get-SchemaExample {
    param($Schema)
    
    if ($Schema.example) {
        return $Schema.example | ConvertTo-Json -Depth 10 -Compress
    }
    
    if ($Schema.type -eq "object" -and $Schema.properties) {
        $example = @{}
        foreach ($prop in $Schema.properties.PSObject.Properties) {
            $example[$prop.Name] = Get-PropertyExample -Property $prop.Value
        }
        return $example | ConvertTo-Json -Depth 10 -Compress
    }
    
    if ($Schema.type -eq "array") {
        $itemExample = Get-PropertyExample -Property $Schema.items
        return "[$itemExample]"
    }
    
    return Get-PropertyExample -Property $Schema
}

# Function to get property example
function Get-PropertyExample {
    param($Property)
    
    if ($Property.example) {
        return $Property.example
    }
    
    switch ($Property.type) {
        "string" {
            if ($Property.format -eq "email") { return "user@example.com" }
            if ($Property.format -eq "date-time") { return "2024-12-24T10:00:00Z" }
            if ($Property.format -eq "date") { return "2024-12-24" }
            if ($Property.enum) { return $Property.enum[0] }
            return "string"
        }
        "integer" { return 1 }
        "number" { return 1.0 }
        "boolean" { return $true }
        "array" { return "[]" }
        "object" { return "{}" }
        default { return "null" }
    }
}

# Function to get property type
function Get-PropertyType {
    param($Property)
    
    if ($Property.type) {
        $type = $Property.type
        if ($Property.format) {
            $type += " ($($Property.format))"
        }
        if ($Property.enum) {
            $type += " enum"
        }
        return $type
    }
    
    if ($Property.`$ref) {
        return $Property.`$ref -replace "#/components/schemas/", ""
    }
    
    return "unknown"
}

# Function to generate Postman collection from OpenAPI
function ConvertTo-PostmanCollection {
    param($Spec, $OutputFile)
    
    try {
        $collection = @{
            info = @{
                _postman_id = [System.Guid]::NewGuid().ToString()
                name = "Umi Health API - $Environment"
                description = "Auto-generated Postman collection from OpenAPI specification"
                schema = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            }
            item = @()
            event = @(
                @{
                    listen = "prerequest"
                    script = @{
                        type = "text/javascript"
                        exec = @("// Auto-generated prerequest script")
                    }
                },
                @{
                    listen = "test"
                    script = @{
                        type = "text/javascript"
                        exec = @(
                            "pm.test('Status code is successful', function () {",
                            "    pm.expect(pm.response.code).to.be.oneOf([200, 201, 204]);",
                            "});",
                            "",
                            "pm.test('Response has valid JSON', function () {",
                            "    pm.expect(pm.response.headers.get('Content-Type')).to.include('application/json');",
                            "});"
                        )
                    }
                }
            )
        }
        
        # Group endpoints by tags
        $tagGroups = @{}
        foreach ($path in $Spec.paths.PSObject.Properties) {
            foreach ($method in $path.Value.PSObject.Properties) {
                $operation = $method.Value
                $tags = if ($operation.tags) { $operation.tags } else { @("Default") }
                
                foreach ($tag in $tags) {
                    if (-not $tagGroups.ContainsKey($tag)) {
                        $tagGroups[$tag] = @()
                    }
                    
                    $postmanRequest = @{
                        name = if ($operation.summary) { $operation.summary } else { "$($method.Name.ToUpper()) $($path.Name)" }
                        request = @{
                            method = $method.Name.ToUpper()
                            header = @(
                                @{
                                    key = "Content-Type"
                                    value = "application/json"
                                },
                                @{
                                    key = "Authorization"
                                    value = "Bearer {{access_token}}"
                                }
                            )
                            url = @{
                                raw = "{{base_url}}$($path.Name)"
                                host = @("{{base_url}}")
                                path = $path.Name -split '/' | Where-Object { $_ -ne '' }
                            }
                        }
                        response = @()
                    }
                    
                    # Add parameters
                    if ($operation.parameters) {
                        $postmanRequest.request.url.query = @()
                        $postmanRequest.request.url.variable = @()
                        
                        foreach ($param in $operation.parameters) {
                            if ($param.in -eq "query") {
                                $postmanRequest.request.url.query += @{
                                    key = $param.name
                                    value = "{{$($param.name)}}"
                                    description = if ($param.description) { $param.description } else { "" }
                                }
                            }
                            elseif ($param.in -eq "path") {
                                $postmanRequest.request.url.variable += @{
                                    key = $param.name
                                    value = "{{$($param.name)}}"
                                    description = if ($param.description) { $param.description } else { "" }
                                }
                            }
                        }
                    }
                    
                    # Add request body
                    if ($operation.requestBody) {
                        $postmanRequest.request.body = @{
                            mode = "raw"
                            raw = if ($operation.requestBody.content.'application/json'.schema) {
                                Get-SchemaExample -Schema $operation.requestBody.content.'application/json'.schema
                            } else {
                                "{}"
                            }
                            options = @{
                                raw = @{
                                    language = "json"
                                }
                            }
                        }
                    }
                    
                    $tagGroups[$tag] += $postmanRequest
                }
            }
        }
        
        # Convert tag groups to Postman folders
        foreach ($tag in $tagGroups.Keys | Sort-Object) {
            $collection.item += @{
                name = $tag
                item = $tagGroups[$tag]
            }
        }
        
        $collection | ConvertTo-Json -Depth 100 | Out-File -FilePath $OutputFile -Encoding UTF8
        Write-Host "✅ Postman collection saved to: $OutputFile" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to generate Postman collection: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Function to generate HTML documentation
function ConvertTo-HtmlDoc {
    param($Spec, $OutputFile)
    
    try {
        $html = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Umi Health API Documentation - $Environment</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .container { max-width: 1200px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; border-radius: 8px 8px 0 0; }
        .content { padding: 30px; }
        .endpoint { border: 1px solid #e1e5e9; border-radius: 6px; margin-bottom: 20px; overflow: hidden; }
        .endpoint-header { padding: 15px; background: #f8f9fa; border-bottom: 1px solid #e1e5e9; }
        .method-get { background: #e7f3ff; color: #0066cc; }
        .method-post { background: #e8f5e8; color: #008800; }
        .method-put { background: #fff3e0; color: #ff8800; }
        .method-delete { background: #ffebee; color: #cc0000; }
        .endpoint-body { padding: 15px; }
        .params-table { width: 100%; border-collapse: collapse; margin: 10px 0; }
        .params-table th, .params-table td { padding: 10px; text-align: left; border-bottom: 1px solid #e1e5e9; }
        .params-table th { background: #f8f9fa; font-weight: 600; }
        .code-block { background: #f8f9fa; border: 1px solid #e1e5e9; border-radius: 4px; padding: 15px; font-family: 'Monaco', 'Menlo', monospace; font-size: 14px; overflow-x: auto; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>Umi Health API Documentation</h1>
            <p>Environment: <strong>$Environment</strong> | Version: $($Spec.info.version) | Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')</p>
            <p>Base URL: <code>$ApiBaseUrl</code></p>
        </div>
        <div class="content">
            <h2>API Information</h2>
            <p><strong>Title:</strong> $($Spec.info.title)</p>
            <p><strong>Description:</strong> $($Spec.info.description)</p>
            <p><strong>Contact:</strong> $($Spec.info.contact.name) &lt;$($Spec.info.contact.email)&gt;</p>
"@
        
        # Add endpoints
        foreach ($path in $Spec.paths.PSObject.Properties | Sort-Object Name) {
            foreach ($method in $path.Value.PSObject.Properties) {
                $methodClass = "method-$($method.Name.ToLower())"
                $html += @"
            <div class="endpoint">
                <div class="endpoint-header $methodClass">
                    <h3>$($method.Name.ToUpper()) $($path.Name)</h3>
                    <p>$(if ($method.Value.summary) { $method.Value.summary } else { "No summary available" })</p>
                </div>
                <div class="endpoint-body">
                    <p>$(if ($method.Value.description) { $method.Value.description } else { "No description available" })</p>
"@
                
                if ($method.Value.parameters) {
                    $html += "                    <h4>Parameters</h4>`n"
                    $html += "                    <table class='params-table'>`n"
                    $html += "                        <tr><th>Name</th><th>Location</th><th>Type</th><th>Required</th><th>Description</th></tr>`n"
                    
                    foreach ($param in $method.Value.parameters) {
                        $name = $param.name
                        $location = $param.in
                        $type = if ($param.schema) { $param.schema.type } else { $param.type }
                        $required = if ($param.required) { "Yes" } else { "No" }
                        $description = if ($param.description) { $param.description } else { "-" }
                        
                        $html += "                        <tr><td>$name</td><td>$location</td><td>$type</td><td>$required</td><td>$description</td></tr>`n"
                    }
                    
                    $html += "                    </table>`n"
                }
                
                if ($method.Value.responses) {
                    $html += "                    <h4>Responses</h4>`n"
                    foreach ($response in $method.Value.responses.PSObject.Properties) {
                        $statusCode = $response.Name
                        $description = if ($response.Value.description) { $response.Value.description } else { "No description" }
                        $html += "                    <p><strong>$statusCode:</strong> $description</p>`n"
                    }
                }
                
                $html += "                </div>`n            </div>`n"
            }
        }
        
        $html += @"
        </div>
    </div>
</body>
</html>
"@
        
        $html | Out-File -FilePath $OutputFile -Encoding UTF8
        Write-Host "✅ HTML documentation saved to: $OutputFile" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Failed to generate HTML documentation: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Main execution
try {
    # Download Swagger specification
    $swaggerSpec = Get-SwaggerSpec -Url $SwaggerUrl
    if (-not $swaggerSpec) {
        Write-Host "❌ Cannot proceed without Swagger specification" -ForegroundColor Red
        exit 1
    }
    
    # Generate OpenAPI JSON
    $openApiFile = "$OutputPath/openapi/swagger-$Environment.json"
    Export-OpenApiSpec -Spec $swaggerSpec -OutputFile $openApiFile
    
    # Generate Markdown documentation
    $markdownFile = "$OutputPath/markdown/api-documentation-$Environment.md"
    ConvertTo-MarkdownDoc -Spec $swaggerSpec -OutputFile $markdownFile
    
    # Generate HTML documentation
    $htmlFile = "$OutputPath/html/api-documentation-$Environment.html"
    ConvertTo-HtmlDoc -Spec $swaggerSpec -OutputFile $htmlFile
    
    # Generate Postman collection if requested
    if ($IncludePostman) {
        $postmanFile = "$OutputPath/postman/umi-health-api-$Environment.postman_collection.json"
        ConvertTo-PostmanCollection -Spec $swaggerSpec -OutputFile $postmanFile
    }
    
    # Generate index file
    $indexFile = "$OutputPath/index.html"
    $indexHtml = @"
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Umi Health API Documentation</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .container { max-width: 800px; margin: 0 auto; background: white; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); padding: 30px; }
        .header { text-align: center; margin-bottom: 30px; }
        .links { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; }
        .link-card { border: 1px solid #e1e5e9; border-radius: 6px; padding: 20px; text-decoration: none; color: inherit; transition: transform 0.2s, box-shadow 0.2s; }
        .link-card:hover { transform: translateY(-2px); box-shadow: 0 4px 20px rgba(0,0,0,0.1); }
        .link-card h3 { margin: 0 0 10px 0; color: #333; }
        .link-card p { margin: 0; color: #666; font-size: 14px; }
        .badge { background: #667eea; color: white; padding: 2px 8px; border-radius: 12px; font-size: 12px; margin-left: 10px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h1>🏥 Umi Health API Documentation</h1>
            <p>Comprehensive API documentation for the Umi Health pharmacy management system</p>
        </div>
        <div class="links">
            <a href="html/api-documentation-$Environment.html" class="link-card">
                <h3>📖 HTML Documentation</h3>
                <p>Interactive HTML documentation with responsive design</p>
            </a>
            <a href="markdown/api-documentation-$Environment.md" class="link-card">
                <h3>📝 Markdown Documentation</h3>
                <p>Markdown format suitable for GitHub and documentation platforms</p>
            </a>
            <a href="openapi/swagger-$Environment.json" class="link-card">
                <h3>🔧 OpenAPI Specification</h3>
                <p>Raw OpenAPI 3.0 specification in JSON format</p>
            </a>
            @if ($IncludePostman) {
            <a href="postman/umi-health-api-$Environment.postman_collection.json" class="link-card">
                <h3>🚀 Postman Collection</h3>
                <p>Import-ready Postman collection for API testing</p>
            </a>
            }
            <a href="$SwaggerUrl" class="link-card" target="_blank">
                <h3>🌐 Swagger UI</h3>
                <p>Interactive Swagger UI for API exploration</p>
            </a>
        </div>
    </div>
</body>
</html>
"@
    
    $indexHtml | Out-File -FilePath $indexFile -Encoding UTF8
    
    Write-Host ""
    Write-Host "🎉 API Documentation Generation Complete!" -ForegroundColor Green
    Write-Host "📁 Output directory: $OutputPath" -ForegroundColor Cyan
    Write-Host "📄 Index file: $indexFile" -ForegroundColor Cyan
    Write-Host ""
    
    if ($OpenInBrowser) {
        Start-Process $indexFile
        Write-Host "🌐 Opening documentation in browser..." -ForegroundColor Blue
    }
}
catch {
    Write-Host "❌ Fatal error during documentation generation: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
