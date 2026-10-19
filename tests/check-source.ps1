<#
  CROWN'S ALBION — engine safety regression.

  Static checks that the recovered safety semantics survive the v2.0 rewrite.
  These are structural ("integration") checks, not live game verification.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$srcFiles = Get-ChildItem (Join-Path $root 'src') -Filter *.cs -File
$src = ($srcFiles | ForEach-Object { Get-Content $_.FullName -Raw -Encoding UTF8 }) -join "`n"

$script:checks = 0
$script:failed = 0
function Check([bool]$ok, [string]$name) {
  $script:checks++
  if ($ok) { Write-Host ("PASS " + $name) -ForegroundColor Green }
  else { $script:failed++; Write-Host ("FAIL " + $name) -ForegroundColor Red }
}

# ---------------------------------------------------------------- identity
Check ($src.Contains('AAC98798E77CA32282D663AF54FC185C3B5256B5317ED8568D7F12A0EF42DA42')) "Patch 2 checksum constant is pinned"
Check ($src.Contains('-- CROWN_ALBION_MANAGED')) "managed-script marker is present"
Check ($src.Contains('CROWN_ALBION_DONE')) "Lua once-only sentinel is present"
Check ($src.Contains('GameProcessName = "Fable3"')) "game process guard targets Fable3"

# ---------------------------------------------------------------- gates
Check ($src.Contains('High-risk commands are locked')) "high-risk gate blocks dispatch"
Check ($src.Contains('CO-OP PROTECTION is ON')) "co-op gate blocks non-test commands"
Check ($src.Contains('!= "test-message"')) "co-op gate still allows only the script test"
Check ($src.Contains('Fable III is not running')) "dispatch refuses when the game is not running"
Check ($src.Contains('belongs to another mod')) "foreign script is refused, never overwritten"
Check ($src.Contains('Non-CROWN script detected')) "manual clear refuses foreign scripts"

# ---------------------------------------------------------------- file safety
Check ($src.Contains('File.Replace(temp, path, null)')) "atomic replace is used for script writes"
Check ($src.Contains('Backup hash mismatch')) "backups are hash-verified before proceeding"
Check ($src.Contains('SHA256File')) "SHA-256 hashing is implemented"
Check ($src.Contains('PulseMs = 5000')) "pulse window defaults to 5000 ms"
Check ($src.Contains('Math.Max(2500, prefs.PulseMs)')) "clear timer cannot go below the safe floor"

# ---------------------------------------------------------------- non-destructive
# The trainer must never write to the game executable, DLC, xlive.dll or saves.
$writeTargets = [regex]::Matches($src, '(?:WriteAllText|WriteAtomic|File\.Copy)\s*\((?:[^\)]*)')
$writeText = ($writeTargets | ForEach-Object { $_.Value }) -join "`n"
Check (-not ($writeText -match 'Fable3\.exe')) "trainer never writes to Fable3.exe"
Check (-not ($writeText -match 'xlive\.dll')) "trainer never writes to xlive.dll"
Check (-not ($writeText -match 'DLC')) "trainer never writes into game DLC"
Check (-not ($writeText -match '\.sav')) "trainer never writes save files"

# ---------------------------------------------------------------- no network / no launching
Check (-not ($src -match 'System\.Net')) "engine performs no network access"
Check (-not ($src -match 'Process\.Start\(')) "engine never launches external executables"
Check (-not ($src -match 'Fable3DebugMenu')) "engine never references the debug-menu executable"

# ---------------------------------------------------------------- presentational intent
Check ($src.Contains('Albion.Init(root)')) "theme/assets initialise from the app root"
Check ($srcFiles.Count -ge 5) "engine is split into focused modules (got $($srcFiles.Count))"

Write-Host ""
if ($script:failed -gt 0) {
  Write-Host ("SAFETY TESTS FAILED: {0} of {1} checks." -f $script:failed, $script:checks) -ForegroundColor Red
  exit 1
}
Write-Host ("PASS {0} engine safety checks." -f $script:checks) -ForegroundColor Green
exit 0
