# Rollback-XLLN-DLC-Copy.ps1
# One-step, surgical rollback of the ONLY change made by the CROWN'S ALBION live-test:
#   files added (additively) to  B:\Games\Fable 3\XLiveLessNess\dlc\4D53090A\10_ScriptInjector\
#
# Both forms work:
#   powershell -ExecutionPolicy Bypass -File tools\Rollback-XLLN-DLC-Copy.ps1
#   (or simply delete the folder  B:\Games\Fable 3\XLiveLessNess\dlc\4D53090A\10_ScriptInjector)
#
# SAFETY: refuses to run if there is anything unexpected inside the folder,
# so it can never delete anything that was not added by this test.
#
# Nothing else is touched: xlive.dll, xlln-config-1.ini, saves, the game's own
# DLC\ folder and all licensing behaviour remain exactly as they were.

$target = 'B:\Games\Fable 3\XLiveLessNess\dlc\4D53090A\10_ScriptInjector'

$expected = @(
  'content.xbx',
  'Content\package_collection_info.xmb',
  'Content\ScriptInjector.bnk',
  'Content\ScriptInjector.bnk.dat'
)

if (-not (Test-Path $target)) {
  "Nothing to remove - '$target' does not exist."
  exit 0
}

$present = @(Get-ChildItem $target -Recurse -File | ForEach-Object { $_.FullName.Substring($target.Length + 1) })
$unexpected = @($present | Where-Object { $_ -notin $expected })

"Files found in the test folder:"
$present | ForEach-Object { "  $_" }

if ($unexpected.Count -gt 0) {
  "REFUSING to delete: unexpected files are present that this test did not add:"
  $unexpected | ForEach-Object { "  $_" }
  exit 2
}

Remove-Item $target -Recurse -Force
"Removed '$target'."
"Test rollback complete. XLLN, saves, DLC and configuration were not touched."
