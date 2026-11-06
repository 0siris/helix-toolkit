---
type: Project
title: SilkToolkit
description: Windows/WPF-facing toolkit layer built on SilkCore.
resource: ../../Source/SilkToolkit/SilkToolkit.csproj
tags: [project, wpf, windows, silknet]
timestamp: 2026-07-08T00:00:00+02:00
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-20T07:15:06Z }
status: stable
verified: 2026-11-03
sources:
  - resource: ../../Source/SilkToolkit/Controls/Viewport3DX.PresentationSelection.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12WpfPresentation.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12ImageInteropBridge.cs
  - resource: ../../Source/SilkToolkit/Controls/D3DImageInteropNative.cs
  - resource: ../../Source/SilkToolkit.Tests/D3DImageInteropOwnershipTests.cs
  - resource: ../../Source/SilkToolkit.Tests/D3D12TransportTimerTests.cs
  - resource: ../../Source/SilkToolkit.Tests/D3D12StableOutputTests.cs
  - resource: ../../Source/SilkToolkit/Extensions/ViewportExtensions.cs
  - resource: ../testing/wpf-composition-phase4.md
---

# Project

## Stable output surface per WPF generation

`D3D12ImageInteropBridge` owns one D3D9Ex output allocation per generation instead of alternating two
surfaces. `D3D12WpfPresentation` completes the source D3D12 fence before acquiring a zero-timeout image
write lease, then submits `CopyResource`, the existing event query and `Flush`. The dispatcher retains
that lease across nonwaiting timer/composition query polls. Only `S_OK` **and** the query completion BOOL
allow DirtyRect and Unlock; Flush alone never proves completion. Normal frames keep the same output
pointer and omit SetBackBuffer; first publication or a real target replacement binds it. A copy-free
frontbuffer recovery dirties the retained pointer without a null binding. All image/bridge access remains
on the creating dispatcher thread; a held lock does not grant cross-thread write permission.

A normal false TryLock still needs Unlock, as confirmed in shipped .NET 11 RC3 IL and a real headless
runtime regression (`11.0.0-rc.3.26522.148`, WPF commit `5254d8bcf59917edc1e1abc33bdf2442b77b1c70`).
A miss starts no copy, retains the accepted source/serial and leaves scene demand untouched. Ready
output survives a dirty/publication failure for retry without another copy. New source rendering remains
blocked until exact publication acknowledgement. Direct image/canvas detach or disposal refuses a held
write lease. Presentation retirement/disposal polls live D3D11 work before releasing that lease and
detaching; timeout or an unconfirmed query failure retains it. Only actual completion or confirmed
D3D11 removal permits ending a pending write. D3D12 removal alone does not retire live D3D11 work.
Existing explicit D3D12 drains remain; there is no D3D11 wait/spin added to dispatcher publication or
disposal. Native output ownership ends only after successful image detach and drained/removed copy work.

The existing bounded recorder keeps schema 1. `publish.begin` now precedes acquisition of the write
lease, hence also precedes a new copy; its latency includes write acquisition and copy transport. Lock
events still mark the real calls, not the duration of a retained lease. `copy.ready` remains query-ready,
not GPU execution time or MIL visibility. The unchanged analyzer represents Ready-to-first-attempt as
unknown when the first attempt precedes Ready; Ready-to-Published and actual event order remain available.
Bound/ready slot 0 may now refer to the same allocation; serials still distinguish completed publications.

`D3D12StableOutputTests` runs the real bridge, owner service and autonomous timer with CPU COM/image
callbacks and a separate real WPF lock-accounting check. Red baseline proves rotating output identity
and pre-lock copying; green cases cover contents/identity, pending completion, lock miss, retry, query
BOOL, source fence, retirement, timeout, device failure and disposal. Controlled content is not native
pixel evidence. No hardware, window, WARP interop or RDP run qualifies asynchronous image continuity or
performance. The fix removes a proven per-frame MIL bitmap/cache replacement path; it does not establish
the cause or cure of user-reported desktop slowness or model loss. Legitimate recreation still detaches.

## Composition-independent accepted transport service

`D3D12WpfPresentation` uses its existing 16-ms Background dispatcher timer while an active canvas has
accepted transport (`HasPendingTransport`), independently of `CompositionTarget.Rendering`. Each active
tick enters the existing `updating` guard, detects generation/size changes and calls only the nonwaiting
`ServiceTransport(false)`. It does not prepare host contracts, reserve render demand, update the camera,
record a scene, create a generation or call `RenderFrame`. Completion listeners remain guarded; manager
lifecycle scheduling follows unwind. The timer stops once accepted transport ends, even with render or
ordinary recovery demand still pending. Disposed, stopping, updating and stale viewport owners reject ticks.
The inactive canvas keeps its existing retirement path, including device-removal and bound-image handling.

