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
