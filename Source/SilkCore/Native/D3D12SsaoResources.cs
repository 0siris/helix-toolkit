/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns the normal, depth, noise, occlusion, and binding resources for DX12 SSAO.
/// </summary>
internal sealed class SilkD3D12SsaoResources : IDisposable {
    /// <summary>
    ///     The normal render-target format.
    /// </summary>
    internal const Format NormalFormat = Format.FormatR16G16B16A16Float;

    /// <summary>
    ///     The occlusion render-target format.
    /// </summary>
    internal const Format OcclusionFormat = Format.FormatR16Float;

    /// <summary>
    ///     The fixed SSAO sample count declared by the repository shader.
    /// </summary>
    internal const int KernelSize = 32;

    /// <summary>
    ///     The native device.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     Full-screen binding tables.
    /// </summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>
    ///     The b0 global constants.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer globalTransforms;

    /// <summary>
    ///     The b1 SSAO kernel and parameters.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer parameters;

    /// <summary>
    ///     Fallback constants for unused registers.
    /// </summary>
    private readonly SilkD3D12Resource fallbackConstants;

    /// <summary>
    ///     The private RTV heap for normal, raw, blurred, and noise targets.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap renderTargetHeap;

    /// <summary>
    ///     The stable RTV descriptors.
    /// </summary>
    private readonly SilkD3D12Descriptor[] renderTargetViews;

    /// <summary>
    ///     The private DSV heap.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap depthStencilHeap;

    /// <summary>
    ///     The stable DSV descriptor.
    /// </summary>
    private readonly SilkD3D12Descriptor depthStencilView;

    /// <summary>
    ///     The deterministic hemisphere sample kernel.
    /// </summary>
    private readonly Vector4[] kernel = CreateKernel();

    /// <summary>
    ///     The size-dependent normal target.
    /// </summary>
    private SilkD3D12Resource? normals;

    /// <summary>
    ///     The size-dependent raw occlusion target.
    /// </summary>
    private SilkD3D12Resource? rawOcclusion;

    /// <summary>
    ///     The size-dependent blurred occlusion target.
    /// </summary>
    private SilkD3D12Resource? occlusion;

    /// <summary>
    ///     The size-dependent typeless depth target.
    /// </summary>
    private SilkD3D12Resource? depth;

    /// <summary>
    ///     The deterministic one-pixel rotation-noise texture.
    /// </summary>
    private readonly SilkD3D12Resource noise;

