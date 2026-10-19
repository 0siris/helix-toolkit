# Knowledge Bundle Update Log

## 2026-10-19 — WPF composition Phase 2 verified

- Integrated the shared native frame recorder and bounded demand coordinator through the actual HWND viewport.
- Reused scene preparation/cleanup storage and delegates; idle, coordinator and two-node preparation measure zero
  warmed allocations. Actual submitted mesh frames measure about 1.75 KB/frame under a tightened 2 KB ceiling.
- Independent reviews found and resolved candidate-list aliasing and failed-recording recovery; final delta has no
  unresolved blockers. The Rider build passes; all 57 new cases pass, with 674/682 standard cases passing and the
  same eight baseline failures. Production WPF and public default-false selection remain later phases.
- Graphify regenerated 1,021 files into 9,841 nodes, 17,265 edges and 2,884 communities, with zero model tokens;
  the existing 17 partial parser results remain disclosed. Phase 3 awaits explicit approval.

## 2026-10-19 — Phase 2 renderer and allocation contracts

- Approved the shared borrowed frame-target boundary and separate render-demand coordinator.
- Made low managed heap pressure an explicit implementation and independent review requirement, with
  warm-state allocation measurements and separate accounting for initialization, growth and capture.

## 2026-10-19 — WPF composition proof started

- Linked the approved phased composition plan and its historical review findings.
- Fast-forwarded to the requested local toolkit baseline and corrected four example import/namespace blockers.
- Rider build passes; standard baseline tests retain eight failures (617 passed / 625 total), documented separately
  from the new interoperability proof. No renderer integration or application changes are part of Phase 1.

## 2026-10-19 — WPF composition Phase 1 verified

- Proved D3D12 NT-shared BGRA through D3D11 GPU copies into two alternating D3D9Ex legacy-shared surfaces.
- All three explicit GTX 1070 tests pass, including actual WPF desktop RGB/alpha, immutable attached-image
  snapshots, resize/recreation and disposal. Independent integration and final-delta reviews found no blockers.
- Final standard suite preserves exactly the eight baseline failures (617/625 passed); no new failing test names.
- Graphify completed after final source formatting: 1,013 files, 9,691 nodes, 16,858 edges, 2,847 communities,
  zero model tokens. Existing 17 partial parser results remain disclosed. Phase 2 awaits explicit approval.

## 2026-10-18 — Shared viewport widget overlay

* Moved the existing WPF/Win32 widget host into SilkToolkit with its public namespace and class body retained.
* Added the required explicit System import; Desktop forwards its former public assembly identity.
* Managed boundary test and all four unchanged native overlay tests pass. Original Helix binds eleven widget slots.
* Updated project concept and index; no alternative renderer or preprocessor switch added.

## 2026-10-18 — Empty geometry startup pass regression

* Reproduced the visible undefined-topology exception with an attached empty mesh and valid material.
* Reject ordinary empty buffers before pass creation; retain attachment, scene maintenance and later real drawing.
* Verified seven Core/GPU and six hidden viewport-host cases; visible Original startup repeated successfully.
* Updated project knowledge; WPF widget overlay adoption remains a separate integration item.

## 2026-10-18 — Application vertex and transparency boundaries

* Added native application buffer/vertex overrides and optional source material pass selection.
* Restored FOV and DefaultVRML source access without replacing existing storage or factories.
* Verified 71 targeted Core/transparency cases and 14 namespace/palette cases; Original application readback pending.
* Documented native test-friend access and updated project concepts/index.

## 2026-10-18 — Original host contracts on the native frame

* Exposed the actual viewport host, renderer-prepared hierarchy and fence-safe retained frame context.
* Preserved original/current name-based WPF bindings and native intersection subclass reuse.
* Headless checks: 29 Toolkit compatibility cases and nine Core hierarchy/GPU cases pass.
* Updated project concepts and index; full application and visible acceptance remain open.

## 2026-10-18 — Restore original public CLR and XAML namespaces

* Restored original toolkit type namespaces from the selected original Core/WPF/Assimp project imports.
* Restored original acronym spelling for 32 types; native Silk-only types and Silk assembly names remain.
* Updated source consumers, examples, global imports and templates mechanically; no wrapper type hierarchy
  or new preprocessor switches were introduced. No renderer algorithm or shader body was replaced.
* Thirteen headless CLR/XAML schema compatibility cases pass. MeshDesktop builds against the restored types.
* All three toolkit test projects compile. Assimp has 76 passing cases and two native glTF-key assertions that
  fail before any toolkit importer is called; those prerequisite failures are recorded separately.
