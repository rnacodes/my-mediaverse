# PowerShell script to run the backend tests with coverage and build an HTML report
#
# Usage (from the repository root):
#   .\scripts\run-backend-coverage.ps1                      # unit tests only
#   .\scripts\run-backend-coverage.ps1 -IncludeIntegration  # unit + integration (needs Docker)
#
# Output:
#   coverage\backend\index.html    full report, browsable by project and class
#   coverage\backend\Summary.txt   plain-text summary
#
# The report tool is restored from dotnet-tools.json, and what gets measured is
# controlled by tests\coverlet.runsettings.
param(
    [switch]$IncludeIntegration
)

$ErrorActionPreference = "Continue"

$reportDir = "coverage/backend"
$resultsDir = "$reportDir/raw"
$settings = "tests/coverlet.runsettings"

if (!(Test-Path $settings)) {
    Write-Host "Run this script from the repository root." -ForegroundColor Red
    exit 1
}

if (Test-Path $reportDir) {
    Remove-Item -Path $reportDir -Recurse -Force
}

& dotnet tool restore

$testFailed = $false

function Invoke-CoverageRun {
    param(
        [string]$TestProject,
        [string]$Name
    )

    Write-Host "`n=== $Name ===" -ForegroundColor Yellow
    & dotnet test $TestProject --settings $settings --collect "XPlat Code Coverage" `
        --results-directory "$resultsDir/$Name" --nologo --verbosity minimal

    if ($LASTEXITCODE -ne 0) {
        $script:testFailed = $true
    }
}

Invoke-CoverageRun -TestProject "tests/MyMediaVerse.UnitTests/MyMediaVerse.UnitTests.csproj" -Name "unit"

if ($IncludeIntegration) {
    Invoke-CoverageRun -TestProject "tests/MyMediaVerse.IntegrationTests/MyMediaVerse.IntegrationTests.csproj" -Name "integration"
}

Write-Host "`n=== Building report ===" -ForegroundColor Yellow
& dotnet reportgenerator `
    "-reports:$resultsDir/**/coverage.cobertura.xml" `
    "-targetdir:$reportDir" `
    "-reporttypes:Html;TextSummary" `
    "-verbosity:Warning"

Write-Host ""
Get-Content "$reportDir/Summary.txt" -TotalCount 20
Write-Host "`nFull report: $reportDir/index.html" -ForegroundColor Cyan

if ($testFailed) {
    Write-Host "Some tests failed, so the coverage numbers above are incomplete." -ForegroundColor Red
    exit 1
}
