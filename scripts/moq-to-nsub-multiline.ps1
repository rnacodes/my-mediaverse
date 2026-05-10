# Follow-up multi-line Moq -> NSubstitute migration sweep.
# The original moq-to-nsub.ps1 only handled single-line patterns.
$ErrorActionPreference = 'Stop'

$root = "C:\Users\rashi\source\repos\MyMediaVerse\tests"
$files = Get-ChildItem -Path $root -Filter "*.cs" -Recurse

# Allow nested parens (1 level)
$parenBody  = '((?:[^()]|\([^()]*\))+)'

$changed = 0
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    if (-not $c) { continue }
    $orig = $c

    # --- Multi-line .Setup(x => x.M(args)).Returns(...) ---
    # (?s) enables single-line mode so . matches newlines
    $c = [regex]::Replace($c, "(?s)\.Setup\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*\)", '.$2')
    $c = [regex]::Replace($c, "(?s)\.SetupGet\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*\)", '.$2')
    # Indexer setup multi-line
    $c = [regex]::Replace($c, "(?s)\.Setup\(\s*(\w+)\s*=>\s*\1\s*(\[[^\]]+\])\s*\)", '$2')

    # --- Multi-line .Verify(x => x.M(args), Times.X) ---
    $c = [regex]::Replace($c, "(?s)\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.Once\(?\)?\s*\)", '.Received(1).$2')
    $c = [regex]::Replace($c, "(?s)\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.Never\(?\)?\s*\)", '.DidNotReceive().$2')
    $c = [regex]::Replace($c, "(?s)\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.AtLeastOnce\(?\)?\s*\)", '.Received().$2')
    $c = [regex]::Replace($c, "(?s)\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.Exactly\(\s*(\d+)\s*\)\s*\)", '.Received($3).$2')

    # --- Multi-line .ThrowsAsync<T>(...) and .ThrowsAsync(...) ---
    $c = $c -replace '\.ThrowsAsync<', '.Throws<'
    $c = $c -replace '\.ThrowsAsync\(', '.Throws('

    # --- await Assert.Throws<T>(async ...) -> await Assert.ThrowsAsync<T>(async ...) ---
    # The `await` marker tells us the test author was already treating it as async
    $c = $c -replace 'await\s+Assert\.Throws<', 'await Assert.ThrowsAsync<'

    if ($c -ne $orig) {
        Set-Content -Path $f.FullName -Value $c -NoNewline -Encoding utf8
        $changed++
        Write-Output "  modified: $($f.FullName.Substring($root.Length + 1))"
    }
}
Write-Output ""
Write-Output "Files modified: $changed"
