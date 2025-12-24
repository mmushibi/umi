<#
One-command production deploy helper.
Prerequisites:
- GitHub CLI (`gh`) installed and authenticated OR `az` authenticated if you adapt for Azure CLI
- Repository reflexively configured to deploy to production via `ci-cd.yml`

Usage:
.
\scripts\one-command-deploy.ps1 -Repo "owner/repo" -Ref "main" -Workflow "ci-cd.yml"
#>
param(
    [Parameter(Mandatory=$true)][string]$Repo,
    [string]$Ref = "main",
    [string]$Workflow = "ci-cd.yml"
)

function Fail([string]$msg){ Write-Error $msg; exit 1 }

if (-not (Get-Command gh -ErrorAction SilentlyContinue)){
    Fail "GitHub CLI 'gh' not found. Install from https://cli.github.com/ and authenticate (gh auth login)."
}

Write-Output "Dispatching workflow '$Workflow' for $Repo@$Ref..."
$run = gh workflow run $Workflow --repo $Repo --ref $Ref
if ($LASTEXITCODE -ne 0){
    Fail "Workflow dispatch failed. Ensure the workflow file name matches and you have permission to dispatch workflows."
}

Write-Output "Workflow dispatched successfully."
Write-Output "Monitor progress with: gh run list --repo $Repo --workflow $Workflow"
Write-Output "Or open: https://github.com/$Repo/actions"
