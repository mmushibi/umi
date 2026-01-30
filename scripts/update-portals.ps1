#!/usr/bin/env pwsh

# Script to update all portal HTML files with the new authentication and SignalR libraries

$portalDirs = @(
    "portals\admin",
    "portals\cashier", 
    "portals\pharmacist",
    "portals\operations"
)

foreach ($dir in $portalDirs) {
    $htmlFiles = Get-ChildItem -Path $dir -Filter "*.html" -Recurse
    
    foreach ($file in $htmlFiles) {
        Write-Host "Updating $($file.FullName)..."
        
        $content = Get-Content -Path $file.FullName -Raw
        
        # Replace the old script references with new ones
        $oldPattern = '<script src="../../js/api-client.js"></script>'
        $newPattern = '<script src="../../js/auth-api.js"></script>
    <script src="../../js/admin-api.js"></script>
    <script src="../../js/signalr-client.js"></script>'
        
        if ($content -match [regex]::Escape($oldPattern)) {
            $content = $content -replace [regex]::Escape($oldPattern), $newPattern
            
            # Also ensure auth-manager.js is present (it should already be there)
            if ($content -notmatch 'auth-manager\.js') {
                $content = $content -replace '<script src="../../js/auth-api.js"></script>', '<script src="../../js/auth-api.js"></script>
    <script src="../../js/auth-manager.js"></script>'
            }
            
            Set-Content -Path $file.FullName -Value $content -NoNewline
            Write-Host "  - Updated $($file.Name)" -ForegroundColor Green
        } else {
            Write-Host "  - No changes needed for $($file.Name)" -ForegroundColor Yellow
        }
    }
}

Write-Host "Portal updates completed!" -ForegroundColor Cyan
