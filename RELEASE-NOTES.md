# Desktop Helper 0.3.0 preview

Optional local memory measurements for RamRatio through Firefox Native Messaging. Choose your OS and processor package, extract, and run Setup. I installs/updates; U unregisters and removes the installed host. No Python, administrator/sudo access, background service, startup job, or native auto-updater.

Includes Windows x64/ARM64, macOS Intel/Apple Silicon, and Linux x64/ARM64 packages plus matching helper source, SHA256 checksums, Terms, Privacy Policy, and security/removal details.

**Unsigned/unnotarized preview:** native-runner protocol tests are required before this workflow publishes, but live Firefox clean-account installation and accessibility tests remain outstanding. Windows ARM64 is cross-built and requires device testing. macOS Gatekeeper can block unsigned binaries; do not bypass protection blindly. Linux targets standard glibc-based distributions; Snap/Flatpak integration is not verified. The signed RamRatio add-on ID must match `memory-optimizer@local`. Firefox stats require an add-on version with Advanced System Stats support.

Process totals may include multiple profiles/double-count shared memory; macOS availability is an estimate. No CPU statistics, browsing content, network telemetry, or remote-control operations. Public source is provided for review and contributions. Installation is voluntary and independent of extension update-notice acknowledgment.

## 0.3.0 development changes

- Adds bounded, opt-in `--diagnose` status output and `--version` with no native messaging expansion.
- Validates existing native messaging registration ownership and refuses foreign manifests/host files.
- Uses staged host/manifest replacement, local backups, and rollback on installation failure.
- Refuses symbolic links or reparse points in managed installation paths; does not recursively delete unrelated files.
- Extends disposable CI installer tests for installed, missing, foreign, and linked manifest states.

This release remains an unsigned preview. A successful protocol/installation test is not evidence of end-to-end Firefox compatibility, Windows ARM64 device compatibility, or notarization.


## 0.3.0 user-friendly setup packaging (preview source)

- Adds one downloadable self-contained graphical Windows setup EXE with embedded native host.
- Adds a macOS setup app wrapped in a DMG using built-in AppleScript dialogs.
- Adds a Linux self-extracting .run with native-host digest verification, GUI selector where available and text fallback.
- Preserves the original command-line ZIP/tar.gz formats, local-only installation and Firefox opt-in permissions.
- Simplifies the quick-start README; advanced diagnostics and build instructions moved to docs/INSTALLATION.md.

Unsigned previews are not a substitute for code-signing or macOS notarization. No stable release has been published.
