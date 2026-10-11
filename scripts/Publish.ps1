[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
if (!$OutputDirectory) { $OutputDirectory = Join-Path $repo ('artifacts/publish/windows-airplay-local-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$output = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $output) { throw 'Choose a new output directory; publishing does not overwrite an existing package.' }
$sdk = Join-Path $repo '.tools/dotnet/dotnet.exe'
$runtime = Join-Path $repo 'artifacts/receiver'
$manifest = Get-Content -Raw (Join-Path $repo 'runtime/manifest.json') | ConvertFrom-Json
$source = Join-Path $repo '.cache/uxplay-source.tar.gz'
if (!(Test-Path -LiteralPath $source) -or (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash -ne $manifest.uxplay.sha256) { throw 'Verified UxPlay source archive is required.' }
$licenses = Join-Path $repo '.tools/msys64/ucrt64/share/licenses'
if (!(Test-Path -LiteralPath $licenses)) { throw 'Native dependency license inventory is missing.' }
Push-Location $repo
try {
    # A separate output keeps the active probe untouched during local preview builds.
    $validator = Join-Path $repo '.cache/package-validator'
    & rtk proxy $sdk build src/WindowsAirplay.Probe/WindowsAirplay.Probe.csproj -c Release --output $validator --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Runtime validator build failed.' }
    & rtk proxy $sdk (Join-Path $validator 'WindowsAirplay.Probe.dll') --validate-runtime $runtime
    if ($LASTEXITCODE -ne 0) { throw 'Runtime layout validation failed.' }
    & rtk proxy powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReceiverRuntime.ps1 -RuntimePath $runtime -SkipServiceCheck
    if ($LASTEXITCODE -ne 0) { throw 'Native runtime loading check failed.' }
    & rtk proxy $sdk publish src/WindowsAirplay.App/WindowsAirplay.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false --output $output --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Desktop app publishing failed.' }
    Copy-Item -LiteralPath $runtime -Destination (Join-Path $output 'receiver') -Recurse
    $provenance = Join-Path $output 'provenance'
    New-Item -ItemType Directory -Path $provenance | Out-Null
    Copy-Item -LiteralPath $licenses -Destination (Join-Path $provenance 'native-licenses') -Recurse
    Copy-Item -LiteralPath $source -Destination $provenance
    Copy-Item -LiteralPath (Join-Path $repo '.cache/dns_sd.h') -Destination $provenance
    foreach ($directory in @('patches','native','scripts','runtime','docs')) { Copy-Item -LiteralPath (Join-Path $repo $directory) -Destination $provenance -Recurse }
    foreach ($name in @('README.md','THIRD-PARTY-NOTICES.md')) { Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $output }
    Copy-Item -LiteralPath (Join-Path $repo 'docs/local-preview.md') -Destination (Join-Path $output 'START-HERE.md')
    & rtk proxy $sdk (Join-Path $validator 'WindowsAirplay.Probe.dll') --validate-runtime (Join-Path $output 'receiver')
    if ($LASTEXITCODE -ne 0) { throw 'Copied runtime failed validation.' }
    & rtk proxy powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReceiverRuntime.ps1 -RuntimePath (Join-Path $output 'receiver') -SkipServiceCheck
    if ($LASTEXITCODE -ne 0) { throw 'Copied runtime failed its native loading test.' }
    $hashes = Get-ChildItem -LiteralPath $output -Recurse -File | ForEach-Object {
        [pscustomobject]@{ Path=$_.FullName.Substring($output.Length+1); SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    }
    $hashes | ConvertTo-Json -Depth 3 | Set-Content -Encoding UTF8 (Join-Path $output 'SHA256SUMS.json')
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [IO.Compression.ZipFile]::CreateFromDirectory($output,($output + '.zip'),[IO.Compression.CompressionLevel]::Optimal,$true)
    Write-Output "LOCAL PREVIEW: $output"
    Write-Output "ZIP: $output.zip"
    Write-Output 'External distribution remains pending dependency/source compliance and project licensing review.'
} finally { Pop-Location }