Ready-slot retention, the exact publication acknowledgement, the last bound image, software-fallback
publication and zero-duration TryLock including its balancing Unlock remain unchanged. Timer progress is
an additional opportunity to observe readiness; it cannot release a WPF lock, guarantee a 16-ms deadline,
or establish asynchronous MIL/RDP output.

`D3D12TransportTimerTests` exercises the actual dispatcher timer and subscribed owner callback without a
window or device. A constructor-injected native-service delegate models held fence/copy/ready/lock states;
the test observes autonomous continuation after release, preserved demand, no scene callbacks, timer idle,
reentrancy, stop/disposal/owner guards and exception cleanup. Seeded counters and serials belong to that
controlled boundary: these tests establish scheduling, not native copying, real TryLock or pixel freshness.
The real bridge requires a hardware adapter and explicitly rejects WARP; hardware/MIL acceptance is separate.

## Bounded user-controlled render flight recorder

`Viewport3DX.FlightRecorder` is disabled by default. Explicit Start allocates fixed typed buffers for
8,192 submitted WPF scene frames and 32,768 events. Stop retains data; Clear is rejected while recording.
The application owns an independent 60-second deadline and cleanup; append hooks also reject expired data.
Writers use nonblocking TryEnter and count contention/capacity drops, never request rendering or perform I/O.
Snapshot requires Stop and copies occupied buffers only for explicit export outside the render path.

Existing `D3D12WpfPresentation`/`D3D12ImageSource` callbacks remain the source of submission, fence,
copy, lock/backbuffer/dirty/unlock and publication observations. Existing native generation/serial plus a
random presentation-owner GUID prevent replacement owners from sharing correlation keys. Repeated lock
misses belong to one submitted frame. The old one-shot publication logger and budgets are preserved;
the flight recorder introduces no second transport chain, GPU query/readback, wait, device, or logging sink.

The common recorder measures CPU preparation through light updates only when enabled. The WPF owner
records the remaining CPU scene-record duration, Execute/Signal interval, reserved invalidation flags,
actual draw count and period between record starts. These are not GPU execution or visible MIL durations.
The existing publication observation's current serial is reused for delayed phases and rebinds.
Front-buffer availability and generation-local slots retain their original meaning. HWND-only presentations
are identified in diagnostics but do not invent WPF frame or interop samples.

The consumer serializes the concrete records as schema 1 camelCase JSON, with monotonic milliseconds
relative to UTC start. Input categories have no positions, text or application identifiers. Application-owned
process/GC samples and one outstanding low-frequency dispatcher probe remain separate from frames.
CPU-only buffer/identity/expiry tests and productive image-callback tests create no device or window;
native WPF/RDP performance and output remain unmeasured.

`SilkToolkit` targets `net10.0-windows` and references [SilkCore](/projects/silkcore.md).

## Read-only renderer diagnostics

`Viewport3DX.GetRenderDiagnostics()` reads only dispatcher-owned existing presentation state. The
composited and HWND owners expose their already initialized device for this bounded read; no host,
scene, frame, GPU wait or second device is created. `ID3D12Device.GetAdapterLuid` supplies the identity;
`IDXGIFactory4.EnumAdapterByLuid` and `IDXGIAdapter1.GetDesc1` resolve matching name/software metadata.
Failed metadata lookup retains the device LUID with nullable unknown name/type. Before initialization,
the entire adapter/presentation observation is unknown rather than a configured adapter candidate.

The D3D11/D3D9Ex bridge's verified LUID and D3DImage front-buffer availability remain separate fields.
Neither a hardware D3D12 device nor an available front buffer proves hardware WPF/MIL composition,
remote-session performance or visible delivery. No presentation, driver or RDP policy changes occur.
`ViewportRenderDiagnosticsTests` verifies missing-device state and repeated reads of an actual explicit
software-WARP device against its native LUID without a window, hardware fallback or frame submission.

## Current presentation contracts

As of Phase 4 (2026-10-19), `EnableSwapChainRendering=false` is the public default: the passive `DPFCanvas`
publishes D3DImage pixels through the demand-driven WPF owner. Static scenes settle after pending render,
copy and publication work completes. Explicit `true` selects the existing continuous HWND swap chain.
Both owners share the D3D12 scene renderer. The first successful native initialization fixes this choice
across unload/reload and template replacement; failed initialization does not fix it.

