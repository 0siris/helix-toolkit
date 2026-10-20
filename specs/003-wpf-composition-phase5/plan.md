# Implementation plan — WPF composition Phase 5

See [spec.md](spec.md), [tasks.md](tasks.md), [the phase plan](../../wpf-komposition-phasen-parallele-tasks-und-reviews.md)
and [Phase 4 evidence](../../knowledge/testing/wpf-composition-phase4.md).

## Execution and ownership

Windows/WPF, .NET 10, Silk.NET D3D12/11/9Ex, PowerShell 7. Use existing fixtures and product diagnostics;
add only the measurement/regression coverage needed by concrete audit findings. No speculative product instrumentation.
The three read-only children audit rendering, lifetime and documentation in isolated worktrees. They run no build,
test, GPU, IDE formatting/diagnostics or Graphify operations. Parent executes those gates serially and integrates fixes.
After the read-only audits, disjoint corrective packages may use separate implementation children; those children
also perform only source/static work. Native lifetime, adapter selection and measurement integration stay with the parent.

Measurements use identical settings and scene on each actual backend. Report warmup/sample counts, adapter identity,
wall versus process CPU versus GPU-clock interval semantics, capture/presentation boundaries and memory scope.
Timing is diagnostic, without machine-specific speed thresholds. Allocation/counter/ownership assertions remain strict.
No physical device removal or forced session/monitor changes are required to fabricate an unavailable test environment.

## Quality gates

Follow AGENTS.md and the Rider/ReSharper skills. Probe the selected solution before builds/tests; verify artifact
timestamps because incremental Rider successes previously retained stale assemblies. Full rebuild is justified when
that occurs. Standard filter matches the baseline: `Category!=Hardware&Category!=DX12`. Hardware/DX12 and visible
tests are explicitly authorized by Phase 5, run separately and serially. Compare failures against the exact eight
known baseline cases; do not exclude them or weaken assertions. Document warnings and unavailable physical checks.

Format changed C# only, inspect warning diagnostics and XML documentation. Regenerate Graphify locally after final
source changes, verify generated output/staging cleanup, update durable OKF concepts/log and finish with a clean local commit.

## Spec-Kit adaptation

The prerequisite script `.specify/scripts/bash/check-prerequisites.sh` is missing; `.specify/feature.json` selects
unrelated Assimp work. Resolve this explicitly to `F:/Repositories/helix-toolkit/specs/003-wpf-composition-phase5`.
Leave the Assimp pointer/tasks unchanged. No feature checklist or extension-hook file exists at setup; recheck hooks
after execution. Existing repository ignores cover build/test/generated artifacts. This is an adaptation, not a
claim that the missing prerequisite script passed.
