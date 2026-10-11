# SilkToolkit

[![Silk tests](https://github.com/0siris/helix-toolkit/actions/workflows/silk-tests.yml/badge.svg?branch=silk-toolkit)](https://github.com/0siris/helix-toolkit/actions/workflows/silk-tests.yml)

SilkToolkit is a Direct3D 12 3D toolkit for WPF on .NET 10, built on
[Silk.NET](https://github.com/dotnet/Silk.NET) instead of legacy SharpDX.
It is developed from the Helix Toolkit codebase toward a fully native,
dependency-light renderer: explicit DXC-built shaders (DXIL-only), a single
productive D3D12 presentation path, and Silk.NET math throughout.

## Solution layout

All projects live under `Source/` in `SilkToolkit.slnx`:

| Project | Purpose |
| --- | --- |
| `SilkCore` | Core renderer, scene graph, geometry, materials, D3D12 backend |
| `SilkToolkit` | WPF-facing controls, elements, camera, demos |
| `SilkAssimp` | Assimp-based model import/export |
| `SilkToolkit.Native.ShaderBuilder` | HLSL sources built to DXIL via DXC at build time |
| `SilkCore.Tests` / `SilkToolkit.Tests` / `SilkAssimp.Tests` | xUnit v3 suites |

`External/Logging` is a git submodule and must be checked out
(`git submodule update --init --recursive`); CI fetches it automatically.

## Build

```powershell
dotnet build Source\SilkToolkit.slnx
```

## Tests

```powershell
dotnet test Source\SilkToolkit.slnx --no-build --filter "Category!=Hardware"
```

`Hardware` and `DX12` test categories are opt-in. Every push and pull
request runs the full gate on Windows via `.github/workflows/silk-tests.yml`.

## Branch

Active development happens on `silk-toolkit`. Upstream Helix Toolkit state
is tracked on the `original` remote (`develop3`) for cherry-picking fixes.

## Knowledge bundle

Agent-facing repository knowledge lives in `knowledge/` (see `AGENTS.md`);
`graphify-out/` holds generated analysis output and must not be edited by hand.
