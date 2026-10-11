# SilkToolkit

[![Silk tests](https://github.com/0siris/helix-toolkit/actions/workflows/silk-tests.yml/badge.svg?branch=silk-toolkit)](https://github.com/0siris/helix-toolkit/actions/workflows/silk-tests.yml)

SilkToolkit is a Direct3D 12 3D toolkit for WPF on .NET 10, built on
[Silk.NET](https://github.com/dotnet/Silk.NET) instead of legacy SharpDX.
It is developed from the Helix Toolkit codebase toward a fully native,
dependency-light renderer: explicit DXC-built shaders (DXIL-only), a single
productive D3D12 presentation path, and Silk.NET math throughout.

## Features

- Direct3D 12 renderer: forward and deferred shading, GPU tessellation
- Screen-space ambient occlusion (SSAO), FXAA, shadow mapping
- Transparency: weighted order-independent transparency (OIT) + depth peeling
- Volume ray casting, GPU particle system, skeletal and morph animation
- Hardware instancing, batched static geometry, octree hit testing
- Transform manipulators, cross-section clipping, X-ray views
- Custom shaders and render techniques (DXC-built DXIL, hot-loadable set)
- Assimp-based model import/export (`SilkAssimp`)
- WPF controls: `Viewport3DX`, camera controller, 40+ demos in the example browser

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

## Architecture sketch

```text
SceneNode graph (SilkCore/Model/Scene)
  -> EffectsManager resolves techniques/passes per material
  -> D3D12ScenePassCatalog selects geometry, shadow, SSAO, OIT passes
  -> SilkD3D12SceneRenderer records draws into command lists
  -> D3D12PresentationSurface presents via the flip-discard swap chain
```

Shaders live in `SilkToolkit.Native.ShaderBuilder` (HLSL) and are compiled
to DXIL by DXC during the build, then embedded into `SilkCore`.
Demos under `Source/Examples/SilkToolkit` (see its README) exercise one
subsystem each; `DemoCore` hosts the shared automation harness.
