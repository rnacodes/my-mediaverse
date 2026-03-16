# PowerShell script to run all tests (backend and frontend) with comprehensive logging
$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$rootPath = Get-Location
$masterLogFile = "$rootPath\logs\test-results-master-$timestamp.log"

# Create logs directory if it doesn't exist
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}

Write-Host "Running All MyMediaVerse Tests..." -ForegroundColor Green
Write-Host "Master log file: $masterLogFile" -ForegroundColor Cyan

# Function to log and display results
function Write-MasterResult {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    
    Write-Host $Message -ForegroundColor $Color
    Add-Content -Path $masterLogFile -Value "$(Get-Date -Format 'HH:mm:ss'): $Message"
}

# Initialize master log
Write-MasterResult "=========================================" "Green"
Write-MasterResult "MyMediaVerse Master Test Run Started" "Green"
Write-MasterResult "Timestamp: $(Get-Date)" "Green"
Write-MasterResult "=========================================" "Green"

$overallSuccess = $true
$backendSuccess = $true
$frontendSuccess = $true
$scriptsSuccess = $true

# Run Backend Tests
Write-MasterResult "`n=== Running Backend Tests ===" "Yellow"
$backendStartTime = Get-Date

try {
    & .\run-backend-tests.ps1
    if ($LASTEXITCODE -ne 0) {
        $backendSuccess = $false
        $overallSuccess = $false
        Write-MasterResult "Backend tests failed!" "Red"
    } else {
        Write-MasterResult "Backend tests passed!" "Green"
    }
} catch {
    $backendSuccess = $false
    $overallSuccess = $false
    Write-MasterResult "Backend tests encountered an error: $($_.Exception.Message)" "Red"
}

$backendDuration = (Get-Date) - $backendStartTime
Write-MasterResult "Backend tests completed in: $($backendDuration.TotalSeconds.ToString('F2')) seconds" "Cyan"

# Run Frontend Tests
Write-MasterResult "`n=== Running Frontend Tests ===" "Yellow"
$frontendStartTime = Get-Date

try {
    & .\run-frontend-tests.ps1
    if ($LASTEXITCODE -ne 0) {
        $frontendSuccess = $false
        $overallSuccess = $false
        Write-MasterResult "Frontend tests failed!" "Red"
    } else {
        Write-MasterResult "Frontend tests passed!" "Green"
    }
} catch {
    $frontendSuccess = $false
    $overallSuccess = $false
    Write-MasterResult "Frontend tests encountered an error: $($_.Exception.Message)" "Red"
}

$frontendDuration = (Get-Date) - $frontendStartTime
Write-MasterResult "Frontend tests completed in: $($frontendDuration.TotalSeconds.ToString('F2')) seconds" "Cyan"

# Run Scripts Tests (Python)
Write-MasterResult "`n=== Running Scripts Tests ===" "Yellow"
$scriptsStartTime = Get-Date

try {
    & .\run-scripts-tests.ps1
    if ($LASTEXITCODE -ne 0) {
        $scriptsSuccess = $false
        $overallSuccess = $false
        Write-MasterResult "Scripts tests failed!" "Red"
    } else {
        Write-MasterResult "Scripts tests passed!" "Green"
    }
} catch {
    $scriptsSuccess = $false
    $overallSuccess = $false
    Write-MasterResult "Scripts tests encountered an error: $($_.Exception.Message)" "Red"
}

$scriptsDuration = (Get-Date) - $scriptsStartTime
Write-MasterResult "Scripts tests completed in: $($scriptsDuration.TotalSeconds.ToString('F2')) seconds" "Cyan"

# Overall Summary
$totalDuration = (Get-Date) - $backendStartTime
Write-MasterResult "`n=========================================" "Green"
Write-MasterResult "Overall Test Summary" "Green"
Write-MasterResult "=========================================" "Green"
Write-MasterResult "Total execution time: $($totalDuration.TotalMinutes.ToString('F2')) minutes" "Cyan"
Write-MasterResult "Backend tests: $(if($backendSuccess) {'PASSED'} else {'FAILED'})" $(if($backendSuccess) {"Green"} else {"Red"})
Write-MasterResult "Frontend tests: $(if($frontendSuccess) {'PASSED'} else {'FAILED'})" $(if($frontendSuccess) {"Green"} else {"Red"})
Write-MasterResult "Scripts tests: $(if($scriptsSuccess) {'PASSED'} else {'FAILED'})" $(if($scriptsSuccess) {"Green"} else {"Red"})

if ($overallSuccess) {
    Write-MasterResult "`nALL TESTS COMPLETED SUCCESSFULLY!" "Green"
    Write-MasterResult "Check individual log files for detailed results:" "Cyan"
    Write-MasterResult "   - Backend: test-results-backend-*.log" "Cyan"
    Write-MasterResult "   - Frontend: test-results-frontend-*.log" "Cyan"
    Write-MasterResult "   - Scripts: test-results-scripts-*.log" "Cyan"
} else {
    Write-MasterResult "`nSOME TESTS FAILED!" "Red"
    Write-MasterResult "Check the following for details:" "Yellow"
    Write-MasterResult "   - Master log: $masterLogFile" "Yellow"
    Write-MasterResult "   - Backend log: test-results-backend-*.log" "Yellow"
    Write-MasterResult "   - Frontend log: test-results-frontend-*.log" "Yellow"
    Write-MasterResult "   - Scripts log: test-results-scripts-*.log" "Yellow"
    Write-MasterResult "   - Test result files (.trx) in tests/ directory" "Yellow"
    Write-MasterResult "   - Coverage reports in tests/TestResults/ and frontend/coverage/" "Yellow"
}

Write-MasterResult "`nMaster test run completed at: $(Get-Date)" "Green"
Write-MasterResult "=========================================" "Green"

if (-not $overallSuccess) {
    exit 1
}

