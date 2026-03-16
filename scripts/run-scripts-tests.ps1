# PowerShell script to run Python scripts tests with detailed logging
$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$rootPath = Get-Location
$logFile = "$rootPath\logs\test-results-scripts-$timestamp.log"

# Create logs directory if it doesn't exist
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}

Write-Host "Running MyMediaVerse Scripts Tests..." -ForegroundColor Green
Write-Host "Log file: $logFile" -ForegroundColor Cyan

# Function to log and display results
function Write-TestResult {
    param(
        [string]$Message,
        [string]$Color = "White",
        [bool]$LogToFile = $true
    )

    Write-Host $Message -ForegroundColor $Color
    if ($LogToFile) {
        Add-Content -Path $logFile -Value "$(Get-Date -Format 'HH:mm:ss'): $Message"
    }
}

# Initialize log file
Write-TestResult "Scripts Test Run Started at $(Get-Date)" "Green"
Write-TestResult "=========================================" "Green"

# Check if pytest is installed
Write-TestResult "`n=== Checking pytest installation ===" "Yellow"
$pytestCheck = python -m pytest --version 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-TestResult "pytest not installed. Installing..." "Yellow"
    $installOutput = pip install pytest 2>&1
    $installOutput | ForEach-Object { Add-Content -Path $logFile -Value $_ }

    if ($LASTEXITCODE -ne 0) {
        Write-TestResult "Failed to install pytest!" "Red"
        exit 1
    }
    Write-TestResult "pytest installed successfully" "Green"
} else {
    Write-TestResult "pytest is available: $pytestCheck" "Green"
}

# Run Python script tests
Write-TestResult "`n=== Running Python Script Tests ===" "Yellow"
Write-TestResult "Command: python -m pytest scripts/ -v" "Gray"

$testOutput = python -m pytest scripts/ -v 2>&1
$testExitCode = $LASTEXITCODE

# Log all output
$testOutput | ForEach-Object { Add-Content -Path $logFile -Value $_ }

# Display output
foreach ($line in $testOutput) {
    if ($line -match "PASSED") {
        Write-TestResult $line "Green"
    } elseif ($line -match "FAILED") {
        Write-TestResult $line "Red"
    } elseif ($line -match "ERROR") {
        Write-TestResult $line "Red"
    } elseif ($line -match "passed|failed|error") {
        Write-TestResult $line "Cyan"
    } else {
        Write-TestResult $line "White"
    }
}

# Final Summary
Write-TestResult "`n=========================================" "Green"
Write-TestResult "Scripts Test Run Completed at $(Get-Date)" "Green"

if ($testExitCode -eq 0) {
    Write-TestResult "All scripts tests completed successfully!" "Green"
} else {
    Write-TestResult "Some scripts tests failed!" "Red"
}

Write-TestResult "Detailed log saved to: $logFile" "Cyan"

exit $testExitCode
