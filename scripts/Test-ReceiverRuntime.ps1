[CmdletBinding()]
param([string]$RuntimePath, [switch]$SkipServiceCheck)
$ErrorActionPreference = 'Stop'
if (!$RuntimePath) { $RuntimePath = Join-Path $PSScriptRoot '../artifacts/receiver' }
try {
    $runtime = [IO.Path]::GetFullPath($RuntimePath)
    $exe = Join-Path $runtime 'uxplay.exe'
    if (!(Test-Path -LiteralPath $exe -PathType Leaf)) { throw "Missing uxplay.exe in $runtime. Run scripts/Prepare-Receiver.ps1 first." }
    $bytes = [IO.File]::ReadAllBytes($exe)
    if ($bytes.Length -lt 64) { throw 'uxplay.exe is not a valid Windows executable.' }
    $pe = [BitConverter]::ToInt32($bytes, 60)
    if ($pe -lt 0 -or $pe + 6 -gt $bytes.Length -or [BitConverter]::ToUInt16($bytes, $pe + 4) -ne 0x8664) { throw 'uxplay.exe must be a Windows x64 executable.' }
    $inspect = Join-Path $runtime 'gst-inspect-1.0.exe'
    if (!(Test-Path -LiteralPath $inspect)) { throw 'Missing gst-inspect-1.0.exe. Rebuild the receiver payload.' }
    $env:PATH = "$runtime;$env:PATH"
    $env:GST_PLUGIN_SYSTEM_PATH_1_0 = Join-Path $runtime 'plugins'
    $env:GST_PLUGIN_PATH_1_0 = ''
    $env:GST_PLUGIN_SCANNER = Join-Path $runtime 'gst-plugin-scanner.exe'
    $env:GST_REGISTRY = Join-Path $runtime 'registry-check.bin'
    foreach ($plugin in @('h264parse','avdec_h264','d3d11videosink')) {
        & $inspect $plugin *> $null
        if ($LASTEXITCODE -ne 0) { throw "GStreamer plugin $plugin cannot load. Rebuild native dependencies." }
    }
    & $exe -h *> $null
    if ($LASTEXITCODE -ne 0) { throw 'uxplay.exe could not start. Check native DLLs and architecture.' }
    if (!$SkipServiceCheck) {
        $service = Get-Service -Name 'Bonjour Service' -ErrorAction SilentlyContinue
        if (!$service -or $service.Status -ne 'Running') { throw 'Bonjour Service is not running. Install Apple Bonjour, then rerun this check.' }
        $ip = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
        $busy = @($ip.GetActiveTcpListeners()) + @($ip.GetActiveUdpListeners()) | Where-Object { $_.Port -ge 35000 -and $_.Port -le 35002 }
        if ($busy) { throw 'Receiver ports 35000-35002 are occupied. Stop the other receiver before starting.' }
    }
    Write-Output 'PASS: receiver executable and required GStreamer plugins load.'
    if ($SkipServiceCheck) { Write-Output 'Bonjour and network readiness NOT CHECKED.' }
    exit 0
} catch {
    [Console]::Error.WriteLine($_.Exception.Message)
    exit 1
}
