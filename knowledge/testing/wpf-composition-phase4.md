---
type: VerificationReport
title: WPF composition Phase 4 implementation and verification
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-19T18:05:52Z }
status: in-progress
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

Final serial build, standard-suite comparison, explicit hardware/heap verification, independent review closure
and Graphify are still in progress. Intermediate results are not the final phase gate.

The current targeted acceptance run passed 91/92 cases. Its remaining light-pixel assertion used a diffuse
camera-facing material that intentionally ignores scene lights; the fixture now uses a Phong material for that
part of the unchanged light-output assertion. Earlier stale Core artifacts were detected by missing test discovery
and DLL timestamps despite an incremental Rider success. A full Rider rebuild exposed the new Core test compile
diagnostic; it was corrected, and subsequent artifact timestamps/discovery confirmed the current Core code.

The existing zero-timeout disposal contract produced one image-detach timeout in a hardware series; the isolated
diagnostic passed. Independent review confirmed the same preflight path exists in the Phase 3 baseline. Owners
remain retained after failure and the explicit timeout/retry test passed. This does not guarantee that a synchronous
dispose always acquires WPF's image lock.

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

# Remaining boundaries

Physical touch/pinch and OS touch-promotion injection, physical monitor changes, RDP, additional adapters and
injected physical device removal remain unverified. Managed routed input and touch cleanup are narrower evidence.
The inherited MSAA limitation and scaled high-resolution capture are unchanged; scaling is not a higher-resolution
scene render. Full effect/performance parity and physical fault acceptance remain Phase 5 work.

The Spec-Kit prerequisite script is absent and its feature pointer selects an unrelated Assimp task. The attempted
script failure and explicit WPF feature-directory adaptation are recorded in the feature plan; the unrelated
pointer/tasks remain unchanged. Extension hooks must be checked again before completion.
