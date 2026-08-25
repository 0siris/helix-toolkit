# Knowledge Bundle Update Log

## 2026-08-25

* **Update**: Completed DX12 migration Phase 4.2 with productive 1D/3D textures, tessellation, shadow depth,
  volume rendering, and GPU compute particles plus focused CPU, contract, and WARP coverage; Phase 4.3 is active.

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

## 2026-08-18

* **Update**: Replaced the DX12 preview backlog with the active, resumable full DX12/DXC cutover plan, including per-phase tests and a mandatory checkpoint.
* **Update**: Completed the DX12 runtime-core phase with deterministic resource, descriptor, barrier, fence,
  WARP readback, and device-recreation coverage; advanced the active checkpoint to WPF presentation.
* **Update**: Completed the automated pure-WPF DX12 presentation phase with HWND, DPI/input, flip-model,
  resize, readback, and repeated lifecycle coverage; advanced the active checkpoint to DXC-only shaders.

## 2026-08-23

* **Update**: Added the first productive 2D `TextureModel` to DX12 upload/SRV bridge with loader lifecycle,
  decoded-image, raw-data, WARP sampling, failure-cleanup, and non-100-percent DPI lifecycle coverage.
* **Verification**: Recorded the green 238-test standard gate and refreshed the 1,036-file Graphify database.
* **Update**: Extended the DX12 texture-model bridge to complete mip chains, arrays, BC1 DDS resources, and
  cube maps with selected-subresource WARP readback and validation coverage.
* **Verification**: Recorded the green 243-test standard gate and refreshed Graphify to 12,541 nodes,
  22,290 edges, and 1,913 communities.
* **Update**: Added shared DX12 geometry, matrix-instance, and texture resource management plus a
  DX11-independent attach/detach and descriptor-bound draw path for existing geometry render cores.
* **Verification**: Recorded the green 247-test standard gate and refreshed the 1,037-file Graphify database to
  12,599 nodes, 22,381 edges, and 1,934 communities.
* **Update**: Added contiguous per-draw DX12 descriptor tables, initialized fallback CBV/SRV/UAV/sampler slots,
  camera/model constant-buffer uploads, and an existing MeshRenderCore WARP draw through the Colors pass.
* **Verification**: Recorded the green 251-test standard gate and refreshed the 1,038-file Graphify database to
  12,639 nodes, 22,481 edges, and 1,943 communities.
* **Update**: Added exact DX12 cbMesh preparation and opaque/tessellation pass selection for existing diffuse,
  Phong, and PBR materials, including TextureModel/SRV/sampler register binding and MeshNode propagation.
* **Verification**: Added a deterministic existing-material diffuse-texture WARP draw, recorded the green
  253-test standard gate, and refreshed Graphify to 12,660 nodes, 22,551 edges, and 1,956 communities.
* **Update**: Added exact DX12 b3 uploads from the existing eight-light model and wired the buffer into
  MeshRenderCore draws.
* **Verification**: Added byte-layout and parameterized Phong/PBR ambient-light WARP coverage, recorded the
  green 256-test standard gate, and refreshed Graphify to 12,666 nodes, 22,568 edges, and 1,945 communities.
* **Update**: Added exact point/line b4 preparation, SceneNode-to-core material propagation, complete descriptor
  bindings, and productive DX12 traversal for existing PointLineRenderCore instances.
* **Verification**: Added parameterized point/line geometry-shader WARP coverage, recorded the green 259-test
  standard gate, and refreshed Graphify to 12,685 nodes, 22,637 edges, and 1,961 communities.
* **Update**: Added DX11-independent CPU preparation, cached geometry/texture ownership, t0/s7 bindings, and
  productive DX12 geometry-shader draws for existing single- and multi-image billboard models.
* **Verification**: Added billboard CPU contracts and a deterministic repository-DXIL WARP readback, recorded
  the green 260-test standard gate, and refreshed Graphify to 12,728 nodes, 22,669 edges, and 1,954 communities.
* **Update**: Added productive DX12 `BoneSkinRenderCore` traversal through shader-equivalent weighted CPU
  transforms, animated buffer updates, material/instance bindings, and a parser-safe split of the legacy buffer
  owners; GPU morph-target and stream-output resources remain explicit follow-up work.
* **Verification**: Added weighted-transform, invalid-index, node/frustum, and translated-mesh WARP coverage,
  recorded the green 262-test standard gate, and refreshed the 1,039-file Graphify database to 12,752 nodes,
  22,737 edges, and 1,978 communities without syntax-recovery warnings.
