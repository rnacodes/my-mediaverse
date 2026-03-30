# update-demo-image-urls.ps1
# Updates old DigitalOcean Spaces URLs in the demo database to the new space.
# Replaces both CDN and non-CDN variants of the old URL.
# Connects via SSH to the droplet and runs psql inside the postgres Docker container.
#
# Usage: .\scripts\update-demo-image-urls.ps1
#        .\scripts\update-demo-image-urls.ps1 -DryRun   # Preview only, no changes
#        .\scripts\update-demo-image-urls.ps1 -Diagnose  # Show sample Thumbnail values

param(
    [switch]$DryRun,
    [switch]$Diagnose
)

$oldNonCdn = "https://project-loopbreaker.atl1.digitaloceanspaces.com"
$oldCdn    = "https://project-loopbreaker.atl1.cdn.digitaloceanspaces.com"
$newUrl    = "https://mymediaverse-storage.nyc3.cdn.digitaloceanspaces.com"

$dropletIp = "165.227.212.135"
$sshUser   = "root"
$remoteTmpFile = "/tmp/mmv-update-urls.sql"

# Get connection string from environment variable to extract DB credentials
$connectionString = $env:DEMO_DB_CONNECTION
if (-not $connectionString) {
    Write-Error "DEMO_DB_CONNECTION environment variable is not set."
    exit 1
}

# Parse connection string to extract database credentials
function Parse-ConnectionString($connStr) {
    $parts = @{}
    foreach ($segment in $connStr.Split(';')) {
        $segment = $segment.Trim()
        if ($segment -match '^([^=]+)=(.+)$') {
            $parts[$matches[1].Trim()] = $matches[2].Trim()
        }
    }
    return $parts
}

$dbParts = Parse-ConnectionString $connectionString
$pgDb   = $dbParts["Database"]
$pgUser = if ($dbParts["Username"]) { $dbParts["Username"] } else { $dbParts["User Id"] }
$pgPass = $dbParts["Password"]

if (-not $pgDb -or -not $pgUser) {
    Write-Error "Could not parse connection string. Expected format: Host=...;Database=...;Username=...;Password=..."
    exit 1
}

Write-Host "Connecting to demo database via SSH ($sshUser@$dropletIp)..." -ForegroundColor Cyan

function Invoke-RemoteSql($query) {
    # Write query to a temp file on the droplet, then run psql -f inside Docker.
    # This avoids shell quoting issues with double quotes around PostgreSQL identifiers.
    $writeCmd = "cat > $remoteTmpFile << 'SQLEOF'`n$query`nSQLEOF"
    ssh "${sshUser}@${dropletIp}" $writeCmd 2>&1 | Out-Null

    $execCmd = "docker exec -e PGPASSWORD=$pgPass postgres psql -U $pgUser -d $pgDb -t -A -f /tmp/mmv-update-urls.sql"
    # Copy the temp file into the container first, then execute
    $copyAndRun = "docker cp $remoteTmpFile postgres:$remoteTmpFile; docker exec -e PGPASSWORD=$pgPass postgres psql -U $pgUser -d $pgDb -t -A -f $remoteTmpFile"
    $result = ssh "${sshUser}@${dropletIp}" $copyAndRun 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Remote psql command failed: $result"
        exit 1
    }
    return $result
}

# Diagnose mode: show sample Thumbnail values and exit
if ($Diagnose) {
    $diagQuery = @"
SELECT 'MediaItems' AS tbl, "Thumbnail" FROM "MediaItems" WHERE "Thumbnail" IS NOT NULL LIMIT 5;
SELECT 'Mixlists' AS tbl, "Thumbnail" FROM "Mixlists" WHERE "Thumbnail" IS NOT NULL LIMIT 5;
"@
    Write-Host "`nSample Thumbnail values:" -ForegroundColor Yellow
    $diagResult = Invoke-RemoteSql $diagQuery
    foreach ($line in $diagResult -split "`n") {
        if ($line.Trim()) {
            Write-Host "  $line" -ForegroundColor White
        }
    }
    # Clean up temp file on droplet
    ssh "${sshUser}@${dropletIp}" "rm -f $remoteTmpFile" 2>&1 | Out-Null
    exit 0
}

