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


# Primitive, context and material ownership

Exported lines use pairs, points use one face per selected point (or every position when unindexed), and triangles
retain triples and their original attributes/skinning. Invalid indices fail before native scene publication.
Internally acquired AssimpContext instances are scoped across format checks, scene conversion and native calls;
external contexts stay caller-owned. Temporary animation lists clear even when scene conversion throws. Existing
native failure/event behavior and successful managed blob lifetime remain intact. Internal exact allocator balance
is not measured; external context usability and actual native blob/file primitive output are directly exercised.

PBR import retains independent metallic and roughness values for the general and glTF property families, including
roughness-only materials. General keys take precedence and the existing shininess fallback remains. Parent-directory
texture fallback joins actual directory components, preserving the other fallback paths and root-null guard.
These scoped tests do not certify every texture stream/channel or the separate animation timeline owner.

# Independent animation channels and source export timing

The importer samples the complete sorted union of position/rotation/scale times using the existing vector
Lerp/quaternion Slerp authority. Missing components use zero/identity/unit, endpoints hold, and the last key
at duplicate times wins. No source channel or artificial time-zero key is created. Ordered finite inputs
and a usable tick rate remain source preconditions. Actual importer duration and key times use file ticks,
with ImporterConfiguration.TickesPerSecond (25 by default) only when file ticks are zero.

Exporter.DefaultTicksPerSecond again exposes the original mutable static double, default 30, consumed by
exported rate, duration and component key times. ExportConfiguration.TickesPerSecond remains unused exactly
as in the pinned source; it is not silently substituted as a different default. Native assbin file reimport
proves rates 24/30/60, component values/durations and subsequent animation-state cleanup. The complete current
Assimp suite passes70 cases, including the earlier primitive/context/material and imported animation changes.
The configurable field is process-wide, matching the source; changes should be coordinated between exports.
These checks do not establish animated decoder equivalence for every format or visible/GPU skinning.
