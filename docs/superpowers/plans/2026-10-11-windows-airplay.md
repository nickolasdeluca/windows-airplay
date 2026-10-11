# Windows AirPlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Turn one Windows-connected monitor into an AirPlay extended display for the user's M1 MacBook Pro on macOS 27.0.1.

**Architecture:** A C# WPF controller supervises a pinned UxPlay process. GStreamer renders the stream; a Windows display adapter places the receiver-owned window on the chosen monitor. A compatibility probe validates this integration before the application UI is built.

**Tech Stack:** C#, .NET 10 LTS, WPF, Win32 display/window APIs, UxPlay 1.73.7 initial candidate, GStreamer D3D11, PowerShell, xUnit.

**Spec:** ../specs/2026-10-11-windows-airplay-design.md (approved by user on 2026-10-11).

## Global Constraints

- Confirmed sender target: Apple Silicon Macs, with the user's MacBook Pro M1 running macOS 27.0.1 as the first acceptance device.
- Intel Mac qualification is outside the initial scope.
- Receiver assumption: Windows 11 x64, one Mac and one selected receiver monitor, on the same local network.
- Start with a requested 1920×1080 at 60 Hz; negotiated resolution and delivered frame rate must be measured, not assumed.
- The Mac keyboard and trackpad continue to control the Mac.
- Windows input forwarding, multiple simultaneous receiver screens, HDR, and protected streaming video are outside the first release.
- Full-screen mirroring alone does not satisfy acceptance.
- Launch with a structured argument list, never a shell-built command string.
- Do not store passwords or pairing material in logs.
- Shell commands use the user's RTK prefix. Local SDK invocations use: rtk proxy .\.tools\dotnet\dotnet.exe.
- Do not publish or claim hardware compatibility without the Mac acceptance result.

## Review Focus

1. A monitor left of the primary display or with different DPI must receive correctly positioned video (Task 3).
2. A selected monitor disappearing must stop presentation instead of unexpectedly occupying another display (Tasks 3, 4).
3. Quick start/stop/restart and stale process output must not leave orphan receivers or false connection status (Task 2).
4. Unicode names, corrupt settings, and an unavailable saved monitor must remain recoverable without shell interpretation (Tasks 2, 4).
5. Missing native DLLs, blocked multicast, unavailable ports, and credentials in engine output must produce useful, sanitized diagnostics (Tasks 1, 2, 5).

## Repository and environment

The repository currently contains Git metadata, .gitattributes, and the design document. Read-only inspection found Windows build 26300, x64; .NET runtimes but no installed SDK. No MSYS2, GStreamer, or Bonjour installations were found at their usual paths; search PATH and uninstall metadata before concluding they are absent. Standard shell-tool process creation failed, but RTK commands executed successfully through the Node tool's execFile with windowsHide enabled. This is an environment issue, not a product test result.

Keep SDK, source downloads, package caches, native build output, and receiver payloads in ignored .tools/, .cache/, and artifacts/ directories. Record their exact versions and hashes; never commit downloaded binaries. Respect existing changes when selecting a branch/worktree at execution time.

## File map

- WindowsAirplay.sln, global.json, Directory.Build.props, .gitignore: reproducible solution and SDK/build settings.
- src/WindowsAirplay.Core/: validated options, settings, receiver lifecycle, and shared display records.
- src/WindowsAirplay.Windows/: Windows process ownership, monitor enumeration, window placement.
- src/WindowsAirplay.Probe/: command-line compatibility test using the same adapters as the app.
- src/WindowsAirplay.App/: WPF controller, tray controls, and user-facing status.
- tests/WindowsAirplay.Tests/: xUnit behavior tests; matching Receiver/, Displays/, Settings/, App/ folders.
- tests/WindowsAirplay.TestReceiver/: deterministic child executable for process and video-window tests.
- scripts/: local toolchain/runtime preparation, firewall setup, and portable publishing.
- runtime/manifest.json: pinned source/runtime provenance and required plugins.
- docs/compatibility/, docs/development.md, README.md, THIRD-PARTY-NOTICES.md: actual results and operating instructions.

## Task 1: Reproducible receiver runtime and baseline Mac test

**Files:** Create scripts/Prepare-Toolchain.ps1, scripts/Prepare-Receiver.ps1, scripts/Test-ReceiverRuntime.ps1, scripts/Configure-Firewall.ps1, runtime/manifest.json, .gitignore, docs/development.md, docs/compatibility/m1-macos-27.0.1.md.

