<#
.SYNOPSIS
    Import books from a Goodreads CSV export file in batches.

.DESCRIPTION
    This script splits a large Goodreads CSV export into smaller chunks and uploads
    them to the MyMediaVerse API with configurable delays between batches.
    This is useful for very large libraries (thousands of books) to avoid timeouts.

.PARAMETER CsvPath
    Required. Path to the Goodreads CSV export file.

.PARAMETER ApiUrl
    Base URL for the API. Defaults to http://localhost:5033/api

.PARAMETER ChunkSize
    Number of books per chunk. Defaults to 50.

.PARAMETER DelayMs
    Delay in milliseconds between chunks. Defaults to 1000 (1 second).

.PARAMETER UpdateExisting
    Switch to update existing books on match. If not specified, existing books will be skipped.

.PARAMETER AuthToken
    JWT authentication token for the API. If not provided, you'll be prompted.

.EXAMPLE
    .\Import-GoodreadsCsv.ps1 -CsvPath "C:\Downloads\goodreads_library_export.csv"

    Imports from a local Goodreads export using default settings.

.EXAMPLE
    .\Import-GoodreadsCsv.ps1 -CsvPath "export.csv" -ChunkSize 100 -DelayMs 2000 -UpdateExisting

    Imports with 100 books per chunk, 2 second delay, and updates existing books.

.EXAMPLE
    .\Import-GoodreadsCsv.ps1 -CsvPath "export.csv" -ApiUrl "https://api.mymediaverseuniverse.com/api"

    Imports to a remote API endpoint.

.NOTES
    Author: MyMediaVerse
    Requires: PowerShell 5.1 or later
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateScript({ Test-Path $_ -PathType Leaf })]
    [string]$CsvPath,

    [Parameter()]
    [string]$ApiUrl = "http://localhost:5033/api",

    [Parameter()]
    [ValidateRange(10, 500)]
    [int]$ChunkSize = 50,

    [Parameter()]
    [ValidateRange(100, 30000)]
    [int]$DelayMs = 1000,

    [Parameter()]
    [switch]$UpdateExisting,

    [Parameter()]
    [string]$AuthToken
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Validate CSV file
if (-not (Test-Path $CsvPath)) {
    Write-Error "CSV file not found: $CsvPath"
    exit 1
}

if (-not $CsvPath.EndsWith(".csv")) {
    Write-Error "File must be a CSV file: $CsvPath"
    exit 1
}

# Prompt for auth token if not provided
if (-not $AuthToken) {
    Write-Host "Please enter your JWT authentication token:" -ForegroundColor Yellow
    $AuthToken = Read-Host -AsSecureString
    $AuthToken = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($AuthToken)
    )
}

