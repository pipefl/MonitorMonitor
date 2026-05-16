<#
.SYNOPSIS
    Publishes mmcli + mmtray as Native AOT, stages them, and compiles the Inno
    Setup installer into ./dist.

.NOTES
    Sets the MSVC + Windows SDK environment manually because .NET 9.0.6's AOT
    probe (findvcvarsall.bat) doesn't recognize Visual Studio 18 yet.
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$installerDir = $PSScriptRoot
$repoRoot     = Split-Path -Parent $installerDir
$stagingDir   = Join-Path $installerDir 'staging'
$distDir      = Join-Path $installerDir 'dist'

$mmcliProj = Join-Path $repoRoot 'mmcli\mmcli.csproj'
$mmtrayProj = Join-Path $repoRoot 'mmtray\mmtray.csproj'

# --- MSVC environment for AOT linking ---------------------------------------
$vc     = 'C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC\14.50.35717'
$sdkVer = '10.0.26100.0'
$sdkInc = "C:\Program Files (x86)\Windows Kits\10\Include\$sdkVer"
$sdkLib = "C:\Program Files (x86)\Windows Kits\10\Lib\$sdkVer"

if (-not (Test-Path $vc))     { throw "MSVC tools not found at $vc" }
if (-not (Test-Path $sdkInc)) { throw "Windows SDK not found at $sdkInc" }

$env:INCLUDE = "$vc\include;$sdkInc\ucrt;$sdkInc\um;$sdkInc\shared;$sdkInc\winrt"
$env:LIB     = "$vc\lib\x64;$sdkLib\ucrt\x64;$sdkLib\um\x64"
$env:PATH    = "$vc\bin\Hostx64\x64;$env:PATH"

# --- Stop running instances so we can overwrite the binaries ---------------
$running = Get-Process -Name 'mmcli', 'mmtray' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host '==> Stopping running instances:' -ForegroundColor Yellow
    $running | ForEach-Object {
        Write-Host "    $($_.ProcessName) (PID $($_.Id))"
        $_ | Stop-Process -Force
    }
}

# --- Publish both binaries --------------------------------------------------
Write-Host '==> Publishing mmcli (Native AOT)' -ForegroundColor Cyan
dotnet publish $mmcliProj -c $Configuration -p:IlcUseEnvironmentalTools=true
if ($LASTEXITCODE -ne 0) { throw 'mmcli publish failed' }

Write-Host '==> Publishing mmtray (Native AOT)' -ForegroundColor Cyan
dotnet publish $mmtrayProj -c $Configuration -p:IlcUseEnvironmentalTools=true
if ($LASTEXITCODE -ne 0) { throw 'mmtray publish failed' }

# --- Stage ------------------------------------------------------------------
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $stagingDir | Out-Null

$mmcliExe  = Join-Path $repoRoot "mmcli\bin\$Configuration\net9.0\win-x64\publish\mmcli.exe"
$mmtrayExe = Join-Path $repoRoot "mmtray\bin\$Configuration\net9.0-windows\win-x64\publish\mmtray.exe"

if (-not (Test-Path $mmcliExe))  { throw "mmcli.exe not found at $mmcliExe" }
if (-not (Test-Path $mmtrayExe)) { throw "mmtray.exe not found at $mmtrayExe" }

Copy-Item $mmcliExe  $stagingDir
Copy-Item $mmtrayExe $stagingDir

Write-Host "==> Staged:" -ForegroundColor Cyan
Get-ChildItem $stagingDir | Format-Table Name, Length -AutoSize

# --- Locate ISCC ------------------------------------------------------------
$isccCandidates = @(
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'ISCC.exe not found. Install Inno Setup 6 first (winget install JRSoftware.InnoSetup).' }

# --- Compile installer ------------------------------------------------------
Write-Host '==> Compiling installer with ISCC' -ForegroundColor Cyan
& $iscc /Q (Join-Path $installerDir 'MonitorMonitor.iss')
if ($LASTEXITCODE -ne 0) { throw 'ISCC compile failed' }

Write-Host ''
Write-Host '==> Done. Output:' -ForegroundColor Green
Get-ChildItem $distDir | Format-Table Name, Length, LastWriteTime -AutoSize
