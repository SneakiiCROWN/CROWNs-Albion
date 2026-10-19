# Read-only health report for CROWN's ALBION. No network, game changes, or administrative rights.
$ErrorActionPreference='Stop'
$tool=$PSScriptRoot
$game=Split-Path -Parent $tool
$report=Join-Path $tool 'CROWN_Diagnostic.txt'
$expected='AAC98798E77CA32282D663AF54FC185C3B5256B5317ED8568D7F12A0EF42DA42'
$lines=New-Object System.Collections.Generic.List[string]
function Add([string]$value){$script:lines.Add($value);Write-Host $value}
Add 'CROWNs ALBION v1.4 — local, read-only diagnostic'
Add ('Date: '+(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'))
Add ('Game target: '+$game)
$exe=Join-Path $game 'Fable3.exe'
if(Test-Path -LiteralPath $exe){
 $hash=(Get-FileHash -LiteralPath $exe -Algorithm SHA256).Hash
 Add ('Executable SHA256: '+$hash)
 Add ('Target checksum matches: '+($hash -eq $expected))
}else{Add 'Game EXE not found next to tool folder. Recheck install path.'}
foreach($file in @('xlive.dll','XLiveEmu','steam_api.dll','Start-Fable3-LAN.bat','data\dir.manifest','data\scripts\MyMod\MyScript01.lua')){
 $full=Join-Path $game $file
 Add ($file+': '+($(if(Test-Path -LiteralPath $full){'PRESENT'}else{'ABSENT'})))
}
$lua=Join-Path $game 'data\scripts\MyMod\MyScript01.lua'
if(Test-Path -LiteralPath $lua){
 $raw=[IO.File]::ReadAllText($lua)
 if($raw.TrimStart().StartsWith('-- CROWN_ALBION_MANAGED')){
  Add ('CROWN script state: '+($(if($raw.Contains('-- GENERATED')){'PENDING / POSSIBLY STILL ACTIVE'}else{'IDLE MARKER'})))
 }elseif([string]::IsNullOrWhiteSpace($raw)){Add 'Script state: empty'}
 else{Add 'Script state: owned by another mod; do not overwrite'}
}
$manifest=Join-Path $game 'data\dir.manifest'
if(Test-Path -LiteralPath $manifest){
 $registered=@([IO.File]::ReadAllLines($manifest)|Where-Object {$_.Trim() -ieq 'scripts\MyMod\MyScript01.lua'}).Count
 Add ('MyScript01.lua manifest registrations: '+$registered)
}
Add 'This checks files, NOT live Lua execution, Xbox input, co-op, or game saves.'
[IO.File]::WriteAllLines($report,$lines.ToArray(),(New-Object System.Text.UTF8Encoding($false)))
Add ('Diagnostic saved: '+$report)
