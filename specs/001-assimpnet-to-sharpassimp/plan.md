# Implementation Plan: AssimpNet to SharpAssimp Migration

**Spec**: `spec.md` | **Research**: `research.md` | **Branch**: `silk-toolkit`
**Issue**: [#3](https://github.com/0siris/helix-toolkit/issues/3)

## Technical Context

- Single target framework `net10.0-windows`; plain `PackageReference` swap,
  no native-DLL handling (ships in-package).
- Touch points (all under `Source/SilkAssimp`):
  `Importer.cs`, `ImporterPartial_Animation/Material/Mesh.cs`, `Exporter.cs`,
  `ExporterPartial_Animation/Material/Mesh.cs`, `Configurations.cs`,
  `Extensions.cs`.
- Key risk: `Vector3`/`Vector2` name collisions with engine global usings —
  qualify as `SharpAssimp.*` or file-local aliases.
- Key risk: field rename `A1..D4` → `M11..M44` is position-identical
  (verified via upstream diff), but each conversion needs re-checking.
- No constitution file in repo — project conventions come from `AGENTS.md`
  (4-space C#, file-scoped namespaces, XML docs on non-generated
  declarations, additive-only public APIs) and `knowledge/`.
- Contracts: skipped — the `SilkAssimp` public surface stays unchanged by
  design (internal binding swap only); no new external interfaces.

## Constitution Check

No `.specify/memory/constitution.md` present; evaluated against `AGENTS.md`
instead: no new abstractions, existing test patterns (`Category` traits),
no public API removals. No violations. Post-design re-check: the `LargeModels`
category adds a test trait only — gate change is limited to the workflow
filter string. No violations.

## Phases (for `/speckit-tasks`)

- **Phase 0 — Baseline**: record pre-migration counts for the sample set
  (quickstart §1); commit nothing.
- **Phase 1 — Swap**: `AssimpNet` → `SharpAssimp 6.0.12` reference;
  port `Configurations.cs`, `Importer.cs`, importer partials.
- **Phase 2 — Export side**: port `Exporter.cs`, exporter partials,
  `Extensions.cs` conversions.
- **Phase 3 — Tests**: round-trip tests (small fixtures in gate, Stanford
  set as `LargeModels` theory); workflow filter excludes `LargeModels`
  from the default gate.
- **Phase 4 — Verify**: full gate + manual large run + CI green; record
  deltas in commit message per FR-006.
