---
type: Build
title: Build environment
description: Build prerequisites and commands for the current SilkToolkit solution.
resource: ../Source/SilkToolkit.slnx
tags: [build, dotnet, powershell]
timestamp: 2026-07-08T00:00:00+02:00
---

# Requirements

| Requirement | Value |
|-------------|-------|
| Shell | PowerShell 7+ |
| SDK | .NET SDK `10.0.0` with `rollForward: latestMajor` |
| Language version | `10.0` from `Source/Directory.Build.props` |
| Solution format | `.slnx` |
| Main solution | `Source/SilkToolkit.slnx` |

# Build

## Rider MCP primary route

Before a build, probe `rider_get_solution_projects` with `rootFolder: "F:/Repositories/helix-toolkit/Source"` and require the SilkToolkit solution context. If the probe succeeds, call `rider_build_solution_start` with `rebuild: false` and poll `rider_build_solution_state` with the returned `sessionId` until it reaches a terminal state. A started build with `buildIsSuccess: false` is a real build failure and must not be hidden by a second backend.

## CLI fallback

Use the `dotnet` command only when the Rider MCP is unavailable or cannot resolve the solution context:

```powershell
dotnet build Source\SilkToolkit.slnx
```

## Notes

The root `global.json` pins SDK `10.0.0` and allows latest-major roll-forward. `Source/Directory.Build.props` sets `LangVersion` to `10.0`, disables assembly signing, and suppresses several repository-wide warnings. Rider paths are relative to the solution root; ReSharper paths are absolute and require `solutionName: "SilkToolkit"` because multiple solutions may be open.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [OKF Specification](/references/okf-spec.md)

# Citations

[1] [global.json](../global.json)
[2] [Source/Directory.Build.props](../Source/Directory.Build.props)
[3] [Repository agent instructions](../AGENTS.md)