The 2026-10-20 Phase 5 documentation correction is source/static only. The dated
[Phase 4 evidence](/testing/wpf-composition-phase4.md) remains historical; parent Phase 5 runtime gates are pending.
Current limits are:

- The WPF bridge explicitly rejects WARP/software adapters; Core WARP checks do not certify WPF composition.
- Sized/high-resolution capture scales the actual captured image; it does not rerender at a larger scene resolution.
- The retained MSAA setting currently uses single-sample rendering. `EnableDeferredRendering` remains an API/XAML
  compatibility flag, without a productive D3D12 deferred shading path.
- Physical monitor transitions, RDP, physical touch/pinch and physical device removal/reset remain unverified.
  Managed input, controlled DPI and manager-reinitialization evidence do not establish those physical scenarios.

# Dependencies

## Original public namespace compatibility

The public WPF types use their original HelixToolkit namespaces again. For example, `Viewport3DX`,
`Material`, `PhongMaterial` and `PBRMaterial` are declared in `HelixToolkit.Wpf.SharpDX`.
The existing `http://helix-toolkit.org/wpf/SharpDX` XML namespace resolves those types without consumer
XAML changes. Assembly names remain SilkCore, SilkToolkit and SilkAssimp.

Original Core type namespaces and acronym spellings are also restored, including `MSAALevel`,
`FXAALevel`, `UVTransform` and `PBRMaterialCore`. Native Silk-specific types retain their namespaces.
There are no compatibility wrapper type hierarchies or newly introduced preprocessor switches.
Mixed source files use separate namespace blocks to retain both original and Silk-specific type ownership.

`OriginalNamespaceCompatibilityTests` verifies representative CLR names and real XAML schema resolution
without constructing viewports or native devices. This compatibility checkpoint does not assert complete
original member compatibility or original application rendering acceptance.

The project uses Silk.NET Direct2D, Direct3D compiler, Direct3D11, Direct3D9, DXGI, and maths packages. It also references `Cyotek.Drawing.BitmapFont`, `Microsoft.CSharp`, and `Microsoft.SourceLink.GitHub`.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkCore](/projects/silkcore.md)

# Citations

[1] [SilkToolkit.csproj](../../Source/SilkToolkit/SilkToolkit.csproj)

## Native child input ownership

Queued native right-button input is tested through a hidden HWND, including ordinary mesh picking and background
rotation before button release, dispatcher heartbeats and release outside the child. The existing capture/input
route is preserved. Pre-pick scene maintenance now prepares the existing hit-test octree; no camera/capture
workaround substitutes for that missing lifecycle phase. Pending presentation frames skip maintenance as well
as scene updates until their fence completes. No visible-window acceptance is inferred from hidden input tests.

Ordinary composition uses a single in-flight Direct3D 12 submission and polls completion rather than waiting on
the WPF dispatcher after every frame. Reusable commands, render bindings and scene preparation remain gated by
the completed fence. Camera updates continue during pending GPU work; the next accepted frame uses current state.
Completed-frame notifications are distinct from skipped ticks and include valid zero-draw frames. Explicit capture
still completes before readback. Effect-manager replacement, resize and disposal retain safe resource lifetimes;
the finite stalled-frame timeout is preserved.

The enabled interactive HwndSwapChainHost explicitly returns HTCLIENT; disabled/noninteractive surfaces return
HTTRANSPARENT. Decoded wheel input is consumed once instead of reaching the WPF owner default procedure.
Native mouse events enter the existing typed WPF InputManager preview/bubble route, retaining the viewport
source during promotion and native DIP coordinates during camera/tool callbacks. The temporary source hook
and position context restore in finally. Existing selection, gesture bindings, click counts and capture owners
remain authoritative. Touch keeps its existing path; physical touch/SpaceMouse behavior is not certified.

The58 ordinary toolkit tests include genuine queued HWND ownership and missing-callback red/green coverage.
Actual Desktop wheel, selection and outside-release capture were inspected at175%; a separate demand-only
VisibleUI test covers the widget-overlay/native boundary. Normal tests create no visible windows.

## Original viewport host and property names

