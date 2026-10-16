---
type: Project
title: SilkCore
description: Core Silk.NET rendering and runtime project.
resource: ../../Source/SilkCore/SilkCore.csproj
tags: [project, core, silknet]
timestamp: 2026-07-08T00:00:00+02:00
---

# Project

`SilkCore` targets `net10.0` and provides the core rendering/runtime layer used by the toolkit and examples.

# Dependencies

The project references `SilkToolkit.Native.ShaderBuilder` with private assets. It uses Silk.NET packages for Direct2D, Direct3D compiler support, Direct3D11, Direct3D12, DXGI, and maths support.

The Direct3D12 presentation path uses the existing scene render cores through native resource and binding adapters.
Per-core mesh, point/line, volume, particle and screen-capture caches release entries for disposed cores at
`D3D12SceneRenderer.BeginFrame()`, after the presentation host has completed the preceding GPU fence. Live cores
retain their descriptor tables. This prevents repeated scene replacement from exhausting contiguous descriptor
ranges; increasing heap capacity does not replace correct ownership cleanup.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkToolkit](/projects/silktoolkit.md)
* [ShaderBuilder](/projects/shaderbuilder.md)

# Citations

[1] [SilkCore.csproj](../../Source/SilkCore/SilkCore.csproj)
[2] [SilkD3D12DeviceFactory.cs](../../Source/SilkCore/Native/SilkD3D12DeviceFactory.cs)
[3] [D3D12DeviceHandles.cs](../../Source/SilkCore/Native/D3D12DeviceHandles.cs)

# Native mesh intersections

MeshIntersectionRenderCore owns immutable mesh/candidate/pose generations at the toolkit boundary. The existing
D3D12 scene renderer records selected-pair generic and actual shared-buffer compute passes, UAV append counters,
GPU-produced indirect line arguments and normal/XRay material output through the existing device, heaps and
pipeline caches. Generations retire only after the completed presentation fence; disposed cores return their
descriptors through BeginFrame cleanup. Caller arrays and borrowed mesh/buffer owners cannot mutate a published
generation. Shared-buffer mismatch/disposal selects the immutable generic input.

Headless WARP checks read back genuine segments, pair IDs, indirect arguments and render targets; these are no
physical adapter or visible-window acceptance. Core-line compatibility remains selectable in the application;
the source GPU kernel excludes coplanar output. The current full SilkCore suite passes323 cases.

The explicit shader package has123 already declared resources. Strict manifest/count/readability checks include
the selected-pair/shared-buffer compute and collision line shaders; no HLSL or generated DXIL was manually edited.

# Source two-dimensional noise

MathHelper.GenerateNoiseMap retains the four-corner quintic two-dimensional gradient field, frequency 0.5,
amplitude 1 and octave doubling/halving. Signed cells wrap periodically at256 before lookup. A map captures
one freshly published immutable permutation; concurrent reseeding cannot mix tables within that map. Completed
field extrema determine normalization. Constant fields are finite zeros; empty dimensions return empty arrays.
Negative dimensions/counts, pixel overflow and nonfinite sampling products fail explicitly. Finite input
coordinates reduce periodically before integer conversion.29 headless owning cases cover analytic samples,
normalization, arithmetic boundaries and bounded concurrent reseeding. No native presentation is involved.