**Interfaces:** Prepare-Toolchain.ps1 installs a pinned .NET 10 SDK into .tools/dotnet. Prepare-Receiver.ps1 -Destination artifacts/receiver produces uxplay.exe, its dependency payload, and GStreamer inspection tools. Test-ReceiverRuntime.ps1 -RuntimePath artifacts/receiver returns nonzero on missing executable/plugin/service, and prints sanitized actionable failures. Configure-Firewall.ps1 -ReceiverPath <absolute-exe> [-Remove] manages only project-owned rules.

- [ ] Inspect available tools and upstream Windows build requirements. Resolve UxPlay tag v1.73.7 to a full commit and record archive SHA-256, dependency versions, download origins, required DLLs/plugins, and discovery-service requirements in the manifest. Do not substitute experimental 1.74 merely to avoid Bonjour; if the stable path is infeasible, report evidence and revise the runtime decision.
- [ ] Prepare the local SDK and receiver payload. Prefer building pinned upstream sources using the documented MSYS2 UCRT64 toolchain. If a traceable prebuilt payload is used, require its exact upstream source revision, license/source materials, and artifact digest. Download only from project/vendor sources. Record any host-level Bonjour requirement separately from the portable payload.
- [ ] Implement prerequisite checks for the H.264 decoder, d3d11videosink, discovery service, x64 binaries, and required TCP/UDP ports. Negative check: an empty runtime directory must fail clearly without attempting to listen. Positive check: all required plugins load and uxplay help/version exits successfully.
- [ ] Implement narrowly scoped firewall setup for the actual fixed ports verified against this pinned receiver (initial choice -p 35000, covering TCP/UDP 35000–35002), with Private profile and LocalSubnet scope. Account separately for mDNS UDP 5353 and the actual discovery service executable; never broaden all applications or public profiles. Verify -Remove leaves unrelated rules alone.
- [ ] Launch a baseline receiver with name Windows AirPlay, requested 1920x1080@60, fixed ports, D3D11 sink, and the pinned version's documented interactive PIN/password mode. Confirm the actual flags from its help/source before encoding them in the later adapter. Keep pairing state in the app-specific local directory; suppress secret-bearing raw output.
- [ ] With the user's Mac, test discoverability, successful and rejected authentication, extended-display selection, independent Mac-window movement, and reconnect. Record exact runtime version, macOS version, actual resolution when observable, and pass/fail with evidence. If the Mac is unavailable, record NOT RUN and leave this milestone open.
- [ ] Commit only scripts, manifests, and truthful documentation after inspecting the diff. No compatibility claim is permitted from a startup check alone.

## Task 2: Receiver lifecycle with executable integration tests

**Files:** Create solution/build files; Core/Receiver/{ReceiverOptions.cs,ReceiverState.cs,ReceiverSupervisor.cs,UxPlayArguments.cs,ReceiverDiagnostics.cs}; corresponding project files under src/; tests/WindowsAirplay.Tests/Receiver/{ArgumentsTests.cs,SupervisorTests.cs,DiagnosticsTests.cs}; tests/WindowsAirplay.TestReceiver/{Program.cs,WindowsAirplay.TestReceiver.csproj}.

**Interfaces:** ReceiverOptions(string Name, string RuntimeDirectory, int PortBase = 35000, int Width = 1920, int Height = 1080, int RefreshRate = 60). UxPlayArguments.Build(ReceiverOptions options) returns ProcessStartInfo with UseShellExecute=false and ArgumentList. ReceiverState contains Stopped, Starting, Listening, Connected, Stopping, Faulted. ReceiverSupervisor exposes Task StartAsync(ReceiverOptions, CancellationToken), Task StopAsync(CancellationToken), ReceiverState State, int? ProcessId, event EventHandler StateChanged, and IAsyncDisposable. ReceiverDiagnostics.Sanitize(string line) returns safe display text; pairing prompts travel through a separate transient UI event and are never persisted.

