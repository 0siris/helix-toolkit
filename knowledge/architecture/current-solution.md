---
type: Architecture
title: Current SilkToolkit solution
description: Main solution structure and project relationships for Source/SilkToolkit.slnx.
resource: ../Source/SilkToolkit.slnx
tags: [architecture, solution, silktoolkit]
timestamp: 2026-07-08T00:00:00+02:00
---

# Solution

`Source/SilkToolkit.slnx` is the current solution file in this workspace. It includes four main library/support projects and example applications.

| Project | Target framework | Role |
|---------|------------------|------|
| [SilkCore](/projects/silkcore.md) | `net10.0` | Core renderer and runtime library. |
| [SilkToolkit](/projects/silktoolkit.md) | `net10.0-windows` | Windows/WPF-facing toolkit layer. |
| [SilkAssimp](/projects/silkassimp.md) | `net10.0` | Assimp model loading integration. |
| [ShaderBuilder](/projects/shaderbuilder.md) | `netstandard2.0` | HLSL shader build support. |

# Project Graph

```text
SilkToolkit.Native.ShaderBuilder
        ^
        |
SilkCore <--- SilkToolkit
    ^
    |
SilkAssimp
```

Examples under `Source/Examples/SilkCore` and `Source/Examples/SilkToolkit` reference the main projects directly.

# Related Concepts

* [Repository](/repository.md)
* [Build](/build.md)

# Citations

[1] [Source/SilkToolkit.slnx](../../Source/SilkToolkit.slnx)
[2] [SilkCore project](../../Source/SilkCore/SilkCore.csproj)
[3] [SilkToolkit project](../../Source/SilkToolkit/SilkToolkit.csproj)
[4] [SilkAssimp project](../../Source/SilkAssimp/SilkAssimp.csproj)
[5] [ShaderBuilder project](../../Source/SilkToolkit.Native.ShaderBuilder/SilkToolkit.Native.ShaderBuilder.csproj)
