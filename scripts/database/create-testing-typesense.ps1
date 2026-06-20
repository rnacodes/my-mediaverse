<#
.SYNOPSIS
    Spin up a local Docker Typesense container to use as a MyMediaVerse testing search backend.

.DESCRIPTION
    Creates a persistent Typesense container with a named volume (so indexed data survives
    stop/start), waits until the /health endpoint is green, and prints the env vars to point the
    backend at it. Pair this with the Docker test DB (create-testing-db.ps1) so local search —
    including auto-embedding and hybrid search — works fully isolated from the Droplet's live
    demo/production Typesense.

    Nothing here touches the live demo or production Typesense — it is an isolated local container
    on a non-default port.

    NOTE: auto-embedding (the `embedding` field) only gets created when the backend that builds the
    collections has OPENAI_API_KEY set. Without a key, collections are created keyword-only. Either
    way, you must reindex (POST /api/search/reindex*) once after pointing the backend here, because
    a fresh container starts with no collections/data.

.EXAMPLE
    .\create-testing-typesense.ps1
    Creates the container 'mmv-testing-typesense' on port 8108 with api key 'test-api-key'.

.NOTES
    Requires Docker Desktop to be running.
    Image is pinned to typesense/typesense:30.2 to match the live Droplet (validated for 3072-dim
    auto-embedding). The named volume is mounted at /data and passed as --data-dir, because the
    image does not create a default data directory on its own.
#>
[CmdletBinding()]
param(
    [string]$ContainerName = "mmv-testing-typesense",
    [string]$VolumeName    = "mmv-testing-typesense-data",
    [string]$ApiKey        = "test-api-key",
    [int]   $Port          = 8108,
    [string]$Image         = "typesense/typesense:30.2"
)

# If a container with this name already exists (running or stopped), don't clobber it.
$existing = docker ps -a --filter "name=^/$ContainerName$" --format "{{.Names}}"
if ($existing -eq $ContainerName) {
    Write-Host "Container '$ContainerName' already exists." -ForegroundColor Yellow
    Write-Host "  Start it again with : docker start $ContainerName"
    Write-Host "  Remove it first with: .\delete-testing-typesense.ps1"
    return
}

Write-Host "Creating container '$ContainerName' from $Image on localhost:$Port ..." -ForegroundColor Cyan
# The named volume is mounted at /data and passed as --data-dir. Mounting the volume makes the
# mount point exist (the bare image otherwise has no /data dir), so Typesense boots cleanly.
docker run -d `
    --name $ContainerName `
    -p "$($Port):8108" `
    -v "$($VolumeName):/data" `
    $Image `
    --data-dir /data `
    --api-key=$ApiKey `
    --enable-cors | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "docker run failed. Is Docker Desktop running?"
    return
}

# Wait for Typesense to report healthy.
Write-Host "Waiting for Typesense to become healthy..." -ForegroundColor Cyan
$ready = $false
for ($i = 0; $i -lt 30; $i++) {
    try {
        $resp = Invoke-RestMethod -Uri "http://localhost:$Port/health" -TimeoutSec 2 -ErrorAction Stop
        if ($resp.ok -eq $true) { $ready = $true; break }
    } catch {
        # not up yet
    }
    Start-Sleep -Seconds 1
}
if (-not $ready) {
    Write-Error "Typesense did not become healthy in 30s. Check 'docker logs $ContainerName'."
    return
}

Write-Host ""
Write-Host "Testing Typesense is up and empty. Point the backend at it with:" -ForegroundColor Green
Write-Host "  `$env:TYPESENSE_HOST = 'localhost'"
Write-Host "  `$env:TYPESENSE_PORT = '$Port'"
Write-Host "  `$env:TYPESENSE_PROTOCOL = 'http'"
Write-Host "  `$env:TYPESENSE_ADMIN_API_KEY = '$ApiKey'"
Write-Host "  `$env:OPENAI_API_KEY = '<key from repo-root .env>'   # needed for auto-embedding"
Write-Host ""
Write-Host "Then reindex to create + populate the collections:" -ForegroundColor Green
Write-Host "  POST /api/search/reindex, /reindex-mixlists, /reindex-notes, /reindex-highlights"
Write-Host ""
Write-Host "Tear down with:  .\delete-testing-typesense.ps1 [-RemoveVolume]" -ForegroundColor Green
