---
type: BacklogPlan
title: DX12 and DXC Cutover Plan
description: Resumable phased plan for replacing the renderer with Direct3D 12 and DXC-built DXIL.
tags:
  - backlog
  - dx12
  - dxc
  - migration
  - renderer
  - testing
timestamp: 2026-07-09T00:00:00+02:00
generated: false
status: active
verified: 2026-08-23
sources:
  - ../../Source/SilkToolkit.slnx
  - ../../Source/SilkToolkit.Native.ShaderBuilder/SilkToolkit.Native.ShaderBuilder.csproj
  - ../../Source/SilkToolkit.ShaderBuild/SilkToolkit.ShaderBuild.targets
---

# DX12 and DXC Cutover Plan

## Current checkpoint

* **Current phase:** Phase 3 - make DXC/SM6/DXIL the only shader build and loading path.
* **Branch:** `feature/wpf-sharpdx`
* **Starting commit:** `a3ac7ca4ee7397e84ddfb7a3b66ac16963b586b0`
* **Completed:** Phase 0, Phase 1, and the automated Phase 2 implementation: hardware/WARP adapter selection,
  descriptor heaps, shared root signature, resource-state tracking, deferred release, frame-fence rotation,
  committed buffers and render-target textures, RTV creation, clear, GPU copy/readback, fence waits, and
  device-removal reporting, subresource upload, UAV barriers, deterministic dispose/recreate contracts,
  pure-WPF HWND hosting, pointer input, three-buffer flip presentation, resize, and surface lifecycle.
