#!/bin/sh
# GUI launcher; only invokes the bundled, tested native helper. No sudo.
set -eu
resources="$(CDPATH= cd -- "$(dirname -- "$0")/../Resources" && pwd)"
helper="$resources/RamRatio.DesktopHelper.Setup"
if [ ! -x "$helper" ]; then
  osascript -e 'display alert "RamRatio Setup" message "Helper component missing. Download the official package again." as warning' || :
  exit 1
fi
choice="$(osascript -e 'set picked to choose from list {"Install / Update", "Remove", "Check status"} with prompt "RamRatio Desktop Helper · optional local memory statistics" OK button name "Continue" cancel button name "Cancel"' -e 'if picked is false then return ""' -e 'return item 1 of picked' 2>/dev/null)" || exit 0
case "$choice" in
  "Install / Update") action=I ;;
  "Remove") action=U ;;
  "Check status")
    raw="$("$helper" --diagnose 2>/dev/null)" || exit 1
    state="$(printf '%s\n' "$raw" | sed -n 's/.*"status":"\([^"]*\)".*/\1/p')"
    osascript - "$state" <<'OSA'
on run argv
  display alert "RamRatio Desktop Helper" message ("Local installation status: " & item 1 of argv & return & "For Firefox permission, use Check connection in the extension.") as informational
end run
OSA
    exit 0 ;;
  *) exit 0 ;;
esac
if [ "$action" = U ]; then
  confirm="$(osascript -e 'button returned of (display dialog "Remove the optional native helper? Firefox settings remain untouched." buttons {"Cancel", "Remove"} default button "Cancel" with icon caution)' 2>/dev/null)" || exit 0
  [ "$confirm" = Remove ] || exit 0
fi
if output="$(printf '%s\n' "$action" | "$helper" 2>&1)"; then
  if [ "$action" = I ]; then
    osascript -e 'display alert "RamRatio Installed" message "Open Firefox → RamRatio settings → Advanced System Stats → Check connection. Firefox permission remains optional." as informational' || :
  else
    osascript -e 'display alert "RamRatio Removed" message "The local native helper was removed. Your Firefox extension and settings remain unchanged." as informational' || :
  fi
else
  printf '%s\n' "$output" >&2
  osascript -e 'display alert "RamRatio Setup Failed" message "The requested change failed. Use the advanced Setup.command for diagnostics. Unrecognized files were not intentionally changed." as warning' || :
  exit 1
fi