Viewport3DX implements the original IRenderHost boundary using its actual native presentation owner. RenderHost.Viewport
is public for original WPF binding paths; PerFrameFlattenedScene and RenderContext come from completed preparation,
not a second graph. The context survives skipped pending-fence frames and is released only at a safe replacement or
disposal. Rendered fires at completed-frame notification, including valid empty frames. Original and current property
names are registered and synchronized while preserving WPF bindings, coercion and one existing render callback.

The headless compatibility run passes 29 namespace/property/host cases; the Core run passes three hierarchy cases
and six native intersection cases. Full application and visible case acceptance remain separate.

PhongMaterials.DefaultVRML preserves original source spelling and delegates to the existing independently mutable
DefaultVrml factory. The palette/namespace filter now passes 14 headless cases.

## Shared application widget overlays (pre-Phase 4 history)

The existing ViewportWidgetOverlay helper moved from MeshDesktop into this assembly with its original
MeshDesktop.UI.Controls namespace and unchanged class body. Only the explicit System import required by the
library's disabled implicit usings was added. The Desktop assembly forwards the old public identity.
The library depends only on WPF/Win32 for this helper and has no productive application/Core reference.

Its bounded popup retains the original widget's logical inheritance, resource/name lookup, routed commands,
owner clipping, DPI placement and modal enable state above the native D3D12 HWND. The managed boundary test
and all four unchanged native overlay tests pass. At that pre-Phase 4 checkpoint, this helper did not restore
the removed DPFCanvas/D3DImage path. Phase 4 restored WPF/D3DImage as the public default on 2026-10-19,
as described above; this earlier HWND overlay evidence is retained as history.

## Input-derived updates before a frame

`Viewport3DX.OnRendering` runs only after the initialized-surface, camera and local visibility admission
checks, before camera advancement and scene recording. Surface identity/readiness and the current camera
are checked again afterward. `OnRendered` retains its existing post-presentation meaning. This order lets
an application apply deferred input before drawing; it does not guarantee delivery before a mouse release.
The callback runs on the actual frame caller; ordinary WPF composition is dispatcher-owned.

`IsRenderReady` exposes the current readiness predicate without scheduling or promising a future frame.
Application input still validates its loaded/visible context. `GetMouseInputPosition` now exposes the existing
authoritative native DIP position during routed dispatch and restores the ordinary WPF fallback afterward.
Callers must copy the position synchronously rather than query an old event later.

The parent retained60 nonvisible checks and a demand-only actual GPU callback sequence for the initial hook.
The final public pointer/readiness source then passed the two normal NativePointerRoutedEvents tests and
the two demand-only parent lifecycle tests, including ready pre-render/present order and disposed/unready
rejection. The first standalone attempt lacked project.assets.json and ran no tests; normal restore/build
and the same strict filter then passed. Rider/ReSharper MCP tools were absent from the active catalog, so
the CLI fallback was used. Independent source review found no actionable P1/P2 in this narrow slice.

Parent logs and TRXs are retained as full-product-selection-native-pointer-r1/r2 and
full-product-selection-render-ready-visible-r4 under reference-runtime. They certify this observation/frame
boundary, not complete brush input, scheduler-before-release delivery or original pixels.

## Direct-core paths and presentation metrics

`PathModel2D` wraps one `PathNode2D` and exposes its existing `PathRenderCore2D` without converting float colors
through WPF byte colors or owning a second point collection. Its node declines hits. Disposal releases its
owned core resources even when the host has never attached it.

`Viewport3DX.TryGetPresentationMetrics` verifies dispatcher access and reads only the current initialized
surface's allocation width, height and actual pixel scale. Failure returns zero outputs. It does not create
a surface, render, flush pending resize work or infer readiness from configured DPI. Application layout,
camera, foreground and input ownership remain independent checks.

The four owning nonvisible tests and the full normal WPF suite pass4/4 and63/63. A real attached model case
first exposed a shared Scene2D invalidation stack overflow; the original assertions pass after correcting
that shared leaf. Earlier compile failures and the crashing test-process result remain in parent evidence.
No visible window, physical input or application curve-pixel equivalence is certified by these tests.

## Current buttons across the native child boundary

`GetMouseInputButtonState` retains the selected event-device or primary WPF device state when that
device is active, the viewport is disposed, or its actual native presentation host is unavailable.
Only an inactive WPF device with the current loaded, visible, initialized native child polls the OS
asynchronous high bit. Primary-button swap mapping is applied to logical Left/Right. This is a current
observation, not a queued event snapshot, and it grants no foreground, focus, capture or gesture ownership.

Disposal is checked before OS polling because `Disposing` listeners run after the viewport has marked
itself disposed but before the native surface is finally detached. Every ordinary fallback and all five
button mappings retain the additive public API's semantics.

