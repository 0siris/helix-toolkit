---
type: Project
title: SilkToolkit
description: Windows/WPF-facing toolkit layer built on SilkCore.
resource: ../../Source/SilkToolkit/SilkToolkit.csproj
tags: [project, wpf, windows, silknet]
timestamp: 2026-07-08T00:00:00+02:00
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-20T07:15:06Z }
status: stable
sources:
  - resource: ../../Source/SilkToolkit/Controls/Viewport3DX.PresentationSelection.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12WpfPresentation.cs
  - resource: ../../Source/SilkToolkit/Controls/D3D12ImageInteropBridge.cs
  - resource: ../../Source/SilkToolkit/Extensions/ViewportExtensions.cs
  - resource: ../testing/wpf-composition-phase4.md
---

# Project

`SilkToolkit` targets `net10.0-windows` and references [SilkCore](/projects/silkcore.md).

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
FXAA/Scene2D. Ordinary groups and disabled nested TopMost groups inherit the nearest active owner.
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
