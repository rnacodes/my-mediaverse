$ErrorActionPreference = 'Stop'
$dir   = 'tests/MyMediaVerse.UnitTests/Application'
$src   = Join-Path $dir 'ListenNotesServiceTests.cs'
$lines = Get-Content -LiteralPath $src

$regions = @{
    Search       = @{ Start = 36;  End = 91  }
    Podcast      = @{ Start = 93;  End = 154 }
    Episode      = @{ Start = 156; End = 198 }
    Playlist     = @{ Start = 200; End = 241 }
    Genre        = @{ Start = 243; End = 264 }
    Curated      = @{ Start = 266; End = 307 }
    Import       = @{ Start = 309; End = 457 }
    Helpers      = @{ Start = 459; End = 631 }
}

function Slice($name) {
    $r = $regions[$name]
    $lines[($r.Start - 1)..($r.End - 1)]
}

$header = @'
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.ListenNotes;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.UnitTests.Application
{
    public partial class ListenNotesServiceTests
    {
'@

$footer = @'
    }
}
'@

function WritePartial($filename, $regionNames) {
    $path = Join-Path $dir $filename
    $body = New-Object System.Collections.Generic.List[string]
    foreach ($n in $regionNames) {
        $body.AddRange([string[]](Slice $n))
        $body.Add('')
    }
    if ($body.Count -gt 0 -and [string]::IsNullOrEmpty($body[$body.Count-1])) {
        $body.RemoveAt($body.Count-1)
    }
    $out = @($header) + $body + @($footer)
    Set-Content -LiteralPath $path -Value $out -Encoding UTF8
    Write-Host "Wrote $path ($($out.Count) lines)"
}

WritePartial 'ListenNotesServiceTests.Catalog.cs' @('Search','Podcast','Episode','Playlist','Genre','Curated')
WritePartial 'ListenNotesServiceTests.Import.cs'  @('Import')

# Main: header (lines 1-35) with class -> partial, then Helpers, then close.
$mainHeader = $lines[0..34]
for ($i = 0; $i -lt $mainHeader.Count; $i++) {
    if ($mainHeader[$i] -match '^\s*public class ListenNotesServiceTests') {
        $mainHeader[$i] = $mainHeader[$i] -replace 'public class', 'public partial class'
    }
}

$mainBody = New-Object System.Collections.Generic.List[string]
$mainBody.AddRange([string[]](Slice 'Helpers'))

$mainClose = @('    }', '}')
$mainOut   = @($mainHeader) + $mainBody + $mainClose
Set-Content -LiteralPath $src -Value $mainOut -Encoding UTF8
Write-Host "Rewrote $src ($($mainOut.Count) lines)"