* **In progress:** Phase 3 shader-object work. All repository declarations are explicit SM6.0 DXC items,
  the embedded resource contract is DXIL-only, and immutable DX12 shader modules plus graphics/compute PSO
  creation and caching are implemented. `ShaderDescription` resolves built-in and custom DXIL modules, all
  24 techniques and 199 passes expose validated stage sets and DX12 input layouts, and `ShaderPass` binds
  cache-owned graphics/compute pipelines to the DX12 command context. Blend, rasterizer, depth/stencil,
  stream-output, topology, input-layout, and target-format state now participates in PSO creation and cache
  identity. Root descriptor tables, constant-buffer views, render-target binding, viewport/scissor state, and
  guarded draw/dispatch ordering are available on the command context. Vertex/index views and indexed drawing
  are included. The first productive repository-DXIL draw now obtains its three vertex streams, R32 index
  buffer, topology, and draw count directly from an existing `DefaultMeshGeometryBufferModel`, renders a
  descriptor-bound indexed quad, and reads the result back on WARP. Static default meshes therefore cross the
  existing geometry-model boundary. Same-capacity dynamic updates write in place, growth creates a complete
  replacement before disposing the old allocation, and `GeometryRenderCore` owns the first DX12 indexed draw
  entry point. Generic existing `IElementsBufferModel<T>` data now crosses the same boundary through a DX12
  vertex buffer with in-place updates and atomic recreation; `GeometryRenderCore` binds it as an instance
  stream and records instanced indexed draws. Existing default line and point models now supply validated
  position/color data to a shared DX12 owner with indexed/non-indexed, dynamic, and instanced draw paths.
  Texture resources now support explicit mip/array sizes plus productive 2D/array/cube SRVs, 2D mip UAVs,
  and samplers. A repository-DXIL sampling pass reads an uploaded texture through the shared descriptor tables
  and passes WARP pixel readback. Existing 2D `TextureModel` byte, `Color4`, pointer, raw-stream, and encoded-image
  data now crosses the load/complete boundary into a native texture, initial upload, SRV, shader state, sampled
  WARP draw, and deterministic disposal. The same bridge now uploads complete decoded 2D arrays and cube maps,
  including all mip levels and block-compressed DDS subresources, in D3D12 array-major order. A shared DX12
  resource manager now caches existing mesh, line, point, matrix-instance, and texture models; tracks managed
  changes; updates within capacity; replaces atomically on growth; and disposes host-owned resources. Existing
  `GeometryRenderCore` instances have a DX11-independent DX12 attach/detach path and record supported geometry,
  pass, descriptor-table, and draw bindings through that manager. Per-draw descriptor tables now allocate the
  complete shared-root-signature ranges contiguously, initialize unused slots with valid fallback descriptors,
  and upload `GlobalTransformStruct` plus the existing per-model material buffer into b0/b1. An existing
  `MeshRenderCore` consumes those bindings and renders the default Colors pass with camera/model data on WARP.
  Existing `MeshNode` material ownership now reaches that core without a DX11 `MaterialVariable`. Diffuse,
  Phong, and PBR cores map to the exact 352-byte `cbMesh` layout, select their opaque/tessellation pass names,
  bind their existing 2D/cube texture models and sampler descriptions to the declared t/s registers, and share
  uploaded textures through the host resource manager. The first productive diffuse material/textured-mesh
  pass renders repository DXIL to a deterministic WARP pixel. The existing `LightsBufferModel` now uploads its
  complete eight-light array, ambient color, light count, and environment metadata into b3. Productive Phong
  and PBR passes consume that buffer and their material payloads in parameterized WARP scenes. General
  render-host traversal plus environment/shadow resources remain. Existing `LineNode` and `PointNode` material
  ownership now reaches `PointLineRenderCore`; their complete 160-byte b4 layout, optional line texture/s7
  sampler, full descriptor tables, geometry-shader passes, and indexed/non-indexed draws are productive on DX12.
  Existing single- and multi-image billboard models now prepare their CPU vertices without DX11 device
  services, reuse the point/line resource owner, bind their `TextureModel` at t0 and sampler at s7, and record
  productive repository geometry-shader draws. DirectWrite text preparation remains in Phase 4.4. Existing
  `BoneSkinRenderCore` instances now consume their managed bone matrices through the productive mesh bindings;
  the first DX12 package applies the shader-equivalent four-weight transform during CPU buffer preparation,
  supports material and instance bindings, and updates animation matrices on every draw. GPU t40/stream-output
  skinning and morph-target buffers remain outstanding. Opaque and transparent scene lists now share one
  renderer-independent camera-frustum selector that updates `IsInFrustum`; the legacy host uses it immediately
  and the DX12 host consumes the identical contract without duplicating culling logic. A host-lifetime DX12
  scene renderer now attaches existing geometry nodes without an `IEffectsManager` or DX11 resources, creates
  their existing default mesh/line/point/billboard/bone buffer models, owns per-core descriptor/constant-buffer
  bindings, records visible cores in candidate order, and detaches every node and native resource deterministically.
  `D3D12PresentationSurface` now owns that renderer and its shader-visible heaps; one frame binds the current
  swap-chain RTV/viewport, resolves each supported node through a host-lifetime root-signature/PSO catalog,
  records the selected scene, transitions to present, submits, presents, and fence-waits. The catalog maps the
  existing mesh/bone/line/point/billboard technique and pass contracts without caller-supplied native state.
  The surface now also owns a size-matched D32/S8 depth target and DSV across attach/resize/detach; command
  recording clears and binds it, catalog PSOs preserve their declared depth/stencil state, and pass-only mesh
  materials cross the same constant-buffer binding path. The existing `Viewport3DX` swap-chain option now owns
  this DX12 surface instead of `DPFSurfaceSwapChain`, drives it from the coalesced WPF composition callback,
  flattens current item scene trees into ordered opaque/transparent candidates, and derives b0 plus the culling
  frustum from the active camera and physical surface dimensions. The non-swap-chain `DPFCanvas` path remains
  temporarily available until its unsupported renderer features have productive DX12 replacements. Existing
  visible ambient, directional, point, and spot nodes now rebuild the shared light buffer in scene order with
  the legacy world-space transforms and eight-light cap. The current existing environment node supplies its
  cube `TextureModel` through the shared resource manager to t20/s4, while b3 publishes the validated mip count;
  non-cube textures are deliberately rejected and clear the environment flags. Native mouse and first-contact
  touch messages from the DX12 child HWND now reuse the viewport's DIP hit testing, current-position updates,
  mouse-wheel policy, configurable right-button camera bindings, and existing gesture handlers. The productive
  bone-skin path now applies the existing morph-target offset/pitch/weight payload before the bone matrices,
  matching the repository shader's position, normal, tangent, and bitangent preparation. The command context
  now also binds validated D3D12 stream-output targets, and structured upload buffers can populate t-register
  SRVs; these are the independently tested native primitives required by the GPU skinning precompute pass.
