---
type: ImplementationPlan
title: D3D12 WPF composition proof and phased integration
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-19T11:17:00Z }
verified: { by: "agent:gpt-6.1-sol", at: 2026-10-19T12:00:00Z }
status: draft
sources:
  - resource: ../../wpf-komposition-phasen-parallele-tasks-und-reviews.md
  - resource: ../../Source/SilkToolkit/SilkToolkit.csproj
  - resource: ../../Source/SilkToolkit/Controls/D3D12PresentationSurface.cs
  - resource: ../../Source/SilkToolkit/Controls/D3DImageInteropProbe.cs
  - resource: ../../Source/SilkToolkit.Tests/D3DImageInteropNativeTests.cs
  - resource: ../../Source/SilkToolkit.Tests/D3DImageInteropWpfTests.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12FrameRenderer.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12RenderDemand.cs
  - resource: ../../Source/SilkToolkit/Controls/Viewport3DX.RenderDemand.cs
  - resource: ../../Source/SilkToolkit.Tests/D3D12FrameRendererTests.cs
  - resource: ../../Source/SilkToolkit.Tests/ViewportRenderDemandTests.cs
  - resource: ../../Source/SilkCore.Tests/NativeFrameAllocationTests.cs
---

# Scope

The [implementation plan](../../wpf-komposition-phasen-parallele-tasks-und-reviews.md) defines six phases,
parallel task ownership, thirteen historical review findings and an explicit approval boundary after each phase.
Phase 1 is an isolated hardware interoperability proof. Production Viewport selection and renderer extraction
belong to later phases.

The intended default is `EnableSwapChainRendering = false`, restoring WPF composition. Explicit `true`
retains native HWND presentation. The future WPF path renders and publishes only on demand; an isolated
invalidation requests six total frames by default. Updates received during a frame must survive its completion.

# Working baseline

The branch was fast-forwarded from `7344b9cf80de00feb6dfc102b41f5690619bc091` to
`72557068603fbe3017dc045782e24631970a4fb1` from the local MeshMeister toolkit checkout. That checkout's
uncommitted knowledge files were not copied or changed. Logging remains at
`ed78901cb880a17b43433afc642449be9e8bc64e`. No application changes or push are authorized by this phase.

The first Rider baseline build found seven existing errors across four example files: ambiguous Assimp
`Exporter`, missing WPF color extension imports and two obsolete XAML namespaces. Scoped import/namespace
corrections produced a successful second Rider build (`4d4900f8-6d6d-47c4-ba27-32d74299b1d7`). Existing
package/analyzer warnings remain outside this proof.

The baseline standard test run used `Category!=Hardware&Category!=DX12`: 617 passed and 8 failed across
625 cases (Assimp 76/78, Core 441/444, Toolkit 100/103). These failures preceded the new probe:

- `LoadNativeGltfFactorsTest`: two cases fail at the material null assertion.
- `WarpManagesSharedTextureResources` and `WarpManagesSharedGeometryResources`: maintenance assertions fail.
- `PostProcessCpuContractsAreDeterministic`: random sample collections differ.
- `RightDragMovesCameraBeforeOutsideRelease`: two cases fail at the geometry octree null assertion.
- `DelayedFenceKeepsDispatcherLiveAndRendersLatestCameraAfterCompletion`: geometry octree null assertion fails.

Rider's terminal response failed with HTTP 404 after running the tests. Per-project TRX files with prefix
`wpf-phase1-baseline` establish the actual outcomes; no CLI fallback was used after these test failures.
The full baseline gate is therefore not green, even if the isolated interoperability proof succeeds.

# Phase 1 proof contract

The bridge uses a D3D12 BGRA render resource exported with an NT handle, opened on a D3D11 device with the
same actual adapter LUID. D3D9Ex creates a separate legacy-shared BGRA texture; D3D11 opens that allocation
and copies the completed D3D12 image into it. WPF receives the corresponding D3D9 surface through `D3DImage`.
Creating the destination in D3D9Ex follows the documented D3D9-to-D3D11 sharing direction.

