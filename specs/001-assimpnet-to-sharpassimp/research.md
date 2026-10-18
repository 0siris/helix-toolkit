# Research: AssimpNet to SharpAssimp Migration

## Decision: target version SharpAssimp 6.0.12

- NuGet registry lists `6.0.1`–`6.0.12`; latest is **6.0.12** (spec bumped from 6.0.5).
- Upstream migrated to 6.0.5 and later moved to 6.0.8; the 6.0.x line carries
  native-bin updates plus binding fixes. API parity 6.0.8→6.0.12 is verified
  at compile time (any drift surfaces as build errors in the touched files).
- Plain `PackageReference`; native runtimes ship inside the package
  (upstream needs no special `PrivateAssets`/`IncludeAssets` handling).

## Decision: namespace and API mapping

Source: upstream migration commit `38b6dc061` (diff of `Extensions.cs`,
`Exporter*.cs`, csproj) compared against our `Source/SilkAssimp` usage:

| AssimpNet (current) | SharpAssimp (target) |
| --- | --- |
| `using Assimp;` / `Assimp.Configs` / `Assimp.Unmanaged` | `using SharpAssimp;` (+ sub-namespaces as needed) |
| `Matrix4x4` fields `A1..D4` | `Matrix4x4` fields `M11..M44` (System.Numerics naming) |
| mutating `matrix.Transpose()` | static `Matrix4x4.Transpose(matrix)` |
| `Vector3D`, `Vector2D` | `Vector3`, `Vector2` |
| `AssimpContext`, `PostProcessSteps`, `PropertyConfig`, `TextureType` | same names under `SharpAssimp` |

Collision risk: our `SilkCore` global usings already define `Vector3` /
`Vector2` (Silk.NET aliases). Touch points that mix binding vectors with
engine vectors MUST use fully qualified `SharpAssimp.Vector3` /
`SharpAssimp.Vector2` or file-local aliases (precedent in our code:
`using Animation = Assimp.Animation`, `using TextureType = Assimp.TextureType`).

## Decision: alias-heavy files need line-level porting, not sed replace

`Extensions.cs` (matrix/vector conversions), `ImporterPartial_Mesh.cs`
(bones, morph targets), `ImporterPartial_Animation.cs`, exporter partials —
each usage must be re-checked because field names change semantics position
(`A1` == `M11`, verified from the upstream diff), not just spelling.

## Decision: baseline before swap

Record pre-migration import counts (mesh/vertex/material per sample file)
BEFORE changing the package reference; the round-trip tests pin those
numbers. Sample set: `Models/obj/bunny`, `Models/glTF/robot nier`,
`Models/stl/cube.stl`, `Models/ply/tetra_ascii.ply` (+ LE/BE variants),
FBX `Solus_The_Knight.fbx`; large Stanford models only in the opt-in theory.

## Alternatives considered

- Stay on AssimpNet 5.0.0-beta1: rejected per issue #3 decision (upstream
  parity, binding unmaintained).
- Pin SharpAssimp 6.0.8 (upstream's exact version): rejected — 6.0.12 is the
  latest stable in the same major line; compile verifies compatibility.
