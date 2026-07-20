---
type: TestStrategy
title: Silk test strategy
description: xUnit v3 test layout, categories, coverage, and local commands for the Silk projects.
tags: [testing, xunit, coverage, silkcore, silktoolkit, silkassimp]
timestamp: 2026-07-20T00:00:00+02:00
---

# Silk Test Strategy

The solution contains one xUnit v3 project per production assembly:

* `Source/SilkCore.Tests`
* `Source/SilkToolkit.Tests`
* `Source/SilkAssimp.Tests`

Tests use only xUnit assertions. `Unit`, `Warp`, and `Wpf` tests run by default. `Hardware` and `DX12` smokes are explicit and remain opt-in so the normal suite is deterministic on machines without suitable hardware.

## Local Commands

Build and run the standard suite:

```powershell
dotnet build Source\SilkToolkit.slnx
dotnet test Source\SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=DX12"
```

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
