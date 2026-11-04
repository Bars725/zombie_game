$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$incomingRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot '_incoming_art'))
$projectAssets = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'Zombie_game/Assets/_Project'))
$archiveRoot = [System.IO.Path]::GetFullPath((Join-Path $incomingRoot '_archive'))
$incomingPrefix = $incomingRoot.TrimEnd('\') + '\'

if (-not (Test-Path -LiteralPath $incomingRoot -PathType Container)) {
    Write-Error "Incoming art folder not found: $incomingRoot"
    exit 1
}
if (-not (Test-Path -LiteralPath $projectAssets -PathType Container)) {
    Write-Error "Unity project assets folder not found: $projectAssets"
    exit 1
}
if (Test-Path -LiteralPath $archiveRoot) {
    $archiveRootItem = Get-Item -LiteralPath $archiveRoot -Force
    if (($archiveRootItem.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        Write-Error "Refusing to use an archive folder that is a reparse point: $archiveRoot"
        exit 1
    }
}

$stamp = Get-Date -Format 'yyyyMMdd_HHmmss_fff'
$archiveSession = Join-Path $archiveRoot $stamp
$archived = 0
$skipped = 0
$failed = 0

Write-Host "Scanning incoming resources against $projectAssets"

$incomingFiles = @(Get-ChildItem -LiteralPath $incomingRoot -File -Recurse | Where-Object {
    -not $_.FullName.StartsWith(($archiveRoot.TrimEnd('\') + '\'), [System.StringComparison]::OrdinalIgnoreCase)
})

$incomingFiles | ForEach-Object {
    $source = $_
    if (($source.Attributes -band [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        Write-Host "SKIP (reparse point): $($source.FullName)"
        $skipped++
        return
    }
    $sourceFull = [System.IO.Path]::GetFullPath($source.FullName)
    if (-not $sourceFull.StartsWith($incomingPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "ERROR: Refusing to process a path outside incoming art: $sourceFull"
        $failed++
        return
    }
    $sourceHash = (Get-FileHash -LiteralPath $sourceFull -Algorithm SHA256).Hash
    $usedAt = $null
    foreach ($candidate in (Get-ChildItem -LiteralPath $projectAssets -Filter $source.Name -File -Recurse -ErrorAction SilentlyContinue)) {
        if ((Get-FileHash -LiteralPath $candidate.FullName -Algorithm SHA256).Hash -eq $sourceHash) {
            $usedAt = $candidate.FullName
            break
        }
    }

    if ($null -eq $usedAt) {
        Write-Host "SKIP (no identical imported asset): $($source.FullName)"
        $skipped++
        return
    }

    $relativePath = $sourceFull.Substring($incomingPrefix.Length)
    $archiveFile = [System.IO.Path]::GetFullPath((Join-Path $archiveSession $relativePath))
    if (-not $archiveFile.StartsWith(($archiveSession.TrimEnd('\') + '\'), [System.StringComparison]::OrdinalIgnoreCase)) {
        Write-Host "ERROR: Refusing to write outside archive folder: $archiveFile"
        $failed++
        return
    }

    try {
        $null = New-Item -ItemType Directory -Path (Split-Path -Parent $archiveFile) -Force
        Copy-Item -LiteralPath $sourceFull -Destination $archiveFile
        $archiveHash = (Get-FileHash -LiteralPath $archiveFile -Algorithm SHA256).Hash
        if ($archiveHash -ne $sourceHash) {
            Write-Host "ERROR: Archive copy hash mismatch; original kept: $sourceFull"
            $failed++
            return
        }

        $manifestLine = "{0}`t{1}`t{2}" -f $sourceHash, $relativePath, $usedAt
        Add-Content -LiteralPath (Join-Path $archiveSession 'manifest.tsv') -Value $manifestLine -Encoding UTF8
        Remove-Item -LiteralPath $sourceFull
        Write-Host "ARCHIVED: $relativePath (matched $usedAt)"
        $archived++
    }
    catch {
        Write-Host "ERROR: Failed to archive $sourceFull : $_"
        $failed++
    }
}

Write-Host "Finished. Archived: $archived; skipped: $skipped; failures: $failed."
if ($archived -gt 0) { Write-Host "Archive: $archiveSession" }
if ($failed -gt 0) { exit 1 }
exit 0
