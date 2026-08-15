<#
.SYNOPSIS
    Finds (and optionally deletes) orphaned notes: rows in the MyMediaVerse database
    whose slug no longer exists in the published Quartz vault's content index.

.DESCRIPTION
    The note sync intentionally never deletes — removing a note from the Obsidian vault
    leaves its row in the database (and in search) until it is deleted through the API.
    This script diffs the vault's published /static/contentIndex.json against
    GET /api/note?vault=<vault> and lists the orphans. Re-run with -Delete to remove
    them via DELETE /api/note/{id} (media/mixlist links cascade away; the search index
    is cleaned up by the next notes reindex).

    Default run is a read-only dry run. Credentials are only required for -Delete
    (or if the API rejects anonymous reads).

.PARAMETER ApiBase
    API base URL including /api.

.PARAMETER VaultUrl
    Base URL of the published Quartz site for the vault.

.PARAMETER Vault
    Vault name as stored in the database (e.g. general, programming).

.PARAMETER Delete
    Delete each orphan via the API instead of just listing them.

.PARAMETER Force
    Skip the confirmation prompt when -Delete is set.

.EXAMPLE
    .\notes-orphan-diff.ps1                 # dry run against the demo environment
    .\notes-orphan-diff.ps1 -Delete         # delete orphans (prompts first)

    Username/password/admin-key fall back to the AUTH_USERNAME, AUTH_PASSWORD and
    DEMO_ADMIN_KEY environment variables. The admin key header is only needed for
    demo-environment writes.
#>
[CmdletBinding()]
param(
    [string]$ApiBase = "https://demo-api.mymediaverseuniverse.com/api",
    [string]$VaultUrl = "https://demogarden.mymediaverseuniverse.com",
    [string]$Vault = "general",
    [switch]$Delete,
    [switch]$Force,
    [string]$Username = $env:AUTH_USERNAME,
    [string]$Password = $env:AUTH_PASSWORD,
    [string]$AdminKey = $env:DEMO_ADMIN_KEY
)

$ErrorActionPreference = "Stop"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$ApiBase = $ApiBase.TrimEnd('/')
$VaultUrl = $VaultUrl.TrimEnd('/')

function Get-AuthHeaders {
    if ([string]::IsNullOrEmpty($script:Username) -or [string]::IsNullOrEmpty($script:Password)) {
        throw "Username/password required (pass -Username/-Password or set AUTH_USERNAME/AUTH_PASSWORD)."
    }
    $loginHeaders = @{}
    if ($script:AdminKey) { $loginHeaders["X-Demo-Admin-Key"] = $script:AdminKey }
    $body = @{ username = $script:Username; password = $script:Password } | ConvertTo-Json
    $login = Invoke-RestMethod -Uri "$ApiBase/auth/login" -Method Post -Headers $loginHeaders `
        -ContentType "application/json" -Body $body
    $headers = @{ Authorization = "Bearer $($login.token)" }
    if ($script:AdminKey) { $headers["X-Demo-Admin-Key"] = $script:AdminKey }
    return $headers
}

# 1. Slugs currently published in the vault (contentIndex.json is a slug-keyed map)
$indexUrl = "$VaultUrl/static/contentIndex.json"
Write-Host "Fetching content index: $indexUrl"
$contentIndex = Invoke-RestMethod -Uri $indexUrl -Method Get
$publishedSlugs = @($contentIndex.PSObject.Properties.Name)
Write-Host "  $($publishedSlugs.Count) published notes in vault '$Vault'"

# 2. Notes currently in the database for that vault
$notesUrl = "$ApiBase/note?vault=$Vault"
Write-Host "Fetching database notes: $notesUrl"
try {
    $dbNotesRaw = Invoke-RestMethod -Uri $notesUrl -Method Get
}
catch {
    Write-Host "  Anonymous read rejected; logging in..."
    $dbNotesRaw = Invoke-RestMethod -Uri $notesUrl -Method Get -Headers (Get-AuthHeaders)
}
# Windows PowerShell 5.1: Invoke-RestMethod emits a JSON array as a single object,
# so wrap the variable (which flattens) rather than the call (which nests).
$dbNotes = @($dbNotesRaw)
Write-Host "  $($dbNotes.Count) notes in the database"

# 3. Diff — case-insensitive, since manual creates lowercase slugs but sync stores raw casing
$publishedLookup = @{}
foreach ($slug in $publishedSlugs) { $publishedLookup[$slug.ToLowerInvariant()] = $true }
$orphans = @($dbNotes | Where-Object { -not $publishedLookup.ContainsKey($_.slug.ToLowerInvariant()) })

if ($orphans.Count -eq 0) {
    Write-Host "`nNo orphans: every database note still exists in the published vault." -ForegroundColor Green
    return
}

Write-Host "`n$($orphans.Count) orphaned note(s) (in database, absent from the published vault):" -ForegroundColor Yellow
$orphans |
    Select-Object @{n = "Title"; e = { $_.title } },
                  @{n = "Slug"; e = { $_.slug } },
                  @{n = "Id"; e = { $_.id } },
                  @{n = "LastSyncedAt"; e = { $_.lastSyncedAt } },
                  @{n = "LinkedMedia"; e = { @($_.linkedMediaItems).Count } } |
    Format-Table -AutoSize

if (-not $Delete) {
    Write-Host "Dry run only. Re-run with -Delete to remove these notes via the API."
    return
}

if (-not $Force) {
    $answer = Read-Host "Delete these $($orphans.Count) note(s) from $ApiBase ? (y/N)"
    if ($answer -ne "y") { Write-Host "Aborted."; return }
}

$headers = Get-AuthHeaders
$deleted = 0
foreach ($note in $orphans) {
    try {
        Invoke-RestMethod -Uri "$ApiBase/note/$($note.id)" -Method Delete -Headers $headers | Out-Null
        $deleted++
        Write-Host "  Deleted: $($note.title) ($($note.slug))"
    }
    catch {
        Write-Warning "  Failed to delete '$($note.slug)': $($_.Exception.Message)"
    }
}
Write-Host "`nDeleted $deleted of $($orphans.Count) orphan(s)." -ForegroundColor Green
Write-Host "Remember to run POST /api/search/reindex-notes so the search index reconciles the deletions."
