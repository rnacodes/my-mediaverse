<#
.SYNOPSIS
    Remove the local testing database container (and optionally its data volume).

.DESCRIPTION
    Forcefully removes the testing container. Pass -RemoveVolume to also delete the named volume
    for a full wipe (use this before recreating from scratch, e.g. after a bad migration).
    Never touches the real demo or production databases.

.EXAMPLE
    .\delete-testing-db.ps1
    Removes the container but keeps the data volume (recreate later and the data is still there).

.EXAMPLE
    .\delete-testing-db.ps1 -RemoveVolume
    Removes the container AND the data volume (totally fresh start next time).

.NOTES
    Requires Docker Desktop to be running.
#>
[CmdletBinding()]
param(
    [string]$ContainerName = "mmv-testing-db",
    [string]$VolumeName    = "mmv-testing-data",
    [switch]$RemoveVolume
)

$existing = docker ps -a --filter "name=^/$ContainerName$" --format "{{.Names}}"
if ($existing -eq $ContainerName) {
    docker rm -f $ContainerName | Out-Null
    Write-Host "Removed container '$ContainerName'." -ForegroundColor Green
} else {
    Write-Host "No container named '$ContainerName' found." -ForegroundColor Yellow
}

if ($RemoveVolume) {
    docker volume rm $VolumeName 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Removed volume '$VolumeName'." -ForegroundColor Green
    } else {
        Write-Host "Volume '$VolumeName' not found or already removed." -ForegroundColor Yellow
    }
}
