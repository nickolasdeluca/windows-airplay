# Third-party components (local development build)

- UxPlay v1.73.7: https://github.com/FDH2/UxPlay — GPLv3 project, with component-specific notices in its source. Source commit and archive hash are in runtime/manifest.json; local changes are in patches/.
- Apple DNS-SD header: https://github.com/apple-oss-distributions/mDNSResponder — the downloaded header contains its copyright and BSD redistribution notice. Revision and hash are in runtime/manifest.json.
- GStreamer and plugins, OpenSSL, libplist, and native dependencies: installed through MSYS2. Exact current package versions are recorded in runtime/packages.txt. Preserve each package's notices and source obligations when producing a distributable.
- .NET: https://github.com/dotnet — installed from Microsoft's official SDK archive.
- xUnit and Microsoft.NET.Test.Sdk: test-only NuGet dependencies.
- Bonjour service: installed separately from Apple; not bundled by this repository.

This inventory is not a complete distributable license bundle. Packaging and applicable source/notice materials must be completed before external distribution. No project-wide distribution license has been selected yet.
