---
type: ArchitectureDecision
title: WPF composition integration contracts
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-19T12:38:20Z }
verified: { by: "agent:gpt-6.1-sol", at: 2026-10-19T12:44:30Z }
status: active
sources:
  - resource: ../../wpf-komposition-phasen-parallele-tasks-und-reviews.md
  - resource: ../../Source/SilkToolkit/Controls/D3D12FrameRenderer.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12FrameTarget.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12RenderDemand.cs
  - resource: ../../Source/SilkToolkit/Controls/D3DImageInteropProbe.cs
  - resource: ../../Source/SilkToolkit/Controls/Viewport3DX.RenderDemand.cs
  - resource: ../../Source/SilkCore/Native/D3D12PostProcessResources.cs
  - resource: ../../Source/SilkToolkit.Native.ShaderBuilder/PS/psLuma.hlsl
  - resource: ../../Source/SilkToolkit.Native.ShaderBuilder/PS/psFXAA.hlsl
---

# Decision and scope

P3.0 records the contracts required before P3.1–P3.3 start in parallel. These are implementation requirements,
not implemented APIs or new runtime verification. The Phase 2 basis is `6f36703f3`. The common recorder and
value-type target remain; no second scene renderer or duplicated viewport loop is introduced in the canvas.
The independent architecture review found no new Phase 2 blocker. Its six findings map to F14–F19 in the
phase plan; the source-format audit adds F20. Productive implementation remains subject to the next phase gate.

# Ownership

The following names describe planned internal components; no public API is removed or renamed.

| Owner | Creates and disposes | Borrows |
| --- | --- | --- |
| WPF presentation owner, integrated by I | One D3D12 device, queue, command context, fence, common frame renderer, frame counters and demand consumption | Viewport scene, camera and external effects manager |
| Native image bridge, owned by A | One shareable BGRA source with RTV, its single toolkit resource wrapper, D3D11 device/context/query, D3D9Ex device and two destination slots, owned NT handles | The exact presentation D3D12 device; an existing top-level WPF HWND for D3D9Ex creation |
| Common frame renderer | Scene/pass/depth resources and reusable frame storage | Device, open command context, color target, scene and manager |
| Canvas/ImageSource, owned by C | D3DImage, visual binding, WPF subscriptions and bound-slot bookkeeping | Presentation owner and bridge destination surfaces |
| Viewport, owned by I | Scene invalidation and public host/event/capture integration | Selected presentation owner |

The presentation owner outlives the bridge and renderer. The bridge derives all adapter identities from that
actual device's LUID. It creates no second D3D12 device, queue, command context, fence or scene renderer.
The existing isolated probe remains a proof/test utility and is not nested inside a hidden HWND renderer.
Initialization waits for a real WPF source/handle; an HWND used for D3D9 creation does not present pixels.

The bridge exposes its source using `D3D12FrameTarget`. There is exactly one owning `SilkD3D12Resource`
wrapper and one resource-state tracker entry per D3D12 allocation. Native COM views must borrow that pointer
or hold an explicitly balanced additional reference; two owning wrappers may not release the same reference.
Creation/adoption must use the existing device resource-state tracker. The bridge owns the RTV descriptor
and heap through GPU completion. The recorder never frees borrowed color resources.

A owns any narrowly required shared-resource factory addition in Core, including adoption/state registration;
B owns other output/effect changes. I integrates common device/factory signatures before their callers merge.
No generic backend framework or new public renderer interface is required for this decision.

# Frame identity and milestones

Use value fields for a presentation generation and monotonically increasing submitted-frame serial. Successful
native resource replacement changes the generation; callbacks and borrowed targets identify their generation.
Stale callbacks must not publish into, dispose, or complete work belonging to a replacement generation.
Counters and slot records are retained values, not newly allocated per-frame task/token objects.

| Milestone | Meaning | Scene budget / host event |
| --- | --- | --- |
| Submission accepted | Commands executed and completion fence successfully signaled | Commit exactly one reserved frame, including zero draws |
| D3D12 complete | Fence covers that submitted serial | Deliver existing Rendered/OnRendered once for that serial |
| D3D11 copy complete | Query covers the copy into an unbound destination | No scene budget or additional Rendered event |
| WPF published | Completed destination bound and dirtied under balanced lock | Record published serial; no scene budget or Rendered event |
| Frontbuffer rebound | Previously completed image rebound after recovery | No new submission/copy serial or scene event |

Submission success is explicit. WPF demand must not infer submission from HWND `PresentedFrames`, draw count,
copy count or publication count. A failed signal after execution poisons the generation: do not reuse commands
or refund and immediately retry them; retain native ownership until a safe drain/device-loss retirement.
The accepted-submission result and fault state must distinguish this case from failure before execution.
The HWND adapter preserves existing behavior. Existing public events remain additive-compatible and continue
to represent GPU-completed scene frames, not a promise that desktop composition has displayed the image.

# Single-frame transport state machine

