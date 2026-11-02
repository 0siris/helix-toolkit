---
type: Project
title: SilkCore
description: Core Silk.NET rendering and runtime project.
resource: ../../Source/SilkCore/SilkCore.csproj
tags: [project, core, silknet]
timestamp: 2026-07-08T00:00:00+02:00
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-20T07:15:06Z }
status: stable
sources:
  - resource: ../../Source/SilkCore/SilkCore.csproj
  - resource: ../../Source/SilkToolkit/Controls/D3D12PresentationSurface.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12WpfPresentation.cs
  - resource: ../testing/wpf-composition-phase4.md
---

# Project

## Diffuse helper meshes without normals

Original helper spheres may intentionally omit their normal channel. The diffuse pixel shader checks the
normal's squared length before normalization. Missing or invalid interpolated normals retain the full diffuse
color, matching the original FXC-compiled shader's measured output; the earlier half-intensity fallback was too dark.
The shared shader is used by ordinary, weighted-OIT and depth-peeling diffuse passes. Geometry, source colors
and valid-normal shading remain unchanged. Toolkit GPU tests cover the three premultiplied passes; Original
Helix additionally reads back its actual cached sphere with color (234,98,31) in both presentation profiles.

`SilkCore` targets `net10.0` and provides the core rendering/runtime layer used by the toolkit and examples.

`SilkCore` targets `net10.0-windows` and provides the core rendering/runtime layer used by the toolkit and examples.

Phase 4 (2026-10-19) shares the D3D12 scene renderer between the continuous native HWND owner and the bounded,
demand-driven WPF/D3DImage owner. WPF is the public default (`EnableSwapChainRendering=false`); explicit `true`
selects HWND presentation. The 2026-10-20 documentation correction uses source/static evidence only;
the dated checks below remain historical and do not establish Phase 5 runtime success. See
[presentation limits](/projects/silktoolkit.md#current-presentation-contracts).

# Dependencies

The project references `SilkToolkit.Native.ShaderBuilder` with private assets. It uses Silk.NET packages for Direct2D, Direct3D compiler support, Direct3D11, Direct3D12, DXGI, and maths support.

The Direct3D12 presentation path uses the existing scene render cores through native resource and binding adapters.
Per-core mesh, point/line, volume, particle and screen-capture caches release entries for disposed cores at
`D3D12SceneRenderer.BeginFrame()`, after the presentation host has completed the preceding GPU fence. Live cores
retain their descriptor tables. This prevents repeated scene replacement from exhausting contiguous descriptor
ranges; increasing heap capacity does not replace correct ownership cleanup.

Transient mesh, line and full-screen effect bindings use a shared renderer-owned paged heap pool. Each lazy
page provides 16,384 CBV/SRV/UAV descriptors and 2,048 samplers, preserving immutable per-draw tables and constants
without taking ordinary scene descriptor capacity. Draws bind their own heap pair; intersection graphics restore
that pair after their compute stage. Existing draw slots reuse the high-water pages only after the completed
presentation fence, and dispose before the pool. This preserves original glow, border and XRay pass behavior.

# Native scene readiness and camera fitting

Native scene preparation respects the scene owner's geometry eligibility and `IsRenderable`, not just an
attached render core. Indexed meshes with no indices remain nonrenderable; replacing or updating indices
revalidates eligibility and its bounds. Attachment is idempotent for an already-owned core while geometry nodes
can complete their own readiness after gaining first primitives. Already-attached resources remain tracked for
teardown even when the node is not ready. Malformed nonempty data still fails strict GPU-buffer validation.

Actual WARP pixel readback covers initial empty geometry becoming ready and attached geometry transitioning
empty/populated through both collection replacement and in-place notification. The visible standalone MeshMixer
viewport also exercises native sparse picking and empty/restored selection highlights without render errors.

Perspective framing converts the inward-facing frustum normals to outward support directions before selecting
extreme bounding-box corners. Front and rotated portrait cases failed before this correction; both now preserve
the view direction and project every corner inside the viewport. Actual original MIX and two-object scenes frame
correctly. Full current SilkCore suite:425passed; this finite visible proof is not general toolkit/UI parity.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkToolkit](/projects/silktoolkit.md)
* [ShaderBuilder](/projects/shaderbuilder.md)

# Citations

