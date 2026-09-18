# Desktop release acceptance

This runbook closes the compositor-specific release gates that headless CI cannot prove. Run it against the exact release-candidate package on a clean Ubuntu desktop. GNOME/Wayland is required; KDE/Plasma is best-effort and must be recorded as unavailable rather than silently skipped when no suitable machine exists.

## Evidence header

Record these values before testing:

```text
Date:
Tester:
Commit SHA:
Package filename:
Package SHA256:
Ubuntu version:
Architecture:
Desktop:
Session type (`echo $XDG_SESSION_TYPE`):
```

Do not reuse evidence from a different commit or package.

## Install and daemon

1. Install the release-candidate package with `sudo dpkg --install ./<package>.deb`.
2. Run `systemctl --user daemon-reload` and `systemctl --user enable --now lertaro.service`.
3. Require `systemctl --user is-active lertaro.service` to print `active`.
4. Require `lertaro daemon-status` to succeed.
5. Create a uniquely named file under the indexed home directory and require `lertaro daemon-search <unique-name>` to return it.
6. Rename the file, require the new name to become searchable, then delete it and require it to disappear from results.

## Desktop application

1. Launch Lertaro from the desktop application menu; do not use a terminal for this first launch.
2. Require the search window to appear without a crash dialog or missing controls.
3. Search for a known file and a known directory. Require both to appear.
4. Activate the directory and require the desktop file manager to open it.
5. Activate the file and require its default application to open it.
6. Exercise search history and add/remove one bookmark. Restart the UI and require persisted state to remain correct.
7. Search for an installed desktop application and launch it from Lertaro.

## Desktop integration

Configure a desktop-environment shortcut that launches `lertaro-ui` using the normal desktop settings UI. Require the shortcut to open Lertaro while another application has focus. This is the supported Wayland model; do not require or install a process-wide keyboard hook.

For a file result, exercise reveal-in-file-manager where exposed by the UI and require the containing folder to open with the file selected when the desktop file manager supports `org.freedesktop.FileManager1`.

## Restart and diagnostics

1. Run `systemctl --user restart lertaro.service`.
2. Require `lertaro daemon-status` and a known search to succeed after restart.
3. Log out and back in once. Require the enabled user service to start and search to work without manual daemon startup.
4. Inspect `journalctl --user -u lertaro.service -b --no-pager`; record unexpected errors even when the UI appears functional.

## Uninstall

1. Run `systemctl --user disable --now lertaro.service`.
2. Run `sudo dpkg --remove lertaro` and `systemctl --user daemon-reload`.
3. Require the application-menu entry and package-owned launchers to be gone.
4. User state may remain by design; do not treat `${XDG_STATE_HOME:-$HOME/.local/state}/lertaro/` as an uninstall failure.

## Required GNOME/Wayland result

Record `PASS` only when every applicable step above passes on a clean supported Ubuntu GNOME Wayland session. For each failure record the exact step, observed behavior, relevant journal output, and whether it blocks release.

```text
GNOME/Wayland: PASS | FAIL
Notes:
```

## KDE/Plasma result

Repeat the same flow on KDE Plasma where a clean test environment is practical. If unavailable, record `NOT AVAILABLE` with the reason; do not fabricate a pass.

```text
KDE/Plasma: PASS | FAIL | NOT AVAILABLE
Notes:
```

Attach the completed evidence to a numbered `DevelopmentLogs/` entry. Only then update `RELEASE_CHECKLIST.md`; headless CI evidence is not a substitute for these desktop checks.
