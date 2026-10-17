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

## Projects

* [SilkCore](/projects/silkcore.md) - Core renderer, paged selection-effect descriptors, native mesh-intersection compute/indirect
  drawing, mesh material facing/culling, fence-safe resource cleanup and reference-counted pool entry ownership.
* [SilkToolkit](/projects/silktoolkit.md) - Windows/WPF-facing toolkit, nonblocking frame completion and safe native lifetimes.
* [SilkAssimp](/projects/silkassimp.md) - Assimp-based model loading, transformed child export and mesh-index ownership.
* [ShaderBuilder](/projects/shaderbuilder.md) - HLSL shader build support, include/compiler/metadata invalidation and headless incremental regression.

## Testing

* [Silk Test Strategy](/testing/silk-tests.md) - xUnit v3 projects, categories, local commands, and non-blocking coverage reporting.

## Backlog

* [DX12 Migration Plan](/backlog/dx12-migration-plan.md) - Resumable phased Direct3D12 and DXC cutover plan.
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
