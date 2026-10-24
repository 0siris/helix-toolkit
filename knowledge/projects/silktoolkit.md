---
type: Project
title: SilkToolkit
description: Windows/WPF-facing toolkit layer built on SilkCore.
resource: ../../Source/SilkToolkit/SilkToolkit.csproj
tags: [project, wpf, windows, silknet]
timestamp: 2026-07-08T00:00:00+02:00
---

# Project

`SilkToolkit` targets `net10.0-windows` and references [SilkCore](/projects/silkcore.md).

# Dependencies

The project uses Silk.NET Direct2D, Direct3D compiler, Direct3D11, Direct3D9, DXGI, and maths packages. It also references `Cyotek.Drawing.BitmapFont`, `Microsoft.CSharp`, and `Microsoft.SourceLink.GitHub`.

# Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [SilkCore](/projects/silkcore.md)

# Citations

[1] [SilkToolkit.csproj](../../Source/SilkToolkit/SilkToolkit.csproj)

## Native child input ownership

The enabled interactive HwndSwapChainHost explicitly returns HTCLIENT; disabled/noninteractive surfaces return
HTTRANSPARENT. Decoded wheel input is consumed once instead of reaching the WPF owner default procedure.
Native mouse events enter the existing typed WPF InputManager preview/bubble route, retaining the viewport
source during promotion and native DIP coordinates during camera/tool callbacks. The temporary source hook
and position context restore in finally. Existing selection, gesture bindings, click counts and capture owners
remain authoritative. Touch keeps its existing path; physical touch/SpaceMouse behavior is not certified.

The58 ordinary toolkit tests include genuine queued HWND ownership and missing-callback red/green coverage.
Actual Desktop wheel, selection and outside-release capture were inspected at175%; a separate demand-only
VisibleUI test covers the widget-overlay/native boundary. Normal tests create no visible windows.

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
