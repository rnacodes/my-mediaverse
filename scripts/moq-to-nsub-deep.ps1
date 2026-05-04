# Paren-balanced Moq -> NSubstitute sweep.
# Handles arbitrarily nested .Setup(x => x.M(...)) and .Verify(x => x.M(...), Times.X) calls
# that previous regex-only passes couldn't reach.
$ErrorActionPreference = 'Stop'

$root = "C:\Users\rashi\source\repos\MyMediaVerse\tests"
$files = Get-ChildItem -Path $root -Filter "*.cs" -Recurse

function Find-MatchingParen {
    param([string]$s, [int]$openIndex)
    # $s[$openIndex] must be '('. Returns index of matching ')', or -1.
    $depth = 0
    for ($i = $openIndex; $i -lt $s.Length; $i++) {
        $ch = $s[$i]
        if ($ch -eq '(') { $depth++ }
        elseif ($ch -eq ')') {
            $depth--
            if ($depth -eq 0) { return $i }
        }
    }
    return -1
}

function Convert-CallChain {
    param(
        [string]$content,
        [string]$callName,            # e.g. "Setup", "SetupGet", "Verify"
        [scriptblock]$rewriter        # ($lambdaVar, $methodInvocation, $afterArgs) -> string
    )
    $sb = [System.Text.StringBuilder]::new($content.Length)
    $i = 0
    $needle = ".$callName("
    while ($i -lt $content.Length) {
        $idx = $content.IndexOf($needle, $i)
        if ($idx -lt 0) {
            [void]$sb.Append($content.Substring($i))
            break
        }
        # Append everything before .$callName(
        [void]$sb.Append($content.Substring($i, $idx - $i))

        # Find the matching ')'
        $openParen = $idx + $needle.Length - 1
        $closeParen = Find-MatchingParen -s $content -openIndex $openParen
        if ($closeParen -lt 0) {
            # Unbalanced, just emit literal and move on
            [void]$sb.Append($content[$idx])
            $i = $idx + 1
            continue
        }

        $body = $content.Substring($openParen + 1, $closeParen - $openParen - 1)

        # Try to parse body as: <ws>VAR<ws>=><ws>VAR<ws>.<methodInvocation>[, ARGS]
        # For Setup/SetupGet there are no extra args. For Verify there may be ", Times.X"
        $bodyMatch = [regex]::Match($body, '^\s*(\w+)\s*=>\s*(\w+)\s*\.\s*(.+)$', 'Singleline')
        if (-not $bodyMatch.Success -or $bodyMatch.Groups[1].Value -ne $bodyMatch.Groups[2].Value) {
            # Not a lambda invocation pattern; emit literal
            [void]$sb.Append($content.Substring($idx, $closeParen - $idx + 1))
            $i = $closeParen + 1
            continue
        }
        $lambdaVar = $bodyMatch.Groups[1].Value
        $methodAndArgsAndExtra = $bodyMatch.Groups[3].Value

        # Within methodAndArgsAndExtra, find the FIRST balanced (...) group
        # That ends the method invocation; whatever follows after the matching ')' is "extras"
        $invokeOpen = $methodAndArgsAndExtra.IndexOf('(')
        if ($invokeOpen -lt 0) {
            # No invocation - probably a property access. Pass it as-is.
            $methodInvocation = $methodAndArgsAndExtra.TrimEnd()
            $extras = ''
        } else {
            $invokeClose = Find-MatchingParen -s $methodAndArgsAndExtra -openIndex $invokeOpen
            if ($invokeClose -lt 0) {
                [void]$sb.Append($content.Substring($idx, $closeParen - $idx + 1))
                $i = $closeParen + 1
                continue
            }
            $methodInvocation = $methodAndArgsAndExtra.Substring(0, $invokeClose + 1).TrimEnd()
            $extras = $methodAndArgsAndExtra.Substring($invokeClose + 1).TrimStart()
        }

        $replacement = & $rewriter $lambdaVar $methodInvocation $extras
        if ($null -eq $replacement) {
            # Rewriter declined; emit literal
            [void]$sb.Append($content.Substring($idx, $closeParen - $idx + 1))
        } else {
            [void]$sb.Append($replacement)
        }
        $i = $closeParen + 1
    }
    return $sb.ToString()
}

$setupRewriter = {
    param($var, $invocation, $extras)
    if ($extras -ne '') { return $null }
    return ".$invocation"
}

$verifyRewriter = {
    param($var, $invocation, $extras)
    # extras should look like ", Times.Once" / ", Times.Never" / ", Times.AtLeastOnce" / ", Times.Exactly(N)"
    if ($extras -match '^,\s*Times\.Once(\s*\(\s*\))?\s*$') {
        return ".Received(1).$invocation"
    }
    if ($extras -match '^,\s*Times\.Never(\s*\(\s*\))?\s*$') {
        return ".DidNotReceive().$invocation"
    }
    if ($extras -match '^,\s*Times\.AtLeastOnce(\s*\(\s*\))?\s*$') {
        return ".Received().$invocation"
    }
    $exactly = [regex]::Match($extras, '^,\s*Times\.Exactly\(\s*(\d+)\s*\)\s*$')
    if ($exactly.Success) {
        return ".Received($($exactly.Groups[1].Value)).$invocation"
    }
    return $null
}

$changed = 0
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    if (-not $c) { continue }
    $orig = $c

    $c = Convert-CallChain -content $c -callName 'Setup' -rewriter $setupRewriter
    $c = Convert-CallChain -content $c -callName 'SetupGet' -rewriter $setupRewriter
    $c = Convert-CallChain -content $c -callName 'Verify' -rewriter $verifyRewriter

    if ($c -ne $orig) {
        Set-Content -Path $f.FullName -Value $c -NoNewline -Encoding utf8
        $changed++
        Write-Output "  modified: $($f.FullName.Substring($root.Length + 1))"
    }
}
Write-Output ""
Write-Output "Files modified: $changed"
