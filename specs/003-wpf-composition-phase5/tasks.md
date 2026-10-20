# WPF composition Phase 5 tasks

Baseline: `acc913f22`; 830/838 standard cases, eight known failures, 151/151 selected acceptance cases.

- [X] T001 Resolve the Phase 5 context, check prerequisites/hooks/ignores and record the current baseline.
- [X] T002 [P] P5.2 read-only rendering audit: map both backends, feature coverage, dynamic demand and GPU collisions;
  return concrete defects, test names and remaining proof gaps.
- [X] T003 [P] P5.3 read-only lifetime audit: recovery, minimize/reload, monitor/DPI, manager/shared generations,
  bounded detach/drain and native ownership; return actionable findings and physical-environment limits.
- [X] T004 [P] P5.4 read-only documentation audit: reconcile defaults, original contracts, supported configuration,
  capture/MSAA limits and claims against source plus runtime evidence.
- [X] T005 Resolve concrete audit findings and add focused regression/measurement coverage using existing fixtures.
- [X] T005a [P] Correct additive RGB effect parity while preserving premultiplied coverage; add contrasting-color GPU checks.
- [X] T005b [P] Restore postprocessing of screen-space children through both backends; add a focused actual-viewport check.
- [X] T005c [P] Remove warm GPU-collision reuse traversal allocations while preserving complete geometry validation.
- [X] T005d Coordinate confirmed removed singleton retirement across suspended/mixed owners and normal HWND polling;
  preserve CPU borrowing, bounded detachment, real completions and zero warm allocations.
- [X] T005e Resolve R3 recovery boundaries: standalone public-render initialization retry and queued recovery
  exceptions routed through the existing renderer error boundary, retaining failed ownership for retry.
- [X] T005f Resolve R4's deferred CPU-borrow teardown error boundary and equivalent automatic WPF paths;
  contain a second teardown failure with retained ownership and actual cancellation/retry regressions.
- [X] T005g Resolve R5's between-frame automatic HWND composition catch through the same bounded owner reporter;
  preserve explicit synchronous errors and qualify the actual automatic composition entry.
- [X] T005h Resolve R6's queued manager/shared preflight removal boundary with owner-aware bounded reporting,
  retained transactions and no failure repost; preserve synchronous and application-requested disposal semantics.
- [X] T005i Resolve R7's automatic membership fault reset: new mismatched consumers retain the stopped shared
  transaction until an explicit manager/resource request; registration discovery uses the bounded owner boundary.
- [X] T006 P5.1 serial quality gate: formatting/diagnostics, fresh solution build, standard comparison and selected
  hardware/DX12 plus visible acceptance. Keep exact baseline failures and allocation limits visible.
- [X] T007 P5.5 compare matched actual backends: CPU/GPU-clock evidence, update-to-published/capture latency,
  process/managed memory, warm heap, copies, settled idle and active animation; document all measurement boundaries.
- [X] T008 Obtain an independent final review, resolve must-fix findings and review corrective deltas in fresh tasks.
- [X] T009 Update phase plan, OKF report/index/log and graph snapshot; regenerate Graphify locally, validate links/XML/
  whitespace, inspect post-execution hooks, commit locally and report actual outcomes plus remaining limits.
