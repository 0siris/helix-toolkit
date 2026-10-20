# WPF composition — Phase 5

User authorization: implement Phase 5 of
[the approved phase plan](../../wpf-komposition-phasen-parallele-tasks-und-reviews.md).
Baseline: `acc913f22`; Phase 4 standard results 830/838, with eight known failures; selected acceptance 151/151.

## Requirements

- Qualify both actual presentation backends against the common renderer and original host contracts.
- Audit shadows, OIT, SSAO, FXAA, postprocessing, screen-space/Scene2D, productive dynamic sources and GPU collisions.
- Audit minimization/reload, frontbuffer/device recovery, monitor/DPI changes, manager epochs, shared consumers and teardown.
- Compare identical scene/camera/render dimensions: CPU and GPU timing evidence, input-to-output latency, memory,
  allocations and transport counters. Distinguish queue elapsed time from isolated GPU execution and physical display latency.
- Compare settled and animated operation: WPF adds no scene/copy/publication frames after demand settles;
  productive continuous sources keep submitting and removing them allows demand to settle.
- Preserve public APIs, default-false selection, exact allocation assertions and existing baseline failures.
- Document actual evidence and unsupported/unavailable scenarios without claiming physical fault, monitor or touch proof
  from synthetic callbacks. MSAA and scaled capture remain explicit limitations unless separately implemented and tested.

## Scope and completion

P5.2–P5.4 are parallel read-only audits. The integrator owns fixes, focused tests, serial runtime tooling and measurements.
Complete feasible automated and visible acceptance, record environmental/physical qualification limits, obtain an
independent closure review, update OKF/Graphify and commit locally. No push or application-checkout changes.
