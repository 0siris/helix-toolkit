---
type: Architecture
title: Current SilkToolkit solution
description: Main solution structure and project relationships for Source/SilkToolkit.slnx.
resource: ../Source/SilkToolkit.slnx
tags: [architecture, solution, silktoolkit]
timestamp: 2026-07-08T00:00:00+02:00
verified: 2026-09-07
---

# Solution

`Source/SilkToolkit.slnx` is the current solution file in this workspace. It includes five main library/support projects, three test projects, and example applications.

| Project | Target framework | Role |
|---------|------------------|------|
| [SilkCore](/projects/silkcore.md) | `net10.0-windows` | Core renderer and runtime library. |
| [SilkToolkit](/projects/silktoolkit.md) | `net10.0-windows` | Windows/WPF-facing toolkit layer. |
| [SilkAssimp](/projects/silkassimp.md) | `net10.0-windows` | Assimp model loading integration. |
| [ShaderBuilder](/projects/shaderbuilder.md) | `netstandard2.0` | HLSL shader build support. |
| ValidSphere | `net10.0` | Runtime guards and invariant assertions via the floating `ValidSphere` NuGet package. |

The `/Tests/` solution folder contains `SilkCore.Tests`, `SilkToolkit.Tests`, and `SilkAssimp.Tests`; each references its matching production project.

# Project Graph

```text
SilkToolkit.Native.ShaderBuilder
        ^
        |
SilkCore <--- SilkToolkit
  ^  ^          ^
  |  |          |
  |  +---- ValidSphere
  |
SilkAssimp
```

Examples under `Source/Examples/SilkCore` and `Source/Examples/SilkToolkit` reference the main projects directly.

# Related Concepts

* [Repository](/repository.md)
* [Build](/build.md)
* [Silk Test Strategy](/testing/silk-tests.md)

# Citations

[1] [Source/SilkToolkit.slnx](../../Source/SilkToolkit.slnx)
[2] [SilkCore project](../../Source/SilkCore/SilkCore.csproj)
[3] [SilkToolkit project](../../Source/SilkToolkit/SilkToolkit.csproj)
[4] [SilkAssimp project](../../Source/SilkAssimp/SilkAssimp.csproj)
[5] [ShaderBuilder project](../../Source/SilkToolkit.Native.ShaderBuilder/SilkToolkit.Native.ShaderBuilder.csproj)