* **Last verified gates:** CLI-fallback solution build passed with 304 warnings and 0 errors. The standard tests
  pass 272/272 (`SilkAssimp` 13, `SilkCore` 224, `SilkToolkit` 35), including the WPF-model-to-DX12 repeated
  mesh-buffer lifecycle that replaced the final CSO-dependent WPF smoke. Explicit DX12 hardware creation
  passed 1/1 in a preceding work package. Graphify passes without syntax-recovery warnings for 1,042 source
  files with 12,906 nodes, 23,064 edges, and 2,025 final communities. `git diff --check` passes with
  line-ending notices only.
* **Validation note:** Rider/ReSharper MCP was not exposed in the implementation session, so its diagnostics
  and formatter gates remain outstanding; the documented `dotnet` fallback was used for build and tests.
* **Latest focused gates:** All 45 DX12 runtime tests, 12 HWND/surface/input tests, and 2 DX12 WARP swap-chain
  tests pass; all 115 texture/resource tests pass, including 103 manifest-parameterized DXC cases. The WARP
  tests create real vertex/pixel, geometry, hull/domain, and compute PSOs from repository DXIL. A catalog gate
  now creates native PSOs for all 199 default passes, including stream output and the DXC-normalized depth,
  line, point, outline, and XRay linkage contracts; every live built-in `ShaderDescription` resolves through
  the explicit manifest. All 12 `TextureModel` CPU/lifecycle/WARP tests pass, including multi-mip BC1 array and
  cube DDS preparation plus selected-subresource WARP readback. The DPI-sensitive WPF lifecycle
  test now derives physical expectations from the surface DPI contract and passes at non-100-percent scaling.
  Four resource-manager/traversal WARP tests cover shared geometry, instances, textures, descriptor exhaustion,
  DX12-only render-core attachment, descriptor-table binding, an existing-core draw, and pixel readback. Five
  focused binding tests additionally cover contiguous descriptor ranges, register mapping, allocation failure
  cleanup, aligned constant-buffer writes, initialized fallback descriptors, and the existing MeshRenderCore
  camera/model WARP draw. One CPU contract test covers the complete diffuse/Phong/PBR layout, flags, factors,
  UV rows, opaque/tessellation pass selection, unsupported materials, and `MeshNode` propagation. One focused
  WARP test covers TextureModel upload, t0/s0 material binding, unlit diffuse shading, cache ownership, and
  deterministic green-pixel readback. A byte-layout WARP test covers all b3 light fields, and a two-case WARP
  theory covers existing Phong/PBR opaque-pass selection plus ambient-light consumption and red-pixel readback.
  One CPU contract test covers the exact point/line b4 layout and SceneNode propagation; a two-case WARP theory
  covers the existing line/point buffer models, geometry shaders, fixed-size expansion, draw modes, and red/green
  center-pixel readback. The same CPU contract covers image-billboard material flags, single/multi-image vertex
  preparation, node propagation, and the explicit text boundary; a focused WARP test covers cached texture
  upload, t0/s7 binding, geometry-shader expansion, and green center-pixel readback. One CPU skinning contract
  covers weighted position/direction transforms, invalid bone indices, node material propagation, and the
  animated-frustum boundary; a focused WARP test covers a translated existing `BoneSkinRenderCore`, descriptor
  and material bindings, cached geometry ownership, and deterministic red center-pixel readback. One camera
  contract covers real in/out mesh bounds, opaque/transparent shared selection state, and disabled culling. One
  DX12 scene-traversal WARP test covers visible/culled real MeshNodes, DX12-only node/buffer attachment, per-core
  bindings, repository-DXIL drawing, red-pixel readback, detach, and descriptor cleanup. The repeated WPF/WARP
  surface lifecycle test now records a real MeshNode through that renderer and the automatic pass catalog on
  each of three attach/present/resize/detach cycles instead of proving only clear/present. A focused CPU test
  verifies the catalog's existing mesh/bone/line/point/billboard mappings and unsupported-node boundary. The
  traversal WARP test now draws near and far existing mesh nodes in reverse visibility order through the
  automatic catalog and asserts the near pixel, covering DSV creation, clear, binding, declared depth state,
  pass-only material payloads, culling, and deterministic cleanup. Invalid depth formats, resource flags,
  descriptor heaps, render-target bindings, and clear values are covered by focused failure contracts.
  A camera contract verifies view/projection/frustum, physical viewport/resolution, DPI, timestamp, and eye
  position fields. A real themed `Viewport3DX` WARP test selects the existing swap-chain option, creates the
  DX12 surface, traverses its WPF mesh item with the active camera, records a frame, presents, and disposes. A
  focused light contract covers existing ambient/directional/point/spot nodes, world transforms, scene order,
  visibility, and overflow capping. A WARP environment contract covers shared cube upload, b3 mip metadata,
  t20/s4 readiness, loader completion, and rejection/reset for an existing non-cube texture. A focused binding
  contract maps all five configurable native right-button modifier gestures to the existing camera handlers;
  the real themed viewport test now additionally covers subscribed HWND mouse move/press/release/wheel input,
  DIP hit-test events, camera zoom/rotation, and first-touch contact filtering. A focused morph-target contract
  covers multiple target/vertex indices, weights, basis reconstruction, invalid pitch/data rejection, and the
  existing bone-skin WARP draw now consumes a non-zero morph target before applying its bone transform. A WARP
  primitive test covers structured buffer SRV creation, range rejection, Common-to-StreamOut transition,
  bounded SO target binding/unbinding, and native device health.