- [ ] Scaffold the projects and pin the installed SDK in global.json. Write tests asserting that Unicode/spaced names remain one argument, control characters and port bases outside 1024–65533 are rejected, missing runtime files fail before process creation, and passwords never appear in persistent diagnostics.
- [ ] Run rtk proxy .\.tools\dotnet\dotnet.exe test --filter FullyQualifiedName~Receiver. Confirm the new behavior tests fail for the intended missing behavior before implementing it.
- [ ] Implement validated argument construction and a single-session supervisor. Isolate user/global UxPlay configuration with an explicit app-owned configuration and working directory. Capture both output streams asynchronously with bounded buffers. Base readiness/session parsing on observed pinned-version messages; unknown text never implies Connected. Redact sensitive output before buffering or disk writes.
- [ ] Extend the test receiver to simulate readiness, disconnect, output floods, crash, and stalled shutdown. Tests assert double-start rejection, idempotent stop, start cancellation cleanup, bounded shutdown, stale-generation output ignored, and zero surviving owned processes. Own the receiver with a Windows job object configured to terminate children when its owner exits.
- [ ] Run the Receiver-filtered tests and build. Require all tests passing; inspect the diff and commit this deliverable.

## Task 3: Monitor enumeration and reliable selected-monitor presentation

**Files:** Create Core/Displays/{MonitorDescriptor.cs,PixelRect.cs}; Windows/Displays/{MonitorCatalog.cs,ReceiverWindowPresenter.cs,NativeDisplayMethods.cs}; Probe/{Program.cs,WindowsAirplay.Probe.csproj}; tests/WindowsAirplay.Tests/Displays/{PlacementTests.cs,WindowOwnershipTests.cs}; extend TestReceiver with a visible video-window mode.

**Interfaces:** PixelRect(int X, int Y, int Width, int Height). MonitorDescriptor(string Id, string Name, PixelRect Bounds, bool IsPrimary). MonitorCatalog.GetMonitors() returns IReadOnlyList<MonitorDescriptor> and exposes event EventHandler Changed. ReceiverWindowPresenter.PresentAsync(int processId, string monitorId, CancellationToken) returns Task; Restore() restores owned window state. The probe supports --list-monitors and --monitor <id> --runtime <directory>.

- [ ] Write tests with a selected display at (-1920, 0, 1920, 1080), mixed DPI, a vanished display, and unrelated windows with matching titles. Assert physical-coordinate placement, explicit missing-display failure, and that only the owned process window can change.
- [ ] Run rtk proxy .\.tools\dotnet\dotnet.exe test --filter FullyQualifiedName~Displays; confirm the intended failures.
- [ ] Implement stable display identity using Windows display configuration paths, with friendly names and physical bounds. Enable per-monitor DPI awareness. Locate visible receiver video windows by process ownership and verified sink characteristics, with cancellation and a bounded wait. Revalidate ownership before each mutation; restore styles/bounds on exit. Do not guess among ambiguous candidate windows.
- [ ] Wire the probe to Tasks 1–2. Verify how D3D11 full-screen interacts with placement using the actual receiver. Prefer normal window placement followed by the tested full-screen mechanism. If process/window control is unreliable, stop and document the need for an explicitly hosted native video sink; do not conceal the failure with title matching.
- [ ] Run display tests; manually verify the probe on the selected monitor, escape/stop, unplugging that monitor, and disconnect/reconnect. The M1 Mac must show distinct desktop content on the target monitor before Task 4 proceeds. Update the compatibility record with PASS/FAIL/NOT RUN for each check.
- [ ] Inspect the diff and commit the tested probe and display adapters.

## Task 4: WPF app, settings, and accessible stop controls

**Files:** Create App/{App.xaml,App.xaml.cs,MainWindow.xaml,MainWindow.xaml.cs,MainViewModel.cs,TrayController.cs,WindowsAirplay.App.csproj,app.manifest}; Core/Settings/{AppSettings.cs,SettingsStore.cs}; tests/WindowsAirplay.Tests/{Settings/SettingsTests.cs,App/MainViewModelTests.cs}.

**Interfaces:** AppSettings(string ReceiverName, string? MonitorId). SettingsStore.LoadAsync(CancellationToken) returns Task<AppSettings>; SaveAsync(AppSettings, CancellationToken) returns Task. Store JSON under LocalApplicationData/WindowsAirplay with atomic replacement. MainViewModel consumes ReceiverSupervisor, MonitorCatalog, ReceiverWindowPresenter, and SettingsStore; exposes Monitors, SelectedMonitor, ReceiverName, Status, CanStart, CanStop, StartCommand, StopCommand. Use standard WPF binding and ICommand without adding a UI framework.

