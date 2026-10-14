# RamRatio Desktop Helper

An optional, on-demand helper that gives the RamRatio Firefox add-on system memory and Firefox process-memory readings. This public repository contains **helper source and downloads**, not the private extension repository.

**[Download helper packages](https://github.com/GitRatio/RamRatio-Desktop-Helper/releases)** · **[Install the Firefox add-on](https://addons.mozilla.org/en-US/firefox/addon/ramratio/)** · **[Report an issue or suggest an improvement](https://github.com/GitRatio/RamRatio-Desktop-Helper/issues)**

## Why a separate download?

Firefox cannot install native software silently. You download and run Setup yourself, choose Install, and separately enable Advanced System Stats in RamRatio settings. Reading or accepting an update notice does not install or enable anything. Standard tab optimization works without the helper.

## Easy installation and removal

| Your computer | Package | Setup |
| --- | --- | --- |
| Windows Intel/AMD | win-x64.zip | Extract, double-click Setup.exe, press I |
| Windows ARM | win-arm64.zip | Same; device testing still required |
| Apple Silicon Mac | osx-arm64.tar.gz | Extract, open Setup.command, enter I |
| Intel Mac | osx-x64.tar.gz | Same |
| Linux Intel/AMD | linux-x64.tar.gz | Extract, run ./Setup.sh, enter I |
| Linux ARM | linux-arm64.tar.gz | Same |

Actual executable name: `RamRatio.DesktopHelper.Setup.exe` on Windows, `RamRatio.DesktopHelper.Setup` elsewhere. Packages carry their own .NET runtime; no Python, sudo, administrator rights, system service, or startup job is required. On Linux, if needed run `chmod +x Setup.sh RamRatio.DesktopHelper.Setup` before `./Setup.sh`. Standard glibc-based Linux distributions are intended; Alpine/musl and sandboxed Firefox require separate packaging/integration.

Then open RamRatio settings, enable Advanced System Stats, allow Firefox's permission prompt, and select Check connection. Your signed add-on ID must match `memory-optimizer@local` in this helper's native manifest; maintainers must verify this before public end-user release.

To remove: turn Advanced System Stats off, run the downloaded Setup again, and select U. Setup deletes the installed host and its Firefox registration. Your downloaded archive/setup, extension settings, and any empty folders remain separate. Close Firefox if Windows reports a locked helper. No recursive deletion of user folders occurs.

## What it can and cannot do

Reads aggregate system memory and Firefox process memory only. It has no network client, HTTP listener, remote-access facility, browser-command executor, process killer, document reader, or credential reader. The host accepts only a bounded `stats` request and responds through Firefox Native Messaging. OS-account permissions still apply; this is not a sandbox or a guarantee against every possible compromise.

Shared memory can be counted twice, other profiles can be included, and inaccessible/exited processes can be skipped. macOS availability is an estimate. There is no CPU measurement or guarantee of memory savings. See [Privacy Policy](PRIVACY.md), [Terms of Use](TERMS.md), [Security and exact installed paths](SECURITY.md), and [License](LICENSE-HELPER.md).

## Preview limitations

Preview builds are unsigned/unnotarized. macOS Gatekeeper and Windows SmartScreen may block them; do not disable security protections or remove quarantine automatically. Trusted signing/notarization and real Firefox integration tests are still required for a polished general release. CI tests protocol/system memory and actually installs, repairs/updates, verifies registration, runs the installed host, removes it twice, and verifies unrelated files survive on disposable runner accounts. These checks passed on Windows x64, macOS Intel/Apple Silicon, and Linux x64/ARM64. They do not prove every user's Firefox installation works. Windows ARM64 is built but not executed in this initial matrix.

## Build and contribute

With .NET SDK 10 and PowerShell, run `./tools/package.ps1 -Runtime win-x64` (or the matching Unix runtime on a Unix host). Run `node tools/test-helper.mjs artifacts/win-x64/RamRatio.DesktopHelper.Setup.exe`. The installer integration test requires explicit opt-in on a disposable CI account; do not run it on a personal workstation. Six-platform GitHub Actions builds preserve Unix executable permissions, bundle the exact .NET runtime license/notices, generate checksums, and can publish a tested preview with matching helper source.

Open an issue with platform, processor architecture, Firefox/add-on/helper versions, sanitized error text, and reproduction steps. Submit focused pull requests with validation. Useful next work: signed/notarized installers, a graphical accessible setup interface, clean-account Firefox install/update/remove tests, sandboxed Linux Firefox support, and more complete process-memory attribution. Never post personal URLs, credentials, or private files.