# Windows AirPlay

Use a Windows-connected monitor as an extended display for an Apple Silicon Mac over the LAN.

**Status: compatibility probe, not yet a finished desktop app.** Built locally for Windows x64. The first acceptance device is an M1 MacBook Pro running macOS 27.0.1. AirPlay interoperability with that device has not yet been tested. The native D3D11 video placement test passed on the host's left 1920×1080 monitor.

## Try the probe

Run commands from this repository in PowerShell. RTK is required by this workspace's command convention.

1. Install Apple's [Bonjour service](https://support.apple.com/106380) on Windows. The probe requires the service to be running. The full Bonjour SDK is not required by this build. `scripts/Install-Bonjour.ps1` downloads the pinned Apple installer, verifies its hash and Apple signature, and installs only the service after a Windows elevation prompt. Bonjour is already installed on this host.
2. Prepare the local tools and runtime (already built in this workspace):

   ~~~powershell
   rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Prepare-Toolchain.ps1
   rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Prepare-Receiver.ps1
   rtk proxy .tools/dotnet/dotnet.exe build src/WindowsAirplay.Probe/WindowsAirplay.Probe.csproj -c Release
   ~~~

3. List monitors or show the short local test pattern:

   ~~~powershell
   rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Probe.ps1 -ListMonitors
   rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Probe.ps1 -Monitor 1 -TestPattern
   ~~~

4. If required, open an **administrator PowerShell** and add the scoped firewall rules. Substitute the actual Bonjour executable location if it differs:

   ~~~powershell
   rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Configure-Firewall.ps1 -ReceiverPath "$PWD/artifacts/receiver/uxplay.exe" -BonjourPath 'C:/Program Files (x86)/Bonjour/mDNSResponder.exe'
   ~~~

   Rules allow only the named executables, Private network profile and LocalSubnet. Use -WhatIf to preview. Use the same command with -Remove to remove only these project-owned rules. The script does not change your network profile.

5. Start the receiver on the selected monitor:

   ~~~powershell
   rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Start-Probe.ps1 -Monitor 1
   ~~~

6. On the Mac, choose **Windows AirPlay** in AirPlay / Screen Mirroring. Enter the code shown in the probe terminal. Choose **Extended Display** in the Mac's display controls, and move an independent Mac window onto it.

Alt+Tab to the probe terminal and press Ctrl+C to stop. Closing the terminal should also terminate the owned receiver through its Windows job. The test pattern is only a renderer test; it does not prove AirPlay compatibility.

Monitor numbers reflect the current enumeration. The probe also accepts the stable ID printed by -ListMonitors. A removed monitor stops the session instead of falling back to another display.

The Mac keyboard/trackpad controls the extended desktop. Requested output is 1920×1080 at 60 Hz; actual negotiated resolution, throughput and latency require a Mac test. Audio is disabled in this first display-focused probe.

## Checks

~~~powershell
rtk proxy .tools/dotnet/dotnet.exe test tests/WindowsAirplay.Tests/WindowsAirplay.Tests.csproj -c Release
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/Runtime.Tests.ps1
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File tests/scripts/NativeUnicode.Tests.ps1
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Test-ReceiverRuntime.ps1
~~~

See [development notes](docs/development.md), [compatibility results](docs/compatibility/m1-macos-27.0.1.md), and the [implementation plan](docs/superpowers/plans/2026-10-11-windows-airplay.md).

The WPF controller, saved settings, tray menu and distributable packaging follow the real Mac compatibility gate. This repository currently provides a local build; it is not an externally published release.