The eight new nonvisible cases and32 connected pointer/wheel/host/disposal checks pass with zero skips.
The parent retains the physical-right admission RED/GREEN and unchanged owned input assertions separately.
A genuine initialized-HWND disposal callback was not observed by these eight tests. Arbitrary stylus,
desktop-switch, system-swap races and Original optical equivalence remain separate acceptance work.
Rider/ReSharper tools were unavailable in the active catalog; scoped CLI fallback was used.

The source graph was rebuilt from996 files:9,424 nodes,16,646 edges and zero LLM tokens. Its17 existing
partially parsed files remain a structural-analysis limitation.

## TopMost overlay boundaries

D3D12FrameRenderer classifies enabled TopMostGroupNode roots and all prepared visible descendants
into the same ordered overlay stream as ScreenSpacedNode, after ordinary scene/post effects and before
Scene2D. Without post-scene groups the stream precedes FXAA; with those groups it retains the existing
post-FXAA placement after their depth-preserving draws. Ordinary groups and disabled nested TopMost
groups inherit the nearest active owner.
A standalone disabled TopMost group stays in the ordinary scene. Every enabled boundary starts a new
segment in flattened preorder, including nested boundaries, as in the original renderer.

TopMost segments restore main-camera constants, the full viewport/scissor and color/depth target,
and clear depth without clearing stencil. They retain traversal order and each child's material depth
and blend states; local occlusion remains enabled. ScreenSpace retains its camera, viewport and existing
opaque/transparent/post-effect handling. TopMost children have no post effects in the original contract.
The shared overlay phase restores full viewport/scissor and render targets before subsequent passes.
The public ClearDepthStencil signature/default behavior is unchanged; an internal overload preserves stencil.

D3D12TopMostTests covers ownership/EnableTopMost without native devices, and actual 32x24 offscreen
WARP glyph pixels behind a nearer quad, preceding ScreenSpace camera/scissor, hidden ancestry, local depth,
transparent selection, multiple and nested roots. Before any submission each new native test verifies
the device LUID maps to software adapter Microsoft 1414:008C; no hardware fallback or windows.
This does not qualify native WPF presentation or hardware stability. Existing billboard resource lifetime
and application label-job fixes are independent and unchanged.


## Actual WPF frame admission and passive allocation metrics

The initialized WPF owner now participates in public IsRenderReady and TryGetPresentationMetrics.
Readiness is derived from the selected owner and existing scene/lifetime gates; it is not a stored flag
or a promise of GPU availability, foreground ownership, successful publication or scanout. WPF metrics
reject a canvas handle/dimension mismatch instead of returning layout-ahead values during pending resize.
The getter reads retained allocation dimensions and coherent render scale without initializing, polling
GPU health, resizing, consuming demand or advancing transport. Disposed native owners report no metrics.

Both real backends now notify OnRendering for an admitted reserved attempt. Already requested frames
notify before their single camera-controller step and scene recording. Idle camera-demand discovery
can precede this notification; it never triggers a second same-timestamp camera step. Busy/transport-only
polls do not notify. The recorder binds the camera current after the callback. The existing frame guard
retains borrowed resources during callbacks and defers requested viewport disposal until unwind; template
replacement and nested recording remain guarded. Existing eligibility is checked again before recording.

Actual unchanged regression data: 18 failures before; 16 passes and two pre-existing WPF warm-drain
failures after. Camera replacement, fractional/disabled DPI, pending resize, manager/hide/automatic-stop,
nested render/template and callback disposal cases pass. The two warm-blocked idle/reload cases and the
existing completion-disposal case do not establish their later assertions. Ten old toolkit neighbors pass;
P4 remains three passes/five unchanged WPF drain failures. No complete hardware/interop acceptance.

The consuming original visible RenderTick now passes its strict [1,2,1,2] order. Parent headless1300
retains all1299 previous outcomes and IDs. A real inactive Selection stale-read preview passes, while
its separate reload test blocks at the second wait. Physical Brush admission reaches ready and two
before/after callbacks but remains blocked by actual foreground ownership, before input injection.

The separately observed frontbuffer loss is unresolved. SetBackBuffer already used
 enableSoftwareFallback:true in13a7; availability must not be forced or suspend guards removed.
Root reports under reference-runtime distinguish source, actual artifacts, GPU completion, publication
and display. All complete Meshmixer product cycles, Original optics and physical DPI gates remain open.