* **Update**: Extracted the existing opaque/transparent camera-frustum selection into one renderer-independent
  scene-node contract for reuse by the productive DX12 render-host traversal.
* **Verification**: Added real in/out mesh-bound and disabled-culling coverage, recorded the green 263-test
  standard gate, and refreshed the 1,040-file Graphify database to 12,757 nodes, 22,740 edges, and 1,965
  communities.
* **Update**: Added a productive host-lifetime DX12 scene renderer with DX12-only SceneNode attach/detach,
  existing default geometry-model creation, ordered visibility traversal, per-core bindings, and deterministic
  resource cleanup.
* **Verification**: Added a real visible/culled MeshNode WARP traversal with repository-DXIL pixel readback and
  descriptor cleanup, recorded the green 264-test standard gate, and refreshed the 1,041-file Graphify database
  to 12,776 nodes, 22,795 edges, and 1,969 communities.
* **Update**: Integrated the host-lifetime scene renderer and shader-visible heaps into the real WPF HWND
  swap-chain back-buffer, viewport, present, and fence lifecycle.
* **Verification**: Upgraded the three-cycle WPF/WARP surface lifecycle test from clear-only presentation to
  real MeshNode scene recording, kept the 264-test standard gate green, and refreshed Graphify to 12,787 nodes,
  22,824 edges, and 2,004 communities.
* **Update**: Added a host-lifetime DX12 scene-pass catalog that resolves existing mesh, bone, line, point, and
  billboard nodes to their declared technique/pass/topology and caches the matching root signature and PSOs.
* **Verification**: Added direct pass-mapping coverage and exercised automatic resolution in the repeated WPF
  scene lifecycle, recorded the green 265-test standard gate, and refreshed the 1,042-file Graphify database to
  12,804 nodes, 22,846 edges, and 1,968 communities.
* **Update**: Added the owned DX12 D32/S8 presentation depth target, DSV creation/clear/binding, resize-safe
  recreation, declared scene-pass depth/stencil states, and pass-only mesh-material constant-buffer support.
* **Verification**: Upgraded scene traversal to prove reverse-order near/far occlusion on WARP, added invalid
  depth/resource/descriptor/clear contracts, kept the repeated WPF resize lifecycle and all 265 standard tests
  green, and refreshed Graphify to 12,815 nodes, 22,866 edges, and 2,019 communities.
* **Update**: Connected the existing `Viewport3DX` swap-chain option to the productive DX12 surface, composition
  loop, current item scene trees, opaque/transparent order, active camera, physical viewport, and frustum.
* **Verification**: Added exact camera-transform coverage plus a real themed Viewport3DX/WARP scene frame,
  recorded the green 267-test standard gate, converted three parser-incompatible C# 14 accessors without
  behavior changes, and refreshed Graphify without syntax recovery to 12,858 nodes, 22,992 edges, and 2,026
  communities.
* **Update**: Collected existing visible ambient, directional, point, and spot scene nodes into the shared DX12
  light payload and bound the current validated environment cube map through t20/s4 with b3 mip metadata.
* **Verification**: Added light-order/transform/cap CPU coverage plus cube/non-cube WARP resource-lifecycle
  coverage, recorded the green 269-test standard gate, and refreshed Graphify to 12,884 nodes, 23,026 edges,
  and 2,020 communities.
* **Update**: Routed native DX12 child-window mouse and first-contact touch messages through the existing
  viewport hit-test, current-position, mouse-wheel, input-binding, and camera-gesture implementations.
* **Verification**: Added exact configurable-gesture mapping plus real WPF/WARP HWND press/move/release/wheel,
  DIP event, camera zoom/rotation, and touch-filtering coverage; recorded the green 270-test standard gate and
  refreshed Graphify to 12,896 nodes, 23,042 edges, and 2,013 communities.
* **Update**: Added shader-equivalent morph-target offset/pitch/weight preparation before the existing productive
  DX12 CPU bone transform, removing the previous non-zero-morph rejection from `BoneSkinRenderCore`.
* **Verification**: Added multi-target indexing, basis, invalid-payload, and combined morph/bone WARP coverage;
  recorded the green 271-test standard gate and refreshed Graphify to 12,903 nodes, 23,059 edges, and 2,036
  communities.
* **Update**: Added validated D3D12 stream-output target binding and structured buffer SRV creation as the native
  foundation for the existing t40/t60-t62/b9 GPU skinning precompute pass.
