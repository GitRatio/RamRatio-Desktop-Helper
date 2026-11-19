# Installation and troubleshooting (advanced)

## Basic requirements

RamRatio Desktop Helper is opt-in. Install RamRatio from the [Mozilla Add-ons listing](https://addons.mozilla.org/en-US/firefox/addon/ramratio/) before enabling Advanced System Stats. The native host name is `com.ratioworks.ramratio` and the expected signed add-on ID is `memory-optimizer@local`.

The setup runs under the current user only; no privilege escalation, system service, or background startup task is added. Browser permission is separate from installing the helper.

## Platform notes

**Windows:** Download a `Setup-win-x64.exe` or `Setup-win-arm64.exe` from the matching preview release and double-click. The graphical executable embeds a SHA-256-checked copy of the console native host. It registers only that extracted helper under your user profile, never the GUI. The existing ZIP/console installer remains for power users. The EXE is currently unsigned; respect SmartScreen and enterprise policies.

**macOS:** Choose the app bundle for your CPU: both Apple Silicon and Intel Macs use `.app.zip`, avoiding unreliable temporary DMG disk mounts in CI. Unzip and double-click `RamRatio Setup.app`. Its built-in AppleScript picker offers Install / Update, Remove, and Check status. The executable remains under `~/Library/Application Support/RamRatio/DesktopHelper`. macOS Gatekeeper may refuse the unsigned, unnotarized preview app; signing/notarization must be completed before broad release. The legacy `Setup.command` inside the tarball is also available as a fallback.

**Linux:** Download the self-extracting `.run`, open a terminal in the download folder and run `sh RamRatio-DesktopHelper-Setup-linux-x64.run` (or the ARM64 filename). The bundled native helper is gzip-compressed to reduce download size, then decompressed and checked against its embedded SHA-256 digest before running. The standard `gzip` utility must be available. If Zenity/KDialog is available under a desktop session, the setup offers a graphical choice; otherwise it displays a text menu. Alternatively use the archived `Setup.sh`. No `sudo` is needed. Snap/Flatpak Firefox integration has not been verified.

## Diagnose

- On Windows, open the setup application and use **Check status**. The console installer also supports `--diagnose` and `--version`.
- On macOS, select **Check status** in the setup app; the advanced helper also supports `--diagnose`.
- On Linux, run `sh YOUR_FILE.run --diagnose`; this only checks local installation and never installs anything.
- The installation status is not the same as Firefox permission: check the Firefox **Advanced System Stats → Check connection** button separately.
- Diagnoses report `installed`, `not-installed`, `repair-needed`, `foreign-manifest`, or `unsafe-path`. Never overwrite or delete a foreign host binary or registry entry.

## Packaging and verification

With .NET SDK 10 and PowerShell, invoke `./tools/package.ps1 -Runtime win-x64` (or an OS-matching runtime). Builds produce unsigned GUI installers as separate assets along with the original CLI archive. Every release includes a `SHA256SUMS-<runtime>.txt` file. All license notices must be distributed with the installers; Mac app bundles carry them as resources and the published release includes them alongside the Windows/Linux downloads.

CI tests Windows x64, macOS Intel/Apple Silicon, Linux x64/ARM64 and cross-builds Windows ARM64. It checks native framing, safe installation/rollback/removal, source packaging, and bundled-payload integrity. GUI human-interaction tests, Windows ARM64 device testing, macOS signing/notarization, and Firefox integration remain separate acceptance requirements.

Public GitRatio/RamRatio-Desktop-Helper is the release source of truth. The private RamRatio extension repository mirrors source under a pinned hash; synchronize and revalidate when the native helper changes.
