using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Diagnostics;
using System.Runtime.InteropServices;
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

    /// Retains the blur input independently of the normal input referenced by the preceding SSAO draw.
    private readonly SilkD3D12GraphicsBindings blurBindings;

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
    ///     The private RTV heap for normal, raw and blurred targets.
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
    ///     The original randomized hemisphere sample kernel.
    /// </summary>
    private readonly Vector4[] kernel;

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
    ///     The original four-by-four XY rotation-noise texture.
    /// </summary>
    private readonly SilkD3D12Resource noise;

    /// Retains the initial noise upload until the resource owner is safely released.
    private readonly SilkD3D12Resource noiseUpload;
    /// Describes the padded Direct3D 12 upload rows for the original noise image.
    private readonly PlacedSubresourceFootprint noiseFootprint;
    /// Tracks whether the immutable original noise image has been recorded for upload.
    private bool noiseUploaded;


    /// <summary>
    ///     Creates stable descriptor owners and the original randomized sample and noise resources.
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
        blurBindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        globalTransforms = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(0),
            GlobalTransformStruct.SizeInBytes);
        parameters = new SilkD3D12ConstantBuffer(device,
            bindings.ConstantBuffer(1),
            SSAOParamStruct.SizeInBytes);
        fallbackConstants = device.CreateBuffer(256, HeapType.Upload);
        fallbackConstants.Write(new byte[256]);
        bindings.InitializeFallbackDescriptors(device, fallbackConstants, 0, 1);
        blurBindings.InitializeFallbackDescriptors(device, fallbackConstants);
        device.CreateConstantBufferView(globalTransforms.Resource, blurBindings.ConstantBuffer(0),
            checked((uint)globalTransforms.Resource.SizeInBytes));
        device.CreateConstantBufferView(parameters.Resource, blurBindings.ConstantBuffer(1),
            checked((uint)parameters.Resource.SizeInBytes));
        device.CreateSampler(bindings.Sampler(0), DefaultSamplers.SsaoSamplerClamp);
        device.CreateSampler(bindings.Sampler(1), DefaultSamplers.SsaoNoise);
        device.CreateSampler(blurBindings.Sampler(0), DefaultSamplers.SsaoSamplerClamp);
        renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 3);
        renderTargetViews = renderTargetHeap.AllocateRange(3);
        depthStencilHeap = device.CreateDescriptorHeap(DescriptorHeapType.Dsv, 1);
        depthStencilView = depthStencilHeap.Allocate();
        var random = new Random((int)Stopwatch.GetTimestamp());
        kernel = CreateKernel(random);
        var noiseValues = new Vector4[4 * 4];
        for (var index = 0; index < noiseValues.Length; index++) {
            var x = random.NextFloat(-1, 1);
            var y = random.NextFloat(-1, 1);
            noiseValues[index] = new Vector4(SilkMath.Normalize(new Vector3(x, y, 0)), 0);
        }
        noise = device.CreateTexture2D(4, 4, Format.FormatR32G32B32A32Float);
        noiseUpload = device.CreateTextureUploadBuffer(noise,
            MemoryMarshal.AsBytes(noiseValues.AsSpan()), 4 * 16, out noiseFootprint);
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
    /// <exception cref="ArgumentOutOfRangeException">The radius or texture scale is invalid.</exception>
    /// <exception cref="InvalidOperationException">The geometry targets have not been initialized.</exception>
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
        if (!noiseUploaded) {
            context.CopyBufferToTexture(noise, noiseUpload, in noiseFootprint);
            context.Transition(noise, ResourceStates.PixelShaderResource);
            noiseUploaded = true;
        }
        context.Transition(normalTarget, ResourceStates.PixelShaderResource);
        context.Transition(depthTarget, ResourceStates.PixelShaderResource);
        context.Transition(rawTarget, ResourceStates.RenderTarget);
        device.CreateShaderResourceView(normalTarget, bindings.ShaderResource(31));
        device.CreateShaderResourceView(noise, bindings.ShaderResource(32));
        device.CreateShaderResourceView(depthTarget, bindings.ShaderResource(33), Format.FormatR32Float);
        var ssao = new SSAOParamStruct {
            NoiseScale = new Vector2(normalTarget.Description.Width / 4f, normalTarget.Description.Height / 4f),
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
        device.CreateShaderResourceView(rawTarget, blurBindings.ShaderResource(31));
        context.SetRenderTarget(renderTargetViews[2]);
        blurPass.BindShader(context);
        blurBindings.BindGraphics(context);
        context.DrawInstanced(4);
        context.Transition(finalTarget, ResourceStates.PixelShaderResource);
        return finalTarget;
    }

    /// <summary>Creates the original randomized hemisphere kernel.</summary>
    /// <returns>The original thirty-two progressively scaled samples above the fifteen-degree threshold.</returns>
    internal static Vector4[] CreateKernel() => CreateKernel(new Random((int)Stopwatch.GetTimestamp()));

    /// <summary>Uses the original random sequence, rejection cone and i/32 scaling rule.</summary>
    /// <param name="random">The random source shared with the four-by-four noise generation.</param>
    /// <returns>The original thirty-two samples in generation order.</returns>
    private static Vector4[] CreateKernel(Random random) {
        var result = new Vector4[KernelSize];
        var threshold = Math.Cos(Math.PI / 2 - Math.PI / 12);
        for (var index = 0; index < result.Length; index++) {
            while (true) {
                var x = random.NextFloat(-1, 1);
                var y = random.NextFloat(-1, 1);
                var z = random.NextFloat(1e-3f, 1);
                var direction = SilkMath.Normalize(new Vector3(x, y, z));
                if (SilkMath.Dot(direction, Vector3.UnitZ) < threshold) continue;
                var scale = index / 32f;
                scale = 0.1f + 0.9f * scale;
                result[index] = new Vector4(direction * scale, 0);
                break;
            }
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
        noiseUpload.Dispose();
        depthStencilView.Dispose();
        depthStencilHeap.Dispose();
        foreach (var view in renderTargetViews) view.Dispose();
        renderTargetHeap.Dispose();
        fallbackConstants.Dispose();
        parameters.Dispose();
        globalTransforms.Dispose();
        blurBindings.Dispose();
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
