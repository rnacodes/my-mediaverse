<#
.SYNOPSIS
    Seed the local testing database with the real demo data from a custom-format pg_dump backup.

.DESCRIPTION
    Restores a MyMediaVerse demo backup (.dump, custom format) into the running testing container.
    This recreates EXACTLY what is in the dump: the schema, the EF Core migration history
    (__EFMigrationsHistory), and all table data -- including pre-computed pgvector embeddings, so
    NO AI / embedding API calls are made and the result is fully deterministic.

    By default it uses the newest *.dump file in the repo's for-claude folder. Re-runnable: it
    drops and recreates the target database each time for a clean reseed.

.EXAMPLE
    .\seed-testing-db.ps1
    Restores the newest .dump from for-claude into 'mmvdemodb' on the testing container.

.EXAMPLE
    .\seed-testing-db.ps1 -DumpFile "C:\path\to\mmvdemodb_2026-06-01_02-00-01.dump"
    Restores a specific dump file.

.NOTES
    Requires the testing container to be running (see create-testing-db.ps1).
    pg_restore runs INSIDE the container, so the container's Postgres version reads the archive.
    Use a pg18 image for these v1.16 archives; a pg17 pg_restore may reject them.
#>
[CmdletBinding()]
param(
    [string]$ContainerName = "mmv-testing-db",
    [string]$DbName        = "mmvdemodb",
    [string]$DbUser        = "postgres",
    [string]$DumpFile,                                  # defaults to newest *.dump in $DumpDir
    [string]$DumpDir       = "$PSScriptRoot\..\..\for-claude"
)

# 1. Resolve which dump file to use (newest in for-claude unless one was passed explicitly).
if (-not $DumpFile) {
    $latest = Get-ChildItem -Path $DumpDir -Filter *.dump -ErrorAction SilentlyContinue |
              Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $latest) {
        Write-Error "No .dump file found in '$DumpDir'. Pass one with -DumpFile."
        return
    }
    $DumpFile = $latest.FullName
}
if (-not (Test-Path $DumpFile)) {
    Write-Error "Dump file not found: $DumpFile"
    return
}
Write-Host "Using dump: $DumpFile" -ForegroundColor Cyan

# 2. Make sure the container is running.
$running = docker ps --filter "name=^/$ContainerName$" --format "{{.Names}}"
if ($running -ne $ContainerName) {
    Write-Error "Container '$ContainerName' is not running. Run .\create-testing-db.ps1 (or 'docker start $ContainerName') first."
    return
}

# 3. Copy the dump into the container.
docker cp "$DumpFile" "$($ContainerName):/tmp/seed.dump"
if ($LASTEXITCODE -ne 0) { Write-Error "docker cp failed."; return }

# 4. Drop and recreate the target database for a clean reseed.
#    (Terminate any open sessions first, or DROP DATABASE will block.)
Write-Host "Recreating database '$DbName' for a clean seed..." -ForegroundColor Cyan
docker exec $ContainerName psql -U $DbUser -d postgres -c `
    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$DbName' AND pid <> pg_backend_pid();" | Out-Null
docker exec $ContainerName psql -U $DbUser -d postgres -c "DROP DATABASE IF EXISTS $DbName;" | Out-Null
docker exec $ContainerName psql -U $DbUser -d postgres -c "CREATE DATABASE $DbName;" | Out-Null

# 5. Restore. --no-owner / --no-privileges ignore the original 'mmv_demo_admin' role,
#    which does not exist in this throwaway container (otherwise you'd get harmless role errors).
Write-Host "Restoring dump into '$DbName' (a few extension/comment warnings are normal)..." -ForegroundColor Cyan
docker exec $ContainerName pg_restore -U $DbUser -d $DbName --no-owner --no-privileges /tmp/seed.dump

# 6. Show row counts -- this is the real proof the data landed.
Write-Host ""
Write-Host "Seed complete. Row counts per table:" -ForegroundColor Green
docker exec $ContainerName psql -U $DbUser -d $DbName -c `
    "SELECT relname AS table, n_live_tup AS rows FROM pg_stat_user_tables WHERE n_live_tup > 0 ORDER BY n_live_tup DESC;"
