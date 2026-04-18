#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Sets GH_TOKEN in the current terminal session for a GitHub App identity.

.DESCRIPTION
    Generates an installation access token and sets GH_TOKEN so all subsequent
    gh commands in this session act as the specified identity.

    *** MUST BE DOT-SOURCED ***

    Correct:
        . .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer

    Wrong (env var is lost when script exits):
        .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer

.PARAMETER Identity
    'implementer' or 'reviewer'

.EXAMPLE
    . .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity implementer
    . .github/skills/github-ops/scripts/Set-GitHubIdentity.ps1 -Identity reviewer
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('implementer', 'reviewer')]
    [string]$Identity
)

$ErrorActionPreference = 'Stop'

$tokenScript = Join-Path $PSScriptRoot 'New-GitHubAppToken.ps1'

Write-Host "Generating token for identity: " -NoNewline -ForegroundColor Cyan
Write-Host $Identity -ForegroundColor Yellow

$token = & $tokenScript -Identity $Identity

if ([string]::IsNullOrEmpty($token)) {
    Write-Error "Token generation returned empty. Check app.config.json and private-key.pem for '$Identity'."
    return
}

$env:GH_TOKEN = $token

Write-Host "GH_TOKEN set for: " -NoNewline -ForegroundColor Green
Write-Host $Identity -ForegroundColor Yellow
Write-Host "Token valid ~1 hour. Re-run on HTTP 401 errors." -ForegroundColor DarkGray
