#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Generates a GitHub App installation access token for the specified identity.

.DESCRIPTION
    Two-step GitHub App auth flow:
      1. Signs a JWT with the App private key (RS256, 9-min expiry)
      2. Exchanges the JWT for a 1-hour installation access token via GitHub API

    Reads from .github/skills/github-ops/identity/{Identity}/:
      - app.config.json  (appId, installationId)
      - private-key.pem  (RSA private key — PKCS#1 or PKCS#8 PEM format)

.PARAMETER Identity
    'implementer' or 'reviewer'

.OUTPUTS
    Installation access token string on stdout.

.EXAMPLE
    $env:GH_TOKEN = & .github/skills/github-ops/scripts/New-GitHubAppToken.ps1 -Identity implementer
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('implementer', 'reviewer')]
    [string]$Identity
)

$ErrorActionPreference = 'Stop'

$identityDir = Join-Path $PSScriptRoot '..' 'identity' $Identity
$configPath  = Join-Path $identityDir 'app.config.json'
$keyPath     = Join-Path $identityDir 'private-key.pem'

if (-not (Test-Path $configPath)) {
    throw "Config file not found: $configPath"
}
if (-not (Test-Path $keyPath)) {
    throw @"
Private key not found: $keyPath
See .github/skills/github-ops/identity/README.md for placement instructions.
"@
}

$config         = Get-Content $configPath -Raw | ConvertFrom-Json
$appId          = [string]$config.appId
$installationId = [string]$config.installationId

if ($appId -like 'PLACEHOLDER*' -or [string]::IsNullOrWhiteSpace($appId)) {
    throw "app.config.json for '$Identity' has not been configured (appId is placeholder)."
}

# Decode PEM — strip all headers/footers and whitespace
$pem      = Get-Content $keyPath -Raw
$base64   = $pem -replace '-----[A-Z ]+-----' -replace '\s', ''
$keyBytes = [Convert]::FromBase64String($base64)

# Import RSA key — try PKCS#1 first (GitHub default), fall back to PKCS#8
$rsa = [System.Security.Cryptography.RSA]::Create()
try {
    [int]$n = 0; $rsa.ImportRSAPrivateKey($keyBytes, [ref]$n)
} catch {
    try {
        [int]$n = 0; $rsa.ImportPkcs8PrivateKey($keyBytes, [ref]$n)
    } catch {
        throw "Cannot import private key for '$Identity'. Expected PKCS#1 or PKCS#8 PEM. Error: $_"
    }
}

function ConvertTo-Base64Url([byte[]]$b) {
    [Convert]::ToBase64String($b).TrimEnd('=').Replace('+', '-').Replace('/', '_')
}

$now = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
$hdr = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes('{"alg":"RS256","typ":"JWT"}'))
$pay = ConvertTo-Base64Url ([Text.Encoding]::UTF8.GetBytes(
    "{`"iat`":$($now - 60),`"exp`":$($now + 480),`"iss`":`"$appId`"}"))
$sig = ConvertTo-Base64Url ($rsa.SignData(
    [Text.Encoding]::UTF8.GetBytes("$hdr.$pay"),
    [Security.Cryptography.HashAlgorithmName]::SHA256,
    [Security.Cryptography.RSASignaturePadding]::Pkcs1))

$jwt = "$hdr.$pay.$sig"

$headers = @{
    Authorization          = "Bearer $jwt"
    Accept                 = 'application/vnd.github+json'
    'X-GitHub-Api-Version' = '2022-11-28'
}

try {
    $resp = Invoke-RestMethod `
        -Uri     "https://api.github.com/app/installations/$installationId/access_tokens" `
        -Method  Post `
        -Headers $headers
} catch {
    $code = $_.Exception.Response.StatusCode.value__
    throw @"
Failed to obtain installation token for '$Identity' (HTTP $code).
Check: appId and installationId in app.config.json, and that the private key matches the app.
Error: $_
"@
}

Write-Output $resp.token