- [ ] Write tests asserting settings round trips preserve Unicode, corrupt JSON yields recoverable defaults and a diagnostic, missing saved monitors require reselection, invalid settings disable Start, and state changes update commands. On unplug, assert presentation stops and the user receives a reselection message.
- [ ] Run rtk proxy .\.tools\dotnet\dotnet.exe test --filter "FullyQualifiedName~Settings|FullyQualifiedName~MainViewModel" and confirm expected failures.
- [ ] Build a compact window with receiver name, monitor selector, Start/Stop, connection state, transient pairing prompt, and sanitized diagnostic details. Show requested and observed output distinctly; never label a requested refresh rate as achieved FPS. Provide concise Mac connection instructions.
- [ ] Implement tray Open/Stop/Exit. Closing the controller window while receiving keeps the tray available; Exit stops the receiver and disposes the tray/job object. Restore the controller if the selected display disappears. Keep audio disabled for this display-focused first release using the verified receiver option.
- [ ] Run the tests and a Release build. Manually check keyboard navigation, mixed DPI, accessible labels, tray stop, restart, and app exit while connected. Inspect the diff and commit.

## Task 5: Portable packaging and end-to-end acceptance

**Files:** Create scripts/Publish.ps1, README.md, THIRD-PARTY-NOTICES.md; update runtime/manifest.json and docs/compatibility/m1-macos-27.0.1.md; create tests/WindowsAirplay.Tests/Packaging/RuntimeLayoutTests.cs.

**Interfaces:** Publish.ps1 -OutputDirectory artifacts/publish produces a win-x64 self-contained application directory and zip with a receiver/ payload, notices, runtime manifest, and setup instructions. Use separate files, no trimming or single-file bundling for WPF/native plugins. Packaging fails if required DLLs/plugins or provenance files are absent.

- [ ] Write RuntimeLayoutTests rejecting an incomplete payload and unexpected architecture. Include missing native dependencies and an executable path containing spaces in the real launch smoke test.
- [ ] Implement publishing and verify rtk proxy .\.tools\dotnet\dotnet.exe test -c Release and rtk proxy powershell -NoProfile -File scripts/Publish.ps1. Require all automated checks passing and a launchable local output; report missing prerequisites explicitly.
- [ ] Include applicable licenses, dependency notices, exact upstream revisions, and reproducible source/build materials before distribution. Do not choose a proprietary distribution model or apply a project-wide license without the user's decision. Packaging may remain a local preview pending that choice.
- [ ] On a clean supported Windows environment, test documented setup, missing Bonjour/plugin diagnostics, Private-profile firewall setup/removal, occupied ports, and discovery failure without changing unrelated firewall rules.
- [ ] Run a 30-minute M1/macOS 27.0.1 session, move independent Mac windows, perform three disconnect/reconnect cycles, stop/restart the app, and unplug the chosen monitor. Record stability, text readability, observed negotiated resolution, and a latency measurement with its method if measurement is available. Mark unavailable metrics as unmeasured.
- [ ] Perform the selected execution workflow's final review, inspect the final diff, and commit the verified deliverable. Deliver the local executable/zip paths plus exact passing checks and remaining hardware limitations. Do not publish externally.

## Execution recommendation and review

Recommend native execution in this session: these tasks share the same receiver and hardware compatibility evidence, so sequential work avoids repeated setup. Subagent-driven execution is an alternative with separate implementer/reviewer passes and higher context cost. The user must review this newly written plan and choose the execution method before implementation, as required by the writing-plans skill.

Self-review: each spec requirement maps to Tasks 1–5; all five review-focus conditions have owning verification steps. The hardware gate precedes UI implementation. Runtime pinning is an explicit Task 1 output, not an assumed installed dependency. License/distribution decisions block external distribution, not local compatibility testing. Nothing in this document records unperformed tests as passed.

## Technical references checked during planning

- https://github.com/FDH2/UxPlay/releases — v1.73.7 includes security and iOS 27 teardown fixes; this does not establish macOS 27 compatibility.
- https://gstreamer.freedesktop.org/documentation/d3d11/d3d11videosink.html — full-screen property requires the appropriate toggle mode.
- https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core — .NET 10 is an actively supported LTS release.
