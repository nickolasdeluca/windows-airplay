# Windows AirPlay receiver — proposed design

Date: 2026-10-11
Status: Approved by the user on 2026-10-11; implementation plan under review.

## Outcome

Use one physical monitor connected to a Windows PC as an extended desktop for a Mac over the local network. The Windows app selects where received video is displayed. The Mac creates the extended desktop through its built-in AirPlay display controls.

Success means the Mac can move a window off its built-in screen onto the selected Windows monitor, reconnect after disconnecting, and leave other Windows monitors usable. Full-screen mirroring alone does not satisfy acceptance.

## Working assumptions

Confirmed sender target: Apple Silicon Macs, with the user's MacBook Pro M1 running macOS 27.0.1 as the first acceptance device. Intel Mac qualification is outside the initial scope. Receiver assumption: Windows 11 x64, one Mac and one selected receiver monitor, on the same local network. Verify the Windows version and architecture before choosing the receiver build. Sender and receiver CPU architectures are independent. Start with a requested 1920×1080 at 60 Hz; negotiated resolution and delivered frame rate must be measured, not assumed. The Mac keyboard and trackpad continue to control the Mac. Windows input forwarding, multiple simultaneous receiver screens, HDR, and protected streaming video are outside the first release.

## Approach options

1. **Recommended: native C# WPF controller plus UxPlay receiver process.** Reuse AirPlay discovery, session handling, and decoding while building the monitor-focused Windows experience. Packaging native dependencies and reliable video-window placement are the main integration risks.
2. **Adapt an existing Windows UxPlay wrapper.** Potentially faster packaging, but inherits another application's architecture and requires a source and license review before reuse.
3. **Implement the receiver protocol ourselves.** Greatest control, with substantial interoperability, authentication, and maintenance work. Not justified for the initial release.

## Components and data flow

Mac AirPlay sender → local network → UxPlay receiver → GStreamer video output → selected Windows monitor.

The WPF controller enumerates monitors using Windows APIs, saves the chosen display identity and receiver name, starts and stops the receiver, reports connection status, and presents actionable errors. Settings and diagnostics stay local.

A receiver adapter owns argument construction, process lifecycle, output capture, and dependency checks. Receiver output is treated as untrusted text. Launch with a structured argument list, never a shell-built command string. Pin a tested upstream version and record dependency provenance.

The display coordinator locates the video window belonging to the receiver process and places it on the selected monitor. The compatibility milestone must verify this works reliably with the Windows GStreamer sink. If it cannot, stop and revise this boundary toward a small native integration that explicitly binds the video sink to a controlled window; do not silently substitute an unreliable window-title match.

## User flow

1. Open the app and select a monitor and receiver name.
2. Start receiving; show discovery readiness separately from a connected session.
3. On the Mac, select the receiver in AirPlay / Screen Mirroring, then choose extended display in the relevant macOS display controls.
4. Show received video full-screen on the selected monitor, preserving its aspect ratio.
5. Stop from the controller or tray menu. Always keep an accessible way to leave full-screen mode.

Persist the selection between launches. If that monitor disappears, stop full-screen presentation and prompt for a new selection. A missing receiver binary, missing runtime plugin, blocked discovery, or receiver crash must produce an actionable status rather than a false connected indicator.

## Network and access

Use LAN discovery and a documented fixed port configuration for reproducible firewall setup. Limit any supplied firewall rules to the receiver executable, private network profile, and local subnet. Do not expose the receiver to the internet. Validate UxPlay pairing or password support with the actual Mac and enable a working access control method before a distributable release. Do not store passwords or pairing material in logs.

## Delivery milestones

1. **Compatibility proof:** build or obtain a traceable Windows receiver runtime, establish a real AirPlay connection from the MacBook Pro M1 running macOS 27.0.1, verify discovery and authentication, verify extended desktop rather than mirroring alone, test selected-monitor placement, and record actual negotiated output and reconnect behavior. UxPlay compatibility with this exact macOS release remains unverified until this test passes.
2. **Usable app:** implement monitor selection, receiver lifecycle, full-screen control, saved settings, and diagnostics against the validated runtime.
3. **Distributable package:** bundle required runtime components, document setup, include dependency notices and applicable source/license materials, and verify on a clean Windows environment.

The UI can be developed locally, but successful Mac interoperability cannot be claimed without a real Mac test. If the first milestone fails, document the observed failure and revise the design before proceeding with a polished shell.

## Verification

Automated checks cover argument validation, settings round trips, lifecycle transitions, and missing-dependency/error handling. Windows integration checks cover mixed-DPI and negative-coordinate monitor layouts, unplugging the selected monitor, process exit, and repeated start/stop. Hardware acceptance covers actual extended desktop, movement of independent Mac windows, reconnect, and a sustained session. Report observed latency and stability; do not promise cable-equivalent behavior.

## Upstream references

- UxPlay: https://github.com/FDH2/UxPlay — documents Windows builds, macOS second-display use, GStreamer rendering, and GPLv3 licensing.
- Existing Windows wrapper for evaluation: https://github.com/leapbtw/uxplay-windows

## Decisions requiring user input

- Whether the recommended UxPlay-based design fits the intended distribution model; upstream is GPLv3, so plan distribution accordingly.

After design review, write the detailed implementation plan, then execute and verify it milestone by milestone.
