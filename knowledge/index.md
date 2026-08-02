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

* [SilkCore](/projects/silkcore.md) - Core renderer and shared runtime library.
* [SilkToolkit](/projects/silktoolkit.md) - Windows/WPF-facing toolkit built on SilkCore.
* [SilkAssimp](/projects/silkassimp.md) - Assimp-based model loading integration.
* [ShaderBuilder](/projects/shaderbuilder.md) - HLSL shader build support project.

## Testing

* [Silk Test Strategy](/testing/silk-tests.md) - xUnit v3 projects, categories, local commands, and non-blocking coverage reporting.

## Backlog

* [DX12 Migration Plan](/backlog/dx12-migration-plan.md) - Parallel opt-in Direct3D12 backend migration plan.
* [WPF Swap-Chain Canvas Plan](/backlog/wpf-swapchain-canvas-plan.md) - Rein WPF basierte IRenderCanvas-Implementierung als schaltbare Alternative zur WinForms-basierten Variante.

## References

* [OKF Specification](/references/okf-spec.md) - Local OKF v0.2 specification used for this bundle.