NT handles are closed by their owner; legacy sharing handles are not NT handles. Adapter ordinals from
different APIs are never treated as identities. Explicit adapter selection must still resolve to the same LUID.

The caller polls GPU completion without blocking the dispatcher. Two legacy-shared destination slots alternate:
the currently attached WPF surface stays immutable while D3D11 copies into the detached slot. After copy
completion, the caller obtains the WPF image lock, replaces the backbuffer, publishes a dirty rectangle and
unlocks. It must publish the completed slot before the next copy may reuse the previously detached slot.
Before native disposal, it detaches the WPF backbuffer. Explicit teardown may use a bounded completion wait.
No scene renderer changes, CPU presentation loop or silent HWND fallback are part of this proof.

The independent synchronization review identified two WPF details that affect later integration as well:
`TryLock` increments the internal lock count even when returning false in the inspected .NET 10 source, so
each normally returning attempt is balanced with `Unlock`; software snapshot/fallback paths can read the user
surface outside the managed lock-count gate. The latter implies a potential race if the attached surface is
written across dispatcher callbacks; immutable attached slots avoid relying on that unproven synchronization.
See the [versioned WPF implementation](https://github.com/dotnet/wpf/blob/v10.0.0/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/D3DImage.cs).

Target hardware: NVIDIA GeForce GTX 1070, driver `32.0.15.6094`. Passing here will not establish WARP,
Remote Desktop, other-adapter or device-loss support. Historical WPF software fallback is not such a guarantee.

# Phase 1 results

P1.1–P1.4 are complete. The integrated code is at `54bb684c7`; the previous integration checkpoint was
`81ebc066b`. Native implementation and WPF tests were developed in separate worktrees. A separate research
review identified the immutable-surface and lock-count requirements. Independent code review found no
additional Phase 1 blockers; a second review of `81ebc066b..54bb684c7` accepted the final visible-test correction.

The final Rider build (`2e1ce8e6-2e92-45e7-a337-3d8217df3a2e`) succeeded. The explicit hardware run passed
all three new tests without skips, recorded in `Source/SilkToolkit.Tests/TestResults/` with the TRX prefix
`wpf-phase1-proof-final`:

- Native sharing: actual D3D9 pixels match premultiplied BGRA, including alpha zero/half/one, distinct red/blue,
  both destination slots, recreation and pending-clear teardown. Invalid adapter selection fails explicitly.
- Hidden WPF: six publications, responsive dispatcher, unchanged explicit-driver counts during idle, safe
  detachment, recreation from 64×48 to 96×72 and repeated disposal.
- Visible WPF: actual desktop RGB values match red `(255,0,0)`, green `(0,255,0)` and blue `(0,0,255)`;
  half-alpha over white produces `(255,128,128)`, `(128,255,128)` and `(128,128,255)`, respectively. Alpha zero
  reveals white. The attached red image and its `RenderTargetBitmap` snapshot remain red while copying green
  into the other slot. Sixteen explicit publications settle without additional driver submissions, copies or
  publications during the approximately 200 ms observation. Resizing/recreation and disposal also pass.

Both default selection and explicit DXGI adapter `0` resolve to the actual LUID `0x000000000001456C` on
this machine. No second physical adapter was available. Polling timings in test output include dispatcher
scheduling and intentional screenshot delays; they are not GPU performance benchmarks.

The first visible test failed before creating the probe because a source-less `Image` had no layout area.
The correction gives the test image explicit dimensions and samples the window only for the source-less
white baseline and disposed state. Every actual RGB/alpha/retained-image/resize measurement still samples
the image itself, and the software snapshot renders that image directly.

The final standard suite still reports exactly 617 passed and the same 8 failed out of 625; per-project TRX
failure-name comparisons against the baseline have zero differences. These pre-existing failures remain open.
ReSharper formatting and warning-level diagnostics were inspected; no compilation or semantic diagnostic
errors remain in the probe files. Stylistic/interop inspection warnings remain and were not globally cleaned up.

Graphify completed locally after the final C# formatting: 1,013 source files, 9,691 nodes, 16,858 edges and
2,847 communities, with zero model tokens. Its existing 17 partially extracted files remain a parser limitation.
The staging corpus was removed and generated output was not manually edited.

This proves the isolated transport on one GPU/driver and desktop session. It does not yet implement production
Viewport selection, demand gating, frontbuffer/device recovery, forced lock contention, injected GPU stalls,
long-duration stress, multi-adapter hardware, RDP or WARP. These are separate integration/validation work.
The probe's alternating-slot API relies on its documented caller publication/detachment precondition.
Test-only staging readback and desktop sampling never supply presentation pixels.

Phase 2 was subsequently approved and is recorded below. The complete feature remains a draft until later phases are integrated.

# Remaining scope

## Phase 2 contracts and allocation gate

Phase 2 is approved. The new coordination and stable-frame paths must minimize managed heap pressure.
The independent review explicitly checks per-tick/frame allocations, cached delegates, retained context/list
ownership, boxing, closures and LINQ. Warm-up, actual capacity growth and explicit capture allocations are
reported separately from steady state. An unchanged scene and the demand coordinator must not allocate per tick.

The shared renderer records into the borrowed value-type `D3D12FrameTarget` (color resource, RTV, pixel size,
format), using an explicit DPI scale rather than reading a window. `D3D12FrameRenderer` owns scene/pass/depth
resources and reusable preparation lists/contexts. It does not own an HWND or swap chain and does not present,
submit, wait on a GPU fence or publish to WPF. Callers guarantee the preceding GPU work is complete before
recording, resizing or replacing its resources. The existing HWND surface retains device/queue/command-context/
fence/swap-chain lifetime, final resource transitions, synchronous capture and completion notifications.

Render demand is a separate thread-safe internal coordinator with a six-frame default budget and distinct
render, scene-graph and renderables invalidation flags. Reserve existing demand before scene updates; retain
any new request arriving during preparation, submission, pending GPU work or completion events. Commit only
an actually submitted frame, and restore a canceled reservation without overwriting newer invalidations.
Publication/recovery work is independent of a clean scene. Existing HWND calls keep their current behavior;
an internal opt-in demand consumer prepares integration without introducing the public presentation switch
or the production WPF canvas before their planned phases.

Writing ownership: agent A owns only the new demand coordinator and its tests; agent B owns the shared frame
renderer, the HWND surface and its dedicated tests; integration owns Viewport files, existing reflection-based
consumer tests, Core hot-path allocation fixes, documentation, project files and generated output. Agent C
reviews attachment/continuous-source/DPI/recovery and allocation risks read-only. Integration runs builds and
GPU checks serially. All writing children start from the same committed contract.

The plan maps findings F01–F13 to later tasks: property invalidation, continuous sources, demand preservation,
frame budgets, manager events, shared scene consumers, DPI, Scene2D, lifecycle/input and capture pixel contracts.
Existing ineffective MSAA and CPU-scaled requested capture dimensions remain explicitly documented limits.

# Phase 2 results

P2.1–P2.5 are complete. Demand and shared-recorder implementation used separate worktrees; attachment research,
an early Core/demand review and two independent integration review rounds were read-only. The common recorder
is integrated at `096adec81`; the final reviewed recovery change is `d4042daee`. The HWND surface uses the common
renderer productively. Existing HWND rendering remains continuous; `RenderOnDemand` is internal opt-in only.
No public presentation switch or production D3DImage canvas is introduced in this phase.

The demand coordinator reserves a frame before updates and commits only an actual presentation submission.
New invalidations during updates, completion callbacks or busy GPU work survive; cancellation restores old
reasons without replacing a newer budget. The public minimum is connected to future requests, with six total
frames and at least one explicit frame for zero/negative settings. Publication/recovery flags remain independent.
Worker requests record demand immediately and coalesce dispatcher notification through a cached callback.

The recorder borrows the target, context and device. It retains pass selectors, scene lists, traversal storage,
light payload and unchanged 3D/2D contexts. Core preparation snapshots roots before updates, observes children
after parent updates, runs all updates before maintenance, and clears scratch references in `finally`.
Frame-cache cleanup avoids LINQ/key snapshots and preserves independently owned versus aliased bindings.
Environment lookup delegates are cached rather than created per mesh draw.

Independent review found and resolved two correctness issues:

- `RenderVisible` cleared the retained `PrepareScene` result before consuming it. Cleanup now occurs at disposal,
  with an actual offscreen direct-candidate mesh/pixel regression.
- Opening command recording before preparation made a throwing node update leave an open command list.
  The HWND owner now closes and discards unsubmitted recording and recreates the native stack, dropping
  tracked resource states with their allocations. No failed frame is presented or counted. This exceptional
  recovery can allocate; its previous GPU frame is complete before teardown. The actual HWND retry regression
  checks retained demand, unchanged submission/completion counts, old-owner disposal and captured mesh pixels.

The second integration review accepted both fixes and found no unresolved blockers. The offscreen Scene2D
failure test explicitly completes its caller-owned partial recording before resetting the context. Initial
attachment can legitimately invalidate inside the first update; budget tests warm attachment before checking
exact isolated requests. These fixture corrections preserve rather than suppress follow-up demand.

Final Rider build `ed31acd2-0327-43fc-b34c-15657e292682` succeeded. The standard filtered suite is recorded by
TRX prefix `wpf-phase2-verified`: **674 passed / 682 total, the same eight baseline failures**. Failure-name
comparisons remain unchanged: Assimp 76/78, Core 444/447, Toolkit 154/157. All **57 new cases pass**, including
six offscreen WARP cases, nineteen actual viewport cases, twenty-nine demand cases and three Core preparation
cases. They cover pixels/depth, borrowed ownership, context replacement, exceptions, empty submitted frames,
runtime budgets, worker bursts, update/completion reentrancy, real queue stalls and idle capture.
Rider's terminal response sometimes returned HTTP 404 after process completion; the completed TRX files,
not a duplicate fallback run, establish results. Hardware/DX12 tests were not rerun in this phase.

Warm allocation evidence uses `GC.GetAllocatedBytesForCurrentThread` synchronously on the same thread,
excluding assertions/logging/capture from measured intervals:

| Measured path | Evidence | Managed allocation |
| --- | --- | --- |
| Coordinator, idle and active | Repeated actual request/reserve/complete operations | 0 bytes |
| Actual idle STA viewport | 1,000 calls; unchanged submissions, updates and completions | 0 bytes |
| Actual Core preparation and cleanup | 1,000 two-node iterations; actual update counts verified | 0 bytes |
| Actual continuous HWND mesh | 128 submitted frames; draws, updates and both completions verified | 223,720 bytes; 1,747.81 bytes/frame |

Earlier mesh runs measured 1,758–1,770 bytes/frame. The regression ceiling is **2,048 bytes/frame**, tightened
from the provisional 4,096 after measurement. This is a bounded low-allocation mesh baseline, not a claim of
zero-allocation GPU rendering. Initialization, growing capacities, capture, exceptional recovery, active Scene2D,
optional effect passes, animation and arbitrary application callbacks are outside that measurement. Remaining
Scene2D geometry/effect allocations require their own scene-specific evidence before stronger claims.

ReSharper formatting and warning diagnostics were inspected. Some diagnostic requests timed out; completed
requests reported style/interop warnings, and the complete Rider build is the compilation gate. Broad formatting
churn in existing files was discarded while retaining scoped semantic changes. No global cleanup was applied.

Graphify completed locally after the final source changes: **1,021 source files, 9,841 nodes, 17,265 edges and
2,884 communities**, with zero model tokens. The existing 17 partial parser results remain a limitation.
`graphify-out/graph.json` exists; the staging corpus was removed and generated output was not hand-edited.

Phase 3 remains unstarted pending explicit user approval. Production WPF publication/rebinding/frontbuffer
recovery and continuous-source scheduling remain P3 work; public default-false selection, complete property
invalidations, shared-scene lifetime and input/DPI integration remain mapped to P4. The global eight-failure
baseline remains open independently of the successful Phase 2 regressions.

# References

- [Microsoft: D3D11 shared resource restrictions](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-opensharedresource)
- [Microsoft: D3DImage locking](https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.d3dimage.lock)
- [Microsoft: WPF and Direct3D9 interoperation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-and-direct3d9-interoperation)
