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

The project references `SilkToolkit.Native.ShaderBuilder` with private assets. It uses Silk.NET packages for Direct2D, Direct3D compiler support, Direct3D11, DXGI, and maths support.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkToolkit](/projects/silktoolkit.md)
* [ShaderBuilder](/projects/shaderbuilder.md)

# Citations

[1] [SilkCore.csproj](../../Source/SilkCore/SilkCore.csproj)
