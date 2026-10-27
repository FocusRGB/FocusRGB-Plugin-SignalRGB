# Migrated SignalRGB research material

This directory preserves the SignalRGB research, examples, and tools moved from the FocusRGB
staging module when this external plugin repository was created. The production plugin is under
`src/`; research content is not installed or packaged with releases.

Everything specific to the interim SignalRGB backend lives in this directory. The future
controller accesses it only through the provider-neutral integration interface and loads the
SignalRGB implementation as a separate plugin. SignalRGB code must not be compiled into or
referenced by the core or Avalonia app projects. Removing this plugin must not require
rewriting detection, persistence, rules, layouts, or other application integrations.

The controller plugin must expose SignalRGB's available effects and layouts through the shared
catalog contract where supported and apply the user's effect with provider rendering authority.
An explicit layout is optional: omission retains SignalRGB's existing LED placements. While
active, SignalRGB's effect/layout wins and the controller must not run its native renderer on
the same owned devices. Catalog discovery and application success must be reported honestly;
do not invent resources or treat a submitted URL as proof of visible output.

SignalRGB already outputs to WLED. Its authority supersedes app rendering, placements, routing,
and calibration on managed devices; a separate WLED plugin is used after release for native
tests. Optional layout export supplies placement data only and does not grant device ownership.

## Layout

```text
effects/    active custom lightscripts; currently empty
plugins/    active SignalRGB user device plugins; currently empty
templates/  starters that are not installed automatically
examples/   integration examples that are not installed automatically
archive/    retained experiments that are not part of the live setup
tools/      SignalRGB install, Canvas API, and MCP launch utilities
docs/       SignalRGB reference and capability research
```

Only files deliberately placed in `effects/` or `plugins/` are installed into SignalRGB.
The research directory remains separate from the controller runtime and published plugin SDK.

## Where SignalRGB looks

| Content | Path |
|---|---|
| Custom effects | `%USERPROFILE%\Documents\WhirlwindFX\Effects\` |
| User plugins | `%USERPROFILE%\Documents\WhirlwindFX\Plugins\` |
| Components | `%USERPROFILE%\Documents\WhirlwindFX\Components\` |
| Built-ins | `%LOCALAPPDATA%\VortxEngine\app-<ver>\Signal-x64\` |
| Logs | `%LOCALAPPDATA%\VortxEngine\app-<ver>\Signal-x64\Logs\` |

The installer detects redirected Documents folders such as OneDrive. User plugins override
built-in plugins with the same VID/PID and survive SignalRGB updates.

## Content workflow

1. Check the [recorded limitations](../../docs/integration-evidence.md). Available features
   depend on the effect, device, account, and interface.
2. Copy a starter from `templates/` or `examples/` into `effects/` or `plugins/` only when it
   should become active SignalRGB user content.
3. From the repository root, run:

   ```powershell
   research\tools\install-signalrgb-content.ps1
   ```

   Add `-Watch`, `-Effects`, or `-Plugins` when needed.
4. Restart SignalRGB for a new effect file. Existing effects reload when reselected; plugins
   reload after device reconnect or SignalRGB restart.

## Canvas API example

Effects receive external events at:

```text
POST http://localhost:16034/canvas/event?sender=<app>&event=<event>
```

```powershell
research\tools\send-signalrgb-canvas-event.ps1 -Sender demo -Event hit
python research\tools\send-signalrgb-canvas-event.py demo hit
```

[`examples/signalrgb-canvas-event-demo.html`](examples/signalrgb-canvas-event-demo.html)
handles `hit`, `heal`, `low`, and `idle`. Copy it into `effects/`, install it, and select it
manually before sending events.

## MCP launcher

`tools/signalrgb-mcp.cmd` resolves the newest installed SignalRGB version instead of pinning
an `app-*` directory. The executable was removed from SignalRGB 2.5.80, so the tool is retained
only for reproducing older-version observations.

## References

- [Slice 14 capability investigation](docs/capability-investigation-2026-10-18.md)
- [Developer cheat sheet](docs/developer-cheatsheet.md)
- [SignalRGB developer hub](https://docs.signalrgb.com/developer/)
- [Lightscripts](https://docs.signalrgb.com/developer/lightscripts/)
- [Game/app integrations](https://docs.signalrgb.com/developer/lightscripts/creating-dev-integrations/)
- [Plugin exports](https://docs.signalrgb.com/developer/plugins/plugin-exports/)
- [Plugin tutorial](https://docs.signalrgb.com/developer/plugins/tutorial/)

