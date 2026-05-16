<#
.SYNOPSIS
    Copies the MonitorMonitor source tree to a GitHub staging folder, excluding
    build outputs and editor caches.

    Default mode: copy + update only. Existing files at the destination that
    aren't in the source (e.g. a hand-edited .gitignore or README) are left
    alone, but listed as a warning so you can see drift.

    With -Mirror: also deletes destination files not in the source, so the
    result is a 1:1 reflection. Files matching $preserveAtDest are kept anyway.

.PARAMETER Destination
    Where to mirror to. Defaults to your standard pipefl GitHub mirror.

.PARAMETER Mirror
    Delete destination files that are not in the source (excluding the
    preserve list). Off by default.

.PARAMETER DryRun
    Show what would change without copying or deleting anything.
#>

[CmdletBinding()]
param(
    [string]$Destination = 'C:\Users\Jar\Dropbox\pipefl\_github\MonitorMonitor',
    [switch]$Mirror,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

$source = $PSScriptRoot
if (-not (Test-Path -LiteralPath $source)) { throw "Source not found: $source" }

# Directories anywhere in the tree to skip (source-side and destination-side)
$excludeDirs  = @('bin', 'obj', 'staging', 'dist', '.vs', '.idea', '.cache', '.git', 'TestResults')

# File patterns to skip on the source side
$excludeFiles = @('*.user', '*.suo', '*.lscache', '*.cache', 'desktop.ini', 'Thumbs.db')

# Files at the destination that should NEVER be deleted, even with -Mirror,
# because they typically belong to the repo (.gitignore, README, license)
# rather than to the source project itself.
$preserveAtDest = @('.gitignore', 'README.md', 'LICENSE', 'LICENSE.md', 'LICENSE.txt')

function Test-Excluded {
    param([string]$RelativePath)

    $parts = $RelativePath -split '[\\/]'
    foreach ($dir in $excludeDirs) {
        if ($parts -contains $dir) { return $true }
    }
    $fileName = Split-Path -Leaf $RelativePath
    foreach ($pattern in $excludeFiles) {
        if ($fileName -like $pattern) { return $true }
    }
    return $false
}

function Test-Preserved {
    param([string]$RelativePath)

    $fileName = Split-Path -Leaf $RelativePath
    foreach ($p in $preserveAtDest) {
        if ($fileName -eq $p) { return $true }
    }
    return $false
}

# --- Build the set of files to keep, with relative paths --------------------
$keepList = @()
foreach ($f in Get-ChildItem -LiteralPath $source -Recurse -File) {
    $rel = $f.FullName.Substring($source.Length).TrimStart('\', '/')
    if (-not (Test-Excluded $rel)) {
        $info = New-Object psobject
        $info | Add-Member -MemberType NoteProperty -Name FullName      -Value $f.FullName
        $info | Add-Member -MemberType NoteProperty -Name Relative      -Value $rel
        $info | Add-Member -MemberType NoteProperty -Name Length        -Value $f.Length
        $info | Add-Member -MemberType NoteProperty -Name LastWriteTime -Value $f.LastWriteTime
        $keepList += $info
    }
}

Write-Host "==> Source files to mirror: $($keepList.Count)" -ForegroundColor Cyan
if ($Mirror) {
    Write-Host '==> Mirror mode: stale destination files will be deleted (preserve list excepted).' -ForegroundColor Yellow
}

# --- Ensure destination exists ----------------------------------------------
if (-not (Test-Path -LiteralPath $Destination)) {
    if ($DryRun) {
        Write-Host "[DRY] Would create destination: $Destination" -ForegroundColor Yellow
    } else {
        New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    }
}

# Index keep paths for fast lookup
$keepSet = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
foreach ($k in $keepList) { [void]$keepSet.Add($k.Relative) }

# --- Identify destination extras (files not in source) ----------------------
$extras = @()
if (Test-Path -LiteralPath $Destination) {
    foreach ($f in Get-ChildItem -LiteralPath $Destination -Recurse -File -ErrorAction SilentlyContinue) {
        $rel = $f.FullName.Substring($Destination.Length).TrimStart('\', '/')
        if (-not $keepSet.Contains($rel)) {
            $extras += $rel
        }
    }
}

# --- Delete extras (only with -Mirror, respecting preserve list) ------------
$deletedCount = 0
$preservedCount = 0
foreach ($rel in $extras) {
    if (Test-Preserved $rel) {
        $preservedCount++
        if ($Mirror) {
            Write-Host "    Preserved: $rel" -ForegroundColor DarkGreen
        }
        continue
    }

    if ($Mirror) {
        $full = Join-Path $Destination $rel
        if ($DryRun) {
            Write-Host "[DRY] Delete: $rel" -ForegroundColor Magenta
        } else {
            Remove-Item -LiteralPath $full -Force
        }
        $deletedCount++
    }
}

# --- Copy / update files ----------------------------------------------------
$newCount = 0
$updatedCount = 0
$unchangedCount = 0
foreach ($k in $keepList) {
    $destFile = Join-Path $Destination $k.Relative
    $destDir  = Split-Path -Parent $destFile

    $exists = Test-Path -LiteralPath $destFile
    if (-not $exists) {
        $action = 'New'
        $newCount++
    } else {
        $existing = Get-Item -LiteralPath $destFile
        if ($existing.Length -ne $k.Length -or $existing.LastWriteTime -ne $k.LastWriteTime) {
            $action = 'Update'
            $updatedCount++
        } else {
            $action = 'Skip'
            $unchangedCount++
        }
    }

    if ($action -ne 'Skip') {
        if ($DryRun) {
            Write-Host "[DRY] $action`: $($k.Relative)" -ForegroundColor DarkCyan
        } else {
            if (-not (Test-Path -LiteralPath $destDir)) {
                New-Item -ItemType Directory -Path $destDir -Force | Out-Null
            }
            Copy-Item -LiteralPath $k.FullName -Destination $destFile -Force
        }
    }
}

# --- Prune empty directories in destination ---------------------------------
$prunedCount = 0
if ($Mirror -and -not $DryRun -and (Test-Path -LiteralPath $Destination)) {
    $dirs = Get-ChildItem -LiteralPath $Destination -Recurse -Directory | Sort-Object FullName -Descending
    foreach ($d in $dirs) {
        if (-not (Get-ChildItem -LiteralPath $d.FullName -Force)) {
            Remove-Item -LiteralPath $d.FullName -Force
            $prunedCount++
        }
    }
}

# --- Notify about drift in copy-only mode -----------------------------------
if (-not $Mirror -and $extras.Count -gt 0) {
    $deletableExtras = $extras | Where-Object { -not (Test-Preserved $_) }
    if ($deletableExtras.Count -gt 0) {
        Write-Host ''
        Write-Host "==> $($deletableExtras.Count) extra file(s) at destination not present in source:" -ForegroundColor Yellow
        $deletableExtras | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkYellow }
        Write-Host '    Re-run with -Mirror to delete them.' -ForegroundColor Yellow
    }
}

# --- Summary ----------------------------------------------------------------
$summary = New-Object psobject
$summary | Add-Member -MemberType NoteProperty -Name Source          -Value $source
$summary | Add-Member -MemberType NoteProperty -Name Destination     -Value $Destination
$summary | Add-Member -MemberType NoteProperty -Name Mode            -Value $(if ($Mirror) { 'Mirror' } else { 'Copy-only' })
$summary | Add-Member -MemberType NoteProperty -Name New             -Value $newCount
$summary | Add-Member -MemberType NoteProperty -Name Updated         -Value $updatedCount
$summary | Add-Member -MemberType NoteProperty -Name Unchanged       -Value $unchangedCount
$summary | Add-Member -MemberType NoteProperty -Name Deleted         -Value $deletedCount
$summary | Add-Member -MemberType NoteProperty -Name Preserved       -Value $preservedCount
$summary | Add-Member -MemberType NoteProperty -Name EmptyDirsPruned -Value $prunedCount

Write-Host ''
if ($DryRun) {
    Write-Host '==> Dry run complete. Re-run without -DryRun to apply.' -ForegroundColor Yellow
} else {
    Write-Host '==> Done.' -ForegroundColor Green
}
$summary | Format-List
