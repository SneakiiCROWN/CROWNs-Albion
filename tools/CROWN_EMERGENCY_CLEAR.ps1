# Emergency stop for CROWN's Albion only. Does not touch xlive.dll, Fable3.exe, saves or third-party scripts.
$ErrorActionPreference='Stop'
$tool=$PSScriptRoot
$game=Split-Path -Parent $tool
$script=Join-Path $game 'data\scripts\MyMod\MyScript01.lua'
$mark='-- CROWN_ALBION_MANAGED'
Write-Host "Game folder: $game"
if(-not (Test-Path -LiteralPath (Join-Path $game 'Fable3.exe'))){
 Write-Host 'Tool does not appear to be directly inside Fable 3 game directory.'
 Write-Host 'Move the whole tool folder next to Fable3.exe and retry.'
 exit 2
}
if(-not (Test-Path -LiteralPath $script)){
 Write-Host 'No MyScript01.lua found. Nothing to clear.'
 exit 0
}
$content=[System.IO.File]::ReadAllText($script)
if(-not $content.TrimStart().StartsWith($mark)){
 Write-Warning 'This script is not marked as CROWN managed. Refusing to erase another mod.'
 exit 3
}
$backups=Join-Path $tool 'Backups'
[System.IO.Directory]::CreateDirectory($backups)|Out-Null
$backup=Join-Path $backups ('emergency-'+(Get-Date -Format 'yyyyMMdd-HHmmss-fff')+'.lua')
[System.IO.File]::Copy($script,$backup)
if((Get-FileHash -LiteralPath $script -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $backup -Algorithm SHA256).Hash){throw 'Backup verification failed. No changes made.'}
[System.IO.File]::WriteAllText($script,"-- CROWN_ALBION_MANAGED`r`n-- EMERGENCY STOP`r`n",(New-Object System.Text.UTF8Encoding($false)))
Write-Host 'CROWN script cleared. Current in-game effects are NOT reversed.'
Write-Host "Backup at: $backup"
