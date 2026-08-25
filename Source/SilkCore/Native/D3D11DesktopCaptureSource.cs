/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Isolates the permitted Direct3D 11 desktop-duplication edge and returns CPU-owned BGRA frames.
/// </summary>
internal sealed unsafe class D3D11DesktopCaptureSource : IDesktopCaptureSource {
    /// <summary>The COM identifier for IDXGIDevice.</summary>
    private static readonly Guid DxgiDeviceGuid = new("54ec77fa-1377-44e6-8c32-88fd5f44c84c");

    /// <summary>The COM identifier for IDXGIOutput1.</summary>
    private static readonly Guid DxgiOutput1Guid = new("00cddea8-939b-4b83-a340-a685226666cc");

    /// <summary>The COM identifier for ID3D11Texture2D.</summary>
    private static readonly Guid Texture2DGuid = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

    /// <summary>DXGI_ERROR_WAIT_TIMEOUT.</summary>
    private const int WaitTimeout = unchecked((int) 0x887A0027);

    /// <summary>DXGI_ERROR_ACCESS_LOST.</summary>
    private const int AccessLost = unchecked((int) 0x887A0026);

    /// <summary>The lazily created D3D11 device and immediate context.</summary>
    private SilkD3DDeviceResources? resources;

    /// <summary>The current output-duplication session.</summary>
    private IDXGIOutputDuplication* duplication;

    /// <summary>The reusable CPU-readable staging texture.</summary>
    private Texture2D? staging;

    /// <summary>Starts duplication on the selected adapter-local output.</summary>
    /// <param name="output">The zero-based output index.</param>
    public void Start(int output) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(output);
        if (resources is not null) throw new InvalidOperationException("Desktop capture has already started.");

#pragma warning disable CS0612
        resources = SilkD3D11DeviceFactory.CreateDefault();
#pragma warning restore CS0612
        IDXGIDevice* dxgiDevice = null;
        IDXGIAdapter* adapter = null;
        IDXGIOutput* dxgiOutput = null;
        IDXGIOutput1* output1 = null;
        try {
            var deviceGuid = DxgiDeviceGuid;
            SilkMarshal.ThrowHResult(resources.Device.Handle->QueryInterface(&deviceGuid, (void**) &dxgiDevice));
            SilkMarshal.ThrowHResult(dxgiDevice->GetAdapter(&adapter));
            SilkMarshal.ThrowHResult(adapter->EnumOutputs(checked((uint) output), &dxgiOutput));
            var outputGuid = DxgiOutput1Guid;
            SilkMarshal.ThrowHResult(dxgiOutput->QueryInterface(&outputGuid, (void**) &output1));
            IDXGIOutputDuplication* createdDuplication = null;
            SilkMarshal.ThrowHResult(output1->DuplicateOutput((IUnknown*) resources.Device.Handle,
                &createdDuplication));
            duplication = createdDuplication;
        } catch {
            DisposeSession();
            throw;
        } finally {
            if (output1 != null) output1->Release();
            if (dxgiOutput != null) dxgiOutput->Release();
            if (adapter != null) adapter->Release();
            if (dxgiDevice != null) dxgiDevice->Release();
        }
    }

    /// <summary>Attempts to acquire and copy one desktop frame.</summary>
    /// <param name="timeout">The bounded wait.</param>
    /// <param name="frame">The complete tightly packed BGRA frame.</param>
    /// <returns>Whether a new frame was available.</returns>
    public bool TryAcquire(TimeSpan timeout, out ScreenCaptureFrame frame) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (duplication == null || resources is null)
            throw new InvalidOperationException("Desktop capture has not started.");
        if (timeout < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));

        frame = default;
        OutduplFrameInfo info = default;
        IDXGIResource* acquired = null;
        var milliseconds = checked((uint) Math.Min(uint.MaxValue, Math.Ceiling(timeout.TotalMilliseconds)));
        var result = duplication->AcquireNextFrame(milliseconds, &info, &acquired);
        if (result == WaitTimeout) return false;
        if (result == AccessLost) throw new ScreenCaptureResetException();
        SilkMarshal.ThrowHResult(result);

        try {
            ID3D11Texture2D* texture = null;
            var textureGuid = Texture2DGuid;
            SilkMarshal.ThrowHResult(acquired->QueryInterface(&textureGuid, (void**) &texture));
            try {
                Texture2DDesc description = default;
                texture->GetDesc(&description);
                EnsureStaging(in description);
                resources.ImmediateContext.Handle->CopyResource((ID3D11Resource*) staging!.TextureHandle,
                    (ID3D11Resource*) texture);
                var mapped = resources.ImmediateContext.MapSubresource(staging, 0, MapMode.Read, MapFlags.None);
                try {
                    var rowPitch = checked(description.Width * 4);
                    var pixels = new byte[checked((int) (rowPitch * description.Height))];
                    for (var row = 0u; row < description.Height; row++)
                        Marshal.Copy(mapped.DataPointer + checked((int) (row * mapped.RowPitch)),
                            pixels,
                            checked((int) (row * rowPitch)),
                            checked((int) rowPitch));
                    frame = new ScreenCaptureFrame(description.Width,
                        description.Height,
                        description.Format,
                        rowPitch,
                        pixels);
                    frame.Validate();
                    return true;
                } finally {
                    resources.ImmediateContext.UnmapSubresource(staging, 0);
                }
            } finally {
                texture->Release();
            }
        } finally {
            acquired->Release();
            SilkMarshal.ThrowHResult(duplication->ReleaseFrame());
        }
    }

    /// <summary>Gets whether this source has released all native resources.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Creates or replaces the staging texture when desktop dimensions or format change.</summary>
    /// <param name="description">The acquired desktop texture description.</param>
    private void EnsureStaging(in Texture2DDesc description) {
        if (staging is { } current &&
            current.Description.Width == description.Width &&
            current.Description.Height == description.Height &&
            current.Description.Format == description.Format)
            return;
        staging?.Dispose();
        staging = resources!.Device.CreateTexture2D(new Texture2DDescription {
            Width = checked((int) description.Width),
            Height = checked((int) description.Height),
            MipLevels = 1,
            ArraySize = 1,
            Format = description.Format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CpuAccessFlags = CpuAccessFlags.Read,
            OptionFlags = ResourceOptionFlags.None
        });
    }

    /// <summary>Releases the current session while allowing failed startup cleanup.</summary>
    private void DisposeSession() {
        staging?.Dispose();
        staging = null;
        if (duplication != null) {
            duplication->Release();
            duplication = null;
        }
        resources?.Dispose();
        resources = null;
    }

    /// <summary>Releases every D3D11 and DXGI object.</summary>
    public void Dispose() {
        if (IsDisposed) return;
        DisposeSession();
        IsDisposed = true;
    }
}
