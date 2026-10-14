# Security, system changes, and removal

The helper accepts only a bounded, length-prefixed JSON request with type `stats`. It returns aggregate memory readings. Unknown operations are rejected. There is no shell-command message, file-upload message, process-kill message, HTTP server, or outbound network call. The native manifest permits only `memory-optimizer@local`; confirm the signed add-on ID before releasing.

| Platform | Installed host | Firefox registration |
| --- | --- | --- |
| Windows | `%LOCALAPPDATA%\RamRatio\DesktopHelper\RamRatio.Host.exe` | HKCU\Software\Mozilla\NativeMessagingHosts\com.ratioworks.ramratio pointing to the JSON manifest in the helper folder |
| macOS | `~/Library/Application Support/RamRatio/DesktopHelper/RamRatio.Host` | `~/Library/Application Support/Mozilla/NativeMessagingHosts/com.ratioworks.ramratio.json` |
| Linux | `~/.local/share/ramratio/desktop-helper/RamRatio.Host` | `~/.mozilla/native-messaging-hosts/com.ratioworks.ramratio.json` |

Setup writes only these host/manifest locations and Windows registry registration, under your own account. Install/update replaces this helper's files. Remove unregisters and deletes those named installed files; it does not recursively delete directories or touch other applications. Downloaded files and extension settings are separate. Stop Advanced System Stats before updating/removing; close Firefox if a host file is locked. Existing empty helper directories may remain.

The helper has the OS permissions of the current user, not a special security sandbox. Its current code limits its operations to measurement. Other programs running as you can alter local files; a signature/integrity check at download does not prevent every later compromise. We do not claim the software is impossible to hack.

Windows SmartScreen and macOS Gatekeeper can flag unsigned previews. Release maintainers should use trusted code signing and macOS notarization. Do not disable endpoint protection or automatically remove quarantine attributes. Standard Firefox installs are the intended Linux target; Snap/Flatpak sandbox integration requires separate validation and may not find a user-level native host.

For vulnerabilities, contact the support address on the official Mozilla add-on listing rather than publicly posting exploit details. Do not include private data in public GitHub issues. A dedicated private advisory channel should be enabled by the repository owner before production release.
