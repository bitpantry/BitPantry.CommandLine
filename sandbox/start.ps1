# Sandbox Start Script
# Builds and runs both server and client for testing

Write-Host "Building sandbox projects..." -ForegroundColor Cyan

# Build projects
Push-Location $PSScriptRoot
dotnet build SandboxServer/SandboxServer.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Server build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

dotnet build SandboxClient/SandboxClient.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Client build failed!" -ForegroundColor Red
    Pop-Location
    exit 1
}

Write-Host ""
Write-Host "Starting server in background..." -ForegroundColor Green

# Start server in background
$serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run --project SandboxServer/SandboxServer.csproj --no-build" -PassThru -WindowStyle Normal

# Wait for server to start
Start-Sleep -Seconds 2

Write-Host "Starting client..." -ForegroundColor Green
Write-Host ""
Write-Host "Use 'server connect -u http://localhost:5000/cli' to connect" -ForegroundColor Yellow
Write-Host "Use 'server upload <file> <destination>' to upload files" -ForegroundColor Yellow
Write-Host ""

# Run client
dotnet run --project SandboxClient/SandboxClient.csproj --no-build

# Cleanup: stop server when client exits
Write-Host ""
Write-Host "Stopping server..." -ForegroundColor Cyan
Stop-Process -Id $serverProcess.Id -Force -ErrorAction SilentlyContinue

Pop-Location