if (-not $AuthToken) {
    Write-Error "Authentication token is required"
    exit 1
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Goodreads CSV Import Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "CSV File: $CsvPath" -ForegroundColor White
Write-Host "API URL: $ApiUrl" -ForegroundColor White
Write-Host "Chunk Size: $ChunkSize books" -ForegroundColor White
Write-Host "Delay: $DelayMs ms between chunks" -ForegroundColor White
Write-Host "Update Existing: $UpdateExisting" -ForegroundColor White
Write-Host ""

# Read and parse CSV
Write-Host "Reading CSV file..." -ForegroundColor Yellow
try {
    $csv = Import-Csv -Path $CsvPath
    $totalBooks = $csv.Count
    Write-Host "Found $totalBooks books in CSV" -ForegroundColor Green
}
catch {
    Write-Error "Failed to read CSV file: $_"
    exit 1
}

if ($totalBooks -eq 0) {
    Write-Host "No books found in CSV file" -ForegroundColor Yellow
    exit 0
}

# Calculate chunks
$totalChunks = [Math]::Ceiling($totalBooks / $ChunkSize)
Write-Host "Will process in $totalChunks chunks" -ForegroundColor White
Write-Host ""

# Statistics
$totalSuccess = 0
$totalCreated = 0
$totalUpdated = 0
$totalSkipped = 0
$totalErrors = 0
$allErrors = @()

# Process chunks
for ($i = 0; $i -lt $totalChunks; $i++) {
    $startIndex = $i * $ChunkSize
    $endIndex = [Math]::Min($startIndex + $ChunkSize - 1, $totalBooks - 1)
    $chunkNumber = $i + 1

    Write-Host "Processing chunk $chunkNumber of $totalChunks (books $($startIndex + 1) to $($endIndex + 1))..." -ForegroundColor Cyan

    # Extract chunk
    $chunk = $csv[$startIndex..$endIndex]

    # Create temporary CSV file for chunk
    $tempPath = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.csv'
    try {
        $chunk | Export-Csv -Path $tempPath -NoTypeInformation -Encoding UTF8

        # Prepare request
        $updateParam = if ($UpdateExisting) { "true" } else { "false" }
        $uri = "$ApiUrl/upload/goodreads-csv?updateExisting=$updateParam&chunkIndex=$i&totalChunks=$totalChunks"

        # Create multipart form data
        $boundary = [System.Guid]::NewGuid().ToString()
        $fileContent = [System.IO.File]::ReadAllBytes($tempPath)
        $fileName = "chunk_$chunkNumber.csv"

        $bodyLines = @(
            "--$boundary",
            "Content-Disposition: form-data; name=`"file`"; filename=`"$fileName`"",
            "Content-Type: text/csv",
            "",
            [System.Text.Encoding]::UTF8.GetString($fileContent),
            "--$boundary--"
        )
        $body = $bodyLines -join "`r`n"

        # Send request
        $headers = @{
            "Authorization" = "Bearer $AuthToken"
            "Content-Type"  = "multipart/form-data; boundary=$boundary"
        }

        $response = Invoke-RestMethod -Uri $uri -Method POST -Headers $headers -Body $body -TimeoutSec 120

        # Process response
        if ($response.result) {
            $result = $response.result
        }
        else {
            $result = $response
        }

        $totalSuccess += $result.successCount
        $totalCreated += $result.createdCount
        $totalUpdated += $result.updatedCount
        $totalSkipped += $result.skippedCount
        $totalErrors += $result.errorCount

        if ($result.errors -and $result.errors.Count -gt 0) {
            $allErrors += $result.errors
        }

        # Progress bar
        $percentComplete = [Math]::Round(($chunkNumber / $totalChunks) * 100)
        Write-Progress -Activity "Importing Goodreads Library" -Status "Chunk $chunkNumber of $totalChunks" -PercentComplete $percentComplete

        Write-Host "  Created: $($result.createdCount) | Updated: $($result.updatedCount) | Errors: $($result.errorCount)" -ForegroundColor $(if ($result.errorCount -gt 0) { "Yellow" } else { "Green" })
    }
    catch {
        Write-Host "  ERROR: Failed to process chunk $chunkNumber - $_" -ForegroundColor Red
        $totalErrors += $chunk.Count
    }
    finally {
        # Clean up temp file
        if (Test-Path $tempPath) {
            Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
        }
    }

    # Delay between chunks (except for the last one)
    if ($i -lt $totalChunks - 1) {
        Start-Sleep -Milliseconds $DelayMs
    }
}

Write-Progress -Activity "Importing Goodreads Library" -Completed

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Import Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Total Processed: $totalBooks" -ForegroundColor White
Write-Host "Successful: $totalSuccess" -ForegroundColor Green
Write-Host "  - Created: $totalCreated" -ForegroundColor Green
Write-Host "  - Updated: $totalUpdated" -ForegroundColor Yellow
Write-Host "Skipped: $totalSkipped" -ForegroundColor Gray
Write-Host "Errors: $totalErrors" -ForegroundColor $(if ($totalErrors -gt 0) { "Red" } else { "Green" })

if ($allErrors.Count -gt 0) {
    Write-Host ""
    Write-Host "Errors:" -ForegroundColor Red
    $allErrors | Select-Object -First 20 | ForEach-Object {
        Write-Host "  - $_" -ForegroundColor Red
    }
    if ($allErrors.Count -gt 20) {
        Write-Host "  ... and $($allErrors.Count - 20) more errors" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "Done!" -ForegroundColor Green
