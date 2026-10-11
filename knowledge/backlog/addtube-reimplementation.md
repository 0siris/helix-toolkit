---
type: BacklogPlan
title: Port upstream MeshBuilder.AddTube reimplementation
description: Resumable plan for porting upstream #2251 (AddTube reimplementation) to Silk.NET math.
tags:
  - backlog
  - geometry
  - meshbuilder
  - upstream
timestamp: 2026-10-11T00:00:00+02:00
generated: false
status: planned
verified: 2026-10-11
sources:
  - ../../Source/SilkCore/Geometry/MeshBuilder.cs
stale_after: 180d
---

# Port upstream MeshBuilder.AddTube reimplementation

## Problem

Our `Source/SilkCore/Geometry/MeshBuilder.cs` is still the pre-#2251 version of `AddTube` and its helpers:

- `GetCircleSegment` returns `thetaDiv` points with a `(thetaDiv - 1)` divisor (off-by-one segment layout).
- `GetCircle` winding is unflipped; upstream flipped it (`-cos`/`+sin`), which changes face orientation and
  normals of all tube/cylinder-derived geometry.
- No `GetRectangle` helper.
- Old `AddTube` framing fails for opposite-direction paths and several special cases (fixed upstream).
- Old overloads use `float[]` for values/diameters.

## Upstream reference

- Upstream commit `8c9d56d15` "Reimplement MeshBuilder.AddTube (#2251)"
  (~478 lines in `Source/HelixToolkit.Geometry/Geometry/MeshBuilder.cs`).
- `GetCircleSegment` now returns `thetaDiv + 1` points, `AddTube` overloads take `IList<float>`,
  new `GetRectangle(width, height)`.

## Scope for the port

1. Port `GetCircleSegment` (`thetaDiv + 1` points), the `GetCircle` winding flip, and `GetRectangle`
   to Silk.NET math (`Vector2D<float>`).
2. Port the reimplemented `AddTube` overloads (`float[]` -> `IList<float>`) and adapt callers
   (`TransformManipulator3D`, `OctreeDemo`, demos).
3. Assess the winding-flip impact on all tube/cylinder-derived meshes (normals/lighting) — the risky part.
4. Add unit tests pinning tube indices/normals (see `MeshBuilderTests` for the pattern).
5. Visual verification via screenshot comparison in the manipulator and octree demos before merge.

## Acceptance

- Full suite (`Category!=Hardware`) green.
- No lighting regression on existing tube-based visuals (manipulator handles, octree demo).

## Origin

Found during upstream comparison (fork base Dec 2023 vs upstream `develop3`); GitHub Issues are disabled
in this repository, so it is tracked here for later pickup.
