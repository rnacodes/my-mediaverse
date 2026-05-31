$ErrorActionPreference = 'Stop'
$dir   = 'tests/MyMediaVerse.UnitTests/Application'
$src   = Join-Path $dir 'NoteServiceTests.cs'
$lines = Get-Content -LiteralPath $src

$regions = @{
    GetById          = @{ Start = 45;  End = 73  }
    GetBySlug        = @{ Start = 75;  End = 108 }
    GetAll           = @{ Start = 110; End = 147 }
    Create           = @{ Start = 149; End = 235 }
    Update           = @{ Start = 237; End = 323 }
    Delete           = @{ Start = 325; End = 350 }
    Link             = @{ Start = 352; End = 442 }
    Unlink           = @{ Start = 444; End = 483 }
    SyncQuartz       = @{ Start = 485; End = 577 }
    GetSyncStatus    = @{ Start = 579; End = 600 }
}

function Slice($name) {
    $r = $regions[$name]
    $lines[($r.Start - 1)..($r.End - 1)]
}

$header = @'
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.Obsidian;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public partial class NoteServiceTests
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

WritePartial 'NoteServiceTests.MediaLinking.cs' @('Link','Unlink')
WritePartial 'NoteServiceTests.QuartzSync.cs'   @('SyncQuartz','GetSyncStatus')

# Main: header (lines 1-44) with class -> partial, then CRUD regions, then close.
$mainHeader = $lines[0..43]
for ($i = 0; $i -lt $mainHeader.Count; $i++) {
    if ($mainHeader[$i] -match '^\s*public class NoteServiceTests') {
        $mainHeader[$i] = $mainHeader[$i] -replace 'public class', 'public partial class'
    }
}

$mainBody = New-Object System.Collections.Generic.List[string]
foreach ($n in @('GetById','GetBySlug','GetAll','Create','Update','Delete')) {
    $mainBody.AddRange([string[]](Slice $n))
    $mainBody.Add('')
}
if ($mainBody.Count -gt 0 -and [string]::IsNullOrEmpty($mainBody[$mainBody.Count-1])) {
    $mainBody.RemoveAt($mainBody.Count-1)
}

$mainClose = @('    }', '}')
$mainOut   = @($mainHeader) + $mainBody + $mainClose
Set-Content -LiteralPath $src -Value $mainOut -Encoding UTF8
Write-Host "Rewrote $src ($($mainOut.Count) lines)"
