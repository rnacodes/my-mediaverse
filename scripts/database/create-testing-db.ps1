<#
.SYNOPSIS
    Spin up a local Docker Postgres (pgvector) container to use as a MyMediaVerse testing database.

.DESCRIPTION
    Creates a disposable/persistent pgvector container with a named volume (so data survives
    stop/start), waits until Postgres is accepting connections, and ensures an empty target
    database exists, ready to be seeded by seed-testing-db.ps1.

    Nothing here touches the real demo or production databases — it is an isolated local container
    on a non-default port.

.EXAMPLE
    .\create-testing-db.ps1
    Creates the container 'mmv-testing-db' on port 5433 with database 'mmvdemodb'.

.NOTES
    Requires Docker Desktop to be running.
    Default image is pg18 because the demo backups (.dump, custom-format v1.16) are produced by
    pg_dump 18.x; a pg17 pg_restore can reject them. Switch -PgImage to 'pgvector/pgvector:pg17'
    if you specifically need to mirror the current production major version.
#>
[CmdletBinding()]
param(
    [string]$ContainerName = "mmv-testing-db",
    [string]$VolumeName    = "mmv-testing-data",
    [string]$DbName        = "mmvdemodb",
    [string]$DbUser        = "postgres",
    [string]$DbPassword    = "test",
    [int]   $Port          = 5433,
    [string]$PgImage       = "pgvector/pgvector:pg18"
)

# If a container with this name already exists (running or stopped), don't clobber it.
$existing = docker ps -a --filter "name=^/$ContainerName$" --format "{{.Names}}"
if ($existing -eq $ContainerName) {
    Write-Host "Container '$ContainerName' already exists." -ForegroundColor Yellow
    Write-Host "  Start it again with : docker start $ContainerName"
    Write-Host "  Remove it first with: .\delete-testing-db.ps1"
    return
}

Write-Host "Creating container '$ContainerName' from $PgImage on localhost:$Port ..." -ForegroundColor Cyan
# PGDATA points at a SUBDIRECTORY of the mounted volume. This is required for the pg18 image
# (its default data layout changed) and is harmless on pg17 -- so the same mount works either way.
docker run -d `
    --name $ContainerName `
    -e POSTGRES_USER=$DbUser `
    -e POSTGRES_PASSWORD=$DbPassword `
    -e POSTGRES_DB=$DbName `
    -e PGDATA=/var/lib/postgresql/data/pgdata `
    -p "$($Port):5432" `
    -v "$($VolumeName):/var/lib/postgresql/data" `
    $PgImage | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker run failed. Is Docker Desktop running?"
    return
}

# Wait for Postgres to accept connections (first boot initialises the data dir).
Write-Host "Waiting for Postgres to accept connections..." -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    $null = docker exec $ContainerName pg_isready -U $DbUser 2>&1
    if ($LASTEXITCODE -eq 0) { $ready = $true; break }
    Start-Sleep -Seconds 1
}
if (-not $ready) {
    Write-Error "Postgres did not become ready in 30s. Check 'docker logs $ContainerName'."
    return
}

Write-Host ""
Write-Host "Testing DB is up and empty. Connection string for the backend:" -ForegroundColor Green
Write-Host "  Host=localhost;Port=$Port;Database=$DbName;Username=$DbUser;Password=$DbPassword"
Write-Host ""
Write-Host "Next step: seed it with the demo backup ->  .\seed-testing-db.ps1" -ForegroundColor Green