* Original application member compatibility and rendering acceptance continue as a separate integration stage.

## 2026-10-18 — Attached 2D startup invalidation

* A visible Desktop startup reproduced a stack overflow in mutually recursive render/visual invalidation.
* Dirty-state propagation now terminates without re-entering layout; both attached-panel regression cases pass.
* Desktop rebuild succeeds; visible startup is repeated separately from treatment loading.

## 2026-10-18 — Remote branch integration

* Merged origin/silk-toolkit at 7344b9cf, retaining local renderer and ownership fixes.
* Adapted independent animation/PBR import and regression fixture types to SharpAssimp 6.0.12.
* Preserved profiled opaque rendering alongside the remote wireframe overlay and valid empty SSAO scenes.
* Desktop and affected import-test compilation are checked separately; test execution and native acceptance remain deferred.
* Imported model fixtures remain byte-identical to upstream, including their existing trailing whitespace.

## 2026-10-18

* **Fix**: Native preparation finishes hierarchy updates, calls existing non-render maintenance on living attached
  nodes including hidden descendants, then selects visible draws. Existing geometry/group/instance/batch hooks
  remain authoritative. Prepared hit-test octrees survive unchanged frames; non-hit-testable gingiva is excluded.
  In-flight frame skips run neither scene updates nor maintenance.
* **Fix**: Actual immutable selected-pair GPU inputs can attach and render without CPU line geometry. Applications
  retain genuine input bounds; ordinary geometry validation remains unchanged. WARP readback covers contact,
  independent poses, coarse false positives and additive XRay with indirect output.
* **Verification**: Final ordinary gates pass 431 SilkCore and 70 Toolkit cases. Hidden queued right-drag checks
  cover actual geometry/background camera movement before release, dispatcher liveness and outside release without
  capture changes. Parent affected Desktop selection passes 232 cases, with one explicit visible case skipped.
  ReSharper inspected the containing MeshMeister solution; the standalone Silk Rider context remains unavailable.
  XML/whitespace checks pass; generated source graph has 9,411 nodes and 16,761 edges with 17 existing parser limits.

* **Fix**: Transient mesh, line and full-screen effect draws share renderer-owned descriptor heap pages, leaving
  the ordinary scene budget intact. Each draw binds its actual heap pair, including intersection graphics after
  compute. Original glow6, border and XRay retain immutable per-draw constants and completed-fence reuse.
* **Fix**: Ordinary WPF composition polls one in-flight frame rather than waiting for its fence on the dispatcher.
  Explicit captures remain synchronous; resize and effect-catalog replacement defer resource changes safely.
  Completion notifications distinguish genuine completed empty frames from skipped ticks. Deferred resize errors
  use the existing renderer error boundary, and detached lifetime notifications are discarded after draining.
* **Verification**: Headless tests cover two-page selection pressure beside 100 ordinary meshes, native colors,
  depth/stencil preservation, fresh intersection compute and a deliberately gated WARP queue. The dispatcher and
  camera remain live while scene updates and binding reuse wait for completion. No visible window is opened.
  All 426 non-Hardware SilkCore and 68 non-Hardware Toolkit cases pass. Parent Desktop integration passes 241
  cases with one explicit visible case skipped. Affected builds, changed XML and whitespace checks pass.
* **Maintenance**: ReSharper inspects the actual containing MeshMeister solution. The standalone Silk Rider context
  is not open, so affected builds use CLI fallback. Existing style/analyzer warnings remain, including conflicting
  inherited/private-field naming policies. The source graph was regenerated with 9,378 nodes and 16,974 edges;
  its 17 existing parser limitations remain distinct from successful C# builds and tests.

## 2026-10-17

* **Fix**: Filled material passes now use the owning mesh rasterizer state, including front-face convention,
  culling, fill and depth bias, in opaque and both transparency paths. Cache keys retain each state independently.
  This restores correct face-dependent custom shader lighting without changing shaders or material parameters.
* **Verification**: Three actual WARP culling regressions fail before the fix; all 32 selected native raster,
  wireframe, transparency, preparation, SSAO and post-effect tests pass. The overlay test now expects culling
  to hide both filled geometry and edges. Hardware Gingiva readback independently confirms front-face lighting.
* **Maintenance**: ReSharper inspected the containing MeshMeister solution; changed production statements have no
  warnings. The source graph was regenerated (9,287 nodes, 16,657 edges); its 17 pre-existing parser limitations
  remain separate from successful C# builds and tests. No visible window was opened.

