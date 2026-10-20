---
type: VerificationReport
title: WPF composition Phase 5 acceptance
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-20T09:59:18Z }
status: stable
verified: 2026-10-20
sources:
  - resource: ../../specs/003-wpf-composition-phase5/tasks.md
  - resource: ../../wpf-komposition-phasen-parallele-tasks-und-reviews.md
  - resource: ../../Source/SilkToolkit.Tests/P5PresentationComparisonTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5DeviceRecoveryTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5WpfRecoveryAcceptanceTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5ScreenSpacePostEffectTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5DynamicBackendTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5MultiOwnerRecoveryTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5RecoveryDiscoveryTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5QueuedRecoveryFailureTests.cs
  - resource: ../../Source/SilkCore.Tests/P5AdditiveEffectParityTests.cs
  - resource: ../../Source/SilkCore.Tests/MeshIntersectionGpuTests.cs
---

# Scope

The user authorized Phase 5 on 2026-10-20. Baseline `acc913f22` passed 830/838 standard cases and 151/151 selected
acceptance cases. Phase 4's dated report remains historical evidence. Parent-owned builds, tests, GPU work and
measurements run serially; isolated children perform source/static audits and scoped corrections only.

# Findings and corrections

| Finding | Correction and evidence |
| --- | --- |
| Premultiplied output replaced original additive destination RGB | Preserve additive `One/One` RGB and summed bounded coverage; contrasting opaque/transparent Bloom and XRay readbacks |
| Tagged screen-space children omitted postprocessing | Per-root candidates, local camera/viewport/scissor for geometry, full-sized blur/composition intermediates; both actual backends verify exact off/on/off restoration |
| Collision reuse allocated LINQ traversals | Indexed full-geometry/owner checks; actual shared/private GPU generations, indirect arguments and pixels retained |
| D3D11-only removal could trap a pending copy | Confirm removal before abandoning a failed query; drain live D3D12 work, detach, then release; live-device failures preserve ownership |
| Removed HWND device could not drain | Skip impossible waits only after confirmed removal, abandon pending work without manufacturing completion, retain observed notifications |
| Configured adapter index ignored | Exact DXGI selection without fallback; productive D3D12 and D3D9 bridge LUID checks |
| Native raw-reference transfers leaked | Balance factory and bridge/probe raw acquisitions after adding `ComPtr` constructors; fresh real device-removal tests expose singleton retention |

The first independent closure review additionally identified multi-owner removed-singleton coordination and normal
HWND completion polling as must-fix recovery boundaries. A weak registry now coordinates exact native-device
generations across owners and dispatchers, preserving CPU borrows and real copy/detach boundaries. Healthy
polls do not mutate registry membership. A second review identified discovery when all older owners are inactive
and automatic adapter probing that misclassified removal as unsupported hardware. Coalesced dispatcher probes,
disposal-time discovery and preserved removal HRESULTs address those cases. The third review identified two
additional P2 boundaries: standalone public rendering must retry recoverable initialization, and queued recovery
exceptions must enter the existing viewport error boundary. Commit `b8f4b2f7e` adds guarded public-render retries
and retained queued wrappers, while preserving synchronous exception propagation. If the error boundary's single
teardown retry also fails, the failure is logged and exposed through `RenderException`; native ownership and the
registry barrier remain retained with automatic retries stopped. Nine new cases cover synchronous behavior and
real queued probe/cross-dispatcher detach failures, including a second teardown failure. All nine pass in the
69-case corrective closure run; independent R4 review remains pending. Formatting-only commit `d1367fc69`
preserves the correction's semantics.

R4 found one remaining P2: CPU borrowing defers native teardown to `ApplyPendingRetirement`, which still called
the error boundary without containing its second failure. Commit `9082db730` shares the existing bounded
reporter across queued removal, deferred retirement and deferred resize. The analogous automatic WPF rendering
and suspended-transport callbacks use the same containment too; synchronous caller-visible rendering errors remain
synchronous. Both dedicated CPU-borrow regressions pass in the 81-case corrective run; fresh R5 review remains
pending. Product correction `9082db730` and regression `9f1ec9673` are integrated with formatting-only `5bc20fb2a`.

