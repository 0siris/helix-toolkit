# Knowledge Bundle Update Log

## 2026-07-08

* **Initialization**: Created the OKF bundle under `knowledge/`.
* **Creation**: Added repository, build, architecture, project, and OKF specification concepts.

## 2026-07-09

* **Update**: Changed ShaderBuilder knowledge to document `JeremyAnsel.HLSL.Targets` as the stable DX11 `.cso` path and the additive `SilkToolkit.ShaderBuild` DXC/SM6 `.dxil` targets path.
* **Update**: Added ShaderBuilder DXC inventory behavior for `DxcCompileAll=true` and incremental `.dxil` output.
* **Update**: Added generated `.dxil` resource handoff from ShaderBuilder into SilkCore `Resources\DX12`.
* **Update**: Added SilkCore embedded DXIL loading through `UWPShaderBytePool.ReadDxil`.
* **Update**: Enabled full ShaderBuilder DXIL generation during normal builds via `DxcCompileAll=true`.
* **Update**: Added isolated SilkCore Direct3D12 native device/command/fence/root-signature foundation.
