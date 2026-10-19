---
type: ImplementationPlan
title: D3D12 WPF composition proof and phased integration
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-19T11:10:00Z }
status: draft
sources:
  - resource: ../../wpf-komposition-phasen-parallele-tasks-und-reviews.md
  - resource: ../../Source/SilkToolkit/SilkToolkit.csproj
  - resource: ../../Source/SilkToolkit/Controls/D3D12PresentationSurface.cs
---

# Scope

The [implementation plan](../../wpf-komposition-phasen-parallele-tasks-und-reviews.md) defines six phases,
parallel task ownership, thirteen historical review findings and an explicit approval boundary after each phase.
Phase 1 is an isolated hardware interoperability proof. Production Viewport selection and renderer extraction
belong to later phases.

The intended default is `EnableSwapChainRendering = false`, restoring WPF composition. Explicit `true`
retains native HWND presentation. The future WPF path renders and publishes only on demand; an isolated
invalidation requests six total frames by default. Updates received during a frame must survive its completion.

# Working baseline

The branch was fast-forwarded from `7344b9cf80de00feb6dfc102b41f5690619bc091` to
`72557068603fbe3017dc045782e24631970a4fb1` from the local MeshMeister toolkit checkout. That checkout's
uncommitted knowledge files were not copied or changed. Logging remains at
`ed78901cb880a17b43433afc642449be9e8bc64e`. No application changes or push are authorized by this phase.

The first Rider baseline build found seven existing errors across four example files: ambiguous Assimp
`Exporter`, missing WPF color extension imports and two obsolete XAML namespaces. Scoped import/namespace
corrections produced a successful second Rider build (`4d4900f8-6d6d-47c4-ba27-32d74299b1d7`). Existing
package/analyzer warnings remain outside this proof.

The baseline standard test run used `Category!=Hardware&Category!=DX12`: 617 passed and 8 failed across
625 cases (Assimp 76/78, Core 441/444, Toolkit 100/103). These failures preceded the new probe:

- `LoadNativeGltfFactorsTest`: two cases fail at the material null assertion.
- `WarpManagesSharedTextureResources` and `WarpManagesSharedGeometryResources`: maintenance assertions fail.
- `PostProcessCpuContractsAreDeterministic`: random sample collections differ.
- `RightDragMovesCameraBeforeOutsideRelease`: two cases fail at the geometry octree null assertion.
- `DelayedFenceKeepsDispatcherLiveAndRendersLatestCameraAfterCompletion`: geometry octree null assertion fails.

Rider's terminal response failed with HTTP 404 after running the tests. Per-project TRX files with prefix
`wpf-phase1-baseline` establish the actual outcomes; no CLI fallback was used after these test failures.
The full baseline gate is therefore not green, even if the isolated interoperability proof succeeds.

# Phase 1 proof contract

The bridge uses a D3D12 BGRA render resource exported with an NT handle, opened on a D3D11 device with the
same actual adapter LUID. D3D9Ex creates a separate legacy-shared BGRA texture; D3D11 opens that allocation
and copies the completed D3D12 image into it. WPF receives the corresponding D3D9 surface through `D3DImage`.
Creating the destination in D3D9Ex follows the documented D3D9-to-D3D11 sharing direction.

NT handles are closed by their owner; legacy sharing handles are not NT handles. Adapter ordinals from
different APIs are never treated as identities. Explicit adapter selection must still resolve to the same LUID.

The caller polls GPU completion without blocking the dispatcher. Two legacy-shared destination slots alternate:
the currently attached WPF surface stays immutable while D3D11 copies into the detached slot. After copy
completion, the caller obtains the WPF image lock, replaces the backbuffer, publishes a dirty rectangle and
unlocks. It must publish the completed slot before the next copy may reuse the previously detached slot.
Before native disposal, it detaches the WPF backbuffer. Explicit teardown may use a bounded completion wait.
No scene renderer changes, CPU presentation loop or silent HWND fallback are part of this proof.

The independent synchronization review identified two WPF details that affect later integration as well:
`TryLock` increments the internal lock count even when returning false in the inspected .NET 10 source, so
each normally returning attempt is balanced with `Unlock`; software snapshot/fallback paths can read the user
surface outside the managed lock-count gate. The latter implies a potential race if the attached surface is
written across dispatcher callbacks; immutable attached slots avoid relying on that unproven synchronization.
See the [versioned WPF implementation](https://github.com/dotnet/wpf/blob/v10.0.0/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/InterOp/D3DImage.cs).

Target hardware: NVIDIA GeForce GTX 1070, driver `32.0.15.6094`. Passing here will not establish WARP,
Remote Desktop, other-adapter or device-loss support. Historical WPF software fallback is not such a guarantee.

# Remaining scope

The plan maps findings F01–F13 to later tasks: property invalidation, continuous sources, demand preservation,
frame budgets, manager events, shared scene consumers, DPI, Scene2D, lifecycle/input and capture pixel contracts.
Existing ineffective MSAA and CPU-scaled requested capture dimensions remain explicitly documented limits.

# References

- [Microsoft: D3D11 shared resource restrictions](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-opensharedresource)
- [Microsoft: D3DImage locking](https://learn.microsoft.com/en-us/dotnet/api/system.windows.interop.d3dimage.lock)
- [Microsoft: WPF and Direct3D9 interoperation](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-and-direct3d9-interoperation)
