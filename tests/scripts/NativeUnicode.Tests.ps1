param([string]$RuntimePath)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (!$RuntimePath) { $RuntimePath = Join-Path $repo 'artifacts/receiver' }
$name = 'escrit' + [char]0x00f3 + 'rio'
$ErrorActionPreference = 'Continue'
$output = & (Join-Path $RuntimePath 'uxplay.exe') -n $name -h 2>&1
$code = $LASTEXITCODE
$ErrorActionPreference = 'Stop'
if ($code -ne 0 -or ($output -join "\n") -notmatch 'UxPlay 1.73.7' -or ($output -join "\n") -match 'Error: detected') { throw 'Native UxPlay rejected a Unicode receiver name.' }
Write-Output 'PASS: native receiver accepts Unicode command-line arguments.'
