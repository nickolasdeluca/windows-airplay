[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$manifest = Get-Content -Raw (Join-Path $repo 'runtime/manifest.json') | ConvertFrom-Json
$cache = Join-Path $repo '.cache'
$target = Join-Path $repo '.tools/dotnet'
New-Item -ItemType Directory -Force $cache,$target | Out-Null
$zip = Join-Path $cache 'dotnet-sdk.zip'
if (!(Test-Path -LiteralPath $zip)) {
    & rtk proxy curl.exe --fail --location --silent --show-error $manifest.sdk.url --output $zip
    if ($LASTEXITCODE -ne 0) { throw 'SDK download failed.' }
}
if ((Get-FileHash -Algorithm SHA512 -LiteralPath $zip).Hash -ne $manifest.sdk.hash) { throw 'SDK checksum mismatch. Remove the cached SDK archive and retry.' }
& rtk proxy tar.exe -xf $zip -C $target
if ($LASTEXITCODE -ne 0) { throw 'SDK extraction failed.' }
$actual = & (Join-Path $target 'dotnet.exe') --version
if ($actual.Trim() -ne $manifest.sdk.version) { throw 'Unexpected SDK version.' }
Write-Output "SDK $actual ready in $target"
