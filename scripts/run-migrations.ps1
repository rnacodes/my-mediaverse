# run-migrations.ps1
# Script to run Entity Framework migrations against both Production and Demo databases
#
# Usage:
#   .\run-migrations.ps1                           # Update both databases with existing migrations
#   .\run-migrations.ps1 -MigrationName "MyMigration"  # Add new migration and update both databases
#   .\run-migrations.ps1 -UpdateOnly               # Only update databases (skip adding migration even if name provided)
#
# Required Environment Variables:
#   PRODUCTION_DB_CONNECTION - Connection string for production database
#   DEMO_DB_CONNECTION       - Connection string for demo database

param(
    [string]$MigrationName,
    [switch]$UpdateOnly
)

$ErrorActionPreference = "Stop"

# Paths
$rootDir = (Split-Path $PSScriptRoot -Parent)
$webApiDir = Join-Path $rootDir "src\MyMediaVerse\MyMediaVerse.Web.API"
$infraProject = "..\MyMediaVerse.Infrastructure"

# Validate environment variables
$productionConnection = $env:PRODUCTION_DB_CONNECTION
$demoConnection = $env:DEMO_DB_CONNECTION

if (-not $productionConnection) {
    Write-Host "ERROR: PRODUCTION_DB_CONNECTION environment variable is not set." -ForegroundColor Red
    exit 1
}

if (-not $demoConnection) {
    Write-Host "ERROR: DEMO_DB_CONNECTION environment variable is not set." -ForegroundColor Red
    exit 1
}

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

    # Update Demo Database
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host "Updating DEMO Database..." -ForegroundColor Magenta
    Write-Host "========================================`n" -ForegroundColor Magenta

    dotnet ef database update --project $infraProject --connection $demoConnection

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to update demo database." -ForegroundColor Red
        exit 1
    }

    Write-Host "Demo database updated successfully!`n" -ForegroundColor Green

    # Success summary
    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "ALL DATABASES UPDATED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "========================================`n" -ForegroundColor Green

} finally {
    Pop-Location
}