* **Known unrelated work:** The untracked `2026-08-18.md` and `Unbenannt*.canvas` files are user work;
  preserve them.
* **Graphify note:** The AST subprocess requires execution outside the workspace sandbox on this machine. The
  canonical wrapper completed successfully and removed `.graphify-code-corpus`; tool-version and community-name
  notices remain non-blocking warnings.
* **Outstanding manual Phase 2 gate:** Visible hardware swap-chain presentation, live DPI changes, and physical
  mouse/touch behavior have not been observed in this non-interactive session.
* **Next action:** Build the bone/morph binding table and output buffer on the now verified structured-SRV/SO
  primitives, then replace the parity-complete CPU morph/skin upload with the existing t40-t43/b9 precompute
  pass. Shadow bindings follow. Productive skybox rendering remains
  separate from the now shared material environment-map binding; multi-touch camera manipulation remains a
  manual/full-parity follow-up beyond first-contact hit testing.

Update this checkpoint after every completed work package, every changed technical decision, and before
stopping. A work package is complete only when its implementation, tests, and checkpoint agree.

## Goal and decisions

* Direct3D 12 becomes the only renderer.
* DXC with Shader Model 6.0 and DXIL replaces JeremyAnsel, DXBC, and `.cso` resources.
* DX11 remains only as a lazily loaded Desktop Duplication interoperability island.
* The migration is delivered in independently buildable phases. DX11 remains temporarily available until
  the final cutover so every intermediate phase has a working comparison path.
* `HelixToolkit.SharpDX.*` assembly and namespace identities remain unchanged.
* The cutover is a breaking release: DX11-specific public types and signatures need no compatibility shims.
* Rendering, compute, and uploads initially share one direct command queue. Add asynchronous queues only after
  profiling shows a need.
* Automated graphics coverage uses D3D12 WARP. Hardware and real Desktop Duplication checks remain explicit.

## Baseline inventory

Record verified counts and results here during Phase 0.

* Shader declarations: 104 legacy `HLSLShader` items, currently mirrored to DXIL by `DxcCompileAll`.
* Render techniques: 24 `TechniqueDescription` declarations.
* Shader passes: 199 `ShaderPassDescription` declarations.
* Example projects: 43 under `Source/Examples/SilkToolkit`.
* Current DX12 implementation: device, command queue/context, fence, and empty root signature bootstrap.
* Current productive renderer: DX11 with embedded `.cso`; embedded DXIL has no productive renderer consumer.
* ShaderBuilder Debug output: 106 DXIL and 103 CSO files; SilkCore resource handoff contains 103 DXIL and
  103 CSO files. The three-output DXIL difference remains a Phase 3 inventory item.
* ShaderBuilder build: passed, 0 warnings, 0 errors.
* Solution build: passed, 227 warnings, 0 errors.
* Standard tests: passed 68/68 (`SilkAssimp` 13, `SilkCore` 37, `SilkToolkit` 18).
* Explicit DX12 bootstrap: native object creation and queue signaling completed; test failed only because it
  asserted `device.IsDisposed` before the `using var` declarations left scope.