The initial production implementation uses strict backpressure with one source and two destination slots.
There is at most one rendered/copied/unpublished frame in transport. There is no queue of frame objects and
no frame dropping/replacement policy to implement. New scene requests coalesce in `D3D12RenderDemand`.

| State | Allowed work | Next state |
| --- | --- | --- |
| Idle | If visible, initialized and requested, reserve before camera/node updates, record, restore source to COMMON, submit and signal | Rendering |
| Rendering | Nonblocking D3D12 completion query; retain newly arriving scene demand | Copying after completion and successful copy submission |
| Copying | Nonblocking D3D11 query; source cannot be rendered or disposed | ReadyToPublish after query completion |
| ReadyToPublish | Attempt one short WPF publication; retain completed target on lock failure | Idle only after successful publication/detachment bookkeeping |
| Suspended / Recovering | Detach/rebind/retire resources as appropriate; retain pending scene requests | Idle or an existing pending transport state after recovery |

D3D12 completion alone never authorizes writing the source while D3D11 is reading it. Destination selection
uses explicit bound/ready/free slots. A new copy must not touch the bound slot or a completed unpublished slot.
Successful publication detaches the preceding bound slot before it can become the next copy target.
The caller must not alternate blindly after an unsuccessful publication.

Each normal dispatcher tick performs bounded work: handle applicable recovery, poll existing transport,
deliver completions, attempt publication, then consider scene demand only when transport is idle. Polling and
publication remain reachable when the scene budget is zero. Hidden/suspended viewports do not submit new
scene frames; pending resource ownership must still be serviced safely by the owner. No GPU waits, polling
loops, per-frame async state machines or dispatcher-operation bursts occur on normal ticks.
Use existing finite GPU/copy deadlines; ordinary WPF lock contention is a publication retry, not GPU timeout.

Take publication/recovery requests before work and re-request on retry. A failed short `TryLock` keeps the
ready serial/slot unchanged and performs no rendering, copy, backbuffer change or dirty publication. Balance
every normally returned TryLock with Unlock, including false, as verified in Phase 1 on this runtime.
If SetBackBuffer succeeds but later publication fails, track the actual new binding immediately and enter
recovery; do not assume the old slot is still bound. Retain both allocations until a safe detach is confirmed.

# Pixel and output contracts

The shared recorder supports an internal immutable output profile selected by its presentation owner:
legacy HWND output retains its current behavior; the WPF profile guarantees premultiplied BGRA8 at the
bridge source. The profile and format must reach relevant pipeline creation without duplicating scene logic.
Do not infer alpha representation from DXGI format alone.

| Boundary | Required representation |
| --- | --- |
| Application colors/materials | Existing public straight-color inputs remain unchanged |
| WPF color/compositing intermediates | Premultiplied color with coverage alpha; explicit non-color buffers retain their own semantics |
| WPF final source and D3D9 surfaces | B8G8R8A8 UNORM, premultiplied, alpha zero implies zero RGB |
| Existing RenderBitmap/capture consumer | Tightly packed straight BGRA for PixelFormats.Bgra32 |

For the WPF profile, premultiply clear color once. For geometry and Scene2D, choose shader output and blend
factors together so the destination contains premultiplied color; multiplying an already blended target again
is prohibited. Opaque draws, translucent overlapping geometry, effect composites, screen-space content and
the Scene2D pass after FXAA must obey the same destination contract. This includes alpha-writing blend factors.
Keep the existing color-space convention; do not silently add gamma/sRGB conversion in this presentation change.

The current luma pass stores luminance in alpha and FXAA forces alpha to one. WPF coverage cannot be recovered
by removing only the latter assignment. B must preserve coverage separately from the luma signal and filter
color/coverage consistently, using retained GPU resources and no readback. Non-color intermediates such as
luma, OIT weights and depth remain explicitly typed; they are not mistaken for premultiplied scene images.

Resource formats and PSO target formats must match every pass. In particular, the current
`SilkD3D12PostProcessResources.TargetFormat` is fixed to R8G8B8A8. Do not CopyResource/CopyTextureRegion between
incompatible RGBA/BGRA families. B must propagate a compatible profile format to affected color intermediates
and pass keys, or use an explicit GPU shader conversion where a different format is necessary. Audit FXAA,
bloom, outline, SSAO composition, OIT composition, screen-space and Scene2D; fixed float/depth formats remain.

Internal capture metadata records actual channel order and alpha representation at readback. Normalize once
into the existing straight-BGRA result before its bitmap/encoder consumer: swap only an RGBA source; unpremultiply
only a premultiplied source, with alpha zero producing zero RGB and bounded rounding. Test quantization at
small alpha. Conversion/readback occurs only for explicit capture; it never supplies WPF presentation pixels.
Do not promise recovery of RGB hidden by alpha zero or bit-identical low-alpha straight color after quantization.

# Recovery and scene lifetime

