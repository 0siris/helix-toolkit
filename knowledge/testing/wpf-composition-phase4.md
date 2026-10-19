---
type: VerificationReport
title: WPF composition Phase 4 implementation and verification
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-19T18:05:52Z }
verified: { by: "agent:gpt-6.1-sol", at: 2026-10-19T18:25:40Z }
status: stable
sources:
  - resource: ../../specs/002-wpf-composition-phase4/tasks.md
  - resource: ../../wpf-komposition-phasen-parallele-tasks-und-reviews.md
  - resource: ../../Source/SilkToolkit.Tests/P4HostFrameContractTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P4ManagerPresentationTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P4WpfBehaviorTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P4VisibleCompositionTests.cs
  - resource: ../../Source/SilkToolkit.Tests/SharedSceneConsumerTests.cs
  - resource: ../../Source/SilkCore.Tests/P4ManagerResourceTests.cs
  - resource: ../../Source/SilkCore.Tests/P4MaterialInvalidationTests.cs
  - resource: ../../Source/SilkCore.Tests/P4AttachmentPermissionTests.cs
  - resource: ../../Source/SilkCore.Tests/P4Scene2DPermissionTests.cs
  - resource: ../../Source/SilkToolkit.Tests/P4Scene2DResourceLifecycleTests.cs
  - resource: ../../Source/SilkToolkit.Tests/ManagerResourceLifecycleTests.cs
---

# Scope and implementation

The user approved Phase 4 through `speckit-implement`, with isolated parallel implementation and test tasks.
Phase 5 remains a separate approval boundary. The baseline is `619feb80d`, with 729/737 standard cases passing
and eight documented pre-existing failures. No push or application-checkout modification belongs to this work.

The public default is now WPF composition (`EnableSwapChainRendering=false`). Explicit `true` preserves HWND
presentation. Successful native initialization fixes selection across reload and template replacement;
failed initialization does not. Public DPI policy controls physical render dimensions without scaling input DIPs.
Image state and material parameters wake bounded demand; status output remains idle.

Shared scenes retain renderer-specific attachment leases. Notification membership and native resource ownership
have distinct lifetimes, preserving unloaded WPF transport while including it in later manager retirement.
Borrowed managers survive viewport disposal; resource disposal drains accepted GPU work before freeing resources.
Reinitialization restores the complete resource generation. A cached permission predicate revokes further scene
access across application callbacks; ordinary manager identity changes use nonblocking completion checks.

# Visible acceptance

`ProductionViewportComposesWithControlsBrushEffectsAndAdorners` passed against the actual public default and
shipped template. The test showed a real Window, restored its cursor position and closed the Window afterward.
Both diagnostic WPF snapshots and physical desktop `GetPixel`/`WindowFromPoint` measurements were checked.
CPU snapshots never supplied pixels to the GPU presentation.

| Check | Observed result |
| --- | --- |
| Productive D3DImage | Nonzero surface; seven submitted, completed, copied and published frames |
| WPF Button above GPU | Desktop RGB `(0,255,0)` and one actual OS mouse click |
| Lowered/restored Button ZIndex | Red GPU pixel, then green Button pixel |
| Clipped viewport | Magenta parent replaces the clipped GPU region |
| VisualBrush | Red mesh and black background from the actual viewport |
| Blur | Mesh-edge red channel changes from `0` to `45` |
| AdornerLayer | Cyan patch over the GPU, followed by successful removal |
| Settled scene | All four native milestone counters remain seven throughout WPF composition operations |

The 200-percent DPI case produced `320x240` pixels versus `160x120` with scaling disabled and retained the
same scene picking point `(0,0,0)`. This is controlled render-scale evidence, not a physical monitor transition.

# Validation status

The final implementation is `cb9f2ca50`, following the independent R6 and R7 reviews without open P1/P2 findings.
The serial Rider full rebuild `5798282c-447f-444a-968c-baafc9f1934e` passed. Artifact timestamps confirm the new
Toolkit DLL at 20:20:59 local time and its test assembly at 20:21:16, after the final source change at 20:19:17.

| Final run | Assimp | Core | Toolkit | Total |
| --- | --- | --- | --- | --- |
| Standard, `wpf-phase4-standard-fresh` | 76/78 | 528/531 | 226/229 | **830/838** |
| Targeted including explicit hardware, `wpf-phase4-acceptance-fresh` | 0 selected | 42/42 | 109/109 | **151/151** |

The standard failures are exactly the eight Phase 3 baseline cases: two `LoadNativeGltfFactorsTest` material-null
cases; `WarpManagesSharedTextureResources` and `WarpManagesSharedGeometryResources` maintenance assertions;
`PostProcessCpuContractsAreDeterministic` random samples; two `RightDragMovesCameraBeforeOutsideRelease`
octree-null cases; and `DelayedFenceKeepsDispatcherLiveAndRendersLatestCameraAfterCompletion` octree-null.
All 101 additional standard cases pass. The selected acceptance cases overlap the standard suite and must not
be added to its count. Per-project TRX files are under each test project's ignored `TestResults` directory.

Earlier failures exposed the binding-coercion issue and missing pre-template native-start subscription.
Pixel fixtures now use a Phong material for scene lighting, and native shape reattachment recreates its owned
brushes as WPF elements do. The retirement-completion fixture explicitly retries failed zero-timeout detachment,
preserving its final event-count and subscription assertions. Final assertions were not relaxed.

