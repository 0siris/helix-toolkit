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

```powershell
dotnet build Source\SilkToolkit.slnx
```

# Notes

The root `global.json` pins SDK `10.0.0` and allows latest-major roll-forward. `Source/Directory.Build.props` sets `LangVersion` to `10.0`, disables assembly signing, and suppresses several repository-wide warnings.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [OKF Specification](/references/okf-spec.md)

# Citations

[1] [global.json](../global.json)
[2] [Source/Directory.Build.props](../Source/Directory.Build.props)
[3] [Repository agent instructions](../AGENTS.md)