## Stable COM outputs in D3DImage interop

Silk.NET 2.23.0's generic ComPtr output overload uses GetAddressOf without pinning. A field or array
element can move during the native call, leaving the output at a stale address despite a successful
HRESULT. The bridge and existing probe now create D3D9Ex through a stack pointer, validate HRESULT
and the returned interface, release a partial failure reference and adopt the successful reference
without AddRef. Other native outputs into their fields and arrays are pinned for the call.

Cleanup detaches the actual ComPtr owner before Release, including array elements; struct copies do
not acquire references. Existing local outputs, balanced D3D11 adoption, thread affinity, adapter-LUID
selection, GPU quiescence and WPF detach requirements remain unchanged. The borrowed device is not released.

Nine headless cases reproduce the legacy address movement without writing stale memory, cover creation
under compacting GC, HRESULT failures with and without a partial reference, success without an interface,
both actual owners' adapter enumeration through fake COM and repeated partial cleanup. No window or GPU
device is created. This proves the defect and correction mechanism, not the exact GC history of a past
application failure or real hardware/WPF compatibility. Consumer packages must be rebuilt to receive it.

Independent source/data review finds no concrete new P1/P2 in this three-file patch; blocked runtime
assertions and the documented idle-discovery boundary remain qualified. Graphify was rebuilt from1075
source files:11068 nodes,20181 edges,3253 communities, zero model tokens and17 existing partial-parser warnings.

## Invalidated curve frames and the offscreen evidence boundary

The public viewport invalidation methods request render demand and visual invalidation; they
neither clear color targets nor retire mesh buffers. Child add/remove requests scene-graph
preparation, while geometry replacement recreates only that node's CPU buffer model. Each
productive frame rebuilds its prepared candidates synchronously before recording the clear.

`D3D12CurveFrameTests.cs` extends the existing windowless `OffscreenFrame` harness. Three
public invalidation variants each exercise twenty individual frames through the productive
recorder: dynamic line/point geometry, TopMost glyph replacement, null/empty curve geometry,
and an invalidation during preparation with a preserved subsequent reservation. The actual
device LUID resolves to a software DXGI adapter with Microsoft WARP vendor/device IDs before
submission. All sixty frames retain the mesh draw, snapshot membership, geometry/CPU buffer
owner and all 196 interior pixels. Four uncovered mesh samples stay exactly green RGBA;
covered pixels may be opaque red. Thirty-four focused demand/TopMost/curve cases pass.

The offscreen test ends at the completed D3D12 color target. It does not instantiate the
D3D11/D3D9 WPF bridge, exercise D3DImage publication, establish frontbuffer or asynchronous
MIL freshness, or identify modules loaded in an application session. Stable native pixels
must not be presented as a correction or disproof of a whole-model presentation dropout.

## Bounded input-triggered publication observation

`Viewport3DX.ObserveNextWpfPublications` arms one passive window per viewport, retained across native
recreation. It schedules no frame and is inactive at startup. Existing application input consumers choose
when to call it. Limits are 48 naturally requested frames, 800 metadata events including the end marker,
and 15 monotonic seconds; repeated input cannot reset the window. Expiry is observed on the next
natural call, without a timer, so idle or blocked code need not emit an immediate end marker.

The productive WPF owner correlates existing counters and serials with call boundaries before/after
GPU submission, copy, image lock, backbuffer, dirty rectangle and unlock. Snapshot milestones include
actual recorder draws, prepared mesh count and successful RenderVisible mesh-pass counts (including
auxiliary/overlay passes, not unique patient scans). Surface identities are generation-local slot indexes;
owner GUID plus native generation identifies the bridge. Module version/MVID/path is recorded at arm.
No raw addresses, scene coordinates, pixels or application tokens are collected. The existing LoggerLib
sink is reused. Image lock balancing, software fallback and native ownership are unchanged.

Headless tests cover arm/budgets/expiry/correlation and the real uninitialized owner's counters/demand.
They neither initialize a GPU device nor establish native interop/MIL output. Successful managed
publication and missing warnings never establish absence of visible flicker.


Integration follow-up: the shipped .NET 11 Unlock decrements before commit/Changed callbacks. The image
clears its managed lease before that call (after dispatcher/mutability checks); a throwing callback leaves
the completed serial ready for fresh-lock, copy-free publication retry. Two CPU regressions include a real
WPF Changed failure and native lock-count check. Assembly metadata `WpfTransportPhases` describes the
actual early-lock transport for consumers without product-version inference or additional render demand.