* **Fix**: Reference-counted pool disposal callbacks retain the inserted key, serialize dictionary mutation and
  remove only their own resource. An old detached resource no longer evicts a newer entry under the same key.
* **Verification**: Two deterministic replacement regressions fail before the change; five pool regressions and
  the existing texture/resource selection pass afterward (141 CPU-only cases). Public signatures and automatic
  disposal policies are preserved. Rider production-file formatting and semantic inspection use the actual
  containing MeshMeister solution; the standalone Rider build endpoint is unavailable, so builds/tests use CLI.

## 2026-10-14

* **Fix**: Preserve direct geometry-child transforms in exported Assimp nodes and assign contiguous indices to
  actual geometry/material pairs instead of indexing the separate geometry cache. Skipped material-less geometry
  and shared geometry with multiple materials no longer produce invalid native mesh references.
* **Verification**: All 15 non-hardware SilkAssimp tests pass, including two targeted hierarchy/index regressions.
  Actual archived-treatment STL/Collada export and scene replacement pass with 1842 native frames and zero errors.
  Graphify regeneration was attempted again and remains blocked by the missing LLM API key.

* **Update**: Release cached resources for disposed mesh, point/line, volume, particle and screen-capture cores
  at the existing completed-fence frame boundary, preserving live owners and reusing descriptor ranges.
* **Verification**: Bounded allocator/ownership regression and descriptor tests pass. Actual desktop treatment
  replacement with archived geometry passes with 2032 GPU frames and zero render errors.
* **Limits**: The non-hardware SilkCore gate has 315 passing tests and two failures in unchanged shader-count
  assertions (103 expected, 123 embedded). Graphify regeneration is blocked by a missing LLM API key;
  no generated artifacts were edited. Rider/ReSharper tools were unavailable, so verification used the CLI.

## 2026-10-11

* **Backlog**: Added `/backlog/addtube-reimplementation.md` tracking the port of upstream #2251
  (`MeshBuilder.AddTube` reimplementation) to Silk.NET math. GitHub Issues are disabled in this
  repository, so the item (problem, upstream reference, scope, acceptance) lives in the knowledge
  bundle for later pickup.

## 2026-09-10

* **Update**: Migrated all 38 `ValidSphere` call sites to the `0.3.1-preview` API (`GuardNotNull()` →
  `AsGuardNotNull()`, `AssertNotNull(...).Value` → `AsNotNull(...)`); the `*-*` float in the five consumers
  resolves `0.3.1-preview`, and `Guard()`/`Is()`/`Range()`/`Satisfy()`/`AssertException` are unchanged.
* **Verification**: Zero-error solution build and the 361-test `Category!=Hardware` gate pass unchanged
  (13 SilkAssimp + 296 SilkCore + 52 SilkToolkit), preserving the `ArgumentNullException`/`AssertException`
  failure contracts.

## 2026-09-08

* **Update**: Removed the `External/Assertions` git submodule and switched all five consumers
  (`SilkCore`, `SilkToolkit`, `SilkAssimp`, `SilkCore.Tests`, `CrossSectionDemo`) to the `ValidSphere`
  `0.2.0-preview.4` NuGet package; dropped the subproject from `Source/SilkToolkit.slnx`.
* **Verification**: Zero-error solution build and the 361-test `Category!=Hardware` gate pass unchanged,
  proving the package covers the used `GuardNotNull`/`AssertNotNull`/`Guard().Range`/`Is().Satisfy` surface.

## 2026-09-07

* **Fix**: Restored DX12 cross-section clipping by uploading the eight plane controls and cutting operation to the
  shared `b6` buffer, and initialized the default labelled ViewCube texture during DX12 technique resolution.
* **Update**: Advanced `External/Assertions` to ValidSphere `v0.1.0-preview.1`, added it to the solution, and migrated
  guards and invariant assertions to the current fluent API while preserving exception contracts.
* **Verification**: Rider build and all 346 `Category!=Hardware` tests pass; the `CrossSectionDemo` live smoke visibly
  clips the model and renders the colored, labelled ViewCube. Graphify reports 940 files, 12,106 nodes, 19,202 edges,
  and 3,055 communities.
* **Fix**: Corrected `DynamicPointsAndLines` to create `2 * (n - 1)` line indices and synchronized a late-bound
  viewport effects manager into the DX12 presentation surface, eliminating its line-index and overlay-billboard
  render exceptions.
* **Fix**: Completed the odd-sized point-color stream in `DynamicTextureDemo`, preserving the shared validation
  that rejects genuinely incomplete geometry streams.
