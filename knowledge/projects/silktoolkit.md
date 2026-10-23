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
