param(
    [Parameter(Mandatory = $true)][string]$Path,
    [Parameter(Mandatory = $true)][ValidateSet('Unit','Integration','Database')][string]$Category
)

$ErrorActionPreference = 'Stop'
$traitLine = '[Trait("Category", "' + $Category + '")]'
$files = Get-ChildItem -Path $Path -Filter '*Tests.cs' -Recurse -File
$modified = 0
$skipped = 0

foreach ($file in $files) {
    $raw = Get-Content -Path $file.FullName -Raw
    if ($raw -notmatch '\[(?:Fact|Theory)') { $skipped++; continue }

    # Detect line ending used in file (preserve)
    $eol = if ($raw -match "`r`n") { "`r`n" } else { "`n" }
    $lines = $raw -split "(?:`r`n|`n)"

    $changed = $false
    $newLines = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $lines.Length; $i++) {
        $line = $lines[$i]
        if ($line -match '^(\s*)public\s+(?:abstract\s+|sealed\s+|static\s+)?(?:partial\s+)?class\s+\w+') {
            $indent = $matches[1]
            $prev = if ($newLines.Count -gt 0) { $newLines[$newLines.Count - 1] } else { '' }
            if ($prev -notmatch '\[Trait\("Category"') {
                $newLines.Add($indent + $traitLine) | Out-Null
                $changed = $true
            }
        }
        $newLines.Add($line) | Out-Null
    }

    if ($changed) {
        $newContent = ($newLines -join $eol)
        # Preserve trailing newline if original had one
        if ($raw.EndsWith("`n") -and -not $newContent.EndsWith("`n")) { $newContent += $eol }
        [System.IO.File]::WriteAllText($file.FullName, $newContent, (New-Object System.Text.UTF8Encoding($false)))
        $modified++
        Write-Host "  modified: $($file.FullName.Substring($Path.Length).TrimStart('\','/'))"
    }
}

Write-Host ""
Write-Host "Done. modified=$modified skipped=$skipped (no [Fact]/[Theory])"
