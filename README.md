# RamRatio Desktop Helper

**Optional system-memory statistics for [RamRatio on Firefox](https://addons.mozilla.org/en-US/firefox/addon/ramratio/).** RamRatio works without the helper; install it only if you want extra local system and Firefox memory readings.

**[Get the latest downloads](https://github.com/GitRatio/RamRatio-Desktop-Helper/releases)** · [Report an issue](https://github.com/GitRatio/RamRatio-Desktop-Helper/issues) · [Security and privacy](SECURITY.md)

## Install in 3 steps

1. **Download the setup for your computer.** Use the newest *preview* release and match your processor.
2. **Run setup and select Install / Update.** It installs for your user account; no administrator/sudo access or background service is required.
3. **Open Firefox → RamRatio → Settings.** Enable **Advanced System Stats**, approve Firefox's native messaging permission, then press **Check connection**.

| Computer | Easy setup download | How to open |
| --- | --- | --- |
| Windows Intel / AMD | `RamRatio-DesktopHelper-Setup-win-x64.exe` | Double-click; choose **Install** |
| Windows ARM64 | `RamRatio-DesktopHelper-Setup-win-arm64.exe` | Double-click; device validation remains pending |
| Mac with Apple Silicon | `RamRatio-DesktopHelper-Setup-osx-arm64.dmg` | Open disk image, then **RamRatio Setup.app** |
| Intel Mac | `RamRatio-DesktopHelper-Setup-osx-x64.dmg` | Open disk image, then **RamRatio Setup.app** |
| Linux Intel / AMD | `RamRatio-DesktopHelper-Setup-linux-x64.run` | Run `sh RamRatio-DesktopHelper-Setup-linux-x64.run` |
| Linux ARM64 | `RamRatio-DesktopHelper-Setup-linux-arm64.run` | Run `sh RamRatio-DesktopHelper-Setup-linux-arm64.run` |

On Linux, **Zenity** or **KDialog** shows a simple action picker when available; otherwise setup uses a terminal menu. Standard glibc Linux is supported; Snap/Flatpak Firefox connections have not been verified. Alternative ZIP/tar.gz packages remain available for advanced users.

> **Preview notice:** These installers are **unsigned** and the Mac app is **not notarized**. Windows SmartScreen or macOS Gatekeeper may block them. Do not disable security protections to proceed. No installer runs silently or adds browser permissions without you opting in.

## Update, check, or remove

Open the **same setup application** and select **Install / Update**, **Check status**, or **Remove**. The Linux `.run` launcher also accepts `--diagnose` and `--version` in a terminal. To remove the helper, first turn off Advanced System Stats in RamRatio. Your Firefox extension, saved settings, and unrelated files remain untouched.

If setup reports a foreign manifest, missing ownership information, or an unsafe path, it refuses to overwrite or remove the unverified file. See [troubleshooting and developer instructions](docs/INSTALLATION.md).

## Privacy and source

The helper accepts only the bounded Firefox native messaging `stats` request. It provides local aggregate memory readings: **no ads, browsing-history collection, remote control, network service, or automatic updater**. Memory estimates can count shared pages more than once. See [Privacy](PRIVACY.md), [Terms](TERMS.md), [Security](SECURITY.md), [License](LICENSE-HELPER.md), and [Release notes](RELEASE-NOTES.md).

The public source and cross-platform builds are validated with automated tests. **Real Firefox end-to-end testing and code signing are still outstanding** for a stable release.
