/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Shaders;
using Silk.NET.Direct3D12;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Uploads isolated desktop frames into Direct3D 12 and renders the latest frame through the existing screen pass.
/// </summary>
internal sealed class SilkD3D12ScreenCaptureResources : IDisposable {
    /// <summary>The native device.</summary>
    private readonly SilkD3D12Device device;

    /// <summary>The complete screen-pass descriptor table.</summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>The existing b9 screen-quad constants.</summary>
    private readonly SilkD3D12ConstantBuffer screenConstants;

    /// <summary>Zero-filled fallback constant storage.</summary>
    private readonly SilkD3D12Resource fallbackConstants;

    /// <summary>The latest uploaded frame, retained across capture timeouts.</summary>
    private SilkD3D12TextureModelResource? texture;

    /// <summary>Initializes one capture renderer.</summary>
    /// <param name="device">The native device.</param>
    /// <param name="resourceHeap">The shared resource heap.</param>
    /// <param name="samplerHeap">The shared sampler heap.</param>
    internal SilkD3D12ScreenCaptureResources(SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap) {
        this.device = device;
        ResourceHeap = resourceHeap;
        bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        screenConstants = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(9),
            ScreenDuplicationModelStruct.SizeInBytes);
        fallbackConstants = device.CreateBuffer(256, HeapType.Upload);
        fallbackConstants.Write(new byte[256]);
        bindings.InitializeFallbackDescriptors(device, fallbackConstants, 9);
        device.CreateSampler(bindings.Sampler(0), Filter.MinMagMipLinear, TextureAddressMode.Clamp);
    }

    /// <summary>Gets the heap used for transient frame texture SRVs.</summary>
    private SilkD3D12DescriptorHeap ResourceHeap { get; }

    /// <summary>Gets the latest uploaded texture.</summary>
    internal SilkD3D12Resource? Texture => texture?.Resource;

    /// <summary>Gets the number of successful frame replacements.</summary>
    internal int Generation { get; private set; }

    /// <summary>Gets whether every owned resource has been released.</summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>Acquires, uploads, and renders the newest frame, retaining the last frame on timeout.</summary>
    /// <param name="context">The open command context.</param>
    /// <param name="pass">The existing screen-duplication pass.</param>
    /// <param name="core">The existing capture core.</param>
    /// <param name="targetWidth">The destination width.</param>
    /// <param name="targetHeight">The destination height.</param>
    /// <returns>Whether a frame was rendered.</returns>
    internal bool TryRender(SilkD3D12CommandContext context,
        ShaderPass pass,
        ScreenCloneRenderCore core,
        uint targetWidth,
        uint targetHeight) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        if (core.TryAcquireFrame(TimeSpan.Zero) is { } frame) ReplaceFrame(context, in frame);
        if (texture is null) return false;

        var constants = CreateConstants(core, texture.Resource.Description.Width, texture.Resource.Description.Height,
            targetWidth, targetHeight);
        screenConstants.Write(in constants);
        context.Transition(texture.Resource, ResourceStates.PixelShaderResource);
        device.CreateShaderResourceView(texture.Resource, bindings.ShaderResource(0));
        pass.BindShader(context);
        bindings.BindGraphics(context);
        context.DrawInstanced(4);
        return true;
    }

    /// <summary>Replaces the current texture after the preceding presentation fence has completed.</summary>
    /// <param name="context">The open command context.</param>
    /// <param name="frame">The complete CPU-owned frame.</param>
    private void ReplaceFrame(SilkD3D12CommandContext context, in ScreenCaptureFrame frame) {
        frame.Validate();
        var replacement = SilkD3D12TextureModelResource.Create(device,
            context,
            ResourceHeap,
            new TextureModel(frame.Pixels, frame.Format, checked((int) frame.Width), checked((int) frame.Height)));
        texture?.Dispose();
        texture = replacement;
        Generation++;
    }

    /// <summary>Creates clip-space and source-UV constants for the current capture settings.</summary>
    /// <param name="core">The capture settings.</param>
    /// <param name="sourceWidth">The captured width.</param>
    /// <param name="sourceHeight">The captured height.</param>
    /// <param name="targetWidth">The target width.</param>
    /// <param name="targetHeight">The target height.</param>
    /// <returns>The complete b9 payload.</returns>
    internal static ScreenDuplicationModelStruct CreateConstants(ScreenCloneRenderCore core,
        ulong sourceWidth,
        uint sourceHeight,
        uint targetWidth,
        uint targetHeight) {
        ArgumentOutOfRangeException.ThrowIfZero(sourceWidth);
        ArgumentOutOfRangeException.ThrowIfZero(sourceHeight);
        ArgumentOutOfRangeException.ThrowIfZero(targetWidth);
        ArgumentOutOfRangeException.ThrowIfZero(targetHeight);
        var crop = core.CloneRectangle.IsEmpty
            ? new Rectangle(0, 0, checked((int) sourceWidth), checked((int) sourceHeight))
            : core.CloneRectangle;
        crop.Left = Math.Clamp(crop.Left, 0, checked((int) sourceWidth) - 1);
        crop.Top = Math.Clamp(crop.Top, 0, checked((int) sourceHeight) - 1);
        crop.Width = Math.Clamp(crop.Width, 1, checked((int) sourceWidth) - crop.Left);
        crop.Height = Math.Clamp(crop.Height, 1, checked((int) sourceHeight) - crop.Top);

        var scaleX = 1f;
        var scaleY = 1f;
        if (!core.StretchToFill) {
            var sourceAspect = (float) crop.Width / crop.Height;
            var targetAspect = (float) targetWidth / targetHeight;
            if (sourceAspect > targetAspect) scaleY = targetAspect / sourceAspect;
            else scaleX = sourceAspect / targetAspect;
        }
        var left = (float) crop.Left / sourceWidth;
        var top = (float) crop.Top / sourceHeight;
        var right = (float) crop.Right / sourceWidth;
        var bottom = (float) crop.Bottom / sourceHeight;
        return new ScreenDuplicationModelStruct {
            TopRight = new Vector4(scaleX, scaleY, 0, 1),
            TopLeft = new Vector4(-scaleX, scaleY, 0, 1),
            BottomRight = new Vector4(scaleX, -scaleY, 0, 1),
            BottomLeft = new Vector4(-scaleX, -scaleY, 0, 1),
            TexTopRight = new Vector2(right, top),
            TexTopLeft = new Vector2(left, top),
            TexBottomRight = new Vector2(right, bottom),
            TexBottomLeft = new Vector2(left, bottom)
        };
    }

    /// <summary>Releases the current frame and all binding resources.</summary>
    public void Dispose() {
        if (IsDisposed) return;
        texture?.Dispose();
        fallbackConstants.Dispose();
        screenConstants.Dispose();
        bindings.Dispose();
        IsDisposed = true;
    }
}
