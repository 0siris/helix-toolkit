---
type: Project
title: SilkAssimp
description: Assimp model loading integration for SilkCore-based applications.
resource: ../../Source/SilkAssimp/SilkAssimp.csproj
tags: [project, assimp, model-loading]
timestamp: 2026-07-08T00:00:00+02:00
---

# Project

`SilkAssimp` targets `net10.0` and references [SilkCore](/projects/silkcore.md).

# Dependencies

Scene export preserves each direct geometry child's local transform beneath its original parent instead of
flattening mesh assignments onto that parent. Exported geometry/material pairs receive contiguous mesh indices;
the geometry cache index is not an Assimp mesh index. This remains correct when unsupported geometry is skipped
or when the same geometry is exported with multiple materials, and prevents invalid native exporter accesses.

The project uses `AssimpNet` version `5.0.0-beta1`.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkCore](/projects/silkcore.md)

# Citations

[1] [SilkAssimp.csproj](../../Source/SilkAssimp/SilkAssimp.csproj)