* **Verification**: Added WARP coverage for structured ranges, SO resource state/ranges, binding/unbinding, and
  device health; recorded the green 272-test standard gate and refreshed Graphify to 12,906 nodes, 23,064
  edges, and 2,025 communities.
* **Update**: Added per-core b9/t40/t60-t62 bone/morph uploads and a validated, growable stream-output vertex
  allocation while preserving the current CPU-skinned productive draw until the precompute PSO is wired.
* **Verification**: Added compact-payload CPU assertions and WARP byte-layout, validation, state, growth, null-SRV,
  descriptor-cleanup, and device-health coverage; recorded the green 273-test standard gate and refreshed the
  1,043-file Graphify database to 12,927 nodes, 23,117 edges, and 2,041 communities.
* **Update**: Replaced the productive CPU bone/morph preparation with the existing DXIL
  `PreComputeMeshBoneSkinned` point-list stream-output pass, including bone-ID input, the required filled-size
  counter, catalog/scene wiring, and indexed consumption of the generated `DefaultVertex` stream.
* **Verification**: Added focused CPU and WARP coverage for counter validation, pass caching, bone-ID rejection,
  complete GPU-written vertex count, combined morph/bone output readback, and final material rendering; recorded
  the green 274-test standard gate and refreshed the 1,043-file Graphify database to 12,949 nodes, 23,157 edges,
  and 2,054 communities.
* **Update**: Completed DX12 Phase 4.2 with 1D/3D textures, Phong/PBR/tessellation, typed shadow depth,
  volume passes, and GPU compute-particle simulation/drawing through existing repository DXIL.
* **Update**: Completed DX12 Phase 4.3 with resize-safe weighted OIT and depth peeling, SSAO, FXAA, bloom,
  outline, and ordered XRay recording. Fullscreen draws now use frame-local descriptor tables, and DX12 pass
  creation preserves the declared stencil reference instead of silently binding zero.
* **Verification**: Eight focused Phase 4.3 CPU/WARP tests pass for transparency, ping-pong/order, alpha/depth,
  outline, XRay stencil/depth, and finite SSAO output. Full solution, standard-suite, Graphify, Rider, and hardware
  gates remain scheduled after the remaining Phase 4 packages.
* **Update**: Completed DX12 Phase 4.4 by routing existing Scene2D shapes, encoded images, and DirectWrite-shaped
  Unicode text through the repository Sprite2D DXIL pass, with a growable host-lifetime glyph atlas and inherited
  transform/clipping traversal after 3D post-processing.
* **Verification**: Focused unit and WARP checks cover glyph caching and shaping, stable atlas growth, Unicode,
  sibling order, inherited clipping, encoded image sampling, solid geometry, resize, and atlas/resource release.
* **Update**: Completed DX12 Phase 4.5 by isolating lazy D3D11/DXGI Desktop Duplication behind a CPU-owned BGRA
  frame boundary and transferring captured frames into productive DX12 screen-pass textures. Output changes,
  timeouts, failed starts, crop/aspect behavior, and deterministic release are handled without loading D3D11 on
  normal renderer startup.
* **Verification**: The complete DX12 runtime class passes 100/100 automated tests with one explicit interactive
  capture test not run; all 14 DX12 texture-model tests, 303 standard tests, the solution build, Graphify over
  1,053 sources, and `git diff --check` pass. Phase 4 is complete and Phase 5 is the resumable next checkpoint.
* **Update**: Started Phase 5 example cutover by making `DeferredShadingDemo` explicitly select the productive
  DX12 swap-chain surface and represent its missing deferred technique without constructing a legacy D3D11
  effects manager.
* **Verification**: The focused demo build passes with zero errors, and an eight-second hardware startup smoke
  produced a responsive `Deferred Shading Demo` window with empty standard output/error, no D3D11 initialization,
  and no missing-`vsMeshDefault.cso` exception. `RenderDeferred` remains a named `NullTechnique`, so deferred
  shading itself remains unavailable.
* **Update**: Hardened the Phase 5 DX12 example path by treating empty optional mesh texture/color collections as
  absent and by logging every handled `Viewport3DX` render failure through the configured console logger before
  the existing event and viewport-message handling.
* **Verification**: Focused WARP and WPF tests cover empty-stream default upload, partial-stream rejection, and
  console output including exception text. The 305-test standard suite and zero-error solution build pass; a
  ten-second `DeferredShadingDemo` hardware smoke remains responsive with empty standard output/error and no
  viewport exception. Graphify refreshed 1,053 sources to 13,324 nodes, 24,482 edges, and 2,085 communities.