## Phase 0 - Persist plan and verify baseline

### Work

- [x] Replace the former preview-only backlog with this full cutover and resume plan.
- [x] Build `SilkToolkit.Native.ShaderBuilder` and record DXIL/CSO output counts.
- [x] Build `Source/SilkToolkit.slnx` through Rider, or use the documented CLI fallback when Rider MCP is
  unavailable before the build starts.
- [x] Run the standard non-hardware/non-DX12 test suite.
- [x] Run the explicit DX12 bootstrap smoke when suitable hardware is available.
- [x] Record baseline warnings or failures without hiding pre-existing problems.

### Exit criteria

* The repository baseline is reproducible from the commands recorded in this file.
* Existing user changes are identified and untouched.
* No production source change starts before the baseline result is documented.

## Phase 1 - DX12 runtime core

### Work

- [x] Complete DXGI factory and adapter selection, including D3D12 WARP selection.
- [x] Complete the D3D12 device, direct queue, command allocator/list, frame fence, and deferred-release
  lifecycles.
- [x] Add RTV, DSV, CBV/SRV/UAV, and sampler descriptor allocation with deterministic reuse.
- [x] Add default, upload, and readback resource creation plus subresource upload helpers.
- [x] Add centralized resource-state tracking and transition/UAV barriers.
- [x] Add the shared SM6.0 root signature for the repository's existing register ranges.
- [x] Report device removal with `GetDeviceRemovedReason`; make renderer teardown and recreation deterministic.

### Tests

- [x] Unit-test descriptor boundaries, exhaustion, release, and reuse.
- [x] Test resource-state transitions and redundant suppression as pure unit tests, and native UAV barriers
  through deterministic WARP contract tests.
- [x] Unit-test fence values, frame rotation, deferred release, and disposed-object failures.
- [x] Unit-test adapter ranking and WARP selection using pure candidate data.
- [x] WARP-test device, queue, command list, fence, root signature, upload, and readback.
- [x] Keep one explicit hardware command-execution smoke.

### Exit criteria

* A WARP command list clears, copies, signals, waits, and reads back without native leaks or debug-layer errors.
* The new deterministic tests pass without mocks of the D3D12 API.

## Phase 2 - WPF presentation

### Work

- [x] Add a pure-WPF `HwndHost` that owns the child HWND used by the DX12 swap chain.
- [x] Add a three-buffer flip-model swap chain, RTV recreation, resize, present, DPI, mouse, and touch handling.
- [x] Add offscreen render targets and readback for automated tests and screenshots.
- [ ] Keep the current D3DImage and WinForms paths only as temporary comparison paths.

### Tests

- [x] Unit-test physical-size, DPI, resize-coalescing, and minimum-size calculations.
- [x] Unit-test back-buffer rotation and present-state transitions.
- [x] WPF/STA-test Loaded/Unloaded, repeated attach/detach, and disposal.
- [x] WARP-test clear color, resize, target recreation, and pixel readback.
- [x] Raw-HWND-test repeated resize/present and close with frames in flight.
- [ ] Keep visible swap-chain and DPI-change checks explicit and hardware-dependent.

### Exit criteria

* The DX12 WPF surface starts, renders, resizes, and closes under WARP and supported hardware.
* Input and DPI contracts required by `Viewport3DX` are covered by automated WPF tests.

## Phase 3 - DXC, root signature, PSOs, and resources

### Work

- [x] Replace the mirrored inventory with explicit `DxcShader` declarations and SM6.0 profiles for all shaders.
- [x] Remove `DxcCompileAll` after explicit DXC declarations cover the complete inventory.
- [ ] Move `DeviceContextProxy` and productive resource binding to DX12 command recording; shader descriptions,
  shader passes, and fixed-function state translation are complete.
- [x] Cache immutable PSOs by shader set, input layout, topology, blend/raster/depth state, and target formats.
- [ ] Port vertex/index/constant buffers, textures, subresources, mips, cubemaps, UAVs, and samplers.
- [x] Allocate complete contiguous shared-root-signature tables, initialize every descriptor slot, and upload
  camera/model constant buffers for an existing MeshRenderCore WARP draw.
