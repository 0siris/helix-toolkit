# Tasks — WPF composition Phase 4

Scope: Phase 4 only. `[P]` tasks use delegated subagents with isolated worktrees and disjoint files.
Baseline: `619feb80d`; 729/737 standard cases passing, eight known failures. See [plan.md](plan.md).

## Setup and original contracts

- [X] T001 Resolve this feature context, check hooks/checklists, verify repository ignores and record baseline evidence.
- [X] T002 [P] Audit original property aliases, initialization/lifecycle and WPF input against `c59fcc424` and
  `6215e5c0b^`; return concrete sites/requirements without editing source.

## Integration wave

- [X] T003 [P] P4.2: Implement shared-consumer invalidation and node lifetime in
  `Source/SilkToolkit/Controls/ModelContainer3DX.cs`, necessary Core ownership paths and dedicated shared-scene tests.
  Coordinate exact integrator hooks before changing callers. Preserve other consumers across unload/disposal.
- [X] T004 [P] P4.1: Add public default-false backend selection, initialization lock, template wiring, DPI and complete
  image-property/alias invalidation in `Source/SilkToolkit/Controls/Viewport3DX*` and relevant canvas/template files.
  Write contract tests before implementation where needed; integrate T002 findings. Can run alongside T003.
- [X] T005 P4.3: Integrate T003, manager invalidation/disposal/reinitialization, external ownership and functional
  2D selection; establish one committed integration point after T003/T004 for the test wave.
- [X] T005a [P] Complete safe HWND retirement/reinitialization and reentrant unload/disposal in
  `Source/SilkToolkit/Controls/D3D12PresentationSurface.cs` after T004; coordinate integrator frame guards.

## Parallel test wave (depends on T005)

- [ ] T006 [P] P4.4: Add dedicated real host/frame/context/scene/capture/completion and manager/shared-scene tests.
- [ ] T007 [P] P4.5: Explicitly select HWND in existing native-specific tests and examples; preserve native input behavior.
- [ ] T008 [P] P4.6: Add WPF default/XAML/backend lock/DPI/input/demand/2D/reload/template regression cases from the full
  phase-plan matrix. Coordinate disjoint test files with T006/T007; do not weaken existing assertions.

## Serial acceptance and phase gate

- [X] T009 P4.7: Verify visible WPF controls over real D3D12 output, ZIndex, clipping, VisualBrush, Blur and Adorner;
  preserve `ViewportWidgetOverlay` and record evidence/limits.
- [ ] T010 Run independent integration/original-contract/heap reviews; resolve findings and re-review deltas.
- [ ] T011 Format changed files, inspect diagnostics, build and run standard plus targeted/hardware/heap checks
  serially. Compare the existing baseline failures and document all unexecuted or environment-limited cases.
- [ ] T012 Update plan/OKF reports/log and task markers, regenerate Graphify, check documentation/whitespace,
  inspect post-implementation hooks, commit locally and stop before Phase 5.
