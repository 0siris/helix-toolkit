# Feature Specification: AssimpNet to SharpAssimp Migration

**Feature Branch**: `silk-toolkit`
**Created**: 2026-10-11
**Status**: Draft
**Input**: Replace package reference `AssimpNet 5.0.0-beta1` with `SharpAssimp 6.0.12`
(upstream reference: helix-toolkit `38b6dc061`, PR #2375; tracks issue #3)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Load 3D models after the migration (Priority: P1)

A demo user opens any model-loading demo (e.g. `FileLoadDemo`) and loads
FBX, OBJ, glTF, STL, and PLY sample models. Every model that loaded before
the migration loads identically afterwards: same meshes, materials, and
textures, with no new errors or missing geometry.

**Why this priority**: Model import is the only user-visible surface of this
change. If it regresses, demos break visibly.

**Independent Test**: Load a fixed set of sample models (one per format:
FBX, OBJ, glTF, STL, PLY) through the importer and compare mesh counts,
vertex counts, and material presence before/after — deliverable as an
automated round-trip check.

**Acceptance Scenarios**:

1. **Given** the migrated importer, **When** loading each sample model,
   **Then** import succeeds with the same mesh/vertex counts as the
   pre-migration baseline.
2. **Given** an OBJ model, **When** importing with default settings,
   **Then** no valid faces are dropped (FindDegenerates stays disabled for
   `.obj` as fixed on `silk-toolkit`).

---

### User Story 2 - Export 3D scenes after the migration (Priority: P2)

A demo user exports a scene (e.g. via `RenderTechniqueImportExport` or model
export paths) to a supported format. Acceptance is verified by re-import
(see test below); opening in external tooling is out of scope for automated
acceptance.

**Why this priority**: Export is the second public surface; breakage is less
visible but corrupts user data.

**Independent Test**: Export a reference scene per supported format and
re-import it, verifying geometry and material survival. The reference scene
is the existing in-memory triangle scene pattern from
`AssimpImportExportTests` (`CreateTriangleScene` / `CreateRichTriangleScene`,
incl. bone/animation variant) — no demo scene copy.

**Acceptance Scenarios**:

1. **Given** the migrated exporter, **When** exporting the reference scene,
   **Then** re-import yields identical mesh counts and no errors.

### Edge Cases

- Models using bone-skinning/animation data must survive the migration
  (animation partials exist in both importer and exporter).
- glTF material keys (`glTFMatKeys.cs`) must resolve identically.
- Behavior differences between the bindings (e.g. post-process flag
  handling) must be documented if they change import results.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The `SilkAssimp` project MUST reference `SharpAssimp 6.0.12`
  and MUST NOT reference `AssimpNet` anymore.
- **FR-002**: All binding touch points MUST compile against SharpAssimp:
  `Importer.cs`, `ImporterPartial_Animation/Material/Mesh.cs`,
  `Exporter.cs`, `ExporterPartial_Animation/Material/Mesh.cs`,
  `Configurations.cs`, `Extensions.cs` (namespaces, type renames, enum
  mappings).
- **FR-003**: Import of FBX, OBJ, glTF, STL, and PLY sample models MUST
  produce results identical to the pre-migration baseline (mesh counts,
  vertex counts, materials, textures). Small fixtures run in the standard
  gate; the large Stanford models (bunny, armadillo, full dragon) run as a
  separate opt-in theory.
- **FR-004**: Export and re-import of a reference scene MUST succeed without
  errors or geometry loss.
- **FR-005**: The test gate (`Category!=Hardware&Category!=LargeModels`, 13 SilkAssimp
  tests) MUST stay green, extended by the round-trip checks from US1/US2.
- **FR-006**: Any intentional behavior change vs. AssimpNet MUST be recorded
  in the migration commit message. Round-trip checks assert strict equality
  by default; justified exceptions MUST be commented inline in the test.

### Key Entities

- **Sample model set**: one file per format (FBX, OBJ, glTF, STL, PLY) used
  as the before/after baseline.
- **Round-trip check**: automated import (and export+re-import) comparison
  producing mesh/vertex/material counts.

## Success Criteria *(mandatory)*

- Users load and export all sample-model formats with results identical to
  before the change (zero regressions in the round-trip checks).
- The full test suite passes with the new binding on the first green CI run.
- No `AssimpNet` reference remains anywhere in the solution.

## Assumptions

- SharpAssimp 6.0.12 is API-compatible enough that the migration stays
  within `Source/SilkAssimp` (no caller changes in demos), following the
  upstream precedent (`38b6dc061` touched only the Assimp projects).
- The native Assimp binaries ship through the SharpAssimp NuGet package,
  so no manual native-DLL handling is needed.
- `External/Logging` submodule availability in CI is unaffected.
- PLY round-trip fixtures are synthetic tetrahedra under `Models/ply/`
  (`tetra_ascii.ply`: float coords, `list uchar int` faces;
  `tetra_binary_little_endian.ply`: double coords, `list uchar uint` faces;
  `tetra_binary_big_endian.ply`: float coords, `list uchar int` faces).

## Clarifications

### Session 2026-10-11

- Q: How should the PLY baseline for the round-trip check be provided, given the repo has no `.ply` sample file? → A: Add minimal synthetic `.ply` test assets to the repo (Option A).
- Vendored under `Models/ply/` (see its README for sources/licenses): synthetic tetrahedra
  (ascii/LE/BE), FSU Burkardt files (`cube`, `cow` ascii + binary BE), three.js dolphins
  (ascii/LE/BE parity set), Stanford bunny (ascii), armadillo (binary BE) and dragon
  (decimated `res4` plus the full 34 MB recon).
- Q: What scope should the round-trip check cover — all formats incl. the large Stanford models, or small fixtures only? → A: Small fixtures in the standard gate, large models as opt-in theory (Option B).
- Q: How should the round-trip check handle behavior differences between AssimpNet and SharpAssimp? → A: Strict by default, justified exceptions commented inline (Option C).
