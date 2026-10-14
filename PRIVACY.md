# RamRatio Privacy Policy

Effective October 14, 2026. Applies to this RamRatio extension/helper implementation maintained through GitRatio. Support: https://github.com/GitRatio/RamRatio-Desktop-Helper/issues. Reports there are public; do not post personal data.

## Extension data stored locally

RamRatio uses Firefox's tabs API to inspect tab URLs/domains, activity, pinned/audio state, and discard state so it can choose eligible inactive tabs. Settings, protected sites, local site-learning records, optimization outcomes, session metadata, and diagnostics are stored in Firefox's local extension storage. These records can include domains and timestamps. This implementation does not upload those records, sell them, use analytics, or send telemetry. It does not access page content, saved passwords, cookies, or private documents. Browser tab access is still a meaningful permission; review your settings before enabling optimization.

## Optional Desktop Helper

Installation is voluntary and separate from the browser add-on. Advanced System Stats is off by default for new installs; acknowledging the update notice does not change an existing setting or grant permission. Firefox separately prompts for Native Messaging permission when you enable the switch.

The helper reads system memory totals/availability and Firefox process memory/counts. Windows enumerates Firefox processes through OS APIs. Linux reads kernel memory/process metadata under /proc. macOS invokes fixed system measurement utilities (sysctl, vm_stat, ps); no commands or executable paths supplied by browser messages are executed. The helper returns measurements to the allowed RamRatio add-on through Firefox Native Messaging. It has no network client, telemetry, remote-control listener, service, or scheduled startup job. It runs under your own account, without administrator elevation. It cannot promise that other software on your account cannot access its files.

Process totals may include other Firefox profiles, double-count shared memory, or omit inaccessible/exited processes. macOS available memory is an estimate. Measurements are not browsing history and do not contain tab titles or URLs. The extension may retain before/after aggregate measurements as local optimization statistics.

## Retention and control

Settings and learning persist locally until reset or removal; this implementation does not impose automatic expiry on all local records. Settings provide learning/statistics resets. Turning Advanced System Stats off stops helper requests and attempts to remove Firefox's Native Messaging permission. Run Setup and choose Remove/U to unregister and delete the installed host. This does not erase extension settings or downloaded ZIPs. Remove the extension to remove its local extension storage according to Firefox behavior; maintainers should verify removal and backups for each supported browser build.

The update notice records only its revision and your acknowledgment time locally. This record is not sent to GitRatio.

## External services

Opening GitHub downloads, support/issues, Mozilla's add-on page, or donation links makes a normal request to that provider. They may receive IP address, browser information, account identity, and information you submit under their own policies. Issue reports, reviews, and donations are deliberate external actions. No helper download or update is fetched automatically by this implementation. Donation information is processed by the linked payment provider, not the helper. No cloud speech integration is enabled.

## Policy changes

Material changes to helper behavior or data handling require updated documentation and a new notice revision. Existing local data must not be silently sent to a new service. Questions or deletion requests for information you deliberately posted to GitHub should be handled through GitHub and the project maintainer; local-only data is controlled on your device.
