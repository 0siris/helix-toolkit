# WPF composition Phase 5 tasks

Baseline: `acc913f22`; 830/838 standard cases, eight known failures, 151/151 selected acceptance cases.

- [X] T001 Resolve the Phase 5 context, check prerequisites/hooks/ignores and record the current baseline.
- [X] T002 [P] P5.2 read-only rendering audit: map both backends, feature coverage, dynamic demand and GPU collisions;
  return concrete defects, test names and remaining proof gaps.
- [X] T003 [P] P5.3 read-only lifetime audit: recovery, minimize/reload, monitor/DPI, manager/shared generations,
  bounded detach/drain and native ownership; return actionable findings and physical-environment limits.
- [X] T004 [P] P5.4 read-only documentation audit: reconcile defaults, original contracts, supported configuration,
  capture/MSAA limits and claims against source plus runtime evidence.
- [ ] T005 Resolve concrete audit findings and add focused regression/measurement coverage using existing fixtures.
- [X] T005a [P] Correct additive RGB effect parity while preserving premultiplied coverage; add contrasting-color GPU checks.
- [X] T005b [P] Restore postprocessing of screen-space children through both backends; add a focused actual-viewport check.
- [X] T005c [P] Remove warm GPU-collision reuse traversal allocations while preserving complete geometry validation.
- [ ] T005d Coordinate confirmed removed singleton retirement across suspended/mixed owners and normal HWND polling;
  preserve CPU borrowing, bounded detachment, real completions and zero warm allocations.
- [ ] T006 P5.1 serial quality gate: formatting/diagnostics, fresh solution build, standard comparison and selected
  hardware/DX12 plus visible acceptance. Keep exact baseline failures and allocation limits visible.
- [ ] T007 P5.5 compare matched actual backends: CPU/GPU-clock evidence, update-to-published/capture latency,
  process/managed memory, warm heap, copies, settled idle and active animation; document all measurement boundaries.
- [ ] T008 Obtain an independent final review, resolve must-fix findings and review corrective deltas in fresh tasks.
- [ ] T009 Update phase plan, OKF report/index/log and graph snapshot; regenerate Graphify locally, validate links/XML/
  whitespace, inspect post-execution hooks, commit locally and report actual outcomes plus remaining limits.
