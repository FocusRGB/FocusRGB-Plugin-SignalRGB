# FocusRGB SignalRGB plugin

<img src="assets/logo-128.png" alt="FocusRGB logo" width="128">

This repository contains the optional SignalRGB output plugin for
[FocusRGB](https://github.com/FocusRGB/FocusRGB). It is independently buildable and consumes the
published `FocusRgb.PluginSdk` package; it is not a submodule and has no source dependency on the
FocusRGB checkout.

Version 0.1 provides:

- SignalRGB process and launcher discovery without pinning an installed version;
- a degraded, read-only catalog of installed bundled and downloaded effects;
- effect selection through SignalRGB's documented application URL while SignalRGB is already
  running; and
- honest `unsupported` results for layout catalog, selection, and export.

The plugin never starts SignalRGB implicitly. Application-URL submission cannot confirm visible
lighting, so a successful request returns `unverified`, not `applied`. Layout omission sends no
layout command and preserves SignalRGB's current placements.

## Build and test

GitHub Packages requires authentication even for public NuGet packages. Set the standard NuGet
source credential environment variable before restoring locally:

<!-- TODO(FocusRGB/FocusRGB#79): remove the MAJ224 feed credentials after the SDK moves to nuget.org. -->

```powershell
$env:NuGetPackageSourceCredentials_github = 'Username=MAJ224;Password=<GitHub token>'
dotnet restore FocusRGB-Plugin-SignalRGB.slnx
dotnet build FocusRGB-Plugin-SignalRGB.slnx -c Release --no-restore
dotnet test --solution FocusRGB-Plugin-SignalRGB.slnx -c Release --no-build
```

The token needs `read:packages`. CI receives narrowly scoped package access through its repository
`GITHUB_TOKEN`.

## Package permissions

The manifest declares the resources used by the plugin:

- `process.read` checks whether SignalRGB is already running;
- `filesystem.read` discovers the installed launcher and effect files; and
- `process.start` submits an application URL through the launcher only after the running-process
  check succeeds.

Provider research and developer utilities moved from the main repository are retained under
[`research/`](research/) for evidence and reproduction. They are not part of the release package.

Repository agents should read [`AGENTS.md`](AGENTS.md) before making changes.

Assisted-by: OpenAI Codex
