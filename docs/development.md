# Development notes

The project uses a local .NET 10.0.401 SDK under .tools/dotnet and an MSYS2 UCRT64 environment under .tools/msys64. SDK archives are verified against Microsoft's SHA-512 metadata. The MSYS2 base and UxPlay source are verified by SHA-256 in runtime/manifest.json. Dependency caches, native binaries and test artifacts are ignored by Git.

UxPlay is pinned to v1.73.7, commit df67c212a433cf6dda3676dd40c097900d24e645. The original Windows CMake configuration fails without Bonjour SDK. Inspection showed that Windows calls DNS-SD through LoadLibrary/GetProcAddress; patches/uxplay-windows-dynamic-dnssd.patch removes the unused import-library link. Apple's public dns_sd.h is fetched from the exact recorded source revision. The runtime Bonjour service remains required.

patches/uxplay-readiness.patch adds a stderr marker only after successful DNS-SD registration. ReceiverSupervisor waits for this marker and treats stream startup separately. It launches without a shell, isolates the configuration and key directory, owns the process in a kill-on-close Windows job, and bounds line and diagnostic buffers. PIN codes use a transient event; diagnostic history is an allowlist of fixed messages. Do not enable raw stdout/stderr logging when testing real clients.

The MSYS2 repository is rolling. runtime/packages.txt records the exact package versions used for the current local build. The preparation script currently resolves packages from the live signed MSYS2 repository, so future rebuilds may resolve newer versions. This is a known reproducibility limitation; archive package artifacts with hashes before a distributed release. The source and SDK pins do not imply native package pinning.

The staged development runtime currently copies all UCRT64 bin DLLs and GStreamer plugins to avoid hidden loader dependencies. It is larger than a release payload. Dependency trimming, license/source bundling and clean-machine packaging belong to the packaging milestone.

The presentation adapter selects a visible GSTD3D11-class window by the owned process ID, refuses ambiguity, preserves styles/position for restoration, and uses physical monitor coordinates. The process manifest enables PerMonitorV2 DPI awareness. Run the built EXE for native display tests; launching its DLL with the dotnet host does not apply the EXE manifest.

Build/test commands are in README.md. Product C# builds treat warnings as errors; third-party UxPlay currently emits unused-variable warnings with GCC 16.2.0. No AirPlay protocol behavior was changed by the build patches.

patches/uxplay-windows-manifest.patch embeds the native UTF-8 code-page and PerMonitorV2 manifest. A real native regression test first reproduced rejection of non-ASCII receiver names, then passed with this manifest. The C# ArgumentList API alone cannot correct the native executable's code-page handling.

Native readiness has a 60-second deadline to allow initial native setup; it remains cancellable. An initial 10-second timeout was observed; later starts including a fresh plugin registry passed. The initial delay has not been conclusively attributed to plugin scanning.

Review status: an independent reviewer identified the native Unicode and buffered-output state issues; both were reproduced and fixed with regression coverage. The reviewer reached its usage limit before a final verdict. Parent-crash/descendant fixture coverage, mixed-DPI and monitor-unplug hardware checks, and the real Mac session remain outstanding; this checkpoint is not a completed release review.

Controller architecture: the WPF shell owns the dispatcher, timer, tray, and Windows adapters. MainViewModel lives in Core/App behind IReceiverSession and IDisplayHost so state transitions and monitor-loss behavior can be tested without a WPF dispatcher or physical monitor. Settings writes use a temporary file followed by replacement; malformed JSON recovers with defaults and a diagnostic.

Updated verification: 44 Release tests pass, now including abrupt supervisor-owner termination and descendant process cleanup. The self-contained WPF app opened and responded. Computer-use native pipe remained unavailable after retry/reset, so visual inspection and connected tray/control interaction are not recorded as passed. The previously incomplete independent review has not been replaced by a completed review.
