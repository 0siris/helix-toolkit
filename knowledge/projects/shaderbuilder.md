---
type: Project
title: SilkToolkit.Native.ShaderBuilder
description: Shader build support project used by SilkCore.
resource: ../../Source/SilkToolkit.Native.ShaderBuilder/SilkToolkit.Native.ShaderBuilder.csproj
tags: [project, shaders, hlsl]
timestamp: 2026-07-08T00:00:00+02:00
---

# Project

`SilkToolkit.Native.ShaderBuilder` targets `netstandard2.0` and provides HLSL shader inputs for [SilkCore](/projects/silkcore.md).

# Dependencies

The project keeps `JeremyAnsel.HLSL.Targets` as the stable DX11 path for existing `HLSLShader` items and `.cso` output.

The project also imports the repository-local `SilkToolkit.ShaderBuild` MSBuild targets for the DX12 migration path. `DxcShader` items compile to SM6 `.dxil` output through the NuGet-pinned `Microsoft.Direct3D.DXC` package.

The DXC path is additive: it does not replace the existing DX11 `.cso` runtime resources until the DX12 renderer path exists.

`DxcCompileAll=true` builds temporary DXC items from existing `HLSLShader` items for inventory and migration checks. The ShaderBuilder project enables it by default so normal solution builds produce the full DXIL migration set. It maps existing Shader Model 4/5 profiles to Shader Model 6 profiles and writes unique `.dxil` outputs under `$(TargetDir)\DX12`.

DXC outputs are incremental by source shader plus common include files under the ShaderBuilder `Common` folder.

The ShaderBuilder copies generated `.dxil` files into `SilkCore\Resources\DX12`, and SilkCore embeds those generated resources during its normal project build.

SilkCore can read embedded DXIL through `UWPShaderBytePool.ReadDxil(stage, name, entryPoint)`, for example `ReadDxil("PS", "psColor")`.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkCore](/projects/silkcore.md)

# Citations

[1] [SilkToolkit.Native.ShaderBuilder.csproj](../../Source/SilkToolkit.Native.ShaderBuilder/SilkToolkit.Native.ShaderBuilder.csproj)
[2] [SilkToolkit.ShaderBuild.targets](../../Source/SilkToolkit.ShaderBuild/SilkToolkit.ShaderBuild.targets)
