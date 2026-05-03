# One-shot Moq -> NSubstitute migration sweep across tests/.
# Idempotent on already-migrated files.
$ErrorActionPreference = 'Stop'

$root = "C:\Users\rashi\source\repos\MyMediaVerse\tests"
$files = Get-ChildItem -Path $root -Filter "*.cs" -Recurse

# Allow up to 1 level of nested generics: <Foo<Bar>>
$genericArg = '((?:[^<>]|<[^<>]*>)+)'
# Allow up to 1 level of nested parens: (foo(bar))
$parenBody  = '((?:[^()]|\([^()]*\))+)'

$changed = 0
foreach ($f in $files) {
    $c = Get-Content $f.FullName -Raw
    if (-not $c) { continue }
    $orig = $c

    # --- using directives ---
    $c = $c -replace '(?m)^\s*using Moq;\s*\r?\n', "using NSubstitute;`r`n"
    $c = $c -replace '(?m)^\s*using Moq\.Protected;\s*\r?\n', ''

    # --- constructors / factories ---
    # new Mock<X>() -> Substitute.For<X>()
    $c = [regex]::Replace($c, "new\s+Mock<$genericArg>\s*\(\s*\)", 'Substitute.For<$1>()')
    # Mock.Of<X>() -> Substitute.For<X>()
    $c = [regex]::Replace($c, "\bMock\.Of<$genericArg>\s*\(\s*\)", 'Substitute.For<$1>()')

    # --- type wrapper stripping (declarations / field types) ---
    # Mock<X> -> X (must run AFTER `new Mock<X>()` so we don't strip the constructor pattern)
    $c = [regex]::Replace($c, "\bMock<$genericArg>", '$1')

    # --- argument matchers ---
    $c = $c -replace '\bIt\.IsAny<', 'Arg.Any<'
    $c = $c -replace '\bIt\.Is<', 'Arg.Is<'

    # --- async helpers ---
    $c = $c -replace '\.ReturnsAsync\(', '.Returns('
    $c = $c -replace '\.ThrowsAsync\(', '.Throws('
    $c = $c -replace '\.ThrowsAsync<', '.Throws<'

    # --- .Object stripping (only on identifiers that look mock-y) ---
    # Match _mockFoo.Object, mockFoo.Object, _xMock.Object, etc.
    $c = [regex]::Replace($c, '(\b\w*[Mm]ock\w*)\.Object\b', '$1')

    # --- .Setup unwrap ---
    # var.Setup(l => l.METHODBODY) -> var.METHODBODY
    $c = [regex]::Replace($c, "\.Setup\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*\)", '.$2')
    # SetupGet
    $c = [regex]::Replace($c, "\.SetupGet\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*\)", '.$2')
    # Indexer setup: Setup(l => l[KEY]) -> [KEY] (preserves the chained .Returns)
    # Pattern eats the leading `.` too so we end up with mock[KEY].Returns(...)
    $c = [regex]::Replace($c, "\.Setup\(\s*(\w+)\s*=>\s*\1\s*(\[[^\]]+\])\s*\)", '$2')

    # --- .Verify unwrap ---
    # var.Verify(l => l.METHODBODY, Times.Once) -> var.Received(1).METHODBODY
    $c = [regex]::Replace($c, "\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.Once\s*\)", '.Received(1).$2')
    $c = [regex]::Replace($c, "\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.Never\s*\)", '.DidNotReceive().$2')
    $c = [regex]::Replace($c, "\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.AtLeastOnce\s*\)", '.Received().$2')
    $c = [regex]::Replace($c, "\.Verify\(\s*(\w+)\s*=>\s*\1\s*\.$parenBody\s*,\s*Times\.Exactly\(\s*(\d+)\s*\)\s*\)", '.Received($3).$2')

    if ($c -ne $orig) {
        Set-Content -Path $f.FullName -Value $c -NoNewline -Encoding utf8
        $changed++
        Write-Output "  modified: $($f.FullName.Substring($root.Length + 1))"
    }
}
Write-Output ""
Write-Output "Files modified: $changed"
