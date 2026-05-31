$ErrorActionPreference = 'Stop'
$dir  = 'tests/MyMediaVerse.UnitTests/Infrastructure'
$src  = Join-Path $dir 'TraktSyncServiceTests.cs'
$lines = Get-Content -LiteralPath $src

# 1-based, inclusive line ranges for each #region in the source file.
$regions = @{
    IsConnected          = @{ Start = 27;   End = 54   }
    GetStatus            = @{ Start = 56;   End = 104  }
    SaveToken            = @{ Start = 106;  End = 159  }
    GetValidAccessToken  = @{ Start = 161;  End = 256  }
    Disconnect           = @{ Start = 258;  End = 306  }
    WatchedMovies        = @{ Start = 308;  End = 479  }
    WatchedShows         = @{ Start = 481;  End = 863  }
    Watchlist            = @{ Start = 865;  End = 1183 }
    Ratings              = @{ Start = 1185; End = 1519 }
    SyncAll              = @{ Start = 1521; End = 1584 }
    Helpers              = @{ Start = 1586; End = 1701 }
}

function Slice($name) {
    $r = $regions[$name]
    # Get-Content returns 0-indexed array; subtract 1.
    $lines[($r.Start - 1)..($r.End - 1)]
}

$header = @'
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Services.Sync;
using MyMediaVerse.Shared.DTOs.Trakt;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    public partial class TraktSyncServiceTests
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
    # Drop trailing blank line we just added so the final region's #endregion sits flush against footer.
    if ($body.Count -gt 0 -and [string]::IsNullOrEmpty($body[$body.Count-1])) {
        $body.RemoveAt($body.Count-1)
    }
    $out = @($header) + $body + @($footer)
    Set-Content -LiteralPath $path -Value $out -Encoding UTF8
    Write-Host "Wrote $path ($($out.Count) lines)"
}

WritePartial 'TraktSyncServiceTests.WatchedMovies.cs' @('WatchedMovies')
WritePartial 'TraktSyncServiceTests.WatchedShows.cs'  @('WatchedShows')
WritePartial 'TraktSyncServiceTests.Watchlist.cs'     @('Watchlist')
WritePartial 'TraktSyncServiceTests.Ratings.cs'       @('Ratings')
WritePartial 'TraktSyncServiceTests.SyncAll.cs'       @('SyncAll')

# Rebuild main file: header (1-26) + AuthToken regions + Helpers + closing braces.
# Original line 14 declares "public class TraktSyncServiceTests" -> change to "public partial class".
$mainHeader   = $lines[0..25] # lines 1..26
# Convert the class declaration to partial.
for ($i = 0; $i -lt $mainHeader.Count; $i++) {
    if ($mainHeader[$i] -match '^\s*public class TraktSyncServiceTests') {
        $mainHeader[$i] = $mainHeader[$i] -replace 'public class', 'public partial class'
    }
}

$mainBody = New-Object System.Collections.Generic.List[string]
foreach ($n in @('IsConnected','GetStatus','SaveToken','GetValidAccessToken','Disconnect','Helpers')) {
    $mainBody.AddRange([string[]](Slice $n))
    $mainBody.Add('')
}
if ($mainBody.Count -gt 0 -and [string]::IsNullOrEmpty($mainBody[$mainBody.Count-1])) {
    $mainBody.RemoveAt($mainBody.Count-1)
}

$mainClose = @('    }', '}')
$mainOut = @($mainHeader) + $mainBody + $mainClose
Set-Content -LiteralPath $src -Value $mainOut -Encoding UTF8
Write-Host "Rewrote $src ($($mainOut.Count) lines)"
