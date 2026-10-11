[CmdletBinding()]
param([string]$Destination)
$ErrorActionPreference = 'Stop'
if (!$Destination) { $Destination = Join-Path $PSScriptRoot '../artifacts/receiver' }
$repo = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content -Raw (Join-Path $repo 'runtime/manifest.json') | ConvertFrom-Json
$cache = Join-Path $repo '.cache'
$tools = Join-Path $repo '.tools'
New-Item -ItemType Directory -Force $cache,$tools,$Destination | Out-Null
function Get-VerifiedFile([string]$Url, [string]$Path, [string]$Hash) {
    if (!(Test-Path -LiteralPath $Path)) {
        & rtk proxy curl.exe --fail --location --silent --show-error $Url --output $Path
        if ($LASTEXITCODE -ne 0) { throw "Download failed: $Url" }
    }
    if ((Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash -ne $Hash) { throw "Checksum mismatch: $Path" }
}
Get-VerifiedFile $manifest.uxplay.url (Join-Path $cache 'uxplay-source.tar.gz') $manifest.uxplay.sha256
Get-VerifiedFile $manifest.msys2.url (Join-Path $cache 'msys2-base.tar.xz') $manifest.msys2.sha256
Get-VerifiedFile $manifest.bonjour.headerUrl (Join-Path $cache 'dns_sd.h') $manifest.bonjour.headerSha256
$bash = Join-Path $tools 'msys64/usr/bin/bash.exe'
if (!(Test-Path -LiteralPath $bash)) {
    & rtk proxy tar.exe -xf (Join-Path $cache 'msys2-base.tar.xz') -C $tools
    if ($LASTEXITCODE -ne 0) { throw 'MSYS2 extraction failed.' }
}
$sourceBase = Join-Path $cache 'source'
New-Item -ItemType Directory -Force $sourceBase | Out-Null
& rtk proxy tar.exe -xf (Join-Path $cache 'uxplay-source.tar.gz') -C $sourceBase
if ($LASTEXITCODE -ne 0) { throw 'UxPlay extraction failed.' }
Push-Location $repo
try {
    $sourceDir = '.cache/source/UxPlay-' + $manifest.uxplay.commit
    & rtk git apply --directory=$sourceDir patches/uxplay-windows-dynamic-dnssd.patch
    if ($LASTEXITCODE -ne 0) { throw 'Pinned UxPlay build patch failed.' }
    & rtk git apply --directory=$sourceDir patches/uxplay-readiness.patch
    if ($LASTEXITCODE -ne 0) { throw 'Pinned readiness patch failed.' }
    & rtk git apply --directory=$sourceDir patches/uxplay-windows-manifest.patch
    if ($LASTEXITCODE -ne 0) { throw 'Native manifest patch failed.' }
    Copy-Item -LiteralPath (Join-Path $repo 'native/windows/uxplay.rc') -Destination (Join-Path $sourceDir 'windows-airplay.rc')
    Copy-Item -LiteralPath (Join-Path $repo 'native/windows/uxplay.manifest') -Destination $sourceDir
    & rtk proxy $bash -lc 'pacman -Syu --noconfirm --needed mingw-w64-ucrt-x86_64-gcc mingw-w64-ucrt-x86_64-cmake mingw-w64-ucrt-x86_64-ninja mingw-w64-ucrt-x86_64-libplist mingw-w64-ucrt-x86_64-openssl mingw-w64-ucrt-x86_64-gstreamer mingw-w64-ucrt-x86_64-gst-plugins-base mingw-w64-ucrt-x86_64-gst-plugins-good mingw-w64-ucrt-x86_64-gst-plugins-bad mingw-w64-ucrt-x86_64-gst-libav'
    if ($LASTEXITCODE -ne 0) { throw 'Native dependency installation failed; rerun after any required MSYS2 restart.' }
    & rtk proxy $bash scripts/configure-receiver.sh
    if ($LASTEXITCODE -ne 0) { throw 'Receiver build failed.' }
    & (Join-Path $PSScriptRoot 'Stage-Receiver.ps1') -Destination $Destination
} finally { Pop-Location }
