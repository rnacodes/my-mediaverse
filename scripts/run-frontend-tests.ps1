# PowerShell script to run frontend tests with detailed logging
param(
    [string]$LogDirectory = "logs"
)

$ErrorActionPreference = "Continue"
$timestamp = Get-Date -Format "yyyy-MM-dd_HH-mm-ss"
$rootPath = Get-Location
$logFile = "$rootPath\$LogDirectory\test-results-frontend-$timestamp.log"

Write-Host "Running MyMediaVerse Frontend Tests..." -ForegroundColor Green
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

# Function to run npm command and capture results
function Invoke-NpmTest {
    param(
        [string]$Command,
        [string]$TestType,
        [bool]$ExpectFailure = $false
    )
    
    Write-TestResult "`n=== $TestType ===" "Yellow"
    Write-TestResult "Command: npm $Command" "Gray"
    
    # Execute npm command with timeout
    $timeoutSeconds = 180  # 3 minutes timeout
    $job = Start-Job -ScriptBlock {
        param($cmd)
        if ($cmd -eq "run test:run") {
            & npm run test:run 2>&1
        } elseif ($cmd -eq "run test:coverage") {
            & npm run test:coverage 2>&1
        } else {
            & npm $cmd 2>&1
        }
        return $LASTEXITCODE
    } -ArgumentList $Command
    
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
    
    # Parse test results for Vitest output
    $passedTests = ($output | Select-String "PASS").Count
    $failedTests = ($output | Select-String "FAIL|Error").Count
    $skippedTests = ($output | Select-String "SKIP|skipped").Count
    
    # Try to extract more detailed results
    $testSummary = $output | Select-String -Pattern "Tests.*\d+.*passed.*\d+.*failed" -AllMatches
    if ($testSummary) {
        Write-TestResult "`n--- $TestType Results ---" "Cyan"
        foreach ($match in $testSummary) {
            Write-TestResult $match.Line "White"
        }
    } else {
        Write-TestResult "`n--- $TestType Results ---" "Cyan"
        Write-TestResult "Passed: $passedTests" "Green"
        Write-TestResult "Failed: $failedTests" "Red"
        Write-TestResult "Skipped: $skippedTests" "Yellow"
    }
    
    # Show failed tests details
    if ($failedTests -gt 0) {
        Write-TestResult "`n--- Failed Tests Details ---" "Red"
        $failedTestDetails = $output | Select-String -Pattern "FAIL|Error" -Context 3
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
    
    # Show coverage results if available
    $coverageInfo = $output | Select-String -Pattern "Coverage|Statements|Branches|Functions|Lines"
    if ($coverageInfo) {
        Write-TestResult "`n--- Coverage Results ---" "Cyan"
        foreach ($line in $coverageInfo) {
            Write-TestResult $line.Line "White"
        }
    }
    
    return $exitCode
}

# Create log directory if it doesn't exist
if (!(Test-Path $LogDirectory)) {
    New-Item -ItemType Directory -Path $LogDirectory -Force | Out-Null
}

# Change to the frontend directory
Set-Location "frontend"

# Initialize log file
Write-TestResult "Frontend Test Run Started at $(Get-Date)" "Green"
Write-TestResult "=========================================" "Green"

# Install Dependencies
Write-TestResult "`n=== Installing Dependencies ===" "Yellow"
$installOutput = & npm install 2>&1
$installExitCode = $LASTEXITCODE

$installOutput | ForEach-Object { Add-Content -Path $logFile -Value $_ }

if ($installExitCode -ne 0) {
    Write-TestResult "Failed to install dependencies!" "Red"
    Write-TestResult "Check the log file for details: $logFile" "Yellow"
    Set-Location ".."
    exit 1
} else {
    Write-TestResult "Dependencies installed successfully" "Green"
}

# Run Unit Tests
$unitTestResult = Invoke-NpmTest -Command "run test:run" -TestType "Unit Tests"

if ($unitTestResult -ne 0) {
    Write-TestResult "`nUnit tests failed!" "Red"
    Write-TestResult "Check the log file for details: $logFile" "Yellow"
    Set-Location ".."
    exit 1
}

# Run Tests with Coverage
$coverageResult = Invoke-NpmTest -Command "run test:coverage" -TestType "Tests with Coverage"

if ($coverageResult -ne 0) {
    Write-TestResult "`nCoverage tests failed!" "Red"
    Write-TestResult "Check the log file for details: $logFile" "Yellow"
    Set-Location ".."
    exit 1
}

# Final Summary
Write-TestResult "`n=========================================" "Green"
Write-TestResult "Frontend Test Run Completed at $(Get-Date)" "Green"

if ($unitTestResult -eq 0 -and $coverageResult -eq 0) {
    Write-TestResult "All frontend tests completed successfully!" "Green"
    Write-TestResult "Coverage report generated in coverage/ directory" "Cyan"
} else {
    Write-TestResult "Some tests failed!" "Red"
}

Write-TestResult "Detailed log saved to: $logFile" "Cyan"
Write-TestResult "Test results also available in console output above" "Cyan"

# Return to root directory
Set-Location ".."

