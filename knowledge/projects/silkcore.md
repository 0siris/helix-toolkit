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

The current runtime renderer remains DX11-based. The Direct3D12 work is isolated in `Native` helper types (`SilkD3D12DeviceFactory`, `SilkD3D12Device`, command queue/context/fence, and empty root signature creation) so DX12 pipeline work can proceed without destabilizing the existing DX11 render host.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkToolkit](/projects/silktoolkit.md)
* [ShaderBuilder](/projects/shaderbuilder.md)

# Citations

[1] [SilkCore.csproj](../../Source/SilkCore/SilkCore.csproj)
[2] [SilkD3D12DeviceFactory.cs](../../Source/SilkCore/Native/SilkD3D12DeviceFactory.cs)
[3] [D3D12DeviceHandles.cs](../../Source/SilkCore/Native/D3D12DeviceHandles.cs)