- [x] Add native 2D/array/cube SRVs, 2D mip UAVs, samplers, and repository-DXIL sampled-texture WARP
  readback, including selected mip, array-slice, and cube-face readback.
- [x] Bridge existing `TextureModel` byte, color, pointer, raw-stream, and encoded-image data into native upload,
  including full 2D arrays, mip chains, block-compressed DDS data, cube maps, SRV creation, loader completion,
  and deterministic disposal.
- [x] Make custom shader descriptions accept precompiled SM6 DXIL using the shared root-signature contract;
  productive custom-pass binding remains part of the command-context bridge.

### Tests

- [x] Parameterize profile and entry-point tests over every shader declaration.
- [x] Verify that embedded DXIL resource names are unique, complete, readable, and non-empty.
- [x] Unit-test root-signature register ranges and binding collisions.
- [x] Unit-test every PSO-key component and cache reuse.
- [x] Unit-test descriptor-heap contracts, constant-buffer alignment, subresources, and mip calculations;
  productive descriptor-table binding remains part of the render-context bridge.
- [x] WARP-test every used shader stage: vertex, pixel, geometry, hull, domain, and compute.
- [x] Add one custom-DXIL shader contract test.
- [x] Test missing shaders, invalid profiles, incompatible root signatures, and descriptor exhaustion.

### Exit criteria

* All repository shaders compile explicitly with DXC and can create their required DX12 objects.
* Resource and PSO behavior is covered by deterministic unit tests and focused WARP tests.

## Phase 4 - Renderer parity

Implement each group as a separate green work package.

### 4.1 Geometry

- [ ] Port cameras, static and dynamic meshes, instancing, skinning, lines, points, and billboards.
- [x] Bridge immutable default-mesh CPU preparation and indexed binding from the existing geometry buffer model.
- [x] Add same-capacity dynamic default-mesh updates, atomic growth replacement, and the first render-core entry
  point.
- [x] Bridge generic existing element/instance buffers and record instanced indexed draws.
- [x] Bridge default line and point CPU preparation, native buffers, dynamic replacement, instancing, and
  indexed/non-indexed draw recording.
- [x] Add shared render-host-lifetime geometry, matrix-instance, and texture caching plus a DX11-independent
  DX12 attach/detach and draw boundary for existing `GeometryRenderCore` instances.
- [x] Bind existing point/line materials and render their existing cores through repository geometry shaders
  with deterministic WARP coverage.
- [x] Bridge existing single- and multi-image billboards through CPU preparation, cached texture/sampler
  bindings, repository geometry shaders, and deterministic WARP readback; text billboards remain in Phase 4.4.
- [x] Bridge existing bone-skinned mesh cores through shader-equivalent CPU transforms, material/instance
  bindings, animation updates, validation contracts, and deterministic WARP readback.
- [x] Share camera-frustum selection and `IsInFrustum` updates between opaque/transparent legacy and DX12 scene
  traversal candidates, including enabled/disabled camera-culling contracts.
- [x] Add DX12-only SceneNode attach/detach, default geometry-model creation, ordered visible-core traversal,
  per-core bindings, host-lifetime cleanup, and deterministic WARP coverage.
- [x] Integrate visible scene recording into the WPF HWND swap-chain back-buffer/present/fence frame lifecycle
  and cover repeated WARP attach, render, resize, detach, and recreation.
- [ ] Move DX12 skinning and morph targets to the declared t40-t43/b9 GPU resources and stream-output path.
- [ ] Cover CPU data preparation, visibility, draw arguments, buffer updates, and representative WARP readback.

### 4.2 Materials and advanced geometry

- [ ] Port textures, Blinn-Phong, PBR, volume rendering, tessellation, compute particles, and shadows.
- [x] Map existing diffuse, Phong, and PBR CPU material data, texture models, samplers, and opaque/tessellation
  pass names into the shared DX12 root tables and exact cbMesh layout.
- [x] WARP-render one existing diffuse textured material through repository DXIL with deterministic readback.
- [x] Upload the existing shared light model into b3 and WARP-render existing Phong and PBR material passes.
- [ ] Cover material/pass selection, texture bindings, tessellation/compute dispatch, and shadow depth behavior.

### 4.3 Transparency and post effects

