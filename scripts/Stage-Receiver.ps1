[CmdletBinding()]
param([string]$Destination)
$ErrorActionPreference = 'Stop'
if (!$Destination) { $Destination = Join-Path $PSScriptRoot '../artifacts/receiver' }
$repo = Split-Path $PSScriptRoot -Parent
$prefix = Join-Path $repo '.tools/msys64/ucrt64'
New-Item -ItemType Directory -Force $Destination,(Join-Path $Destination 'plugins') | Out-Null
Copy-Item -LiteralPath (Join-Path $repo '.cache/receiver-build/uxplay.exe') -Destination $Destination
Get-ChildItem -LiteralPath (Join-Path $prefix 'bin') -Filter '*.dll' | Copy-Item -Destination $Destination
Copy-Item -LiteralPath (Join-Path $prefix 'bin/gst-inspect-1.0.exe') -Destination $Destination
Copy-Item -LiteralPath (Join-Path $prefix 'bin/gst-launch-1.0.exe') -Destination $Destination
Copy-Item -LiteralPath (Join-Path $prefix 'libexec/gstreamer-1.0/gst-plugin-scanner.exe') -Destination $Destination
Get-ChildItem -LiteralPath (Join-Path $prefix 'lib/gstreamer-1.0') -Filter '*.dll' | Copy-Item -Destination (Join-Path $Destination 'plugins')
Copy-Item -LiteralPath (Join-Path $repo 'runtime/manifest.json') -Destination $Destination
$packages = & (Join-Path $repo '.tools/msys64/usr/bin/pacman.exe') -Q
if ($LASTEXITCODE -ne 0) { throw 'Could not record native dependency versions.' }
$packages | Set-Content -Encoding UTF8 (Join-Path $Destination 'packages.txt')
Copy-Item -LiteralPath (Join-Path $Destination 'packages.txt') -Destination (Join-Path $repo 'runtime/packages.txt')
Write-Output "Receiver staged in $Destination. Bonjour is a separate host prerequisite."
