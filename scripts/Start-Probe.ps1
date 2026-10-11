[CmdletBinding()]
param([string]$Monitor = '1',[switch]$ListMonitors,[switch]$TestPattern)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$probe = Join-Path $repo 'src/WindowsAirplay.Probe/bin/Release/net10.0-windows/WindowsAirplay.Probe.exe'
if (!(Test-Path -LiteralPath $probe)) { throw 'Build the probe first; see README.md.' }
if ($ListMonitors) { & $probe --list-monitors; exit $LASTEXITCODE }
$runtime = Join-Path $repo 'artifacts/receiver'
if (!$TestPattern) {
    & powershell.exe -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot 'Test-ReceiverRuntime.ps1') -RuntimePath $runtime
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
$probeArgs = @('--monitor',$Monitor,'--runtime',$runtime)
if ($TestPattern) { $probeArgs += '--test-pattern' }
& $probe @probeArgs
exit $LASTEXITCODE
