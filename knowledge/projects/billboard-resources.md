---
type: Concept
title: Billboard texture preparation and descriptor ownership
description: Preparation order and null descriptor lifetime for native billboard generations.
tags: [silkcore, billboard, d3d12, resource-lifetime]
status: stable
verified: 2026-11-02
sources:
  - resource: ../../Source/SilkCore/Core/Buffers/BillboardBufferModel.cs
  - resource: ../../Source/SilkCore/Core/PointLineRenderCore.cs
  - resource: ../../Source/SilkCore/Native/D3D12Bindings.cs
  - resource: ../../Source/SilkCore/Native/D3D12GeometryBuffers.cs
  - resource: ../../Source/SilkCore.Tests/D3D12BillboardPreparationTests.cs
---

# Billboard texture preparation and descriptor ownership

`DefaultBillboardBufferModel.PrepareForD3D12` initializes the current geometry before either its texture
or vertex stream is consumed. `PointLineRenderCore` reads the prepared texture before updating bindings;
native vertex preparation uses the same method. A fresh geometry replacing a previously bound geometry
therefore produces its own texture before t0 is populated. Existing geometry preparation remains idempotent.
The public API and ordinary line/point vertex paths remain unchanged.

`SilkD3D12PointLineBindings.Update` explicitly writes a null t0 SRV when there is no texture. Shared graphics
bindings clear a registered texture's descriptor before disposing its registration, including completed-frame
cleanup of replaced or detached owners. Unregistered pass-specific buffer/texture slots are not cleared by
this registration cleanup. Replacement registration retains its target through the existing resource manager;
native resources are still reclaimed at the existing completed-frame boundary. This change introduces no
additional queue submission, fence wait, task scheduling or exception suppression.

`D3D12BillboardPreparationTests` combines CPU geometry generation/replacement/empty-state checks with
source contracts for preparation order, actual t0 destinations and clearing before registration disposal.
The CPU billboard double does not rasterize text. These checks require no native device or submission;
they do not establish actual glyph pixels, driver descriptor behavior or the cause of an application freeze.