[1] [SilkCore.csproj](../../Source/SilkCore/SilkCore.csproj)
[2] [SilkD3D12DeviceFactory.cs](../../Source/SilkCore/Native/SilkD3D12DeviceFactory.cs)
[3] [D3D12DeviceHandles.cs](../../Source/SilkCore/Native/D3D12DeviceHandles.cs)

# Native mesh intersections

Native scene preparation first updates the whole attached hierarchy, then calls the existing `UpdateNotRender`
hooks on living attached nodes, and only afterward prunes visible draw candidates. Hidden branches keep their
maintenance; nodes disposed or detached during the update phase do not receive maintenance. Existing geometry,
group, instance and batch hooks remain authoritative. Hit-testable geometry prepares its octree before picking;
unchanged geometry retains it. Non-hit-testable application gingiva never receives a render octree. Pending-frame
fence skips perform neither updates nor maintenance, preserving resource reuse ownership.

`GeometryNode` also accepts actual immutable selected-pair GPU intersection inputs without a CPU line buffer.
Applications provide conservative bounds from their genuine mesh/pose inputs; no placeholder line geometry is
required. Ordinary geometry validation is unchanged. Headless WARP readback covers GPU-only attachment, culling,
indirect lines and additive XRay, while explicit Core-line compatibility remains available.

Filled mesh materials honor the owning `MeshRenderCore.RasterDescription` in opaque, weighted OIT and
depth-peeling passes. Pass-cache identity includes this state. Wireframe overlays retain their own bias/fill
override. Ignoring the filled mesh's front-face setting reverses `SV_IsFrontFace` for custom shaders even with
culling disabled; ignoring its cull mode also leaves filled surfaces behind a correctly culled wire overlay.
Headless WARP regressions toggle front-facing and culling on the same mesh, check fence-completed pixels in all
three stages, restore prior state, and verify descriptor cleanup. Visible Desktop acceptance remains separate.

MeshIntersectionRenderCore owns immutable mesh/candidate/pose generations at the toolkit boundary. The existing
D3D12 scene renderer records selected-pair generic and actual shared-buffer compute passes, UAV append counters,
GPU-produced indirect line arguments and normal/XRay material output through the existing device, heaps and
pipeline caches. Generations retire only after the completed presentation fence; disposed cores return their
descriptors through BeginFrame cleanup. Caller arrays and borrowed mesh/buffer owners cannot mutate a published
generation. Shared-buffer mismatch/disposal selects the immutable generic input.

Headless WARP checks read back genuine segments, pair IDs, indirect arguments and render targets; these are no
physical adapter or visible-window acceptance. Core-line compatibility remains selectable in the application;
the source GPU kernel excludes coplanar output. A previous full SilkCore suite passed323 cases.

The explicit shader package has125 declared resources. Strict manifest/count/readability checks include
the selected-pair/shared-buffer compute, collision line shaders and dedicated Meshmixer PHONG vertex/pixel pair.
The new source shaders use the existing material b2 and slot-three transport; no shared GPU API changes are needed.
Generated DXIL and the manifest are produced by the normal shader-builder target rather than manually edited.

# Source two-dimensional noise

MathHelper.GenerateNoiseMap retains the four-corner quintic two-dimensional gradient field, frequency 0.5,
amplitude 1 and octave doubling/halving. Signed cells wrap periodically at256 before lookup. A map captures
one freshly published immutable permutation; concurrent reseeding cannot mix tables within that map. Completed
field extrema determine normalization. Constant fields are finite zeros; empty dimensions return empty arrays.
Negative dimensions/counts, pixel overflow and nonfinite sampling products fail explicitly. Finite input
coordinates reduce periodically before integer conversion.29 headless owning cases cover analytic samples,
normalization, arithmetic boundaries and bounded concurrent reseeding. No native presentation is involved.

# Double-key collection compatibility

DoubleKeyDictionary now replaces values for Add/indexer on the same ordered pair and implements association-based
structural equality with order-independent hashing. The original source compared keys and values independently,
incorrectly treating swapped associations as equal. Current tuple enumeration/LINQ, bool Remove, null keys and
lookup/clear APIs remain. Additive GetPairs yields fresh mutable public DoubleKeyPairValue carriers with actual
keys/values; carrier mutation does not rewrite entries. Flat current enumeration order is retained deliberately.
The22 owning headless cases pass, including16 dedicated overwrite/equality/carrier/API checks. This is collection
behavior, not native/visible runtime acceptance; the existing TryGetValue nullable annotation warning remains.

