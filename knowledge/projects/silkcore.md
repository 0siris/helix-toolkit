---
type: Project
title: SilkCore
description: Core Silk.NET rendering and runtime project.
resource: ../../Source/SilkCore/SilkCore.csproj
tags: [project, core, silknet]
timestamp: 2026-07-08T00:00:00+02:00
---

# Project

`SilkCore` targets `net10.0` and provides the core rendering/runtime layer used by the toolkit and examples.

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

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkToolkit](/projects/silktoolkit.md)
* [ShaderBuilder](/projects/shaderbuilder.md)

# Citations

[1] [SilkCore.csproj](../../Source/SilkCore/SilkCore.csproj)
[2] [SilkD3D12DeviceFactory.cs](../../Source/SilkCore/Native/SilkD3D12DeviceFactory.cs)
[3] [D3D12DeviceHandles.cs](../../Source/SilkCore/Native/D3D12DeviceHandles.cs)

# Native mesh intersections

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

The explicit shader package has123 already declared resources. Strict manifest/count/readability checks include
the selected-pair/shared-buffer compute and collision line shaders; no HLSL or generated DXIL was manually edited.

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
