<#
  CROWN'S ALBION — data integrity regression.

  Validates the generated runtime data (catalogue, item vault, ledger) without
  launching Fable III or the trainer UI. Run after tools\generate-data.ps1.

  Exit code 0 = all checks passed.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$data = Join-Path $root 'data'
$iconsDir = Join-Path $root 'Assets\CROWN_UI\icons'
$docs = Join-Path $root 'docs'

$script:checks = 0
$script:failed = 0
function Check([bool]$ok, [string]$name) {
  $script:checks++
  if ($ok) { Write-Host ("PASS " + $name) -ForegroundColor Green }
  else { $script:failed++; Write-Host ("FAIL " + $name) -ForegroundColor Red }
}

function Read-Json([string]$path) {
  if (-not (Test-Path $path)) { throw "Missing file: $path" }
  return (Get-Content $path -Raw -Encoding UTF8 | ConvertFrom-Json)
}

# ---------------------------------------------------------------- catalogue
$cat = Read-Json (Join-Path $data 'catalog.json')
$base = Read-Json (Join-Path $data 'catalog_base.json')
$vault = Read-Json (Join-Path $data 'item_vault.json')

$ids = @($cat | ForEach-Object { [string]$_.id })
$vaultIds = @($vault | ForEach-Object { [string]$_.id })

Check ($base.Count -eq 95) "baseline catalogue is the preserved 95 commands (got $($base.Count))"
Check ($cat.Count -ge 175) "catalogue has curated additions (got $($cat.Count))"
Check (($ids | Sort-Object -Unique).Count -eq $ids.Count) "every catalogue id is unique"
Check (($vaultIds | Sort-Object -Unique).Count -eq $vaultIds.Count) "every vault id is unique"
Check ($vault.Count -ge 300) "item vault is populated (got $($vault.Count))"

# Baseline is preserved byte-for-byte relative to the recovered build: the
# generated catalogue must contain every baseline id.
$missingBase = @($base | Where-Object { $ids -notcontains [string]$_.id })
Check ($missingBase.Count -eq 0) "no baseline command was dropped"

# Required fields.
$badFields = New-Object System.Collections.ArrayList
foreach ($c in $cat) {
  foreach ($f in @('id', 'title', 'group', 'summary', 'template', 'risk', 'kind', 'evidence', 'coop', 'icon')) {
    if ([string]::IsNullOrWhiteSpace([string]$c.$f)) { [void]$badFields.Add("$($c.id): empty $f") }
  }
  if (@('low', 'medium', 'high') -notcontains [string]$c.risk) { [void]$badFields.Add("$($c.id): bad risk '$($c.risk)'") }
  if (@('safe', 'local', 'unverified', 'unsafe') -notcontains [string]$c.coop) { [void]$badFields.Add("$($c.id): bad coop '$($c.coop)'") }
  if (@('action', 'toggle') -notcontains [string]$c.kind) { [void]$badFields.Add("$($c.id): bad kind '$($c.kind)'") }
}
Check ($badFields.Count -eq 0) "catalogue fields are valid ($($badFields.Count) problems)"
if ($badFields.Count -gt 0) { $badFields | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red } }

# Template placeholders must match declared parameters.
$tmplErrors = New-Object System.Collections.ArrayList
foreach ($c in $cat) {
  $declared = @()
  if ($null -ne $c.params) { foreach ($p in $c.params) { $declared += [string]$p.id } }
  if ($declared.Count -gt 1) { [void]$tmplErrors.Add("$($c.id): more than one parameter") }
  foreach ($m in [regex]::Matches([string]$c.template, '\{([A-Za-z0-9_]+)\}')) {
    if ($declared -notcontains $m.Groups[1].Value) { [void]$tmplErrors.Add("$($c.id): placeholder {$($m.Groups[1].Value)} undeclared") }
  }
}
Check ($tmplErrors.Count -eq 0) "templates and parameters agree ($($tmplErrors.Count) problems)"
if ($tmplErrors.Count -gt 0) { $tmplErrors | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red } }

# Lua templates must not smuggle file/OS/network side effects.
$danger = @('os\.execute', 'os\.remove', 'os\.rename', 'io\.', 'dofile', 'loadstring', 'loadfile', 'require\s*\(', 'package\.', 'debug\.getinfo', 'System\.', '\\\\', '\.\./')
$dangerHits = New-Object System.Collections.ArrayList
foreach ($c in $cat) {
  foreach ($d in $danger) { if ([regex]::IsMatch([string]$c.template, $d)) { [void]$dangerHits.Add("$($c.id): $d") } }
}
Check ($dangerHits.Count -eq 0) "catalogue templates contain no file/OS/network calls"

# Expected curated groups.
$groups = @($cat | ForEach-Object { [string]$_.group } | Sort-Object -Unique)
foreach ($g in @('Appearance', 'Chaos', 'Spawning', 'World', 'Kingdom', 'Hero')) {
  Check ($groups -contains $g) "curated group present: $g"
}

# ---------------------------------------------------------------- icons
$icons = @{}
if (Test-Path $iconsDir) {
  Get-ChildItem $iconsDir -Filter *.svg | ForEach-Object { $icons[$_.BaseName] = $true }
}
$iconRefs = @()
$iconRefs += @($cat | ForEach-Object { [string]$_.icon })
$iconRefs += @($vault | ForEach-Object { [string]$_.icon })
$iconRefs = $iconRefs | Sort-Object -Unique
$missingIcons = @($iconRefs | Where-Object { -not $icons.ContainsKey($_) })
Check ($icons.Count -ge 100) "icon set extracted (got $($icons.Count))"
Check ($missingIcons.Count -eq 0) "every referenced icon exists ($($missingIcons -join ', '))"

# ---------------------------------------------------------------- vault
$vaultProblems = New-Object System.Collections.ArrayList
foreach ($v in $vault) {
  if ([string]::IsNullOrWhiteSpace([string]$v.name)) { [void]$vaultProblems.Add("$($v.id): empty name") }
  if ([string]::IsNullOrWhiteSpace([string]$v.template)) { [void]$vaultProblems.Add("$($v.id): empty template") }
  if ([string]::IsNullOrWhiteSpace([string]$v.category)) { [void]$vaultProblems.Add("$($v.id): empty category") }
  foreach ($d in $danger) { if ([regex]::IsMatch([string]$v.template, $d)) { [void]$vaultProblems.Add("$($v.id): $d") } }
}
Check ($vaultProblems.Count -eq 0) "vault entries are valid ($($vaultProblems.Count) problems)"
if ($vaultProblems.Count -gt 0) { $vaultProblems | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red } }

# ---------------------------------------------------------------- ledger
$ledger = Join-Path $docs 'FEATURE_LEDGER.md'
Check (Test-Path $ledger) "feature ledger was generated"
if (Test-Path $ledger) {
  $text = Get-Content $ledger -Raw -Encoding UTF8
  Check ($text.Contains("CROWN's ALBION Feature Ledger")) "ledger has a title"
  Check ($text.Contains("$($cat.Count) commands") -or $text.Contains("Verified in game")) "ledger documents command count"
  Check ($text.Contains("Integration-verified")) "ledger explains integration verification"
}

# ---------------------------------------------------------------- result
Write-Host ""
if ($script:failed -gt 0) {
  Write-Host ("DATA TESTS FAILED: {0} of {1} checks." -f $script:failed, $script:checks) -ForegroundColor Red
  exit 1
}
Write-Host ("PASS {0} data integrity checks." -f $script:checks) -ForegroundColor Green
exit 0
