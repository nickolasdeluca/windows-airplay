# Windows AirPlay

Use a Windows-connected monitor as an extended display for an Apple Silicon Mac over the LAN.

**Status: working extended-display receiver with a local WPF app preview.** The user confirmed independent extended desktop on an M1 MacBook Pro running macOS 27.0.1. Native D3D11 placement passed on the host's left 1920×1080 monitor. The new controller builds and opens; connected UI and tray acceptance remain pending.

## Windows app

Build and open the controller:

~~~powershell
rtk proxy .tools/dotnet/dotnet.exe build src/WindowsAirplay.App/WindowsAirplay.App.csproj -c Release
rtk proxy src/WindowsAirplay.App/bin/Release/net10.0-windows/WindowsAirplay.App.exe
~~~

Select a monitor, choose Start receiving, and enter the password displayed in the app on your Mac. The controller saves the receiver name and stable monitor identity. Closing it while receiving keeps the tray available; tray Stop and Exit end the session. A missing saved monitor requires reselection.

Stop the compatibility probe before starting the app: they use the same ports. Development builds reuse artifacts/receiver, so existing host firewall rules apply. A packaged preview has its own receiver path and requires rules for that path; see [local preview instructions](docs/local-preview.md).

Create a self-contained local preview and ZIP with:

~~~powershell
rtk proxy powershell -NoProfile -ExecutionPolicy Bypass -File scripts/Publish.ps1
~~~

The script uses a new output folder each time, validates native architecture and plugin loading, and includes native license texts and source provenance. External distribution is pending complete dependency compliance and a project license decision.

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

The WPF controller, saved settings, tray menu and self-contained local packaging are implemented. Automated settings, controller, process ownership, display policy and packaging checks pass. UI automation was unavailable on this host; manual connected-controller, tray, monitor-unplug, mixed-DPI, repeated reconnect and clean-machine checks remain outstanding. Nothing is externally published.
