---
type: TestStrategy
title: Silk test strategy
description: xUnit v3 test layout, categories, coverage, and local commands for the Silk projects.
tags: [testing, xunit, coverage, silkcore, silktoolkit, silkassimp]
timestamp: 2026-07-20T00:00:00+02:00
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-20T07:15:06Z }
status: stable
sources:
  - resource: ../../AGENTS.md
  - resource: ../../Source/SilkCore.Tests/SilkCore.Tests.csproj
  - resource: ../../Source/SilkToolkit.Tests/SilkToolkit.Tests.csproj
  - resource: ../../Source/SilkAssimp.Tests/SilkAssimp.Tests.csproj
  - resource: wpf-composition-phase4.md
---

# Silk Test Strategy

The solution contains one xUnit v3 project per production assembly:

* `Source/SilkCore.Tests`
* `Source/SilkToolkit.Tests`
* `Source/SilkAssimp.Tests`

Tests use only xUnit assertions. The standard command excludes both `Hardware` and `DX12` categories;
those checks remain opt-in, including WARP cases also marked `DX12`. Other `Unit`, `Warp` and `Wpf` cases
remain eligible under the standard filter.

This 2026-10-20 Phase 5 documentation correction is source/static only; no commands below were run for it.
The dated [Phase 4 results](/testing/wpf-composition-phase4.md) retain their original scope and counts;
parent Phase 5 build, test and runtime gates are pending.

## Local Commands

### Rider MCP primary route

After a successful Rider build, run the deterministic standard suite through `rider_execute_terminal_command` with `rootFolder: "F:/Repositories/helix-toolkit/Source"`, `executeInShell: false`, and:

```powershell
dotnet test SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=DX12"
```

The discovered `SilkCore.Tests`, `SilkToolkit.Tests`, and `SilkAssimp.Tests` Rider configurations do not support dynamic launch overrides, so the terminal route preserves the repository's category filter. Use `rider_get_run_configurations` plus `rider_execute_run_configuration` only for targeted projects or run points.

### CLI fallback

If Rider is unavailable before the test process starts, run the same standard suite from the repository root:

```powershell
dotnet build Source\SilkToolkit.slnx
dotnet test Source\SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=DX12"
```

Do not rerun a started Rider test process through the fallback after a test failure. Both `Hardware` and `DX12`
require a separate opt-in run. Core WARP evidence does not imply support for the WPF bridge, which rejects WARP.

Run explicit smokes locally through the VSTest RunSettings option:

```powershell
dotnet test Source\SilkCore.Tests\SilkCore.Tests.csproj --filter "Category=DX12" -- xUnit.Explicit=only
dotnet test Source\SilkToolkit.Tests\SilkToolkit.Tests.csproj --filter "Category=Hardware" -- xUnit.Explicit=only
```

Collect coverage for one production assembly:

```powershell
dotnet test Source\SilkCore.Tests\SilkCore.Tests.csproj -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura '-p:Include=[SilkCore]*'
```

## Coverage

The GitHub workflow writes separate Cobertura reports for `SilkCore`, `SilkToolkit`, and `SilkAssimp`. It publishes each line rate in the step summary and emits a warning below 70%. The 70% value is a direction, not a build or merge threshold.

Tests must not depend on the repository's absolute path or the full `Models` directory. Temporary files belong in the operating system's temporary directory and must be removed by the creating test.

## Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [DX12 Migration Plan](/backlog/dx12-migration-plan.md)

# Citations

[1] [SilkCore tests](../../Source/SilkCore.Tests/SilkCore.Tests.csproj)
[2] [SilkToolkit tests](../../Source/SilkToolkit.Tests/SilkToolkit.Tests.csproj)
[3] [SilkAssimp tests](../../Source/SilkAssimp.Tests/SilkAssimp.Tests.csproj)
[4] [Silk test workflow](../../.github/workflows/silk-tests.yml)
