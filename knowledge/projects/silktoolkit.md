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
