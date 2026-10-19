# Implementation plan — WPF composition Phase 4

Specification: [spec.md](spec.md). Tasks: [tasks.md](tasks.md).
Architecture: [P3 contracts](../../knowledge/architecture/wpf-composition-contracts.md).
Detailed acceptance: [phase plan](../../wpf-komposition-phasen-parallele-tasks-und-reviews.md).

## Technical context

Windows/WPF, .NET 10, Silk.NET Direct3D12/11/9Ex, PowerShell 7. The internal P3 bridge, passive `DPFCanvas`,
`D3D12WpfPresentation`, common frame recorder and bounded render-demand coordinator are already implemented.
One presentation owns its queue/context/fence; bridge targets are borrowed. Preserve strict serial backpressure,
immutable WPF-bound slots, explicit detach and bounded teardown. Keep initialization/recovery separate from scene demand.

## Ownership and execution

- Integrator: final ownership of central `Viewport3DX*`, manager lifecycle, DPI/2D integration,
  visible acceptance and all runtime tooling. T004 delegates exclusive presentation/property edits to one child; the integrator edits those files only after merging that package. T005a delegates the native surface exclusively while manager integration uses separate files. Only the integrator builds/runs tests/GPU/Graphify, serially.
- Shared-scene child: `ModelContainer3DX`, minimal Core attachment/lifetime changes and dedicated tests. Coordinate
  any required Viewport hooks before integration; do not edit central Viewport files.
- Original-contract investigator: read-only mapping of original property/lifecycle/input behavior for the integrator.
- After integration: independent child owners for new host/frame tests, HWND regressions/examples and WPF behavior tests.
  Separate worktrees start from a common committed integration point. Each task returns an owned-files-only commit.
- Independent reviews cover correctness, historical behavior, ownership/reentrancy and heap pressure. Each new review
  round gets a new delegated task with prior findings/responses. No child runs builds/tests/GPU or Graphify.

## Repository conventions and gates

Follow `AGENTS.md`; full XML documentation on new C# declarations, additive public APIs, existing LoggerLib/Assertions.
Use Rider/ReSharper workflow and scoped formatting, then the full standard filter `Category!=Hardware&Category!=DX12`.
Hardware/DX12 runs are explicitly authorized for this phase and serial; visible test windows were already authorized.
Retain exact allocation assertions and the Core test-host-only GC qualification established in P3.
Regenerate Graphify locally after final source changes; record counts/warnings and update durable OKF knowledge/log.
No push and no application checkout edits.

## Spec-Kit setup adaptation

The repository contains only the Assimp feature pointer and no `.specify/scripts/bash/check-prerequisites.sh`.
The prescribed prerequisite command was attempted and failed with `No such file or directory`. This feature's
context is therefore resolved explicitly to `F:/Repositories/helix-toolkit/specs/002-wpf-composition-phase4`.
Required `spec.md`, `plan.md` and `tasks.md` are derived from the existing approved plan. No checklist directory,
constitution or extension hooks exists for this feature. Do not execute or alter the unrelated Assimp task list/pointer.
This records the fallback; it is not a claim that the missing prerequisite script passed.