| Trigger | Required action |
| --- | --- |
| Frontbuffer returns | Rebind the last completed compatible image without rerendering; resume pending publication if needed |
| Resize / effective DPI changes | Coalesce latest dimensions; quiesce source/copy, detach old WPF binding, replace allocations safely, request a scene frame |
| Unload | Suspend new frames, remove subscriptions/cancel pending callbacks, retain scene/model/manager objects, detach image before retiring native allocations |
| Reload / template replacement | Restore the fixed backend choice and subscriptions once; rebind retained compatible image or request recreation/rendering |
| Device/manager resource replacement | Change presentation generation, preserve demand, rebuild required resources and request a scene frame; ignore stale callbacks |
| Final disposal | Detach WPF, finish or safely retire native GPU/copy use, dispose bridge and recorder before device; release this consumer's scene attachment |

Actual device removal permits a device-loss retirement path; absence of an ordinary fence completion is not
by itself proof that resources are free. Explicit teardown has a finite deadline. If detachment/drain cannot
complete, retain the owning objects for a controlled retry and report failure; never free a still-bound slot.
Coalesce replacements rather than allocating a generation on every resize/recovery tick. Do not build an
unbounded queue of retired resources. Ordinary frontbuffer rebind does not dispose the common scene renderer.

Nodes, geometry and externally supplied managers belong to the application. Unload/recovery never calls their
final Dispose. Releasing this presentation's GPU bindings is distinct from releasing shared attachment. A
renderer recreation may detach exclusively owned attachments only at a quiescent boundary; it must not detach
another live consumer's scene or Scene2D roots. The existing single mutable RenderHost pointer does not prove
exclusive ownership. I preserves this boundary during P3 integration; P4.2 implements and validates complete
multi-consumer lifetime/distribution before the public switch is enabled. P3 does not claim shared-scene support.

# Invalidation and allocation contracts

Continuous render nodes, active particles and active desktop capture request follow-up scene frames only
while active. Capture timeout/restart must keep the active source progressing. Scene2D paint/layout invalidation
must notify attached host consumers as well as marking node flags. B supplies the Core notification/source
hooks; I owns Viewport subscriptions and C forwards canvas recovery/publication requests. Do not embed WPF
Dispatcher or D3DImage code into Core, and do not restore continuous rendering to hide missing notifications.
P4.1/P4.3/P4.6 finish public property/Scene2D switching and shared-consumer integration; the P3 test harness
must already verify that an explicit Scene2D notification can wake a drained owner.

Reuse slots, contexts, selectors and polling callbacks. Do not promote the probe test driver's Task.Delay,
capturing delegates or async polling loops into the production tick. Preserve separately measured evidence:
idle coordination, actual mesh rendering, real copying/publication, lock retries/recovery and active 2D/effects.
Fix avoidable warmed pass-cache allocations in B's touched lookup path rather than adding a generic cache.
Keep initialization, capacity growth, capture and exceptional recovery separate. Zero-allocation claims must
verify actual frame/copy/publication counts, so skipped work cannot produce a false zero.

# Parallel ownership and acceptance

I owns this contract, central Viewport/host files, shared frame renderer/target integration, project files and
documentation. A owns the productive native bridge and shared-resource creation/adoption path plus its tests.
B owns Core scene/pass/postprocess/source-notification code and shaders plus its tests. C owns canvas and
ImageSource plus its tests. Existing common files have one owner; required cross-boundary changes go through I.
Any signature revision is coordinated and committed before dependent children start from it.

The next implementation phase must establish these checks serially on the integrated worktree:

1. The recorder uses the bridge target on the same actual D3D12 device; LUID selection is verified; only one
   D3D12 submission stack exists per selected presentation. Source state returns to COMMON before copying.
2. Actual GPU/copy stalls and failed publication locks preserve source/slot contents, new demand and serials.
   No additional render or copy occurs merely to retry publication. New serials reach each milestone once.
3. Actual pixels for separate red/blue and alpha 0/0.5/1, overlapping translucent geometry, FXAA on/off and a
   Scene2D overlay match WPF composition. Explicit capture normalizes channels/alpha once. Intermediates use
   compatible formats; alpha/luma and bitmap metadata are covered independently.
4. Frontbuffer rebind reuses a completed image; resize/reload/device replacement retains demand and rejects
   stale-generation callbacks. Partial publication failure cannot make a bound slot writable. Unload preserves
   application scene/manager lifetime. Multi-consumer claims remain gated by the later P4 regression suite.
5. Static scenes settle without further render/copy/publication work; continuous sources and Scene2D changes
   wake work and settle after deactivation. Heap measurements include real publication, not only the probe driver.

P3.0 itself changes documentation only. Build, shader, GPU, allocation and Graphify gates are not rerun for
this contract. The previous Phase 2 runtime results remain historical evidence, not proof of these new requirements.

Independent read-only review of contract commit `1913dab60` found no must-fix issues in ownership, transport,
execute/signal failure, recovery, pixels/capture, task ownership or provenance. The parent checked all five
changed Markdown files for documentation-only scope, OKF type declarations and 44 resolving local links/resource
references; `git diff --check` passed. P3.0 is complete. P3.1–P3.4 remain unstarted pending explicit approval.
