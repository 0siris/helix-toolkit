---
type: Project
title: SilkToolkit.Native.ShaderBuilder
description: Shader build support project used by SilkCore.
resource: ../../Source/SilkToolkit.Native.ShaderBuilder/SilkToolkit.Native.ShaderBuilder.csproj
tags: [project, shaders, hlsl]
timestamp: 2026-10-16T00:00:00+02:00
---

# Project

`SilkToolkit.Native.ShaderBuilder` targets `netstandard2.0` and provides HLSL shader inputs for [SilkCore](/projects/silkcore.md).

# Dependencies

The project imports the repository-local `SilkToolkit.ShaderBuild` targets and uses the build-only NuGet-pinned
`Microsoft.Direct3D.DXC` package, version1.9.2607.13. Its123 `DxcShader` items declare explicit stage-specific SM6.0
profiles. Entry-point and output-name metadata retain distinct compiled variants, including depth-peeling entries.

DXC outputs track all local HLSL/HLSLI sources and includes, the consuming/imported projects and target,
the compiler executable and its DXIL/compiler runtime DLLs. An intermediate input stamp records evaluated flags,
entry points, profiles, output aliases and the dependency set; it is rewritten only when those values change.
Include-only, option, entry-point and tool/metadata edits invalidate the real compiler batches. Unchanged builds
skip them. The standalone headless regression is `Source/SilkToolkit.ShaderBuild/Tests/test_incremental.py` and
uses the actual current DXC through this target in an isolated fixture.

The copy target collects generated `.dxil` files into `SilkCore/Resources/DX12` and writes the shader manifest.
SilkCore embeds those resources during its normal build; the runtime reader resolves the declared stage/name/
entry-point identity. These compiler/resource checks do not certify every shader body or native GPU behavior.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkCore](/projects/silkcore.md)

# Citations

[1] [SilkToolkit.Native.ShaderBuilder.csproj](../../Source/SilkToolkit.Native.ShaderBuilder/SilkToolkit.Native.ShaderBuilder.csproj)
[2] [SilkToolkit.ShaderBuild.targets](../../Source/SilkToolkit.ShaderBuild/SilkToolkit.ShaderBuild.targets)
