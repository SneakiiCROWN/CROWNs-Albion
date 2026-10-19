<#
  CROWN'S ALBION build script.
  Compiles every src\*.cs file with the stock .NET Framework compiler and
  stages a runnable folder under "Exe, Menu".
#>
[CmdletBinding()]
param(
  [string]$Configuration = 'Release',
  [switch]$NoStage
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $root 'src'
$dataDir = Join-Path $root 'data'
$outDir = Join-Path $root 'Exe, Menu'
$exe = Join-Path $outDir 'CROWNsAlbion.exe'

Write-Host "CROWN'S ALBION build" -ForegroundColor Yellow
Write-Host "  root: $root"

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
if (-not (Test-Path $csc)) { throw 'A .NET Framework 4.x C# compiler (csc.exe) was not found.' }

$sources = Get-ChildItem -Path $srcDir -Filter *.cs -File | Sort-Object Name | Select-Object -ExpandProperty FullName
if (-not $sources) { throw "No C# sources found in $srcDir" }
Write-Host "  sources: $($sources.Count) file(s)"

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$args = @(
  '/nologo', '/target:winexe', '/optimize+', '/warnaserror-', '/utf8output',
  '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Core.dll',
  '/r:System.Web.Extensions.dll',
  "/out:$exe"
) + $sources

if ($Configuration -eq 'Debug') { $args = $args | Where-Object { $_ -ne '/optimize+' } }

& $csc @args
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

if (-not $NoStage) {
  foreach ($name in @('catalog.json', 'references.json', 'unlocker_commands.json')) {
    $from = Join-Path $dataDir $name
    if (Test-Path $from) { Copy-Item $from $outDir -Force }
  }
}

$info = Get-Item $exe
Write-Host ("  built: {0} ({1:N0} bytes)" -f $info.FullName, $info.Length) -ForegroundColor Green
Write-Host '  Build OK.' -ForegroundColor Green
