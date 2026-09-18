# Lertaro for Ubuntu

Lertaro Linux is the Ubuntu port of Lertaro's fast local file search and launcher. The packaged application contains a self-contained CLI, per-user search daemon, and Avalonia desktop UI.

## Supported release target

The release acceptance target is Ubuntu on `amd64`; `arm64` packages are built and archive-validated in CI. The desktop implementation follows freedesktop conventions and avoids unsupported Wayland-wide keyboard hooks.

## Install

Install the package matching the machine architecture:

```bash
sudo dpkg --install lertaro_<version>_amd64.deb
# or, on ARM64:
sudo dpkg --install lertaro_<version>_arm64.deb
```

The package installs the application under `/usr/lib/lertaro`, command launchers under `/usr/bin`, a desktop entry, and the `lertaro.service` systemd user unit.

Enable and start the per-user daemon:

```bash
systemctl --user daemon-reload
systemctl --user enable --now lertaro.service
```

By default the daemon indexes the current user's home directory. Its persistent state follows XDG conventions: `${XDG_STATE_HOME:-$HOME/.local/state}/lertaro/`. The index is `index.bin` and bookmarks are stored beside it. The Unix socket is under `$XDG_RUNTIME_DIR/lertaro/search.sock` when `XDG_RUNTIME_DIR` is available.

## Start and use

Launch the desktop application from the application menu or run:

```bash
lertaro-ui
```

Useful diagnostics and CLI operations:

```bash
lertaro daemon-status
lertaro daemon-search report
lertaro daemon-rebuild
lertaro bookmark-list
lertaro bookmark-add ~/Documents
lertaro apps terminal 20
```

For a one-off scan without the daemon:

```bash
lertaro ~/Documents report 50
```

The daemon executable also accepts explicit root, index, and socket paths when a non-default indexing root is required:

```bash
/usr/lib/lertaro/lertarod /data "$HOME/.local/state/lertaro/index.bin" "$XDG_RUNTIME_DIR/lertaro/search.sock"
```

For a persistent custom root, create a user-level systemd override rather than editing the packaged unit:

```bash
systemctl --user edit lertaro.service
```

Set an override for `ExecStart`, then run `systemctl --user daemon-reload` and restart the service.

## Logs and troubleshooting

Check service state and recent logs with:

```bash
systemctl --user status lertaro.service
journalctl --user -u lertaro.service -n 100 --no-pager
lertaro daemon-status
```

If the persistent index is corrupt, the daemon is designed to rebuild it on startup. Do not delete state as a first response; inspect the journal first.

## Uninstall

Stop and disable the user service before removing the package:

```bash
systemctl --user disable --now lertaro.service
sudo dpkg --remove lertaro
systemctl --user daemon-reload
```

Package removal intentionally does not delete per-user state. To remove the index and bookmarks as well, delete `${XDG_STATE_HOME:-$HOME/.local/state}/lertaro/` after uninstalling.

## Known limitations

- GNOME/Wayland and KDE/Plasma behavior still requires release-candidate manual desktop smoke testing; headless CI cannot prove compositor-specific shortcut behavior.
- Lertaro does not install a process-wide Wayland keyboard hook. Global shortcut configuration is delegated to desktop-environment mechanisms.
- The Linux plugin surface is intentionally smaller than the Windows plugin ecosystem. See `PLUGIN_COMPATIBILITY.md`; Windows/WPF/COM-dependent plugins are not portable by default.
- The current automated performance gate is a regression safety rail on shared CI hardware, not an Everything-class benchmark claim.
- `arm64` is publish/package validated in CI but is not natively executed on the GitHub-hosted x64 runner.
- The default daemon indexes the user's home directory. Other roots currently require explicit daemon arguments/systemd override rather than a packaged settings wizard.

## Release evidence

The authoritative release gate is `RELEASE_CHECKLIST.md`. The Ubuntu port must not be declared complete until every required item there is satisfied and the final release-candidate CI run is recorded.