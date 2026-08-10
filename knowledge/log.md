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
* **Creation**: Added backlog concept for the second pure-WPF `IRenderCanvas` implementation (`DPFCanvasSwapChain` + `HwndSwapChainHost`) as a switchable alternative to the WinForms-based `DPFSurfaceSwapChain`.

## 2026-07-20

* **Creation**: Added the xUnit v3 test strategy for SilkCore, SilkToolkit, and SilkAssimp, including local commands and non-blocking 70% coverage reporting.

## 2026-08-01

* **Update**: Embedded the upstream OKF v0.2 specification and aligned the repository guidance and bundle index with OKF v0.2.
* **Initialization**: Added the generated graph database workflow for the `knowledge/` bundle.
* **Initialization**: Built the project source knowledge graph with 7,668 nodes, 17,157 edges, and 422 communities; normalized all source paths to `Source/...`.
* **Creation**: Added the reproducible `tools/build_graphify_source.py` builder and its OKF concept documentation.
* **Creation**: Added the phased C#/.NET coding-conventions policy covering LoggerLib, Assertions, analyzer enforcement, tests, and additive library compatibility.

## 2026-08-02

* **Update**: Documented Rider MCP-first build and deterministic test execution with `dotnet` fallback rules, plus ReSharper diagnostics, quick-fix, formatting, and semantic navigation guidance.

## 2026-08-07

* **Update**: Allowed compact property XML documentation and omitted redundant inherited-member documentation in Rider.