R5 source-closed R4's deferred paths but identified the automatic HWND composition catch before frame reservation.
Removal between ticks could still propagate the second teardown failure through `Viewport3DX`. Commit `eda84f518`
routes that catch through the same owner reporter and ignores stale native callbacks when no native surface remains.
Explicit synchronous rendering keeps its exception contract. The corresponding actual composition-entry regression
passes in the 82-case corrective run. Regression `4218a17a5` and formatting `f3e6d62cf` are integrated.

R6 source-closed the automatic composition correction and identified removal during a queued manager preflight.
Changing a manager inside `SceneNode.Update`, then removing the device before the queued operation, reaches
removal retirement from `HasPendingGpuWork`/`CompletePendingFrame` outside the rendering catch. The queued
manager callback previously caught only timeouts. The matching shared-container asynchronous preflight now uses
owner-aware reporting and retained transaction state. Explicit/synchronous manager operations and
application-requested deferred disposal keep their existing exception contract; this is not a universal application
lifecycle exception-swallowing policy. A source audit confirmed those disposal semantics predate Phase 5.

Commit `ed6edaef9` implements the queued-manager correction, using the existing WPF reporter exposed by
`3075cdc36`. Both manager queues retain pending work behind a fault flag and stop timer/operation reposts.
Explicit manager/resource requests reset the flag. The shared transaction reports only its failing consumer and
suspends before that report, so successful peer retirement cannot schedule an accidental retry. Synchronous
manager operations retain propagation and timeout retries retain their previous behavior. ReSharper warning
diagnostics complete without errors; the Container formatter's internal exception was resolved through Rider's
format-only fallback (`5e9841239`, one blank line). The four real queued-manager/shared cases pass after the
test-only entry isolation described below; R7's additional membership correction is independently source-closed.

R7 identified membership-triggered synchronization incorrectly clearing the shared failure gate. Commit `4a404f0c9`
distinguishes automatic registration from an explicit manager request, preserves the gate during registration and
uses bounded owner reporting if registration itself first discovers removal. Explicit manager/resource requests
remain the reset boundary. Regression strengthening is integrated in `5064efb8e`. Fresh R8 independently
reviewed `acc913f22..4a404f0c9` without open P1/P2 findings. The four R6 cases are
integrated in `971c857ac` with whitespace-only `4c4962268`; fresh build `30a6aabc-42a1-4aca-9fc3-61425aee0b07`
succeeds, and their intended preflight stacks are checked rather than assuming callback order.

The outline fixture originally assumed additive blending. Original `BsGlowBlending` is `One/InverseSourceAlpha`,
so its final shader requires `R+B=255±1` over opaque blue. That source-backed equation replaces the incorrect
blue-channel invariant; placement, affected-pixel count, coverage and exact restoration checks remain strict.

# Runtime evidence

- Rider build `3eb59a40-6c1e-40de-bdcf-c94db5bd2a20` succeeds with the R3 correction. Fresh Toolkit/test DLL
  timestamps exceed their changed source timestamps. All five corrected files received ReSharper formatting and
  warning diagnostics without error findings; formatting was independently checked as whitespace-only.
- Fresh solution rebuild `bc9156a6-bc51-4db6-9c1b-10cf31d3dc06` succeeded after an incremental success retained a
  stale Core DLL. Timestamp verification exposed the stale binary; the corrected factory was then actually tested.
- `wpf-phase5-focused-r2`: Core 26/26 and Toolkit 13/13, including actual isolated-device removal and subsequent
  rendering in both backends, configured adapter identity, screen-space effects and routed wheel input.
- `wpf-phase5-visible-recovery-r1`: 3/3. Real Window `IsIconic` changes false/true/false; restored material produces
  a fresh blue image/readback. Unchanged minimize intervals add no scene/copy/publication frames.
- Actual D3DImage read-only-property availability injection runs its real event subscription chain and retains
  unavailable demand; return first rebinds unchanged pixels without scene/copy work, then publishes changed material.
  A separate direct-owner-callback case is explicitly narrower. Neither test represents physical front-buffer loss.
