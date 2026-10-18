# Tasks: AssimpNet to SharpAssimp Migration

**Spec**: `spec.md` | **Plan**: `plan.md` | **Issue**: #3

## Dependencies

- Phase 2 (baseline) blocks Phase 3–4 (numbers must exist before assertions).
- US1 (import) and US2 (export) are otherwise independent; US2 reuses the
  binding swap from US1, so implement US1 first (MVP), then US2.
- Phase 5 (polish/CI) last.

## Phase 1: Setup

- [X] T002 Record pre-migration import counts for the sample set (bunny.obj, scene.gltf, cube.stl, tetra_ascii.ply, Solus_The_Knight.fbx) in `specs/001-assimpnet-to-sharpassimp/baseline.md` as a table (file | meshes | vertices | materials)

## Phase 2: Foundational (binding swap)

- [X] T003 Replace the `AssimpNet 5.0.0-beta1` reference with `SharpAssimp 6.0.12` in `Source/SilkAssimp/SilkAssimp.csproj`
- [X] T004 Port `using Assimp[.Configs/.Unmanaged]` to `SharpAssimp` namespaces in `Source/SilkAssimp/Assimp/Configurations.cs`
- [X] T005 [P] Verify `Source/SilkAssimp/SilkAssimp.csproj` restores cleanly (`dotnet restore`)

## Phase 3: US1 – Import after migration (P1)

Goal: all sample models import identically to baseline.
Independent test: T009–T011 green.

- [X] T006 [US1] Port `Source/SilkAssimp/Assimp/Importer.cs` (`AssimpContext`, `PostProcessSteps`, `PropertyConfig`; keep the OBJ `FindDegenerates` exclusion)
- [X] T007 [US1] Port `Source/SilkAssimp/Assimp/ImporterPartial_Mesh.cs` (bones, morph-target attachments, `HasBones`/`HasMeshAnimationAttachments` APIs)
- [X] T008 [US1] Port `Source/SilkAssimp/Assimp/ImporterPartial_Material.cs` and `Source/SilkAssimp/Assimp/ImporterPartial_Animation.cs` (`TextureType` alias, `Animation` alias)
- [X] T009 [US1] Add import round-trip tests pinning baseline mesh/vertex/material counts in `Source/SilkAssimp.Tests/` (new file, `Category=Unit`, small fixtures incl. `Models/ply/tetra_*.ply`)
- [X] T010 [US1] Add Stanford opt-in theory (`Category=LargeModels`: bunny, armadillo, full dragon) in `Source/SilkAssimp.Tests/`
- [X] T011 [US1] Build `Source/SilkAssimp/SilkAssimp.csproj` with zero errors and run the US1 tests green

## Phase 4: US2 – Export after migration (P2)

Goal: export + re-import round-trips without loss.
Independent test: T015 green.

- [X] T012 [US2] Port `Source/SilkAssimp/Assimp/Exporter.cs` (`AssimpContext`, winding-order flags)
- [X] T013 [US2] Port `Source/SilkAssimp/Assimp/ExporterPartial_Mesh.cs`, `Source/SilkAssimp/Assimp/ExporterPartial_Material.cs`, `Source/SilkAssimp/Assimp/ExporterPartial_Animation.cs`
- [X] T014 [US2] Port matrix/vector conversions (`A1..D4` → `M11..M44`, static `Transpose`) in `Source/SilkAssimp/Assimp/Extensions.cs`, qualifying `SharpAssimp.Vector3/Vector2` against engine global usings
- [X] T015 [US2] Add export round-trip test (export reference scene, re-import, compare counts) in `Source/SilkAssimp.Tests/` (extend the `CreateTriangleScene` pattern in `AssimpImportExportTests.cs`)
- [X] T019 [US2] Port the test project itself to SharpAssimp (`using Assimp` → `using SharpAssimp`, `Matrix4x4` 16-arg ctor, `Vector3D` in `Source/SilkAssimp.Tests/AssimpImportExportTests.cs` and `AssimpExtensionsTests.cs`)

## Phase 5: Polish & cross-cutting

- [X] T016 Exclude `LargeModels` from the default gate filter in `.github/workflows/silk-tests.yml` and document the manual large run
- [X] T017 Full solution gate green (`Category!=Hardware&Category!=LargeModels`) plus manual `Category=LargeModels` run
- [X] T018 Verify no `AssimpNet` reference remains (`Select-String` over `Source/`) and record intentional deltas in the commit message per FR-006

## Parallel opportunities

- T004 + T005 (different concerns, same phase).
- T007 + T008 (different files, shared binding knowledge from T006).
- T012 + T013 + T014 (different files; T015 after all three).

## MVP scope

US1 only (T001–T011): import parity with round-trip tests. US2 follows.
