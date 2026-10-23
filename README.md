# FocusRGB SignalRGB plugin

<img src="assets/logo-128.png" alt="FocusRGB logo" width="128">

This repository contains the optional SignalRGB output plugin for
[FocusRGB](https://github.com/FocusRGB/FocusRGB). It is independently buildable and consumes the
published `FocusRgb.PluginSdk` package; it is not a submodule and has no source dependency on the
FocusRGB checkout.

Version 1.0 provides:

- SignalRGB process and launcher discovery without pinning an installed version;
- a degraded, read-only catalog of installed bundled and downloaded effects;
- effect selection through SignalRGB's documented application URL while SignalRGB is already
  running; and
- honest `unsupported` results for layout catalog, selection, and export.

The plugin never starts SignalRGB implicitly. Application-URL submission cannot confirm visible
lighting, so a successful request returns `unverified`, not `applied`. Layout omission sends no
layout command and preserves SignalRGB's current placements.

## Build and test

`FocusRgb.PluginSdk` restores from nuget.org without repository credentials:

```powershell
dotnet restore FocusRGB-Plugin-SignalRGB.slnx
dotnet build FocusRGB-Plugin-SignalRGB.slnx -c Release --no-restore
dotnet test --solution FocusRGB-Plugin-SignalRGB.slnx -c Release --no-build
```

## Package permissions

The manifest declares the resources used by the plugin:

- `process.read` checks whether SignalRGB is already running;
- `filesystem.read` discovers the installed launcher and effect files; and
- `process.start` submits an application URL through the launcher only after the running-process
  check succeeds.

Provider research and developer utilities moved from the main repository are retained under
[`research/`](research/) for evidence and reproduction. They are not part of the release package.

## Releases and discovery

The repository uses the `focusrgb-plugin` GitHub topic so FocusRGB can discover it as an official
plugin. Each stable release publishes the installable zip, a standalone `focusrgb-plugin.json`
for listing metadata, and `SHA256SUMS` covering both files. Pull requests that change packaging
run the same assembly path and upload those three files as a dry-run artifact.


Assisted-by: OpenAI Codex