- `wpf-phase5-standard-r1`: Assimp 76/78, Core 548/551, Toolkit 227/230: **851/859**, exactly eight existing failures.
- `wpf-phase5-standard-r2`: Assimp 76/78, Core 548/551, Toolkit 229/232: **853/861**, the same eight failures;
  both additional recovery-registry/discovery managed cases pass.
- Final `wpf-phase5-standard-r3`: Assimp 76/78, Core 548/551, Toolkit 230/233: **854/862**, exactly the same
  eight baseline failures. All 24 additional standard cases relative to Phase 4 pass.
- Final-candidate `wpf-phase5-standard-r5`: again Assimp 76/78, Core 548/551, Toolkit 230/233: **854/862**.
  The exact eight baseline names and failure types are unchanged; all 24 additional standard cases pass.
  The standard command retains both category exclusions and does not enable explicit hardware cases.
- `wpf-phase5-dynamic-r1`: **10/10** actual-backend cases. Particle counters, indirect arguments, energy decay
  and visible pixels advance, then hiding/removing the source restores WPF idle. Both outputs agree for
  ordinary/weighted/peeling transparent overlap at alpha 0/0.5/1, with FXAA off/on.
- `wpf-phase5-features-r1`: **122/122** Core cases including explicit hardware selections: premultiplied output,
  peeling determinism, registered post effects, SSAO planes, source invalidation, particle textures and collisions.
- `wpf-phase5-acceptance-final-r1`: **342/342** selected cases, Core 169/169 and Toolkit 173/173. This includes
  all authored P5 recovery/discovery, dynamic-backend, screen-space, measurement and visible cases, plus the
  selected P4/shared/manager/bridge/lifetime and Core rendering/allocation regressions. This run precedes the
  third review's two additional corrections; subsequent affected gates below qualify those corrective deltas.
- `wpf-phase5-native-lifetime-r1`: **4/4**, the three native recording-callback retirement routes and the real
  pending-GPU unload/completion case. Canceled recordings retain borrowed resources until unwind and create no
  submission; completed prior GPU work delivers exactly one completion.
- `wpf-phase5-r3-closure-r1`: **69/69**, including all nine new recovery-error cases, the public standalone
  retry, prior multi-owner/discovery cases, actual-backend timing/input/idle checks, WPF presentation/heap cases
  and native recording/pending-GPU retirement. A second real detachment failure retains context, native device
  and registry ownership until explicit retry. Fresh green pixels confirm successful rendering after release.
- `wpf-phase5-r4-closure-r1`: **81/81** on fresh build `0bd29bba-06a8-4424-947f-1b08ecc86da8`. Both new
  CPU-borrow cases invoke actual process-device removal from a productive `SceneNode.Update`. HWND uses its
  genuine deferred retirement callback; WPF uses its automatic rendering callback with ordinary WPF timestamp
  arguments. Both preserve native commands/context/device while CPU-borrowed, cancel the unsubmitted frame,
  contain two real detach failures, retain the registry barrier without automatic repost, and release on explicit
  retry. Fresh green output succeeds afterward. The selection repeats the prior recovery/heap/native lifetime
  coverage and adds frontbuffer/minimize plus managed native-lifetime cases.
- `wpf-phase5-r5-closure-r1`: **82/82** on fresh build `9a36d274-560a-41cb-9e6b-a8f633163540`. The additional
  case removes the device between frames and invokes the genuine HWND automatic composition delegate. Two
  successive detach failures are reported/retained without dispatcher escape or automatic retirement repost;
  explicit retry releases ownership and fresh green output succeeds. Queued-manager corrective work follows this run.
- `wpf-phase5-r8-manager-r2`: **4/4**, both backend variants of queued manager and shared-container preflight.
  Both real detach failures remain required, with retained native/context/registry ownership, stopped automatic
  retries and fresh pixels after explicit release. Both shared cases also exercise genuine cold-consumer loading
  while the shared fault gate remains asserted; no generation or submission starts before explicit recovery.