- [ ] Port OIT, depth peeling, SSAO, FXAA, bloom, outline, XRay, and remaining post effects.
- [ ] Cover pass ordering, ping-pong targets, alpha/depth contracts, and stable reference pixels.

### 4.4 Native DX12 2D

- [ ] Render shapes as geometry and images/sprites as textured quads.
- [ ] Shape and rasterize text with DirectWrite glyph analysis; upload glyphs into a DX12 atlas.
- [ ] Cover glyph caching, shaping, atlas growth, Unicode, 2D ordering, clipping, and resize behavior.

### 4.5 Desktop Duplication

- [ ] Isolate the only permitted D3D11 use in the screen-duplication implementation.
- [ ] Load D3D11 only when capture starts and transfer captured frames into DX12 textures.
- [ ] Cover format, dimensions, frame replacement, timeout, monitor changes, and resource release.
- [ ] Keep real desktop capture explicit because it requires an interactive hardware session.

### Tests common to every parity package

* Unit-test CPU-side data preparation, sorting, visibility, draw parameters, and pass selection.
* Contract-test affected scene nodes, render cores, materials, resource ownership, and state changes.
* WARP-render small deterministic scenes and assert dimensions, formats, fixed colors, alpha, and depth.
* Do not use pixel-perfect golden images; assert stable functional contracts.
* Test empty scenes, missing optional resources, resize, and repeated attach/detach.
* Parameterize technique and pass initialization for every affected feature group.

### Exit criteria

* Every existing render technique and pass belongs to a passing parity package.
* All supported scene features have automated CPU contracts and the smallest useful WARP render check.

## Phase 5 - Final cutover

### Work

- [ ] Make DX12 the only `Viewport3DX` and `IRenderHost` path.
- [ ] Remove DX11 render hosts, render buffers, D3DImage/WinForms presentation, and DX11-specific public types.
- [ ] Remove `JeremyAnsel.HLSL.Targets`, CSO copy targets, `.cso` resources, and redundant shader-reader paths.
- [ ] Retain `Silk.NET.Direct3D11` only for the isolated screen-duplication implementation.
- [ ] Update examples, test commands, project knowledge, and this checkpoint to the final state.

### Tests and gates

- [ ] Preserve and convert existing unit, WPF, and WARP tests; do not delete tests merely to make the cutover
  pass.
- [ ] Require complete DXIL resources, zero `.cso`, and zero JeremyAnsel references.
- [ ] Enforce that D3D11 source references exist only in the screen-duplication area.
- [ ] Initialize all 24 techniques and 199 pass descriptions under WARP and render a representative case for
  each feature group.
- [ ] Build all 43 SilkToolkit example projects.
- [ ] Sequentially start the central demos and verify startup, rendering, resize, and clean shutdown.
- [ ] Change the standard suite to exclude only `Hardware`; DX12 is no longer an exceptional category.
- [ ] Explicitly hardware-test present, resize, materials, OIT, post effects, 2D/text, and Desktop Duplication.

### Exit criteria

* DX12/DXIL is the only productive rendering and shader path.
* The only D3D11 code is the documented Desktop Duplication interop island.
* All automated gates pass and manual/hardware checks are recorded precisely.

## Public contract changes

* `Viewport3DX`, scene, material, assembly, and namespace identities remain.
* DX11-specific types and native D3D11 signatures may be removed or replaced without adapters.
* `EffectsManagerConfiguration.EnableSoftwareRendering` selects D3D12 WARP.
* `EnableDeferredRendering` controls parallel DX12 command-list recording.
* Canvas-selection properties disappear when only the WPF HWND swap-chain path remains.
* Custom shader contracts move from SM4/5 DXBC to SM6.0 DXIL.

## Per-work-package gate

1. Run the changed unit, contract, and WARP tests.
2. Format only changed C# files and inspect ReSharper warnings.
3. Build `Source/SilkToolkit.slnx` through Rider, using CLI only when Rider is unavailable before start.
4. Run the complete non-explicit test suite.
5. Rebuild Graphify after relevant `Source/` changes.
6. Run `git diff --check` and inspect repository status for unexpected files.
7. Update the checkpoint with exact results and the next action before stopping.

Coverage is reported per production assembly and remains non-blocking. Missing graphics hardware does not block
unit or WARP acceptance, but it must remain recorded as an outstanding explicit gate.