* **Verification**: Rider build, ReSharper diagnostics, the 344-test standard gate, and a ten-second
  `DynamicPointsAndLines` and `DynamicTextureDemo` startup/render smokes pass. Graphify now reports 940 files,
  12,058 nodes, 19,134 edges, and 3,049 communities.
* **Verification**: Repeated the final solution build, 344-test `Category!=Hardware` gate, six-demo startup/render
  smoke, source-name hygiene scan, and `git diff --check`; all automated closure gates remain green. The visible
  hardware interaction and consented live Desktop Duplication checks remain manual.

## 2026-09-06

* **Update**: Completed the automated Phase 5 DX12-only source cutover by removing the remaining native D3D11
  handle, shader, state, view, resource, material-variable, pool, and `DeviceContextProxy` paths while retaining
  the isolated Desktop Duplication source and data-only public descriptions.
* **Update**: Restored D3D11-free billboard/text-atlas generation through the WPF software bitmap path, prepared
  text billboards before DX12 upload, treated empty billboards as zero-draw geometry, and matched post-process and
  depth-peeling resources to the presentation texture format.
* **Verification**: The solution builds with zero errors; the `Category!=Hardware` gate passes 344 tests; six
  central demos complete clean five-second startup/render smokes without error diagnostics; direct D3D11 source
  use is confined to `Native/D3D11DesktopCaptureSource.cs`.
* **Verification**: Refreshed the 940-file Graphify database to 12,054 nodes, 19,154 edges, and 3,045 communities.
  Visible presentation, resize/input behavior, and interactive Desktop Duplication remain manual gates.

## 2026-08-26

* **Update**: Isolated Desktop Duplication from the shared D3D11 device wrapper and removed unused DXGI handles,
  legacy OIT/depth-peeling/SSAO render orchestrators, never-created D3D11 resource-pool implementations, and the
  closed `SharpDX.Toolkit.Graphics.Texture*` D3D11 GPU island while retaining CPU image/DDS/WIC decoding.
* **Update**: Removed the four public legacy `DeviceContextProxy` render dispatches from `SceneNode` and the
  corresponding empty dynamic-cube-map dispatch loops, while retaining the productive DX12 traversal.
* **Update**: Reconciled the resumable Phase 5 plan with the committed cutover state: closed completed WPF and
  DX12 resource work, split completed presentation removal from the remaining `RenderCore` boundary, listed all
  eight legacy D3D11 handle files, and aligned the standard gate with the current `Hardware`/`DX12` opt-in rule.
* **Verification**: Recorded the 321-warning/zero-error solution build across all 43 examples, the green 338-test
  repository standard gate and 29 focused cutover contract cases, and the remaining nine-file D3D11 source
  inventory before the matching `RenderCore` boundary is removed; refreshed the 986-file Graphify database to
  12,742 nodes, 21,970 edges, and 2,518 communities.

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
* **Update**: Advanced Phase 5 to one WPF HWND/DX12 `Viewport3DX` path; removed legacy D3D11 hosts, render
  buffers, D3DImage/WinForms presentation, `IRenderHost`, and obsolete WinForms/off-screen examples. DX12 now
  owns screenshot readback, custom scene-node technique selection, and WARP presentation tests. Desktop
  Duplication now owns its raw lazy D3D11 device/context/staging handles without the renderer device factory.
* **Verification**: The zero-error solution build covers all 43 SilkToolkit examples. The standard suite now
  excludes only hardware and passes 308/308 before the final capture-island lifecycle additions. Reflection
  contracts keep removed host/canvas APIs absent, capture conversion covers padded RGBA-to-BGRA rows, and
  Graphify refreshed 1,007 sources to 12,908 nodes, 22,694 edges, and 2,351 communities.
* **Update**: Replaced the split legacy/DX12 `RenderCore` attachment state with one canonical lifecycle and removed
  its D3D11 technique, device, update, and render boundary plus the derived legacy implementations. Public
  `SceneNode.Detach()` now also releases cores attached by the DX12 traversal.
* **Verification**: The solution builds with zero errors; 33 cutover contracts, 102 DX12 runtime checks, and all
  342 standard tests pass. Graphify refreshed 986 sources to 12,647 nodes, 21,496 edges, and 2,510 communities;
  the interactive hardware capture remains intentionally excluded.

## 2026-10-15 — Native intersection generations and offline source graph

- Added the genuine selected-pair/shared-buffer D3D12 compute, append and indirect line path using existing
  renderer owners and fence-safe resource retirement. Headless WARP/complete SilkCore checks pass323.
