[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$service = Get-Service -Name 'Bonjour Service' -ErrorAction SilentlyContinue
if ($service -and $service.Status -eq 'Running') { Write-Output 'Bonjour is already running.'; exit 0 }
$manifest = Get-Content -Raw (Join-Path $repo 'runtime/manifest.json') | ConvertFrom-Json
$archive = Join-Path $repo '.cache/BonjourPSSetup.exe'
$destination = Join-Path $repo '.cache/bonjour'
New-Item -ItemType Directory -Force $destination | Out-Null
if (!(Test-Path -LiteralPath $archive)) {
    & rtk proxy curl.exe --fail --location --silent --show-error $manifest.bonjour.installerUrl --output $archive
    if ($LASTEXITCODE -ne 0) { throw 'Apple Bonjour download failed.' }
}
if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $manifest.bonjour.installerSha256) { throw 'Bonjour installer checksum mismatch.' }
$signature = Get-AuthenticodeSignature -LiteralPath $archive
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Apple Inc\.') { throw 'Bonjour installer must have a valid Apple signature.' }
& rtk proxy tar.exe -xf $archive -C $destination Bonjour64.msi
if ($LASTEXITCODE -ne 0) { throw 'Bonjour MSI extraction failed.' }
$msi = Join-Path $destination 'Bonjour64.msi'
$signature = Get-AuthenticodeSignature -LiteralPath $msi
if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Apple Inc\.') { throw 'Bonjour service MSI must have a valid Apple signature.' }
$log = Join-Path $repo '.cache/bonjour-install.log'
$installerArguments = @('/i',('"' + $msi + '"'),'/qn','/norestart','/L*v',('"' + $log + '"'))
$installer = Start-Process -FilePath 'msiexec.exe' -ArgumentList $installerArguments -Verb RunAs -WindowStyle Hidden -PassThru -Wait
if ($installer.ExitCode -notin @(0,3010)) { throw "Bonjour installation failed with exit code $($installer.ExitCode). See $log" }
$service = Get-Service -Name 'Bonjour Service' -ErrorAction Stop
if ($service.Status -ne 'Running') { throw 'Bonjour is installed but not running. Start Bonjour Service as administrator.' }
Write-Output 'PASS: Apple Bonjour Service is installed and running.'
