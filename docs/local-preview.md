# Windows AirPlay local preview

Run **WindowsAirplay.App.exe**, select a Windows monitor, then choose **Start receiving**. On the Mac, use Control Center > Screen Mirroring, select the receiver, enter the password displayed in the app, and select Extended Display.

Closing the controller while receiving keeps it available in the notification area. Its tray menu provides Open, Stop, and Exit. Exit terminates the receiver. A disconnected target monitor stops the session and requires reselection.

This folder includes the Windows x64 .NET runtime and native receiver. Keep the entire folder together. Bonjour is a separate host prerequisite and is already installed on the development host. Settings are saved under `%LOCALAPPDATA%/WindowsAirplay/settings.json`.

Only one receiver can use the configured ports at a time. Stop the compatibility probe before starting the app. The development host's firewall rules refer to its `artifacts/receiver/uxplay.exe`; this package has a different executable path. Before using the packaged receiver, configure the same Private/LocalSubnet rules for this folder's `receiver/uxplay.exe`, using `provenance/scripts/Configure-Firewall.ps1` in an administrator PowerShell. Supply the actual Bonjour service executable path. Do not run both receivers together.

The native receiver and extended display were confirmed on the user's M1 MacBook Pro running macOS 27.0.1. The WPF controller is a new local preview; connected UI, tray, mixed-DPI, monitor-unplug, three reconnect cycles, clean-machine installation and timed stability tests remain separate acceptance checks. Requested output is 1920x1080 at 60 Hz; delivered frame rate and latency are not measured.

Source provenance, native patches, dependency version inventory and installed native license texts are included. This is **not an externally distributable release**: complete transitive dependency source/license obligations and choose the project license before distributing it. Nothing has been published externally.
