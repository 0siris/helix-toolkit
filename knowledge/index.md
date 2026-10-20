---
okf_version: "0.2"
---

# Helix Toolkit Knowledge Bundle

## Repository

* [Repository](/repository.md) - Current repository purpose, layout, and source references.
* [Build](/build.md) - SDK, shell, solution, and build commands.
* [Project Source Knowledge Graph](/graphify-database.md) - Generated Graphify database for C# and MSBuild source relationships.
* [CSharp Coding Conventions](/policies/csharp-coding-conventions.md) - Phased C#/.NET style, Logging, Assertions, testing, and compatibility policy.

## Architecture

* [Current Solution](/architecture/current-solution.md) - `Source/SilkToolkit.slnx` and the main project graph.
* [WPF Composition Contracts](/architecture/wpf-composition-contracts.md) - P3.0 ownership, transport, pixels, recovery and parallel integration boundaries.

## Projects

* [SilkCore](/projects/silkcore.md) - Core renderer, update/maintenance/visibility ordering, pre-pick octree preparation,
  GPU-only intersection attachment, paged selection-effect descriptors, native mesh-intersection compute/indirect
  drawing, mesh material facing/culling, fence-safe resource cleanup and reference-counted pool entry ownership.
* [SilkToolkit](/projects/silktoolkit.md) - Windows/WPF-facing toolkit, hidden native camera-input regressions,
  nonblocking frame completion, safe native lifetimes and restored original public CLR/XAML namespaces.
* [SilkAssimp](/projects/silkassimp.md) - Assimp-based model loading, transformed child export and mesh-index ownership.
* [ShaderBuilder](/projects/shaderbuilder.md) - HLSL shader build support, include/compiler/metadata invalidation and headless incremental regression.

## Testing

* [Silk Test Strategy](/testing/silk-tests.md) - xUnit v3 projects, categories, local commands, and non-blocking coverage reporting.
* [WPF Composition Phase 4](/testing/wpf-composition-phase4.md) - Public presentation integration, original-contract reviews and serial acceptance evidence.
* [WPF Composition Phase 5](/testing/wpf-composition-phase5.md) - Rendering/lifetime audits, actual backend measurements and recovery qualification limits.

## Backlog

* [D3D12 WPF Composition](/backlog/d3d12-wpf-composition.md) - Phased demand-driven composition plan and isolated GPU interoperability proof.
* [DX12 Migration Plan](/backlog/dx12-migration-plan.md) - Resumable phased Direct3D12 and DXC cutover plan.
* [AddTube Reimplementation](/backlog/addtube-reimplementation.md) - Port of upstream #2251 to Silk.NET math.
* [WPF Swap-Chain Canvas Plan](/backlog/wpf-swapchain-canvas-plan.md) - Rein WPF basierte IRenderCanvas-Implementierung als schaltbare Alternative zur WinForms-basierten Variante.

## References

* [OKF Specification](/references/okf-spec.md) - Local OKF v0.2 specification used for this bundle.


SilkAssimp knowledge records actual primitive-face conversion, internal/external context ownership, temporary
animation cleanup and independent PBR/texture-path fallback behavior, preserving child placement and mesh indices.

SilkAssimp now also records independent animation channels and the restored source export timebase; SilkCore records periodic noise, final-field normalization and permutation publication.

Toolkit input ownership now retains ordinary WPF mouse callbacks across the native child boundary; build
knowledge records explicit same-version CodeDom/CSharp package restoration instead of cache-only HintPaths.

SilkCore records source-faithful two-key overwrite/lookup and corrected association equality, with additive pair
carriers preserving established tuple enumeration and removal signatures.

SilkCore also records paged ordinary/peeling descriptors, headless capacity/lifecycle regressions and opt-in PIX instrumentation.

SilkAssimp records the SharpAssimp 6.0.12 upgrade with preserved animation/material ownership fixes;
remote wireframe additions coexist with current profiling, completed-fence and empty-scene SSAO behavior.

SilkCore records bounded attached-2D invalidation and repaint propagation after the visible Desktop startup fix.

SilkCore and SilkToolkit record the restored original host boundary backed by the actual native frame, retained
fence-safe context and original/current WPF property bindings.

The project concepts also cover application-owned native buffer serialization and original material pass selection.

SilkToolkit also owns the shared bounded WPF viewport-widget overlay, retaining its historical public namespace.
