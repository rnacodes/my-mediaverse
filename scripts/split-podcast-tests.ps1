$ErrorActionPreference = 'Stop'
$dir   = 'tests/MyMediaVerse.UnitTests/Application'
$src   = Join-Path $dir 'PodcastServiceTests.cs'
$lines = Get-Content -LiteralPath $src

$regions = @{
    Series       = @{ Start = 30;  End = 237 }
    Episodes     = @{ Start = 239; End = 450 }
    Inheritance  = @{ Start = 452; End = 567 }
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
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    public partial class PodcastServiceTests
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

WritePartial 'PodcastServiceTests.Episodes.cs' @('Episodes','Inheritance')

# Main: header (lines 1-29) with class -> partial, then Series region, then close.
$mainHeader = $lines[0..28]
for ($i = 0; $i -lt $mainHeader.Count; $i++) {
    if ($mainHeader[$i] -match '^\s*public class PodcastServiceTests') {
        $mainHeader[$i] = $mainHeader[$i] -replace 'public class', 'public partial class'
    }
}

$mainBody = New-Object System.Collections.Generic.List[string]
$mainBody.AddRange([string[]](Slice 'Series'))

$mainClose = @('    }', '}')
$mainOut   = @($mainHeader) + $mainBody + $mainClose
Set-Content -LiteralPath $src -Value $mainOut -Encoding UTF8
Write-Host "Rewrote $src ($($mainOut.Count) lines)"