# Reference-counted pool entry ownership

`ReferenceCountedDictionaryPool` associates a disposal callback with the exact inserted key and resource instance.
The callback takes the same dictionary lock as acquisition, detachment and clearing, and removes an entry only
while that entry still refers to the disposing resource. Releasing a resource after `TryDetach` cannot evict a
replacement subsequently cached under the same key.

The public API and automatic-disposal policy remain unchanged. Five CPU-only regressions cover replacement
identity under both disposal policies, ordinary eviction and automatic return-to-pool reentrancy. Two replacement
cases fail before the correction; all five and the existing texture/resource checks pass afterward (141 cases).
These checks require neither a native device nor a window; no new GPU or presentation acceptance is implied.

# Paged scene descriptors and external profiling

Ordinary mesh and point/line bindings, including independent peeling-input caches, allocate complete tables from
an instance of the existing D3D12EffectBindingPool. Scene pages are separate from transient effect pages and from
the shared resource heap. One shared-root-signature table occupies 118 resource and 10 sampler descriptors;
16,384/2,048-entry pages hold 138 tables. Heap pairs are bound per draw, including shadow mesh/bone draws and GPU
intersection lines. Cleanup remains gated by the completed presentation fence. Disposal releases table leases
before their pool pages; high-water pages are retained for reuse during the renderer lifetime.

The pressure test adds 80 offscreen meshes to one physical translucent plane: 162 tables cross one production
page. It checks stable pixels, opaque/peeling switches, resize, disposal, scene replacement and descriptor reuse.
Existing multilayer composition tests retain analytic alpha/readback verification; ordinary and instanced planes
continue to use the original geometry and shaders.

Set SILK_D3D12_PROFILE=1 before starting a process to enable the official PIX runtime's named CPU/GPU regions,
resource names and descriptor occupancy counters. WinPixEventRuntime 1.0.240308001 provides the x64 DLL copied
into consumer output/publish directories. The default path emits no PIX calls, timers or extra GPU waits.
Only internal helpers are added. No public diagnostics API or application diagnostics window is introduced.
PIX capture tooling remains a separate installation; marker execution is not evidence of an actual capture.

## Attached 2D invalidation

The visible Desktop startup exposed a recursive InvalidateRender/InvalidateVisual cycle introduced during the
D3D12 cutover. Render invalidation now directly marks the node and ancestors dirty; visual invalidation delegates
to it once. The native HWND continuous loop consumes that state without re-entering layout invalidation;
the WPF owner instead wakes bounded demand and settles after pending transport completes.
Two device-free attached-panel regressions cover both entry points and the startup attachment path.

## Original host scene boundary

The native renderer publishes its actual prepared nondisposed hierarchy as preorder depth/node pairs, including
hidden nodes and descendants of attachment failures. Draw pruning remains separate. SceneNode.RenderHost identifies
the actual frame owner and clears on detach. Application intersection cores can derive from the existing native
MeshIntersectionRenderCore and retain the same dispatch, indirect-draw and fence resource path.

Native geometry attachment honors protected application buffer and vertex-serialization overrides. The default
serializer keeps its existing validation; original application overrides retain their own source channel policy.
ID3D12MeshShaderProvider can preserve original per-material transparency pass selection; its default keeps native
selection. The FOV alias shares the existing Fov field without changing frustum storage. HelixTests has friend
access to genuine native readback contracts.

EmptyGeometryBufferModel never creates a graphics pass. Attached placeholders remain in the actual scene and can
render after geometry is assigned. The startup failure was reproduced before the fix; GPU-only intersection
passes retain their explicit line topology and are not subject to the ordinary empty-buffer rejection.

# Native connected path strokes

`PathRenderCore2D` builds its existing Figure commands through actual Direct2D path/sink wrappers. A closed
native geometry is cached by figure publication and factory identity; replacement invalidates it before render
observers run. Failed closure does not publish partial geometry. Detach, disposal and factory replacement
release derived resources. In-place Figure edits still require explicit invalidation or list replacement.