Incremental Rider successes sometimes retained stale Core or Toolkit artifacts; DLL timestamps and missing test
discovery exposed this. Full rebuilds supplied the final evidence above. Rider terminal calls returned HTTP 404
after starting tests; completed TRX results and process termination establish the outcomes. No duplicate CLI
fallback was started in response to those transport errors.

ReSharper formatted all 45 changed C# files in `format` mode. The isolated formatting commit changes only
whitespace. Warning-level diagnostics completed for 44 files; the large Core scene renderer timed out twice.
Completed inspections reported no errors. Existing namespace/API compatibility, deliberate disposal/reentrancy
tests and style warnings were inspected without semantic cleanup; the full compiler build remains authoritative.
The final changed C# XML documentation blocks parse successfully.

Reproduce through the repository Rider workflow, running one operation at a time:

```powershell
dotnet test SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=DX12"
dotnet test SilkToolkit.slnx --no-build --filter "FullyQualifiedName~P4|FullyQualifiedName~SharedScene|FullyQualifiedName~ManagerResourceLifecycle|FullyQualifiedName~D3D12WpfPresentationTests|FullyQualifiedName~D3D12ImageInteropBridgeNativeTests" -- xUnit.Explicit=on
```

These commands use `Source` as the Rider solution root, after a successful solution probe/build. Explicit
hardware execution is part of this authorized phase, not the default repository test gate.

## Warm heap results

| Measured operation | Final result |
| --- | --- |
| WPF mesh, 128 accepted frames | 20,480 bytes, **160 bytes/frame** |
| WPF mesh + FXAA + Scene2D, 128 accepted frames | 20,480 bytes, **160 bytes/frame** |
| WPF idle, 1,000 STA calls | **0 bytes** |
| Native bridge, 64 real copies and 1,024 ready polls | **0 bytes** in both measured regions |
| Core mesh, textured and untextured, 256 frames each | 18,432 bytes each, **72 bytes/frame** |
| Native Scene2D, 1,000 warm preparations/cleanup | **0 bytes**, positive control 64 bytes |
| New cached-permission Scene2D with update subscriber, 1,000 preparations | **0 bytes** |
| Failed WPF locks, 1,000 attempts | Framework and owner each **32,000 bytes** |

Existing exact-zero assertions and productive-frame limits remain unchanged. The separate managed alias/status
diagnostic measures 49,152 bytes over 1,024 operations, with a 1,048-byte positive control; this is not the GPU
frame measurement. Initialization, capacity growth, capture and exceptional recovery are outside the warm regions.

The existing zero-timeout disposal contract produced one image-detach timeout in an earlier hardware series; the isolated
diagnostic passed. Independent review confirmed the same preflight path exists in the Phase 3 baseline. Owners
remain retained after failure and the explicit timeout/retry test passed. This does not guarantee that a synchronous
dispose always acquires WPF's image lock.

## Generated knowledge and closure

The local Graphify wrapper exited successfully, prepared **1,054 source files** and wrote **10,703 nodes,
20,095 edges and 2,819 communities**. It used zero model tokens, reported the same 17 partial-parser warnings
and removed `.graphify-code-corpus`. Generated output was not edited manually. The durable snapshot is recorded
in [the graph concept](/graphify-database.md).

All Phase 4 tasks are complete. Plan findings F26–F34 include the review corrections; the original Assimp feature
pointer remains untouched. Changes are committed locally. Phase 5 and application-checkout changes were not started.

# Review findings and closure

- Initialization, binding coercion, template ownership, selective image invalidation and DPI policy were checked
  against the original host flow and tested through the public property/template surface.
- Shared hierarchy removal must invalidate even with native null-manager attachment. Renderer leases preserve
  the final consumer and restore a surviving host. Actual shared WPF/WPF and WPF/HWND tests cover departure,
  descendant/material/collection changes, manager epochs and replacement/rejoin.
- Timeout retry retains owners and barriers. Frame-exit scheduling prevents nested dispatcher callback loops;
  unchanged shared reload uses a retained invalidation revision instead of generating scene work.
- Retirement callbacks must not resubscribe a disposed viewport; capture must reject old readbacks after skipped,
  canceled or reentrant requests. A complete same-instance manager restart includes all 2D resources.
- Material observation follows attachment lifetime and remains suppressed throughout reentrant teardown.
  Technique and Scene2D callbacks require permission checks before later resource access.
- Disabled Scene2D stops input as well as drawing/capture, releases hover/press state and preserves cube/scene
  priority. A hidden view cube must not consume picking.
- A consumer joining during disposal needs availability published before the notification, while preserving
  the public `Initialized` event-order contract.
- An observer rejecting resource disposal restores concrete live availability for existing and late consumers;
  pending retirement and shared barriers still finish before new preparation. Four managed regressions cover
  shared/nonshared and immediate/deferred retirement.
- Explicit HWND `StartRendering()` retains one handler before template creation. The WPF selection never
  subscribes that native handler; the existing public host-binding regression verifies pre-template behavior.

# Remaining boundaries

Physical touch/pinch and OS touch-promotion injection, physical monitor changes, RDP, additional adapters and
injected physical device removal remain unverified. Managed routed input and touch cleanup are narrower evidence.
The inherited MSAA limitation and scaled high-resolution capture are unchanged; scaling is not a higher-resolution
scene render. Full effect/performance parity and physical fault acceptance remain Phase 5 work.

The Spec-Kit prerequisite script is absent and its feature pointer selects an unrelated Assimp task. The attempted
script failure and explicit WPF feature-directory adaptation are recorded in the feature plan; the unrelated
pointer/tasks remain unchanged. The final extension-hook check found no `.specify/extensions.yml`, so there were
no registered post-implementation hooks.
