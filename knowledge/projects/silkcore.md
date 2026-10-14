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