The D3D12 Scene2D consumer widens the native geometry and tessellates it into the existing Sprite2D transport.
It preserves connected strokes, transforms, scissor and sibling order without another managed stroker. The
native tessellation callback retains its managed owner through the synchronous call and defers exceptions
until control returns from native code. This consumer rejects non-null fill brushes explicitly.

The fixed-size line geometry shader admits only finite, nonzero projected directions. The existing arrow
includes and nonfixed construction remain compatible. Existing line thickness is a half-width multiplied by
the actual global DPI; callers requiring a physical full width compensate once at their material boundary.

Real attached Scene2D nodes exposed a pre-existing recursion between `InvalidateRender` and `InvalidateVisual`.
The latter now terminates after setting the visual dirty flags up the parent chain. Other invalidation entry
points retain their dirty-state work and delegate into that leaf. Six attached parent/child regressions cover
these entry points; the originally crashing WPF attachment case keeps its original assertions.

The complete normal Core suite passes469/469, including36 path/line cases and the six invalidation cases.
The path tests exercise genuine native geometry and fenced WARP readback. They do not establish Original
OpenGL pixels, inherited line caps/blending, application Window preview delivery or physical-input acceptance.

## Fence-reusable collision publications

At the completed host frame boundary, the scene renderer updates candidate uploads and transform constants
in place when immutable mesh identities, live shared owners, active batch slots and existing capacities match.
The active candidate count controls dispatch and shader tail guarding independently of allocated capacity.
Output, counter, indirect arguments, mesh uploads and descriptor ranges survive these publications; compute
output is invalidated for the next draw. Within a recorded frame, publication changes still allocate an
independent generation and defer its release until the host fence. Geometry, owner or batch-layout changes
and capacity growth still replace the complete group. A failed upload disposes the unusable group before
propagating the infrastructure failure, preventing later reuse of partial data.
`IntersectionPublicationReuseTests` checks real small WARP allocations and disposal without recording commands
or submitting a queue; it does not establish physical GPU behavior or cure an application freeze.

## Empty annotation lines

`SilkD3D12PointLineBuffers.Prepare(DefaultLineGeometryBufferModel)` accepts absent/empty line indices
as no segments and returns empty vertex and index streams. This includes open builders with zero or one
marker and cleared lines retaining old positions. Zero vertices prevent a non-indexed draw fallback.
Actual indices still require positions, complete endpoint pairs and valid references; missing geometry
remains invalid. CPU-only `D3D12LinePreparationTests` covers empty/populated/repopulated topology and
invalid indices without a native device. Minimum upload sizes and capacity-driven recreation stay unchanged.

## Collision dispatch dimensions and output bound

The native intersection adapter rejects nonempty batches exceeding 65,535 groups of 512 threads before
allocating native resources. The same checked calculation records dispatch dimensions. Valid candidate
selection, original shader code and output semantics remain unchanged. Append capacity is the sum of
active candidates; each valid candidate appends at most one segment. Reset, UAV barriers, counter copy
and indirect drawing stay GPU-side. CPU-only boundary tests cover empty input, tail rounding, the largest
supported count and excessive counts without allocating candidate arrays or submitting GPU commands.
This validation does not bound GPU execution time or establish the cause of an application freeze.

## Billboard preparation and descriptor ownership

The [billboard resource contract](billboard-resources.md) documents preparation before texture reads,
explicit null t0 bindings and descriptor clearing before registration release. Native vertex consumption
uses the same prepared geometry. Existing framefence reclamation, shaders and public APIs remain intact.
CPU/source regression evidence does not replace native descriptor, glyph or WPF presentation acceptance.

## Bounded collision diagnostics and rejected publications

`MeshIntersectionRenderCore` marks oversized publications unsupported before renderer upload reuse or
allocation. The affected category records no draw, logs entry into the rejected state, and can resume
with the next valid publication. Native constructor/recording validation remains a second boundary.
No truncation, splitting, scheduling change or claim of no collision. Workload diagnostics retain exact
counts/groups/capacity while emitting only on scale/capacity/batch changes or slow preparation, at most
once per second per core. WPF reports slow submission and one pending-fence warning after one second
per frame, plus failure fence/device metadata, without adding waits or readbacks. A consumer must bind
`LoggerLib` to its existing file logger using the existing `SerilogLoggerLib` adapter (Helix does so).
