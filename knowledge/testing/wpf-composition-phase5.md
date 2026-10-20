---
type: VerificationReport
title: WPF composition Phase 5 acceptance
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-20T07:30:00Z }
status: in-progress
sources:
  - resource: ../../specs/003-wpf-composition-phase5/tasks.md
  - resource: ../../wpf-komposition-phasen-parallele-tasks-und-reviews.md
  - resource: ../../Source/SilkToolkit.Tests/P5PresentationComparisonTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5DeviceRecoveryTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5WpfRecoveryAcceptanceTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5ScreenSpacePostEffectTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P5DynamicBackendTests.cs
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
HWND completion polling as must-fix recovery boundaries. Their corrective implementation and closure review are pending.

The outline fixture originally assumed additive blending. Original `BsGlowBlending` is `One/InverseSourceAlpha`,
so its final shader requires `R+B=255±1` over opaque blue. That source-backed equation replaces the incorrect
blue-channel invariant; placement, affected-pixel count, coverage and exact restoration checks remain strict.

# Runtime evidence so far

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
- `wpf-phase5-dynamic-r1`: **10/10** actual-backend cases. Particle counters, indirect arguments, energy decay
  and visible pixels advance, then hiding/removing the source restores WPF idle. Both outputs agree for
  ordinary/weighted/peeling transparent overlap at alpha 0/0.5/1, with FXAA off/on.
- `wpf-phase5-features-r1`: **122/122** Core cases including explicit hardware selections: premultiplied output,
  peeling determinism, registered post effects, SSAO planes, source invalidation, particle textures and collisions.
- Actual `SimpleDemo` and `SwapChainRenderingDemo` startups expose 17,006 and 1,697,001 triangles respectively.
  Automation status/tree/logs and GPU screenshots were inspected; both error logs were empty and both owned
  processes closed normally. Rider run-configuration startup timed out without starting a process; the already
  built executables were then launched directly, one at a time. No debugger or application checkout was used.

The eight baseline failures remain two Assimp `LoadNativeGltfFactorsTest` cases; Core
`WarpManagesSharedTextureResources`, `WarpManagesSharedGeometryResources` and
`PostProcessCpuContractsAreDeterministic`; two Toolkit `RightDragMovesCameraBeforeOutsideRelease` cases; and
`DelayedFenceKeepsDispatcherLiveAndRendersLatestCameraAfterCompletion`. Assertions and exclusions are unchanged.
Standard selection is `Category!=Hardware&Category!=DX12`. Selected and standard counts overlap.

# Measurement boundaries

`P5PresentationComparisonTests` compares the same filled mesh, camera, 640×480 target and actual adapter LUID
`0x000000000001456C`, with 32 warm frames and 64 samples per backend. Native GPU timestamp queries surround
the actual mesh bindings/draw. They exclude clear, postprocessing, D3D11 copy, DWM and scanout. Caller wall time
includes bounded fence/interop waits; `Process.TotalProcessorTime` separately reports CPU work.

Routed wheel measurements begin at managed WPF event dispatch, verify an actual camera change, and end at
accepted output. They exclude OS delivery and physical display latency. Process private/managed memory is
diagnostic process-wide state, not isolated VRAM, and excludes explicit capture from the measured interval.
Timings carry no machine-specific pass threshold; allocations and counters remain strict.

Intermediate diagnostic sample `wpf-phase5-focused-r2` (milliseconds, nearest-rank percentiles):

| Metric | WPF p50 / p95 | HWND p50 / p95 |
| --- | --- | --- |
| Caller wall, including explicit wait | 0.6337 / 1.1042 | 0.4242 / 1.0055 |
| Native mesh GPU interval | 0.011264 / 0.012288 | 0.009216 / 0.011264 |
| Request to accepted output | 0.6433 / 1.1245 | 0.4278 / 1.0227 |
| Managed routed wheel to accepted output | 0.7331 / 1.8554 | 0.5409 / 2.3442 |

The 64-frame intervals consume 46.875/31.250 process CPU milliseconds for WPF/HWND. WPF records exactly
64 cross-device copies and 64 publications; HWND records neither extra transport stage. The measured WPF
frame path allocates 160 bytes/frame. The separate native diagnostic is 74.25 bytes/frame in this run and is
not an exact allocation qualification; the Core test host's strict 72-byte and collision-reuse zero-byte checks
pass independently. Toolkit concurrent GC/framework accounting is not an isolation boundary for that diagnostic.
Process private bytes move from 222,875,648 to 222,859,264 for WPF and remain 216,862,720 for HWND;
managed bytes move 8,239,544→8,313,560 and 5,543,464→5,601,032 respectively. These short process-wide
samples demonstrate the measurement scope, not absence of a long-duration leak or an isolated VRAM comparison.

The actual continuous source produces 32 accepted/completed frames on each backend. After its removal and
settling, 64 more pulses produce zero WPF frames/copies/publications. Explicit HWND remains continuous and
produces 64 frames, preserving the original selected-backend policy.

# Explicit limits

Scoped `ID3D12Device5.RemoveDevice` invalidates the test process's native singleton, not the physical adapter.
[Microsoft documents device singletons](https://learn.microsoft.com/windows/win32/api/d3d12/nf-d3d12-d3d12createdevice)
and [removal's fence behavior](https://learn.microsoft.com/windows/win32/api/d3d12/nf-d3d12-id3d12device5-removedevice).
Physical adapter reset, multi-adapter/monitor transitions, RDP, OS touch promotion and physical scanout latency
remain unqualified. Isolated D3D11-only removal is source-reviewed without a physical fault injection.

WPF's D3D9Ex bridge explicitly rejects WARP; native WARP tests do not qualify WPF WARP or RDP.
MSAA is a retained compatibility option with current single-sample targets; FXAA is productive independently.
Sized bitmap capture scales captured pixels rather than rerendering the scene at the requested resolution.
Deferred-rendering configuration is retained without a productive deferred-shading path.

Final broad regression, independent corrective review, diagnostics/formatting and Graphify evidence will be appended
after the remaining recovery changes, without rewriting these intermediate results as final acceptance.
