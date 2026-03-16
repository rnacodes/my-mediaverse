# PowerShell script to run all backend tests with detailed logging
$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$rootPath = Get-Location
$logFile = "$rootPath\logs\test-results-backend-$timestamp.log"

# Create logs directory if it doesn't exist
if (!(Test-Path "logs")) {
    New-Item -ItemType Directory -Path "logs" -Force | Out-Null
}

Write-Host "Running MyMediaVerse Backend Tests..." -ForegroundColor Green
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

# Function to run tests and capture results
function Invoke-TestRun {
    param(
        [string]$TestProject,
        [string]$TestType,
        [bool]$WithCoverage = $false
    )
    
    Write-TestResult "`n=== Running $TestType ===" "Yellow"
    
    $testArgs = @(
        "test", $TestProject,
        "--verbosity", "minimal",
        "--logger", "console;verbosity=minimal",
        "--logger", "trx;LogFileName=$TestType-results-$timestamp.trx"
    )
    
    if ($WithCoverage) {
        $testArgs += @("--collect", "XPlat Code Coverage", "--results-directory", "./TestResults")
    }
    
    Write-TestResult "Command: dotnet $($testArgs -join ' ')" "Gray"
    
    # Add timeout handling
    $timeoutSeconds = 300  # 5 minutes timeout
    $job = Start-Job -ScriptBlock {
        param($args)
        & dotnet @args 2>&1
        return $LASTEXITCODE
    } -ArgumentList $testArgs
    
    try {
        $result = Wait-Job -Job $job -Timeout $timeoutSeconds
        if ($result) {
            $output = Receive-Job -Job $job
            $exitCode = $output[-1]
            $output = $output[0..($output.Length-2)]
        } else {
            Write-TestResult "Test timed out after $timeoutSeconds seconds!" "Red"
            Stop-Job -Job $job
            Remove-Job -Job $job
            return 1
        }
    } finally {
        Remove-Job -Job $job -Force
    }
    
    # Log all output
    $output | ForEach-Object { Add-Content -Path $logFile -Value $_ }
    
    # Parse and display test results
    $passedTests = ($output | Select-String "Passed!").Count
    $failedTests = ($output | Select-String "Failed!").Count
    $skippedTests = ($output | Select-String "Skipped!").Count
    $totalTests = $passedTests + $failedTests + $skippedTests
    
    Write-TestResult "`n--- $TestType Results ---" "Cyan"
    Write-TestResult "Total Tests: $totalTests" "White"
    Write-TestResult "Passed: $passedTests" "Green"
    Write-TestResult "Failed: $failedTests" "Red"
    Write-TestResult "Skipped: $skippedTests" "Yellow"
    
    # Show failed tests details
    if ($failedTests -gt 0) {
        Write-TestResult "`n--- Failed Tests Details ---" "Red"
        $failedTestDetails = $output | Select-String -Pattern "Failed.*:" -Context 2
        foreach ($detail in $failedTestDetails) {
            Write-TestResult $detail.Line "Red"
            if ($detail.Context.PreContext) {
                foreach ($line in $detail.Context.PreContext) {
                    Write-TestResult "  $line" "DarkRed"
                }
            }
            if ($detail.Context.PostContext) {
                foreach ($line in $detail.Context.PostContext) {
                    Write-TestResult "  $line" "DarkRed"
                }
            }
        }
    }
    
    return $exitCode
}

# Initialize log file
Write-TestResult "Backend Test Run Started at $(Get-Date)" "Green"
Write-TestResult "=========================================" "Green"

# Build the solution first
Write-TestResult "`n=== Building Solution ===" "Yellow"
$buildOutput = & dotnet build "src\MyMediaVerse\MyMediaVerse.sln" --verbosity minimal 2>&1
$buildExitCode = $LASTEXITCODE

$buildOutput | ForEach-Object { Add-Content -Path $logFile -Value $_ }

if ($buildExitCode -ne 0) {
    Write-TestResult "Build failed! Cannot run tests." "Red"
    Write-TestResult "Build errors:" "Red"
    $buildOutput | ForEach-Object { Write-TestResult "  $_" "Red" }
    exit 1
} else {
    Write-TestResult "Build successful!" "Green"
}

# Change to the tests directory for running tests
Set-Location "tests"

# Run Unit Tests
$unitTestResult = Invoke-TestRun -TestProject "MyMediaVerse.UnitTests/MyMediaVerse.UnitTests.csproj" -TestType "Unit Tests"

if ($unitTestResult -ne 0) {
    Write-TestResult "`nUnit tests failed!" "Red"
    Write-TestResult "Check the log file for details: $logFile" "Yellow"
    Set-Location ".."
    exit 1
}

# Run Integration Tests
$integrationTestResult = Invoke-TestRun -TestProject "MyMediaVerse.IntegrationTests/MyMediaVerse.IntegrationTests.csproj" -TestType "Integration Tests"

if ($integrationTestResult -ne 0) {
    Write-TestResult "`nIntegration tests failed!" "Red"
    Write-TestResult "Check the log file for details: $logFile" "Yellow"
    Set-Location ".."
    exit 1
}

# Run All Tests with Coverage
Write-TestResult "`n=== Running All Tests with Coverage ===" "Yellow"
$coverageResult = Invoke-TestRun -TestProject "." -TestType "All Tests" -WithCoverage $true

# Final Summary
Write-TestResult "`n=========================================" "Green"
Write-TestResult "Backend Test Run Completed at $(Get-Date)" "Green"

if ($unitTestResult -eq 0 -and $integrationTestResult -eq 0) {
    Write-TestResult "All backend tests completed successfully!" "Green"
    Write-TestResult "Coverage report generated in ./TestResults/" "Cyan"
} else {
    Write-TestResult "Some tests failed!" "Red"
}

Write-TestResult "Detailed log saved to: $logFile" "Cyan"
Write-TestResult "Test result files (.trx) saved in current directory" "Cyan"

# Return to root directory
Set-Location ".."