- `wpf-phase5-r8-closure-r1`: **195/195**, Core 33/33 and Toolkit 162/162, on final candidate `c292739b0`
  with fresh successful build `173dacfc-9d50-4d30-8bc1-07a28f7f2db4`. Test DLL timestamps exceed the final
  source timestamp. This repeats all P5 cases plus affected manager/shared/native lifetime, real interop,
  collision and WPF heap coverage after every product correction. Both strengthened shared cases pass.
  Productive WPF mesh and FXAA/Scene2D remain exactly 160 bytes/frame; 1,000 idle calls and 1,024 settled
  generation polls allocate zero. Failed-lock costs still exactly match WPF's framework costs.
- Actual `SimpleDemo` and `SwapChainRenderingDemo` startups expose 17,006 and 1,697,001 triangles respectively.
  Automation status/tree/logs and GPU screenshots were inspected; both error logs were empty and both owned
  processes closed normally. Rider run-configuration startup timed out without starting a process; the already
  built executables were then launched directly, one at a time. No debugger or application checkout was used.

The eight baseline failures remain two Assimp `LoadNativeGltfFactorsTest` cases; Core
`WarpManagesSharedTextureResources`, `WarpManagesSharedGeometryResources` and
`PostProcessCpuContractsAreDeterministic`; two Toolkit `RightDragMovesCameraBeforeOutsideRelease` cases; and
`DelayedFenceKeepsDispatcherLiveAndRendersLatestCameraAfterCompletion`. Assertions and exclusions are unchanged.
Standard selection is `Category!=Hardware&Category!=DX12`. Selected and standard counts overlap.

The first multi-owner run observed 15/19 passing; four new native fixture drains incorrectly required cumulative
submitted/completed equality after deliberately abandoning a removed-device submission. The corrected helper
requires exactly one new accepted/completed frame and preserves the abandoned delta. A subsequent 30/31 recovery
run exposed an immediate timer assertion before WPF's zero-timeout image detachment had succeeded. The fixture
now waits for actual detachment and timer stop, retaining all final counter/pixel/ownership assertions.
These intermediate outcomes are not final acceptance. VSTest's diagnostic stream recorded completed test-host
results when the Rider output transport stalled during failure reporting; only the identified owned runner
processes were stopped afterward. Later runs use quiet console logging plus full TRX output.

The queued shared fixture initially passed 3/4 cases across the manager selection. Actual stack diagnostics
showed that WPF's higher-priority composition callback discovered removal before the intended shared retry
timer, despite scene rendering being stopped. Test-only `c292739b0` temporarily removes that genuine canvas
subscription while qualifying the real shared timer, restoring it on surviving owners. The independently tested
automatic composition paths remain unchanged. WPF's active removal scope prevents a membership repost entirely;
HWND still requires its actual Pending operation to become Aborted. Both injected detach failures, retained
ownership, no-spin, cold-registration and fresh-pixel assertions remain strict. Temporary diagnostics were removed.

# Measurement boundaries

`P5PresentationComparisonTests` compares the same filled mesh, camera, 640×480 target and actual adapter LUID
`0x000000000001456C`, with 32 warm frames and 64 samples per backend. Native GPU timestamp queries surround
the actual mesh bindings/draw. They exclude clear, postprocessing, D3D11 copy, DWM and scanout. Caller wall time
includes bounded fence/interop waits; `Process.TotalProcessorTime` separately reports CPU work.

Routed wheel measurements begin at managed WPF event dispatch, verify an actual camera change, and end at
accepted output. They exclude OS delivery and physical display latency. Process private/managed memory is
diagnostic process-wide state, not isolated VRAM, and excludes explicit capture from the measured interval.
Timings carry no machine-specific pass threshold; allocations and counters remain strict.

Broad acceptance diagnostic sample `wpf-phase5-acceptance-final-r1` (milliseconds, nearest-rank percentiles):

| Metric | WPF p50 / p95 | HWND p50 / p95 |
| --- | --- | --- |
| Caller wall, including explicit wait | 0.6687 / 1.3482 | 0.6049 / 3.4595 |
| Native mesh GPU interval | 0.012288 / 0.013312 | 0.009216 / 0.012288 |
| Request to accepted output | 0.6751 / 1.3543 | 0.6091 / 3.4712 |
| Managed routed wheel to accepted output | 2.1124 / 8.8603 | 0.7021 / 4.3148 |