# Build the dry-run query to show affected rows
$countQuery = @"
SELECT 'MediaItems (non-CDN)' AS source, COUNT(*) AS count
FROM "MediaItems" WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.digitaloceanspaces.com%'
  AND "Thumbnail" NOT LIKE '%cdn.digitaloceanspaces.com%'
UNION ALL
SELECT 'MediaItems (CDN)', COUNT(*)
FROM "MediaItems" WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.cdn.digitaloceanspaces.com%'
UNION ALL
SELECT 'Mixlists (non-CDN)', COUNT(*)
FROM "Mixlists" WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.digitaloceanspaces.com%'
  AND "Thumbnail" NOT LIKE '%cdn.digitaloceanspaces.com%'
UNION ALL
SELECT 'Mixlists (CDN)', COUNT(*)
FROM "Mixlists" WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.cdn.digitaloceanspaces.com%';
"@

# Show counts
Write-Host "`nRows to update:" -ForegroundColor Yellow
$counts = Invoke-RemoteSql $countQuery
foreach ($line in $counts -split "`n") {
    if ($line.Trim()) {
        $parts = $line.Split('|')
        if ($parts.Count -eq 2) {
            Write-Host "  $($parts[0].Trim()): $($parts[1].Trim())" -ForegroundColor White
        }
    }
}

if ($DryRun) {
    Write-Host "`n[DRY RUN] No changes made." -ForegroundColor Yellow
    Write-Host "Mapping:" -ForegroundColor Cyan
    Write-Host "  $oldNonCdn -> $newUrl"
    Write-Host "  $oldCdn -> $newUrl"
}
else {
    Write-Host "`nApplying updates..." -ForegroundColor Cyan

    # CDN variant first (more specific), then non-CDN
    $updateQuery = @"
BEGIN;

UPDATE "MediaItems"
SET "Thumbnail" = REPLACE("Thumbnail", '$oldCdn', '$newUrl')
WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.cdn.digitaloceanspaces.com%';

UPDATE "MediaItems"
SET "Thumbnail" = REPLACE("Thumbnail", '$oldNonCdn', '$newUrl')
WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.digitaloceanspaces.com%';

UPDATE "Mixlists"
SET "Thumbnail" = REPLACE("Thumbnail", '$oldCdn', '$newUrl')
WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.cdn.digitaloceanspaces.com%';

UPDATE "Mixlists"
SET "Thumbnail" = REPLACE("Thumbnail", '$oldNonCdn', '$newUrl')
WHERE "Thumbnail" LIKE '%project-loopbreaker.atl1.digitaloceanspaces.com%';

COMMIT;
"@

    Invoke-RemoteSql $updateQuery
    Write-Host "Updates applied successfully." -ForegroundColor Green

    # Verify no old URLs remain
    $verifyQuery = @"
SELECT COUNT(*) FROM "MediaItems" WHERE "Thumbnail" LIKE '%project-loopbreaker%'
UNION ALL
SELECT COUNT(*) FROM "Mixlists" WHERE "Thumbnail" LIKE '%project-loopbreaker%';
"@
    $remaining = Invoke-RemoteSql $verifyQuery
    $remainingCounts = ($remaining -split "`n" | Where-Object { $_.Trim() -match '^\d+$' } | ForEach-Object { [int]$_.Trim() })
    $totalRemaining = ($remainingCounts | Measure-Object -Sum).Sum

    if ($totalRemaining -eq 0) {
        Write-Host "Verified: No old URLs remain in the database." -ForegroundColor Green
    }
    else {
        Write-Host "WARNING: $totalRemaining rows still contain old URLs!" -ForegroundColor Red
    }
}

# Clean up temp file on droplet
ssh "${sshUser}@${dropletIp}" "rm -f $remoteTmpFile" 2>&1 | Out-Null
