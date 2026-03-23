# run-migrations.ps1
# Script to run Entity Framework migrations against both Production and Demo databases
# Demo DB requires an SSH tunnel to the DigitalOcean Droplet (port 5432 is firewalled).
#
# Usage:
#   .\run-migrations.ps1                           # Update both databases with existing migrations
#   .\run-migrations.ps1 -MigrationName "MyMigration"  # Add new migration and update both databases
#   .\run-migrations.ps1 -UpdateOnly               # Only update databases (skip adding migration even if name provided)
#
# Required Environment Variables:
#   PRODUCTION_DB_CONNECTION - Connection string for production database (Render)
#   DEMO_DB_CONNECTION       - Connection string for demo database (via SSH tunnel, Host=localhost;Port=5433)
#   DEMO_DROPLET_IP          - IP address of the DigitalOcean Droplet hosting the demo database

param(
    [string]$MigrationName,
    [switch]$UpdateOnly
)

$ErrorActionPreference = "Stop"

# Paths
$rootDir = (Split-Path $PSScriptRoot -Parent)
$webApiDir = Join-Path $rootDir "src\MyMediaVerse\MyMediaVerse.Web.API"
$infraProject = "..\MyMediaVerse.Infrastructure"

# SSH tunnel config
$sshLocalPort = 5433
$sshRemotePort = 5432
$sshUser = "root"

# Validate environment variables
$productionConnection = $env:PRODUCTION_DB_CONNECTION
$demoConnection = $env:DEMO_DB_CONNECTION
$dropletIp = $env:DEMO_DROPLET_IP

if (-not $productionConnection) {
    Write-Host "ERROR: PRODUCTION_DB_CONNECTION environment variable is not set." -ForegroundColor Red
    exit 1
}

if (-not $demoConnection) {
    Write-Host "ERROR: DEMO_DB_CONNECTION environment variable is not set." -ForegroundColor Red
    exit 1
}

if (-not $dropletIp) {
    Write-Host "ERROR: DEMO_DROPLET_IP environment variable is not set." -ForegroundColor Red
    exit 1
}

# Open SSH tunnel for demo database
Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Opening SSH tunnel to Demo Droplet..." -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

$tunnelProcess = Start-Process -NoNewWindow -PassThru ssh "-fNL ${sshLocalPort}:localhost:${sshRemotePort} ${sshUser}@${dropletIp}"
Start-Sleep -Seconds 3

# Verify tunnel is open
$tunnelCheck = Test-NetConnection -ComputerName localhost -Port $sshLocalPort -WarningAction SilentlyContinue
if (-not $tunnelCheck.TcpTestSucceeded) {
    Write-Host "ERROR: SSH tunnel failed to open on port $sshLocalPort." -ForegroundColor Red
    exit 1
}

Write-Host "SSH tunnel established (localhost:$sshLocalPort -> Droplet:$sshRemotePort)`n" -ForegroundColor Green

# Change to Web.API directory
Push-Location $webApiDir

try {
    # Add migration if name provided and not UpdateOnly
    if ($MigrationName -and -not $UpdateOnly) {
        Write-Host "`n========================================" -ForegroundColor Cyan
        Write-Host "Adding Migration: $MigrationName" -ForegroundColor Cyan
        Write-Host "========================================`n" -ForegroundColor Cyan

        dotnet ef migrations add $MigrationName --project $infraProject

        if ($LASTEXITCODE -ne 0) {
            Write-Host "ERROR: Failed to add migration." -ForegroundColor Red
            exit 1
        }

        Write-Host "Migration added successfully!`n" -ForegroundColor Green
    }

    # Update Demo Database first (canary)
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host "Updating DEMO Database..." -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta

    dotnet ef database update --project $infraProject --connection $demoConnection

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to update demo database." -ForegroundColor Red
        exit 1
    }

    Write-Host "Demo database updated successfully!`n" -ForegroundColor Green

    # Update Production Database
    Write-Host "`n========================================" -ForegroundColor Yellow
    Write-Host "Updating PRODUCTION Database..." -ForegroundColor Yellow
    Write-Host "========================================`n" -ForegroundColor Yellow

    dotnet ef database update --project $infraProject --connection $productionConnection

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to update production database." -ForegroundColor Red
        exit 1
    }

    Write-Host "Production database updated successfully!`n" -ForegroundColor Green

    # Success summary
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "ALL DATABASES UPDATED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Green

} finally {
    Pop-Location

    # Close SSH tunnel
    if ($tunnelProcess -and -not $tunnelProcess.HasExited) {
        Write-Host "Closing SSH tunnel..." -ForegroundColor Cyan
        Stop-Process -Id $tunnelProcess.Id -Force
        Write-Host "SSH tunnel closed.`n" -ForegroundColor Green
    }
}