- Corrected two stale103-resource expectations to the already declared123 shader package, retaining exact
  manifest/readability checks and requiring the consumed intersection resources.
- The source graph builder now calls deterministic AST/build/cluster/report APIs directly, without an LLM
  credential preflight or process pool. Fresh graph:960 prepared files,9,191 nodes,21,132 edges,465 communities,
  zero model tokens;17 source files remain partially parsed. Generated files were rebuilt by the script.
- Initial CLI failed without a backend key. A later standard32-worker run was explicitly stopped; the bounded
  single-process run succeeded and cleaned its verified staging directory. No source was sent to an LLM.


## 2026-10-16 — DXC include and metadata invalidation

* Fixed the existing shader target's Common-only dependency list: local HLSL/HLSLI, imported project/target and
  compiler/runtime inputs now invalidate output, including PS helper changes. A write-only-when-different stamp
  tracks evaluated compile options, stage/entry/output metadata and include additions/removals.
* Actual DXC fixture reproduces the old include-only miss and passes13 corrected controls with genuine skipped
  compiler batches on unchanged builds. No generated product DXIL is manually edited; SM6 profiles/resources,
  current entry aliases and the public runtime contract remain unchanged.
* Updated ShaderBuilder concept/index/log. Rider MCP tools are unavailable in this session; affected verification
  uses the CLI. Full shader semantics/GPU/visible/cold application acceptance remains explicitly separate.

* **Graphify verification**: The repository code-only script completed after the target change:973 source files,
  9,125 nodes,16,989 edges and2,489 communities, with0 LLM tokens. Its parser reports17 partially extracted
  source files; this graph is navigation evidence, not complete C# syntax/API certification. Generated artifacts
  were created only by the script; no generated output was manually edited.


## 2026-10-16 — Assimp primitive export and material/resource boundaries

* Corrected line/point face arity, index validation and internally scoped exporter context cleanup, with temporary
  scene animation cleanup. External context ownership and prior hierarchy/material-index corrections remain.
* Corrected independent metallic/roughness import and parent-directory texture-path joining. Native glTF and
  primitive file/blob fixtures exercise actual AssimpNet5beta1; exact internal allocation counters remain unproved.
* Current source graph was last regenerated after the shader-target change; regeneration follows the remaining
  active animation/noise source batch. No generated graph artifact is edited by hand.

## 2026-10-16 — Animation and noise integration

Integrated independent TRS timeline/default/interpolation fixes, restored the mutable source export tick rate,
and fixed signed noise lookup, completed-field normalization and concurrent permutation ownership. Fresh owning
Assimp70 and noise29 cases pass. Shared documentation states source assumptions and acceptance limits.
Graphify regenerated this composed batch: 979 files, 9,218 nodes, 16,366 edges and 2,640 communities, zero model tokens. The existing 17 partial parser results remain disclosed; no visible application or producer was launched.

## 2026-10-17 — Native mouse route and restoring runtime references

- Made the interactive native surface an actual hit-test recipient and consumed wheel default propagation.
- Reused WPF InputManager preview/bubble, tool/camera bindings, click counts and capture with original DIP coordinates.
- Toolkit58 headless, Desktop23 headless and one explicitly demanded VisibleUI boundary check pass; real Desktop
  wheel/selection/outside release and popup click work at175%. Physical touch/SpaceMouse remain unverified.
- Replaced same-version CodeDom/CSharp cache HintPaths with restoring PackageReferences; affected builds pass.
- Graphify regenerated982 source files into9248 nodes/16457 edges/2616 communities, zero model tokens;
  existing17 partially extracted files remain disclosed. Rider/ReSharper were unavailable; CLI fallback used.

## 2026-10-17 — Source two-key dictionary correction

- Restored pair-specific overwrite/indexer/ContainsKey and corrected the original swapped-value equality defect.
- Added original public carriers through GetPairs while retaining existing tuple enumeration/LINQ/bool Remove.
- Parent22 owning headless cases pass; exact three-path source/parent raw replays and unchanged-prefix proof retained.
- Graphify regenerated984 files into9264 nodes/16366 edges/2696 communities with zero model tokens; existing17
  partial parser files remain disclosed. No generated output was hand-edited; Rider/ReSharper were unavailable.

## 2026-10-18 — Scene descriptor paging and external profiler markers

- Reused the existing paged table allocator for ordinary mesh/point-line and immutable peeling bindings, preserving fence ownership.
- Added opt-in official PIX command-list regions, native object names and actual scene/effect/shared descriptor counters.
- Added a headless pressure regression reproducing the original heap-exhaustion exception; visible performance capture is separate.
