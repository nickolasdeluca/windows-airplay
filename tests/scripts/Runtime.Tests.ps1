$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$empty = Join-Path $repo 'artifacts/empty-runtime-test'
New-Item -ItemType Directory -Force $empty | Out-Null
$ErrorActionPreference = 'Continue'
$result = & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $repo 'scripts/Test-ReceiverRuntime.ps1') -RuntimePath $empty 2>&1
$code = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($code -eq 0) { throw 'Missing receiver was incorrectly accepted.' }
if (($result -join "\n") -notmatch 'uxplay.exe') { throw 'Missing executable did not produce an actionable diagnostic.' }
Write-Output 'PASS: missing receiver fails with an actionable diagnostic.'