    /// <summary>
    ///     Creates stable descriptor owners and the deterministic noise resource.
    /// </summary>
    /// <param name="device">The native device.</param>
    /// <param name="resourceHeap">The shared resource heap.</param>
    /// <param name="samplerHeap">The shared sampler heap.</param>
    internal SilkD3D12SsaoResources(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        this.device = device;
        bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        globalTransforms = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(0),
            GlobalTransformStruct.SizeInBytes);
        parameters = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(1),
            SsaoParamStruct.SizeInBytes);
        fallbackConstants = device.CreateBuffer(256, HeapType.Upload);
        fallbackConstants.Write(new byte[256]);
        bindings.InitializeFallbackDescriptors(device, fallbackConstants, 0, 1);
        device.CreateSampler(bindings.Sampler(0), DefaultSamplers.SsaoSamplerClamp);
        device.CreateSampler(bindings.Sampler(1), DefaultSamplers.SsaoNoise);
        renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 4);
        renderTargetViews = renderTargetHeap.AllocateRange(4);
        depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        depthStencilView = depthStencilHeap.Allocate();
        noise = device.CreateRenderTargetTexture2D(1, 1, Format.FormatR32G32B32A32Float);
        device.CreateRenderTargetView(noise, renderTargetViews[3]);
    }

    /// <summary>
    ///     Gets the completed occlusion texture.
    /// </summary>
    internal SilkD3D12Resource Occlusion => occlusion
        ?? throw new InvalidOperationException("SSAO resources are not initialized.");

    /// <summary>
    ///     Gets whether the complete resource set has been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Clears and binds the geometry normal/depth targets.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="width">The target width.</param>
    /// <param name="height">The target height.</param>
    internal void BeginGeometry(SilkD3D12CommandContext context, uint width, uint height) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        Resize(width, height);
        context.Transition(normals!, ResourceStates.RenderTarget);
        context.Transition(depth!, ResourceStates.DepthWrite);
        context.ClearRenderTarget(normals!, renderTargetViews[0], [0, 0, 0, 1]);
        context.ClearDepthStencil(depth!, depthStencilView);
        context.SetRenderTargets(renderTargetViews[0], depthStencilView);
        context.SetViewport(width, height);
    }

    /// <summary>
    ///     Evaluates and blurs SSAO, returning the completed shader-readable map.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="ssaoPass">The existing SSAO evaluation pass.</param>
    /// <param name="blurPass">The existing SSAO blur pass.</param>
    /// <param name="transforms">The current global constants.</param>
    /// <param name="radius">The view-space sample radius.</param>
    /// <param name="textureScale">The quality-dependent texture scale.</param>
    /// <returns>The completed occlusion texture.</returns>
    internal SilkD3D12Resource Complete(
        SilkD3D12CommandContext context,
        ShaderPass ssaoPass,
        ShaderPass blurPass,
        in GlobalTransformStruct transforms,
        float radius,
        int textureScale
    ) {
        if (!float.IsFinite(radius) || radius <= 0) throw new ArgumentOutOfRangeException(nameof(radius));
        if (textureScale < 1) throw new ArgumentOutOfRangeException(nameof(textureScale));
        var normalTarget = normals ?? throw new InvalidOperationException("SSAO resources are not initialized.");
        var rawTarget = rawOcclusion ?? throw new InvalidOperationException("SSAO resources are not initialized.");
        var finalTarget = Occlusion;
        var depthTarget = depth ?? throw new InvalidOperationException("SSAO resources are not initialized.");
        context.Transition(noise, ResourceStates.RenderTarget);
        context.ClearRenderTarget(noise, renderTargetViews[3], [1, 0, 0, 0]);
        context.Transition(noise, ResourceStates.PixelShaderResource);
        context.Transition(normalTarget, ResourceStates.PixelShaderResource);
        context.Transition(depthTarget, ResourceStates.PixelShaderResource);
        context.Transition(rawTarget, ResourceStates.RenderTarget);
        device.CreateShaderResourceView(normalTarget, bindings.ShaderResource(31));
        device.CreateShaderResourceView(noise, bindings.ShaderResource(32));
        device.CreateShaderResourceView(depthTarget, bindings.ShaderResource(33), Format.FormatR32Float);
        var ssao = new SsaoParamStruct {
            NoiseScale = new Vector2(normalTarget.Description.Width, normalTarget.Description.Height),
            TextureScale = textureScale,
            Radius = radius,
            InvProjection = transforms.Projection.Inverted()
        };
        globalTransforms.Write(in transforms);
        parameters.Write<Vector4>(kernel);
        parameters.Write(in ssao, KernelSize * 16);
        context.SetRenderTarget(renderTargetViews[1]);
        context.SetViewport(checked((uint) normalTarget.Description.Width), normalTarget.Description.Height);
        ssaoPass.BindShader(context);
        bindings.BindGraphics(context);
        context.DrawInstanced(4);

        context.Transition(rawTarget, ResourceStates.PixelShaderResource);
        context.Transition(finalTarget, ResourceStates.RenderTarget);
        device.CreateShaderResourceView(rawTarget, bindings.ShaderResource(31));
        context.SetRenderTarget(renderTargetViews[2]);
        blurPass.BindShader(context);
        bindings.BindGraphics(context);
        context.DrawInstanced(4);
        context.Transition(finalTarget, ResourceStates.PixelShaderResource);
        return finalTarget;
    }

    /// <summary>
    ///     Creates the deterministic hemisphere sample kernel used by every host.
    /// </summary>
    /// <returns>Exactly 32 normalized and progressively scaled samples.</returns>
    internal static Vector4[] CreateKernel() {
        var result = new Vector4[KernelSize];
        const float goldenAngle = 2.39996323f;
        for (var index = 0; index < result.Length; index++) {
            var unit = (index + 0.5f) / result.Length;
            var radius = MathF.Sqrt(unit);
            var angle = index * goldenAngle;
            var direction = SilkMath.Normalize(new Vector3(MathF.Cos(angle) * radius,
                MathF.Sin(angle) * radius,
                MathF.Sqrt(1 - unit)));
            var scale = 0.1f + 0.9f * index / (result.Length - 1f);
            result[index] = new Vector4(direction * scale, 0);
        }
        return result;
    }

    /// <summary>
    ///     Releases every native resource and descriptor owner.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        depth?.Dispose();
        occlusion?.Dispose();
        rawOcclusion?.Dispose();
        normals?.Dispose();
        noise.Dispose();
        depthStencilView.Dispose();
        depthStencilHeap.Dispose();
        foreach (var view in renderTargetViews) view.Dispose();
        renderTargetHeap.Dispose();
        fallbackConstants.Dispose();
        parameters.Dispose();
        globalTransforms.Dispose();
        bindings.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Recreates every size-dependent target atomically.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    private void Resize(uint width, uint height) {
        ArgumentOutOfRangeException.ThrowIfZero(width);
        ArgumentOutOfRangeException.ThrowIfZero(height);
        if (normals is { Description.Width: var oldWidth, Description.Height: var oldHeight } &&
            oldWidth == width && oldHeight == height)
            return;
        SilkD3D12Resource? nextNormals = null;
        SilkD3D12Resource? nextRaw = null;
        SilkD3D12Resource? nextFinal = null;
        SilkD3D12Resource? nextDepth = null;
        try {
            nextNormals = device.CreateRenderTargetTexture2D(width, height, NormalFormat);
            nextRaw = device.CreateRenderTargetTexture2D(width, height, OcclusionFormat);
            nextFinal = device.CreateRenderTargetTexture2D(width, height, OcclusionFormat);
            nextDepth = device.CreateTexture2D(width,
                height,
                Format.FormatR32Typeless,
                ResourceFlags.AllowDepthStencil,
                ResourceStates.DepthWrite);
            device.CreateRenderTargetView(nextNormals, renderTargetViews[0]);
            device.CreateRenderTargetView(nextRaw, renderTargetViews[1]);
            device.CreateRenderTargetView(nextFinal, renderTargetViews[2]);
            device.CreateDepthStencilView(nextDepth, depthStencilView, Format.FormatD32Float);
        } catch {
            nextDepth?.Dispose();
            nextFinal?.Dispose();
            nextRaw?.Dispose();
            nextNormals?.Dispose();
            throw;
        }
        depth?.Dispose();
        occlusion?.Dispose();
        rawOcclusion?.Dispose();
        normals?.Dispose();
        normals = nextNormals;
        rawOcclusion = nextRaw;
        occlusion = nextFinal;
        depth = nextDepth;
    }
}