The 64-frame intervals consume 31.250/46.875 process CPU milliseconds for WPF/HWND. WPF records exactly
64 cross-device copies and 64 publications; HWND records neither extra transport stage. The measured WPF
frame path allocates 160 bytes/frame. The separate native diagnostic is 37.88 bytes/frame in this run and is
not an exact allocation qualification; the Core test host's strict 72-byte and collision-reuse zero-byte checks
pass independently. Toolkit concurrent GC/framework accounting is not an isolation boundary for that diagnostic.
Process private bytes remain 269,021,184 for WPF and move 265,854,976→267,603,968 for HWND;
managed bytes move 16,482,608→16,531,952 and 21,758,640→13,000,088 respectively. These short process-wide
samples demonstrate the measurement scope, not absence of a long-duration leak or an isolated VRAM comparison.

The actual continuous source produces 32 accepted/completed frames on each backend. After its removal and
settling, 64 more pulses produce zero WPF frames/copies/publications. Explicit HWND remains continuous and
produces 64 frames, preserving the original selected-backend policy.

The final `wpf-phase5-r8-closure-r1` sample reconfirms these counters and the same LUID. WPF/HWND caller wall
p50/p95 is 0.6156/1.0699 and 0.3763/0.5396 ms; GPU mesh intervals are 0.011264/0.012288 and
0.009216/0.010240 ms. WPF remains exactly 160 bytes/frame. The process-wide HWND allocation diagnostic varies
to 81.12 bytes/frame, illustrating why it is not a strict isolated allocation gate; Core's independent exact
72-byte frame and zero-byte collision-reuse assertions pass. No timing or diagnostic-memory threshold was added.

# Explicit limits

Changed-file ReSharper warning diagnostics were inspected; style and compatibility warnings remain rather than
being rewritten through global cleanup. The large `D3D12SceneRenderer.cs` diagnostic request repeatedly timed
out, including the final 45-second retry. Its build and selected runtime regressions pass, but this is not a claim
that every changed file received a completed IDE diagnostic result. The final queued-recovery test file's warning
diagnostics complete with 15 warning findings and no errors; retained callbacks and deliberate explicit fixture
disposal account for lifecycle warnings. XML-block parsing validates 1,190 blocks without errors; final local-link
and OKF validation is recorded below.

Scoped `ID3D12Device5.RemoveDevice` invalidates the test process's native singleton, not the physical adapter.
[Microsoft documents device singletons](https://learn.microsoft.com/windows/win32/api/d3d12/nf-d3d12-d3d12createdevice)
and [removal's fence behavior](https://learn.microsoft.com/windows/win32/api/d3d12/nf-d3d12-id3d12device5-removedevice).
Physical adapter reset, multi-adapter/monitor transitions, RDP, OS touch promotion and physical scanout latency
remain unqualified. Isolated D3D11-only removal is source-reviewed without a physical fault injection.

WPF's D3D9Ex bridge explicitly rejects WARP; native WARP tests do not qualify WPF WARP or RDP.
MSAA is a retained compatibility option with current single-sample targets; FXAA is productive independently.
Sized bitmap capture scales captured pixels rather than rerendering the scene at the requested resolution.
Deferred-rendering configuration is retained without a productive deferred-shading path.

# Closure

Phase 5 is complete for the authorized feasible scope. Final product `4a404f0c9` has no open P1/P2 findings in
independent R8; final test-only candidate `c292739b0` passes the fresh build and 195/195 affected acceptance cases.
The standard gate is 854/862 with the exact eight known baseline failures. Earlier broad 342/342 acceptance and
visible/demo evidence remain dated above; later corrective deltas are qualified by the final affected selection.

The final local Graphify run exits zero and writes `graphify-out/graph.json`: 1,064 prepared source files,
10,888 nodes, 19,850 edges and 3,150 communities, zero model tokens and the same 17 partial-parser warnings.
Temporary staging is removed; generated output is never hand-edited. The plan, task list, OKF log and graph
snapshot are reconciled. The final hook check finds no `.specify/extensions.yml`; the unrelated Assimp feature
pointer remains unchanged. The physical/environment and retained feature limits above remain explicit.
Final validation passes for 1,190 XML blocks, all six changed OKF concepts and 59 local Markdown links;
`git diff --check` is clean. All Phase 5 task markers are complete.
