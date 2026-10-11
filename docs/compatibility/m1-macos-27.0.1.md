# Compatibility record: M1 MacBook Pro / macOS 27.0.1

Date: 2026-10-11. Sender details supplied by the user. Initial AirPlay session connected; user reports that the image looks great. User explicitly confirmed extended-desktop mode and is actively using it.

| Check | Result | Evidence |
|---|---|---|
| Windows host architecture | PASS | x64, Windows build 26300 |
| Local .NET SDK | PASS | 10.0.401; downloaded archive matched official SHA-512 |
| Native receiver build | PASS | UxPlay 1.73.7 source commit df67c212a433cf6dda3676dd40c097900d24e645; recorded build/readiness patches |
| Native runtime loading | PASS | uxplay -h and GStreamer h264parse, avdec_h264, d3d11videosink inspection succeeded |
| Monitor enumeration | PASS | Two 1920×1080 displays; left at (-1920,0), primary at (0,0) |
| Actual D3D11 placement | PASS | Test-pattern process placed at (-1920,0,1920,1080) and exited successfully |
| Receiver and display automated tests | PASS | 44 Release tests passed, including settings, controller, process descendants/owner crash, and runtime layout |
| Bonjour service | PASS | Signed Apple Bonjour64.msi installed; service running |
| Native listener startup | PASS | Actual probe reached Listening after DNS-SD registration; repeated with a fresh plugin cache |
| Receiver process cleanup | PASS | No uxplay process remained after terminating the probe |
| Firewall setup | PASS | Three enabled per-executable rules verified: Private profile, LocalSubnet, receiver TCP/UDP 35000–35002 and Bonjour UDP 5353 |
| Mac discovery and authentication | PASS WITH RETRY | Mac discovered the receiver. Initial code attempt failed; after restarting the receiver, a new code was accepted and the receiver reported Connected |
| Initial image quality | USER CONFIRMED | User reports connected video looks great; no objective resolution/FPS measurements yet |
| Extended desktop / independent Mac window | PASS | User explicitly confirmed independent extended desktop and active use on the M1 Mac |
| Reconnect / three cycles | NOT RUN | Requires a real session |
| Monitor unplug / mixed DPI | NOT RUN | Coordinate and missing-monitor policy covered by unit tests only |
| WPF controller / local package | BUILD AND LAUNCH PASS | Zero build warnings/errors; self-contained executable opened a responsive Windows AirPlay window; connected controls and tray not yet manually verified |
| 30-minute session | NOT RUN | Required before claiming stable interoperability |
| Actual resolution / FPS / latency | UNMEASURED | Requested 1920×1080 at 60 Hz is not observed delivery |

Next: switch from the working probe to the controller when the user is ready, verify the pairing prompt, Stop and tray Exit, then run reconnect, unplug and sustained-session checks.

Host Ethernet 2 changed to Private with explicit user authorization for this trusted home network. Project firewall rules were applied and read back. Bonjour service executable is C:/Program Files (x86)/Bonjour/mDNSResponder.exe. The probe is connected to the Mac; extended-desktop mode is confirmed by the user. Initial native launch timed out at 10 seconds; subsequent normal and fresh-cache starts passed. Startup allowance is now 60 seconds, with cancellation supported. The cause of the initial delay is not confirmed.

Authentication investigation: the original code was confirmed by receiver output, but the initial rejection cause remains unknown. Added allowlisted success/failure/challenge-mismatch diagnostics, then restarted without changing authentication mode. New-session authentication succeeded. This does not establish that the underlying intermittent issue is fixed.
