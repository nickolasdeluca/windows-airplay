# Compatibility record: M1 MacBook Pro / macOS 27.0.1

Date: 2026-10-11. Sender details supplied by the user. AirPlay session not yet tested.

| Check | Result | Evidence |
|---|---|---|
| Windows host architecture | PASS | x64, Windows build 26300 |
| Local .NET SDK | PASS | 10.0.401; downloaded archive matched official SHA-512 |
| Native receiver build | PASS | UxPlay 1.73.7 source commit df67c212a433cf6dda3676dd40c097900d24e645; recorded build/readiness patches |
| Native runtime loading | PASS | uxplay -h and GStreamer h264parse, avdec_h264, d3d11videosink inspection succeeded |
| Monitor enumeration | PASS | Two 1920×1080 displays; left at (-1920,0), primary at (0,0) |
| Actual D3D11 placement | PASS | Test-pattern process placed at (-1920,0,1920,1080) and exited successfully |
| Receiver and display automated tests | PASS | 26 tests passed in Debug and Release, including the buffered-output crash regression |
| Bonjour service | PASS | Signed Apple Bonjour64.msi installed; service running |
| Native listener startup | PASS | Actual probe reached Listening after DNS-SD registration; repeated with a fresh plugin cache |
| Receiver process cleanup | PASS | No uxplay process remained after terminating the probe |
| Firewall setup | PASS | Three enabled per-executable rules verified: Private profile, LocalSubnet, receiver TCP/UDP 35000–35002 and Bonjour UDP 5353 |
| Mac discovery and authentication | NOT RUN | Requires network setup and the Mac |
| Extended desktop / independent Mac window | NOT RUN | Required before GUI work |
| Reconnect / three cycles | NOT RUN | Requires a real session |
| Monitor unplug / mixed DPI | NOT RUN | Coordinate and missing-monitor policy covered by unit tests only |
| 30-minute session | NOT RUN | Required before claiming stable interoperability |
| Actual resolution / FPS / latency | UNMEASURED | Requested 1920×1080 at 60 Hz is not observed delivery |

Next: start the probe on monitor 1 after network setup, connect the M1 Mac, choose Extended Display, and move a window onto the left monitor. Record the actual outcome, including failed authentication and reconnect, before proceeding to the WPF app.

Host Ethernet 2 changed to Private with explicit user authorization for this trusted home network. Project firewall rules were applied and read back. Bonjour service executable is C:/Program Files (x86)/Bonjour/mDNSResponder.exe. The probe is listening; Mac connection test is pending. Initial native launch timed out at 10 seconds; subsequent normal and fresh-cache starts passed. Startup allowance is now 60 seconds, with cancellation supported. The cause of the initial delay is not confirmed.
