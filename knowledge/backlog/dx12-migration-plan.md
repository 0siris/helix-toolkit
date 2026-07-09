---
type: BacklogPlan
title: DX12 Migration Plan
description: Parallel opt-in migration plan for the Direct3D12 backend.
tags: [backlog, dx12, migration, renderer]
timestamp: 2026-07-09T00:00:00+02:00
---

# DX12 Migration Plan

## Summary

* DX11 remains the stable default backend.
* DX12 is built as a separate opt-in backend path.
* The first runtime milestone is a WPF HWND swap chain that renders a real D3D12 shader/vertex path using SM6 DXIL.
* The existing `IRenderHost`, `EffectsManager`, `ShaderPass`, and `DeviceContextProxy` stack is not made backend-neutral first because it is currently deeply DX11-typed.

## Current Baseline

* ShaderBuilder already builds SM6 `.dxil` through the repository-local DXC MSBuild integration.
* SilkCore already embeds generated DXIL resources under `Resources\DX12`.
* SilkCore has an isolated Direct3D12 native foundation for device, command queue/context, fence, and empty root signature creation.
* Existing DX11 rendering remains based on `DX11RenderHostBase`, `DefaultRenderHost`, `SwapChainRenderHost`, `DX11RenderBufferProxyBase`, and `DeviceContextProxy`.

## Milestone 1: DX12 Preview Backend

Build the smallest real DX12 render path that proves the toolchain, runtime device setup, command submission, and presentation path.

Required behavior:

* Create a D3D12 device through the existing `SilkD3D12DeviceFactory`.
* Add DXGI factory/adapter selection and HWND swap chain creation.
* Add RTV descriptor heap and back buffer render target views.
* Add optional depth buffer only if needed by the first mesh test.
* Record command lists with the required resource barriers:
  * `Present -> RenderTarget`
  * clear/render
  * `RenderTarget -> Present`
* Execute the command list and present through the swap chain.
* Synchronize frame completion with the existing D3D12 fence wrapper.
* Resize by waiting for GPU idle, releasing back buffers, resizing swap chain buffers, and recreating RTVs.

Acceptance:

* A DX12 preview surface opens in WPF.
* It clears to the requested background color.
* It renders a colored triangle or quad through an SM6 DXIL vertex/pixel shader pair.
* Resize and window close do not crash or leak obvious native resources.
* DX11 demos still run unchanged.

## Milestone 2: WPF Opt-in Surface

Add a WPF-only DX12 preview control without touching the default `Viewport3DX` path.

Required behavior:

* Reuse the existing `WinformHostExtend`, `RenderControl`, and `CompositionTargetEx` pattern.
* Add a new DX12 preview canvas/control for HWND swap-chain rendering.
* Keep `DPFCanvas` unchanged because D3DImage is not the short stable path for D3D12.
* Add the preview to `SwapChainRenderingDemo` or a small dedicated DX12 demo.
* The demo must be opt-in; no existing sample should switch to DX12 by default.

Acceptance:

* The preview demo can be launched directly.
* Switching/running DX12 does not change existing DX11 sample behavior.
* Failure to create a DX12 device reports a controlled error instead of breaking DX11.

## Milestone 3: First Scene Data Bridge

After the preview backend is stable, bridge the first real scene data without attempting full feature parity.

Required behavior:

* Reuse the existing scene traversal concepts from `DefaultRenderHost`, but do not refactor the whole DX11 render host yet.
* Add a DX12 mesh upload path for basic static geometry.
* Add a minimal PSO/root-signature path for one default material.
* Use existing camera matrices and per-frame constants where possible.
* Support only opaque basic mesh rendering in this milestone.

Out of scope:

* D2D overlays.
* Hit testing.
* Shadows.
* OIT/transparency.
* Post effects.
* Texture/material feature parity.

Acceptance:

* A simple mesh renders through DX12 using the existing camera.
* Existing DX11 material rendering is unchanged.
* DX12 unsupported scene features are skipped or clearly reported.

## Later Migration Backlog

Order after Milestone 3:

1. Constant-buffer and descriptor-table management.
2. PSO cache keyed by shader pair, input layout, blend/depth/raster state, render-target format, and topology.
3. Texture upload and SRV descriptor management.
4. Default material parity for diffuse/color/PBR basics.
5. Transparent rendering and OIT.
6. Shadow maps.
7. Post effects and screen-space passes.
8. D2D/frame-stat overlays.
9. Screenshot/capture path.
10. Public backend selection on `Viewport3DX` once DX12 has useful feature coverage.

## Testing

Build checks:

```powershell
dotnet build Source\SilkToolkit.Native.ShaderBuilder\SilkToolkit.Native.ShaderBuilder.csproj --no-restore
dotnet build Source\SilkToolkit.slnx --no-restore /clp:ErrorsOnly
```

Runtime checks:

* DX11 `SimpleDemo` still starts.
* DX11 `SwapChainRenderingDemo` still starts.
* DX12 preview demo starts, resizes, renders, and closes cleanly.
* DX12 device creation failure is handled without disabling DX11.

## Assumptions

* DX12 remains Windows/WPF/HWND-swap-chain-only until the preview backend works.
* DX11 remains the default runtime path until DX12 reaches useful feature parity.
* New abstractions are added only when at least two working backends need the same interface.
* New HLSL is allowed only for the minimal DX12 preview shader if existing shaders do not match the required input/output contract.
