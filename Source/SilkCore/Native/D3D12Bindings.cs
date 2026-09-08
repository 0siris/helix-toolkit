/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.ShaderManager;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Owns one contiguous pair of resource and sampler tables matching the shared root signature.
/// </summary>
internal sealed class SilkD3D12GraphicsBindings : IDisposable {
    /// <summary>
    ///     The number of constant-buffer registers in one resource table.
    /// </summary>
    internal static readonly int ConstantBufferCount = GetRangeCount(DescriptorRangeType.Cbv);

    /// <summary>
    ///     The number of shader-resource registers in one resource table.
    /// </summary>
    internal static readonly int ShaderResourceCount = GetRangeCount(DescriptorRangeType.Srv);

    /// <summary>
    ///     The number of unordered-access registers in one resource table.
    /// </summary>
    internal static readonly int UnorderedAccessCount = GetRangeCount(DescriptorRangeType.Uav);

    /// <summary>
    ///     The number of sampler registers in one sampler table.
    /// </summary>
    internal static readonly int SamplerCount = checked((int) D3D12DefaultRootSignatureLayout.SamplerRange
        .DescriptorCount);

    /// <summary>
    ///     The resource descriptor heap containing this table.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap resourceHeap;

    /// <summary>
    ///     The sampler descriptor heap containing this table.
    /// </summary>
    private readonly SilkD3D12DescriptorHeap samplerHeap;

    /// <summary>
    ///     The contiguous CBV, SRV, and UAV descriptors.
    /// </summary>
    private readonly SilkD3D12Descriptor[] resources;

    /// <summary>
    ///     The contiguous sampler descriptors.
    /// </summary>
    private readonly SilkD3D12Descriptor[] samplers;

    /// <summary>
    ///     Initializes one complete graphics binding-table pair.
    /// </summary>
    /// <param name="resourceHeap">The shader-visible CBV/SRV/UAV heap.</param>
    /// <param name="samplerHeap">The shader-visible sampler heap.</param>
    internal SilkD3D12GraphicsBindings(
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        resourceHeap.GuardNotNull();
        samplerHeap.GuardNotNull();
        ValidateHeap(resourceHeap, DescriptorHeapType.CbvSrvUav, nameof(resourceHeap));
        ValidateHeap(samplerHeap, DescriptorHeapType.Sampler, nameof(samplerHeap));

        this.resourceHeap = resourceHeap;
        this.samplerHeap = samplerHeap;
        resources = resourceHeap.AllocateRange(ConstantBufferCount + ShaderResourceCount + UnorderedAccessCount);
        try {
            samplers = samplerHeap.AllocateRange(SamplerCount);
        } catch {
            foreach (var descriptor in resources) descriptor.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets whether both descriptor tables have been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets the first descriptor in the CBV/SRV/UAV table.
    /// </summary>
    internal SilkD3D12Descriptor ResourceTableStart {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return resources[0];
        }
    }

    /// <summary>
    ///     Gets the first descriptor in the sampler table.
    /// </summary>
    internal SilkD3D12Descriptor SamplerTableStart {
        get {
            ObjectDisposedException.ThrowIf(IsDisposed, this);
            return samplers[0];
        }
    }

    /// <summary>
    ///     Gets the descriptor for one constant-buffer register.
    /// </summary>
    /// <param name="shaderRegister">The b-register index.</param>
    /// <returns>The destination CBV descriptor.</returns>
    internal SilkD3D12Descriptor ConstantBuffer(int shaderRegister) =>
        GetDescriptor(resources, shaderRegister, ConstantBufferCount, 0, nameof(shaderRegister));

    /// <summary>
    ///     Gets the descriptor for one shader-resource register.
    /// </summary>
    /// <param name="shaderRegister">The t-register index.</param>
    /// <returns>The destination SRV descriptor.</returns>
    internal SilkD3D12Descriptor ShaderResource(int shaderRegister) =>
        GetDescriptor(resources,
            shaderRegister,
            ShaderResourceCount,
            ConstantBufferCount,
            nameof(shaderRegister));

    /// <summary>
    ///     Gets the descriptor for one unordered-access register.
    /// </summary>
    /// <param name="shaderRegister">The u-register index.</param>
    /// <returns>The destination UAV descriptor.</returns>
    internal SilkD3D12Descriptor UnorderedAccess(int shaderRegister) =>
        GetDescriptor(resources,
            shaderRegister,
            UnorderedAccessCount,
            ConstantBufferCount + ShaderResourceCount,
            nameof(shaderRegister));

    /// <summary>
    ///     Gets the descriptor for one sampler register.
    /// </summary>
    /// <param name="shaderRegister">The s-register index.</param>
    /// <returns>The destination sampler descriptor.</returns>
    internal SilkD3D12Descriptor Sampler(int shaderRegister) =>
        GetDescriptor(samplers, shaderRegister, SamplerCount, 0, nameof(shaderRegister));

    /// <summary>
    ///     Binds both heaps and tables after a graphics root signature has been set.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    internal void BindGraphics(SilkD3D12CommandContext context) {
        context.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        context.SetGraphicsDescriptorTables(resources[0], samplers[0]);
    }

    /// <summary>
    ///     Binds both heaps and tables after a compute root signature has been set.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    internal void BindCompute(SilkD3D12CommandContext context) {
        context.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.SetDescriptorHeaps(resourceHeap, samplerHeap);
        context.SetComputeDescriptorTables(resources[0], samplers[0]);
    }

    /// <summary>
    ///     Initializes every unused root-table slot with a valid fallback descriptor.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="fallbackConstants">The 256-byte zero constant buffer.</param>
    /// <param name="ownedConstantBuffers">The b-registers populated by dedicated buffers.</param>
    internal void InitializeFallbackDescriptors(
        SilkD3D12Device device,
        SilkD3D12Resource fallbackConstants,
        params int[] ownedConstantBuffers
    ) {
        for (var shaderRegister = 0; shaderRegister < ConstantBufferCount; shaderRegister++) {
            if (ownedConstantBuffers.Contains(shaderRegister)) continue;
            device.CreateConstantBufferView(fallbackConstants, ConstantBuffer(shaderRegister), 256);
        }
        for (var shaderRegister = 0; shaderRegister < ShaderResourceCount; shaderRegister++)
            device.CreateNullShaderResourceView(ShaderResource(shaderRegister));
        for (var shaderRegister = 0; shaderRegister < UnorderedAccessCount; shaderRegister++)
            device.CreateNullUnorderedAccessView(UnorderedAccess(shaderRegister));
        for (var shaderRegister = 0; shaderRegister < SamplerCount; shaderRegister++)
            device.CreateSampler(Sampler(shaderRegister));
    }

    /// <summary>
    ///     Releases both contiguous descriptor ranges.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        foreach (var descriptor in resources) descriptor.Dispose();
        foreach (var descriptor in samplers) descriptor.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Gets the declared count for one resource range.
    /// </summary>
    /// <param name="type">The descriptor range type.</param>
    /// <returns>The range count.</returns>
    private static int GetRangeCount(DescriptorRangeType type) => checked((int) D3D12DefaultRootSignatureLayout
        .ResourceRanges.Single(range => range.Type == type).DescriptorCount);

    /// <summary>
    ///     Validates a shader-visible descriptor heap.
    /// </summary>
    /// <param name="heap">The heap to validate.</param>
    /// <param name="type">The required heap type.</param>
    /// <param name="parameterName">The constructor parameter name.</param>
    private static void ValidateHeap(
        SilkD3D12DescriptorHeap heap,
        DescriptorHeapType type,
        string parameterName
    ) {
        ObjectDisposedException.ThrowIf(heap.IsDisposed, heap);
        if (heap.Type != type || !heap.IsShaderVisible)
            throw new ArgumentException($"A shader-visible {type} heap is required.", parameterName);
    }

    /// <summary>
    ///     Resolves one validated shader register to its table descriptor.
    /// </summary>
    /// <param name="descriptors">The table descriptors.</param>
    /// <param name="shaderRegister">The shader register.</param>
    /// <param name="registerCount">The declared register count.</param>
    /// <param name="tableOffset">The register-range offset within the table.</param>
    /// <param name="parameterName">The shader-register parameter name.</param>
    /// <returns>The resolved descriptor.</returns>
    private static SilkD3D12Descriptor GetDescriptor(
        IReadOnlyList<SilkD3D12Descriptor> descriptors,
        int shaderRegister,
        int registerCount,
        int tableOffset,
        string parameterName
    ) {
        if ((uint) shaderRegister >= (uint) registerCount)
            throw new ArgumentOutOfRangeException(parameterName);
        return descriptors[tableOffset + shaderRegister];
    }
}

/// <summary>
///     Owns one persistently mapped-style upload allocation and its borrowed constant-buffer descriptor.
/// </summary>
internal sealed class SilkD3D12ConstantBuffer : IDisposable {
    /// <summary>
    ///     Initializes one zeroed constant buffer and creates its view.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="descriptor">The borrowed destination CBV descriptor.</param>
    /// <param name="dataSizeInBytes">The logical constant-buffer byte size.</param>
    internal SilkD3D12ConstantBuffer(
        SilkD3D12Device device,
        SilkD3D12Descriptor descriptor,
        int dataSizeInBytes
    ) {
        device.GuardNotNull();
        descriptor.GuardNotNull();
        dataSizeInBytes.Guard().Range(1, 64 * 1024);
        ObjectDisposedException.ThrowIf(descriptor.IsDisposed, descriptor);
        if (descriptor.Type != DescriptorHeapType.CbvSrvUav)
            throw new ArgumentException("A CBV/SRV/UAV descriptor is required.", nameof(descriptor));

        DataSizeInBytes = dataSizeInBytes;
        var allocationSize = D3D12ResourceLayout.AlignConstantBufferSize(checked((ulong) dataSizeInBytes));
        Resource = device.CreateBuffer(allocationSize, HeapType.Upload);
        try {
            Resource.Write(new byte[dataSizeInBytes]);
            device.CreateConstantBufferView(Resource, descriptor, checked((uint) allocationSize));
        } catch {
            Resource.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets the logical byte size exposed to structure writes.
    /// </summary>
    internal int DataSizeInBytes { get; }

    /// <summary>
    ///     Gets the owned upload resource.
    /// </summary>
    internal SilkD3D12Resource Resource { get; }

    /// <summary>
    ///     Gets whether the upload allocation has been disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Writes one unmanaged structure into the logical constant-buffer data.
    /// </summary>
    /// <typeparam name="T">The unmanaged structure type.</typeparam>
    /// <param name="value">The value to write.</param>
    /// <param name="destinationOffset">The logical destination byte offset.</param>
    internal void Write<T>(in T value, int destinationOffset = 0) where T : unmanaged {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var copy = value;
        var bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref copy, 1));
        if (destinationOffset < 0 || destinationOffset > DataSizeInBytes - bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(destinationOffset));
        Resource.Write(bytes, checked((ulong) destinationOffset));
    }

    /// <summary>
    ///     Writes a contiguous unmanaged array into the logical constant-buffer data.
    /// </summary>
    /// <typeparam name="T">The unmanaged element type.</typeparam>
    /// <param name="values">The values to write.</param>
    /// <param name="destinationOffset">The logical destination byte offset.</param>
    internal void Write<T>(ReadOnlySpan<T> values, int destinationOffset = 0) where T : unmanaged {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var bytes = MemoryMarshal.AsBytes(values);
        if (destinationOffset < 0 || destinationOffset > DataSizeInBytes - bytes.Length)
            throw new ArgumentOutOfRangeException(nameof(destinationOffset));
        Resource.Write(bytes, checked((ulong) destinationOffset));
    }

    /// <summary>
    ///     Releases the upload allocation while leaving the table-owned descriptor intact.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        Resource.Dispose();
        IsDisposed = true;
    }
}

/// <summary>
///     Owns the root tables and b0/b1 buffers required by one in-flight default-mesh draw.
/// </summary>
internal sealed class SilkD3D12MeshBindings : IDisposable {
    /// <summary>
    ///     The Direct3D 12 device used to populate borrowed descriptors.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The complete descriptor tables.
    /// </summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>
    ///     The b0 global-transform constant buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer globalTransforms;

    /// <summary>
    ///     The b1 mesh/material constant buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer model;

    /// <summary>
    ///     The b3 shared light constant buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer lights;

    /// <summary>
    ///     The b6 clipping or custom-pass/post-effect constants.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer effect;

    /// <summary>
    ///     The b5 shared shadow-map constant buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer shadow;

    /// <summary>
    ///     A shared zero buffer used for unused b2-b9 registers.
    /// </summary>
    private readonly SilkD3D12Resource fallbackConstants;

    /// <summary>
    ///     The optional GPU bone-skinning and morph-target resource set.
    /// </summary>
    private SilkD3D12BoneSkinResources? boneSkinning;

    /// <summary>
    ///     The source color lists represented by the synthetic stripe textures.
    /// </summary>
    private readonly IList<Color4>?[] stripeSources = new IList<Color4>?[2];

    /// <summary>
    ///     The synthetic 1D textures used by color-stripe materials.
    /// </summary>
    private readonly TextureModel?[] stripeTextures = new TextureModel?[2];

    /// <summary>
    ///     The resource manager currently owning the synthetic stripe textures.
    /// </summary>
    private SilkD3D12ResourceManager? stripeResources;

    /// <summary>
    ///     Initializes one default-mesh binding set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="resourceHeap">The shader-visible CBV/SRV/UAV heap.</param>
    /// <param name="samplerHeap">The shader-visible sampler heap.</param>
    internal SilkD3D12MeshBindings(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        device.GuardNotNull();
        this.device = device;
        bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        SilkD3D12ConstantBuffer? createdGlobalTransforms = null;
        SilkD3D12ConstantBuffer? createdModel = null;
        SilkD3D12ConstantBuffer? createdLights = null;
        SilkD3D12ConstantBuffer? createdEffect = null;
        SilkD3D12ConstantBuffer? createdShadow = null;
        SilkD3D12Resource? createdFallbackConstants = null;
        try {
            createdGlobalTransforms = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(0),
                GlobalTransformStruct.SizeInBytes);
            createdModel = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(1),
                PhongPbrMaterialStruct.SizeInBytes);
            createdLights = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(3),
                LightsBufferModel.SizeInBytes);
            createdEffect = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(6),
                ClipPlaneStruct.SizeInBytes);
            createdShadow = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(5),
                ShadowMapParamStruct.SizeInBytes);
            createdFallbackConstants = device.CreateBuffer(256, HeapType.Upload);
            createdFallbackConstants.Write(new byte[256]);
            bindings.InitializeFallbackDescriptors(device, createdFallbackConstants, 0, 1, 3, 5, 6);

            globalTransforms = createdGlobalTransforms;
            model = createdModel;
            lights = createdLights;
            effect = createdEffect;
            shadow = createdShadow;
            fallbackConstants = createdFallbackConstants;
        } catch {
            createdFallbackConstants?.Dispose();
            createdEffect?.Dispose();
            createdShadow?.Dispose();
            createdLights?.Dispose();
            createdModel?.Dispose();
            createdGlobalTransforms?.Dispose();
            bindings.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets whether all buffers and descriptors have been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Uploads the b0 and b1 structures for the next draw.
    /// </summary>
    /// <param name="transforms">The global camera and viewport transforms.</param>
    /// <param name="modelData">The per-model structure.</param>
    internal void Update(in GlobalTransformStruct transforms, in ModelStruct modelData) {
        var materialData = D3D12MeshMaterialData.Create(in modelData, null);
        Update(in transforms, in materialData);
    }

    /// <summary>
    ///     Uploads complete camera, model, material, texture, and sampler bindings for the next draw.
    /// </summary>
    /// <param name="context">The command context receiving first-use texture uploads.</param>
    /// <param name="resources">The render-host resource manager.</param>
    /// <param name="transforms">The global camera and viewport transforms.</param>
    /// <param name="modelData">The existing per-model structure.</param>
    /// <param name="material">The existing material core, or <see langword="null" /> for a pass-only draw.</param>
    /// <param name="environmentMap">The optional shared environment cube map.</param>
    internal void Update(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        in GlobalTransformStruct transforms,
        in ModelStruct modelData,
        MaterialCore? material,
        TextureModel? environmentMap = null
    ) {
        context.GuardNotNull();
        resources.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var materialData = D3D12MeshMaterialData.Create(in modelData, material);
        BindMaterialResources(context, resources, material, environmentMap);
        Update(in transforms, in materialData);
    }

    /// <summary>
    ///     Uploads the existing shared-light model into b3.
    /// </summary>
    /// <param name="lightData">The existing light-buffer model.</param>
    internal void UpdateLights(LightsBufferModel lightData) {
        lightData.GuardNotNull();
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        lights.Write<LightStruct>(lightData.Lights);
        var offset = LightStruct.SizeInBytes * Constants.MaxLights;
        var ambient = lightData.AmbientLight;
        lights.Write(in ambient, offset);
        offset += 16;
        var lightCount = lightData.LightCount;
        lights.Write(in lightCount, offset);
        offset += 4;
        var hasEnvironmentMap = lightData.HasEnvironmentMap ? 1 : 0;
        lights.Write(in hasEnvironmentMap, offset);
        offset += 4;
        var environmentMapMipLevels = lightData.EnvironmentMapMipLevels;
        lights.Write(in environmentMapMipLevels, offset);
    }

    /// <summary>
    ///     Uploads b5 and binds the optional typeless shadow map through typed t30/s5 views.
    /// </summary>
    /// <param name="parameters">The current shadow-map parameters.</param>
    /// <param name="shadowMap">The sampled shadow-map resource, or <see langword="null" />.</param>
    internal void UpdateShadow(in ShadowMapParamStruct parameters, SilkD3D12Resource? shadowMap) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        shadow.Write(in parameters);
        if (shadowMap is null) {
            device.CreateNullShaderResourceView(bindings.ShaderResource(30));
        } else {
            device.CreateShaderResourceView(shadowMap,
                bindings.ShaderResource(30),
                Format.FormatR32Float);
        }
        device.CreateSampler(bindings.Sampler(5), DefaultSamplers.ShadowSampler);
    }

    /// <summary>
    ///     Gets the b3 resource for focused byte-layout verification.
    /// </summary>
    internal SilkD3D12Resource LightResource => lights.Resource;

    /// <summary>
    ///     Gets the b5 resource for focused byte-layout verification.
    /// </summary>
    internal SilkD3D12Resource ShadowResource => shadow.Resource;

    /// <summary>
    ///     Gets the b6 resource for focused byte-layout verification.
    /// </summary>
    internal SilkD3D12Resource EffectResource => effect.Resource;

    /// <summary>
    ///     Uploads the existing cross-section parameters into b6.
    /// </summary>
    /// <param name="core">The cross-section render core.</param>
    internal void UpdateClipping(CrossSectionMeshRenderCore core) {
        core.GuardNotNull();
        var planeEnabled = core.PlaneEnabled;
        var plane5To8Enabled = core.Plane5To8Enabled;
        var sectionColor = core.SectionColor;
        effect.Write(in planeEnabled);
        effect.Write(in plane5To8Enabled, 16);
        effect.Write(in sectionColor, 32);
        var operation = (int)core.CuttingOperation;
        effect.Write(in operation, 48);
        ReadOnlySpan<Vector4> planes = [
            core.Plane1Params,
            core.Plane2Params,
            core.Plane3Params,
            core.Plane4Params,
            core.Plane5Params,
            core.Plane6Params,
            core.Plane7Params,
            core.Plane8Params
        ];
        effect.Write(planes, 64);
    }

    /// <summary>
    ///     Uploads b6 for an existing custom geometry pass.
    /// </summary>
    /// <param name="value">The post-effect constants.</param>
    internal void UpdateEffect(in BorderEffectStruct value) => effect.Write(in value);

    /// <summary>
    ///     Creates or updates the per-core GPU skinning table and stream-output allocation.
    /// </summary>
    /// <param name="vertexBoneIds">The vertex bone identifiers and weights.</param>
    /// <param name="boneMatrices">The current bone matrices.</param>
    /// <param name="morphTargets">The current morph-target payload.</param>
    /// <param name="vertexCount">The number of vertices written by the precompute pass.</param>
    /// <returns>The current GPU skinning resources.</returns>
    internal SilkD3D12BoneSkinResources UpdateBoneSkinning(
        ReadOnlySpan<BoneIds> vertexBoneIds,
        ReadOnlySpan<Matrix> boneMatrices,
        MorphTargetUploaderCore morphTargets,
        int vertexCount
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        morphTargets.GuardNotNull();
        boneSkinning ??= new SilkD3D12BoneSkinResources(device, bindings);
        boneSkinning.Update(vertexBoneIds, boneMatrices, morphTargets, vertexCount);
        return boneSkinning;
    }

    /// <summary>
    ///     Gets the initialized GPU skinning resources, if this core has recorded a precompute pass.
    /// </summary>
    internal SilkD3D12BoneSkinResources? BoneSkinning => boneSkinning;

    /// <summary>
    ///     Uploads the complete b0 and b1 structures.
    /// </summary>
    /// <param name="transforms">The global camera and viewport transforms.</param>
    /// <param name="materialData">The complete mesh/model/material structure.</param>
    private void Update(in GlobalTransformStruct transforms, in PhongPbrMaterialStruct materialData) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        globalTransforms.Write(in transforms);
        model.Write(in materialData);
    }

    /// <summary>
    ///     Populates the material texture and sampler registers used by diffuse, Phong, and PBR shaders.
    /// </summary>
    /// <param name="context">The command context receiving texture uploads.</param>
    /// <param name="resources">The shared resource manager.</param>
    /// <param name="material">The existing material core.</param>
    /// <param name="environmentMap">The optional shared environment cube map.</param>
    private void BindMaterialResources(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        MaterialCore? material,
        TextureModel? environmentMap
    ) {
        switch (material) {
            case DiffuseMaterialCore diffuse:
                BindTexture(context, resources, diffuse.DiffuseMap, 0);
                device.CreateSampler(bindings.Sampler(0), diffuse.DiffuseMapSampler);
                break;
            case PhongMaterialCore phong:
                BindTexture(context, resources, phong.DiffuseMap, 0);
                BindTexture(context, resources, phong.NormalMap, 1);
                BindTexture(context, resources, phong.DiffuseAlphaMap, 2);
                BindTexture(context, resources, phong.SpecularColorMap, 3);
                BindTexture(context, resources, phong.DisplacementMap, 4);
                BindTexture(context, resources, phong.EmissiveMap, 5);
                device.CreateSampler(bindings.Sampler(0), phong.DiffuseMapSampler);
                device.CreateSampler(bindings.Sampler(3), phong.DisplacementMapSampler);
                break;
            case PbrMaterialCore pbr:
                BindTexture(context, resources, pbr.AlbedoMap, 0);
                BindTexture(context, resources, pbr.NormalMap, 1);
                BindTexture(context, resources, pbr.RoughnessMetallicMap, 2);
                BindTexture(context, resources, pbr.AmbientOcculsionMap, 3);
                BindTexture(context, resources, pbr.DisplacementMap, 4);
                BindTexture(context, resources, pbr.EmissiveMap, 5);
                BindTexture(context, resources, pbr.IrradianceMap, 21);
                device.CreateSampler(bindings.Sampler(0), pbr.SurfaceMapSampler);
                device.CreateSampler(bindings.Sampler(1), pbr.IblSampler);
                device.CreateSampler(bindings.Sampler(3), pbr.DisplacementMapSampler);
                break;
            case ColorStripeMaterialCore stripe:
                BindColorStripe(context, resources, stripe.ColorStripeX, 0, 12);
                BindColorStripe(context, resources, stripe.ColorStripeY, 1, 13);
                device.CreateSampler(bindings.Sampler(0), stripe.ColorStripeSampler);
                break;
        }

        if (environmentMap is null) return;
        BindTexture(context, resources, environmentMap, 20);
        device.CreateSampler(bindings.Sampler(4), DefaultSamplers.EnvironmentSampler);
    }

    /// <summary>
    ///     Loads one optional texture and writes an SRV directly into its per-draw register.
    /// </summary>
    /// <param name="context">The command context receiving a first-use upload.</param>
    /// <param name="resources">The shared resource manager.</param>
    /// <param name="textureModel">The optional existing texture model.</param>
    /// <param name="shaderRegister">The destination t-register.</param>
    private void BindTexture(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        TextureModel? textureModel,
        int shaderRegister
    ) {
        if (textureModel is null) return;
        var texture = resources.GetOrCreate(context, textureModel);
        device.CreateShaderResourceView(texture.Resource,
            bindings.ShaderResource(shaderRegister),
            texture.IsCubeMap);
    }

    /// <summary>
    ///     Creates or reuses one synthetic 1D color-stripe texture.
    /// </summary>
    /// <param name="context">The command context receiving a first-use upload.</param>
    /// <param name="resources">The shared resource manager.</param>
    /// <param name="colors">The source stripe colors.</param>
    /// <param name="index">The stripe cache index.</param>
    /// <param name="shaderRegister">The destination t-register.</param>
    private void BindColorStripe(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        IList<Color4>? colors,
        int index,
        int shaderRegister
    ) {
        stripeResources = resources;
        if (!ReferenceEquals(stripeSources[index], colors)) {
            ReleaseColorStripe(index);
            stripeSources[index] = colors;
            if (colors is { Count: > 0 })
                stripeTextures[index] = new TextureModel([.. colors], colors.Count);
        }

        if (stripeTextures[index] is not { } textureModel) {
            device.CreateNullShaderResourceView(bindings.ShaderResource(shaderRegister));
            return;
        }

        var texture = resources.GetOrCreate(context, textureModel);
        device.CreateTexture1DShaderResourceView(texture.Resource, bindings.ShaderResource(shaderRegister));
    }

    /// <summary>
    ///     Removes one synthetic color-stripe texture from the shared resource manager.
    /// </summary>
    /// <param name="index">The stripe cache index.</param>
    private void ReleaseColorStripe(int index) {
        if (stripeTextures[index] is { } texture && stripeResources is { IsDisposed: false } manager)
            manager.Remove(texture.Guid);
        stripeSources[index] = null;
        stripeTextures[index] = null;
    }

    /// <summary>
    ///     Gets the first descriptor in the resource table.
    /// </summary>
    internal SilkD3D12Descriptor ResourceTableStart => bindings.ResourceTableStart;

    /// <summary>
    ///     Gets the first descriptor in the sampler table.
    /// </summary>
    internal SilkD3D12Descriptor SamplerTableStart => bindings.SamplerTableStart;

    /// <summary>
    ///     Writes one pass-local texture into this mesh draw's shader-resource table.
    /// </summary>
    /// <param name="resource">The texture resource.</param>
    /// <param name="shaderRegister">The destination t-register.</param>
    /// <param name="format">The optional typed view format.</param>
    internal void BindPassTexture(
        SilkD3D12Resource resource,
        int shaderRegister,
        Format? format = null
    ) => device.CreateShaderResourceView(resource,
        bindings.ShaderResource(shaderRegister),
        format ?? resource.Description.Format);

    /// <summary>
    ///     Releases the constant buffers before returning the descriptor ranges.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        ReleaseColorStripe(0);
        ReleaseColorStripe(1);
        boneSkinning?.Dispose();
        shadow.Dispose();
        effect.Dispose();
        model.Dispose();
        globalTransforms.Dispose();
        lights.Dispose();
        fallbackConstants.Dispose();
        bindings.Dispose();
        IsDisposed = true;
    }
}

/// <summary>
///     Owns the shared root tables plus b0/b4 used by one existing volume render core.
/// </summary>
internal sealed class SilkD3D12VolumeBindings : IDisposable {
    /// <summary>
    ///     The device used to write volume descriptors.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The complete shared-root-signature descriptor tables.
    /// </summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>
    ///     The b0 global-transform buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer globalTransforms;

    /// <summary>
    ///     The b4 volume-model buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer volume;

    /// <summary>
    ///     The zero buffer backing unused constant-buffer registers.
    /// </summary>
    private readonly SilkD3D12Resource fallbackConstants;

    /// <summary>
    ///     The source identity currently represented by the synthetic volume texture model.
    /// </summary>
    private object? volumeSource;

    /// <summary>
    ///     The currently bound volume texture model.
    /// </summary>
    private TextureModel? volumeModel;

    /// <summary>
    ///     Whether the current volume texture model is owned by this binding set.
    /// </summary>
    private bool ownsVolumeModel;

    /// <summary>
    ///     The source transfer-map array currently represented by the synthetic 1D texture.
    /// </summary>
    private Color4[]? transferSource;

    /// <summary>
    ///     The current synthetic transfer-map texture model.
    /// </summary>
    private TextureModel? transferModel;

    /// <summary>
    ///     The resource manager currently owning synthetic texture allocations.
    /// </summary>
    private SilkD3D12ResourceManager? resources;

    /// <summary>
    ///     Initializes one volume binding set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="resourceHeap">The shader-visible resource heap.</param>
    /// <param name="samplerHeap">The shader-visible sampler heap.</param>
    internal SilkD3D12VolumeBindings(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        device.GuardNotNull();
        this.device = device;
        bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        SilkD3D12ConstantBuffer? createdGlobalTransforms = null;
        SilkD3D12ConstantBuffer? createdVolume = null;
        SilkD3D12Resource? createdFallbackConstants = null;
        try {
            createdGlobalTransforms = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(0),
                GlobalTransformStruct.SizeInBytes);
            createdVolume = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(4),
                D3D12VolumeParams.SizeInBytes);
            createdFallbackConstants = device.CreateBuffer(256, HeapType.Upload);
            createdFallbackConstants.Write(new byte[256]);
            bindings.InitializeFallbackDescriptors(device, createdFallbackConstants, 0, 4);
            globalTransforms = createdGlobalTransforms;
            volume = createdVolume;
            fallbackConstants = createdFallbackConstants;
        } catch {
            createdFallbackConstants?.Dispose();
            createdVolume?.Dispose();
            createdGlobalTransforms?.Dispose();
            bindings.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets whether all buffers, descriptors, and synthetic textures have been released.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Updates b0/b4 plus t0/t2/t12/s9 for one volume draw.
    /// </summary>
    /// <param name="context">The command context receiving first-use uploads.</param>
    /// <param name="resources">The shared resource manager.</param>
    /// <param name="transforms">The current camera transforms.</param>
    /// <param name="modelMatrix">The volume world transform.</param>
    /// <param name="material">The existing volume material.</param>
    /// <param name="backPositions">The completed back-position texture.</param>
    /// <returns>Whether a valid 3D volume texture was bound.</returns>
    internal bool Update(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        in GlobalTransformStruct transforms,
        in Matrix modelMatrix,
        MaterialCore? material,
        SilkD3D12Resource backPositions
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        context.GuardNotNull();
        resources.GuardNotNull();
        backPositions.GuardNotNull();
        this.resources = resources;
        if (!TryGetVolumeModel(material, out var currentModel)) return false;
        var texture = resources.GetOrCreate(context, currentModel);
        if (texture.Resource.Description.Dimension != ResourceDimension.Texture3D)
            throw new NotSupportedException("Volume materials require a three-dimensional texture.");

        var parameters = D3D12VolumeMaterialData.Create(modelMatrix,
            (IVolumeTextureMaterial) material!,
            checked((int) texture.Resource.Description.Width),
            checked((int) texture.Resource.Description.Height),
            texture.Resource.Description.DepthOrArraySize);
        globalTransforms.Write(in transforms);
        volume.Write(in parameters);
        device.CreateTexture3DShaderResourceView(texture.Resource, bindings.ShaderResource(0));
        device.CreateShaderResourceView(backPositions, bindings.ShaderResource(2));
        BindTransferMap(context, resources, material as IVolumeTextureMaterial);
        device.CreateSampler(bindings.Sampler(9), ((IVolumeTextureMaterial) material!).Sampler);
        return true;
    }

    /// <summary>
    ///     Binds both graphics descriptor tables after a volume pass selects its root signature.
    /// </summary>
    /// <param name="context">The open command context.</param>
    internal void Bind(SilkD3D12CommandContext context) => bindings.BindGraphics(context);

    /// <summary>
    ///     Releases synthetic cached textures, constant buffers, and descriptor ranges.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        ReleaseOwnedVolume();
        ReleaseTransfer();
        volume.Dispose();
        globalTransforms.Dispose();
        fallbackConstants.Dispose();
        bindings.Dispose();
        IsDisposed = true;
    }

    /// <summary>
    ///     Resolves an existing volume material into a reusable texture model and b4 payload.
    /// </summary>
    /// <param name="material">The existing material.</param>
    /// <param name="model">The resolved texture model.</param>
    /// <returns>Whether the material contains a supported volume texture.</returns>
    private bool TryGetVolumeModel(
        MaterialCore? material,
        out TextureModel model
    ) {
        object? source;
        TextureModel? candidate;
        var ownsCandidate = false;
        switch (material) {
            case VolumeTextureRawDataMaterialCore raw when raw.VolumeTexture.VolumeTextures is { Length: > 0 } bytes:
                source = bytes;
                candidate = ReferenceEquals(volumeSource, source) ? volumeModel :
                    new TextureModel(bytes,
                        raw.VolumeTexture.Format,
                        raw.VolumeTexture.Width,
                        raw.VolumeTexture.Height,
                        raw.VolumeTexture.Depth);
                ownsCandidate = true;
                break;
            case VolumeTextureDiffuseMaterialCore diffuse
                when diffuse.VolumeTexture.VolumeTextures is { Length: > 0 } gradients:
                source = gradients;
                candidate = ReferenceEquals(volumeSource, source) ? volumeModel :
                    new TextureModel(MemoryMarshal.AsBytes(gradients.AsSpan()).ToArray(),
                        diffuse.VolumeTexture.Format,
                        diffuse.VolumeTexture.Width,
                        diffuse.VolumeTexture.Height,
                        diffuse.VolumeTexture.Depth);
                ownsCandidate = true;
                break;
            case VolumeTextureDds3DMaterialCore { VolumeTexture: { } dds }:
                source = dds;
                candidate = dds;
                break;
            default:
                model = null!;
                return false;
        }

        if (!ReferenceEquals(volumeSource, source) || volumeModel is null) {
            ReleaseOwnedVolume();
            volumeSource = source;
            volumeModel = candidate;
            ownsVolumeModel = ownsCandidate;
        }
        model = volumeModel!;
        return true;
    }

    /// <summary>
    ///     Creates or clears the optional 1D transfer-map binding.
    /// </summary>
    /// <param name="context">The command context receiving first-use upload commands.</param>
    /// <param name="resources">The shared resource manager.</param>
    /// <param name="material">The volume material.</param>
    private void BindTransferMap(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        IVolumeTextureMaterial? material
    ) {
        var transfer = material?.TransferMap;
        if (transfer is null || transfer.Length == 0) {
            ReleaseTransfer();
            device.CreateNullShaderResourceView(bindings.ShaderResource(12));
            return;
        }
        if (!ReferenceEquals(transferSource, transfer)) {
            ReleaseTransfer();
            transferSource = transfer;
            transferModel = new TextureModel(transfer, transfer.Length);
        }
        var texture = resources.GetOrCreate(context, transferModel!);
        device.CreateTexture1DShaderResourceView(texture.Resource, bindings.ShaderResource(12));
    }

    /// <summary>
    ///     Removes the currently owned synthetic volume texture from the shared manager.
    /// </summary>
    private void ReleaseOwnedVolume() {
        if (ownsVolumeModel && volumeModel is not null && resources is { IsDisposed: false } manager)
            manager.Remove(volumeModel.Guid);
        volumeSource = null;
        volumeModel = null;
        ownsVolumeModel = false;
    }

    /// <summary>
    ///     Removes the synthetic transfer-map texture from the shared manager.
    /// </summary>
    private void ReleaseTransfer() {
        if (transferModel is not null && resources is { IsDisposed: false } manager)
            manager.Remove(transferModel.Guid);
        transferSource = null;
        transferModel = null;
    }
}

/// <summary>
///     Converts existing volume materials into the exact 176-byte cbVolumeModel layout.
/// </summary>
internal static class D3D12VolumeMaterialData {
    /// <summary>
    ///     Creates one complete volume parameter payload.
    /// </summary>
    /// <param name="modelMatrix">The volume world transform.</param>
    /// <param name="material">The existing volume material.</param>
    /// <param name="width">The volume width.</param>
    /// <param name="height">The volume height.</param>
    /// <param name="depth">The volume depth.</param>
    /// <returns>The complete b4 payload.</returns>
    internal static D3D12VolumeParams Create(
        in Matrix modelMatrix,
        IVolumeTextureMaterial material,
        int width,
        int height,
        int depth
    ) {
        material.GuardNotNull();
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (depth <= 0) throw new ArgumentOutOfRangeException(nameof(depth));
        var sampleDistance = (float) material.SampleDistance;
        if (!float.IsFinite(sampleDistance) || sampleDistance <= 0)
            throw new ArgumentOutOfRangeException(nameof(material), "Sample distance must be positive and finite.");
        return new D3D12VolumeParams {
            World = modelMatrix,
            WorldInverse = modelMatrix.Inverted(),
            Color = material.Color,
            StepSize = sampleDistance / Math.Max(width, Math.Max(height, depth)),
            IterationOffset = checked((uint) Math.Max(0, material.IterationOffset)),
            EnablePlaneAlignment = material.EnablePlaneAlignment ? 1 : 0,
            MaxIterations = checked((uint) Math.Max(0, material.MaxIterations)),
            HasGradientMap = material.TransferMap is { Length: > 0 } ? 1 : 0,
            IsoValue = (float) material.IsoValue,
            BaseSampleDistance = 1,
            ActualSampleDistance = sampleDistance
        };
    }
}

/// <summary>
///     Matches the repository shader's cbVolumeModel layout at b4.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct D3D12VolumeParams {
    /// <summary>The volume world transform.</summary>
    internal Matrix World;

    /// <summary>The inverse volume world transform.</summary>
    internal Matrix WorldInverse;

    /// <summary>The material tint.</summary>
    internal Color4 Color;

    /// <summary>The normalized ray-march step.</summary>
    internal float StepSize;

    /// <summary>The first ray-march iteration.</summary>
    internal uint IterationOffset;

    /// <summary>Whether sample planes align between frames.</summary>
    internal int EnablePlaneAlignment;

    /// <summary>The maximum ray-march iteration count.</summary>
    internal uint MaxIterations;

    /// <summary>Whether t12 contains a transfer map.</summary>
    internal int HasGradientMap;

    /// <summary>The normalized iso-value threshold.</summary>
    internal float IsoValue;

    /// <summary>The reference sample distance.</summary>
    internal float BaseSampleDistance;

    /// <summary>The requested material sample distance.</summary>
    internal float ActualSampleDistance;

    /// <summary>The byte size of cbVolumeModel.</summary>
    internal const int SizeInBytes = 176;
}

/// <summary>
///     Owns root tables and b0/b4 buffers for one in-flight point, line, or billboard draw.
/// </summary>
internal sealed class SilkD3D12PointLineBindings : IDisposable {
    /// <summary>
    ///     The Direct3D 12 device used to populate material descriptors.
    /// </summary>
    private readonly SilkD3D12Device device;

    /// <summary>
    ///     The complete shared-root-signature descriptor tables.
    /// </summary>
    private readonly SilkD3D12GraphicsBindings bindings;

    /// <summary>
    ///     The b0 global-transform buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer globalTransforms;

    /// <summary>
    ///     The b4 point/line model and material buffer.
    /// </summary>
    private readonly SilkD3D12ConstantBuffer pointLine;

    /// <summary>
    ///     The zero buffer backing unused constant-buffer registers.
    /// </summary>
    private readonly SilkD3D12Resource fallbackConstants;

    /// <summary>
    ///     Initializes one point/line binding set.
    /// </summary>
    /// <param name="device">The Direct3D 12 device.</param>
    /// <param name="resourceHeap">The shader-visible resource heap.</param>
    /// <param name="samplerHeap">The shader-visible sampler heap.</param>
    internal SilkD3D12PointLineBindings(
        SilkD3D12Device device,
        SilkD3D12DescriptorHeap resourceHeap,
        SilkD3D12DescriptorHeap samplerHeap
    ) {
        device.GuardNotNull();
        this.device = device;
        bindings = new SilkD3D12GraphicsBindings(resourceHeap, samplerHeap);
        SilkD3D12ConstantBuffer? createdGlobalTransforms = null;
        SilkD3D12ConstantBuffer? createdPointLine = null;
        SilkD3D12Resource? createdFallbackConstants = null;
        try {
            createdGlobalTransforms = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(0),
                GlobalTransformStruct.SizeInBytes);
            createdPointLine = new SilkD3D12ConstantBuffer(device,
                bindings.ConstantBuffer(4),
                PointLineMaterialStruct.SizeInBytes);
            createdFallbackConstants = device.CreateBuffer(256, HeapType.Upload);
            createdFallbackConstants.Write(new byte[256]);
            bindings.InitializeFallbackDescriptors(device, createdFallbackConstants, 0, 4);
            globalTransforms = createdGlobalTransforms;
            pointLine = createdPointLine;
            fallbackConstants = createdFallbackConstants;
        } catch {
            createdFallbackConstants?.Dispose();
            createdPointLine?.Dispose();
            createdGlobalTransforms?.Dispose();
            bindings.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     Gets whether all point/line bindings have been disposed.
    /// </summary>
    internal bool IsDisposed { get; private set; }

    /// <summary>
    ///     Gets the first descriptor in the resource table.
    /// </summary>
    internal SilkD3D12Descriptor ResourceTableStart => bindings.ResourceTableStart;

    /// <summary>
    ///     Gets the first descriptor in the sampler table.
    /// </summary>
    internal SilkD3D12Descriptor SamplerTableStart => bindings.SamplerTableStart;

    /// <summary>
    ///     Writes one pass-local texture into this point, line, or billboard draw's shader-resource table.
    /// </summary>
    /// <param name="resource">The texture resource.</param>
    /// <param name="shaderRegister">The destination t-register.</param>
    /// <param name="format">The optional typed view format.</param>
    internal void BindPassTexture(
        SilkD3D12Resource resource,
        int shaderRegister,
        Format? format = null
    ) => device.CreateShaderResourceView(resource,
        bindings.ShaderResource(shaderRegister),
        format ?? resource.Description.Format);

    /// <summary>
    ///     Uploads transforms and one existing point or line material.
    /// </summary>
    /// <param name="context">The command context receiving an optional texture upload.</param>
    /// <param name="resources">The shared resource manager.</param>
    /// <param name="transforms">The global transforms.</param>
    /// <param name="model">The point/line model fields.</param>
    /// <param name="material">The existing point or line material.</param>
    /// <param name="billboardTexture">The optional texture owned by prepared billboard geometry.</param>
    internal void Update(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        in GlobalTransformStruct transforms,
        in PointLineModelStruct model,
        MaterialCore material,
        TextureModel? billboardTexture = null
    ) {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        var data = D3D12PointLineMaterialData.Create(in model, material);
        var textureModel = material is LineMaterialCore line ? line.Texture : billboardTexture;
        if (textureModel is not null) {
            var texture = resources.GetOrCreate(context, textureModel);
            device.CreateShaderResourceView(texture.Resource, bindings.ShaderResource(0), texture.IsCubeMap);
            var sampler = material switch {
                LineMaterialCore lineMaterial => lineMaterial.SamplerDescription,
                BillboardMaterialCore billboardMaterial => billboardMaterial.SamplerDescription,
                _ => default
            };
            device.CreateSampler(bindings.Sampler(7), sampler);
        }
        globalTransforms.Write(in transforms);
        pointLine.Write(in data);
    }

    /// <summary>
    ///     Releases buffers before returning both descriptor ranges.
    /// </summary>
    public void Dispose() {
        if (IsDisposed) return;
        pointLine.Dispose();
        globalTransforms.Dispose();
        fallbackConstants.Dispose();
        bindings.Dispose();
        IsDisposed = true;
    }
}

/// <summary>
///     Converts existing point, line, and billboard materials into the fixed cbPointLineModel layout.
/// </summary>
internal static class D3D12PointLineMaterialData {
    /// <summary>
    ///     Creates one complete point/line/billboard constant-buffer payload.
    /// </summary>
    /// <param name="model">The existing model fields.</param>
    /// <param name="material">The existing point or line material.</param>
    /// <returns>The complete b4 payload.</returns>
    internal static PointLineMaterialStruct Create(in PointLineModelStruct model, MaterialCore material) =>
        material switch {
            LineMaterialCore line => CreateLine(in model, line),
            PointMaterialCore point => CreatePoint(in model, point),
            BillboardMaterialCore billboard => CreateBillboard(in model, billboard),
            _ => throw new NotSupportedException($"DX12 point/line material '{material.GetType().Name}' is not supported.")
        };

    /// <summary>
    ///     Creates a billboard payload.
    /// </summary>
    /// <param name="model">The existing model fields.</param>
    /// <param name="material">The billboard material.</param>
    /// <returns>The billboard payload.</returns>
    private static PointLineMaterialStruct CreateBillboard(
        in PointLineModelStruct model,
        BillboardMaterialCore material
    ) => new() {
        Model = model,
        Parameters = new Vector4((int) material.Type, 0, 0, 0),
        FixedSizeFlag = Flag(material.FixedSize),
        HasTexture = 1
    };

    /// <summary>
    ///     Creates a line payload.
    /// </summary>
    /// <param name="model">The existing model fields.</param>
    /// <param name="material">The line material.</param>
    /// <returns>The line payload.</returns>
    private static PointLineMaterialStruct CreateLine(in PointLineModelStruct model, LineMaterialCore material) =>
        new() {
            Model = model,
            Parameters = new Vector4(material.Thickness, material.Smoothness, 0, 0),
            Color = material.LineColor,
            FixedSizeFlag = Flag(material.FixedSize),
            EnableDistanceFadingFlag = Flag(material.EnableDistanceFading),
            FadeNearDistanceValue = material.FadingNearDistance,
            FadeFarDistanceValue = material.FadingFarDistance,
            HasTexture = Flag(material.Texture is not null),
            TextureScale = material.TextureScale,
            AlphaThreshold = material.AlphaThreshold
        };

    /// <summary>
    ///     Creates a point payload.
    /// </summary>
    /// <param name="model">The existing model fields.</param>
    /// <param name="material">The point material.</param>
    /// <returns>The point payload.</returns>
    private static PointLineMaterialStruct CreatePoint(in PointLineModelStruct model, PointMaterialCore material) =>
        new() {
            Model = model,
            Parameters = new Vector4(material.Width, material.Height, (int) material.Figure, material.FigureRatio),
            Color = material.PointColor,
            FixedSizeFlag = Flag(material.FixedSize),
            EnableDistanceFadingFlag = Flag(material.EnableDistanceFading),
            FadeNearDistanceValue = material.FadingNearDistance,
            FadeFarDistanceValue = material.FadingFarDistance,
            EnableBlending = Flag(material.EnableColorBlending),
            BlendingFactor = material.BlendingFactor
        };

    /// <summary>
    ///     Converts a Boolean to the HLSL 32-bit representation.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>One for true; otherwise zero.</returns>
    private static int Flag(bool value) => value ? 1 : 0;
}

/// <summary>
///     Converts the existing diffuse, Phong, and PBR material cores into the fixed cbMesh layout.
/// </summary>
internal static class D3D12MeshMaterialData {
    /// <summary>
    ///     Selects the existing opaque material pass without constructing DX11 material variables.
    /// </summary>
    /// <param name="material">The existing material core.</param>
    /// <returns>The matching default pass name.</returns>
    internal static string GetPassName(MaterialCore? material) => material switch {
        ViewCubeMaterialCore => DefaultPassNames.ViewCube,
        DiffuseMaterialCore => DefaultPassNames.Diffuse,
        PhongMaterialCore {EnableTessellation: true} => DefaultPassNames.MeshTriTessellation,
        PhongMaterialCore => DefaultPassNames.Default,
        PbrMaterialCore {EnableTessellation: true} => DefaultPassNames.MeshPbrTriTessellation,
        PbrMaterialCore => DefaultPassNames.Pbr,
        NormalMaterialCore => DefaultPassNames.Normals,
        ColorMaterialCore => DefaultPassNames.Colors,
        PositionMaterialCore => DefaultPassNames.Positions,
        NormalVectorMaterialCore => DefaultPassNames.NormalVector,
        ColorStripeMaterialCore => DefaultPassNames.ColorStripe1D,
        null => throw new InvalidOperationException("A DX12 mesh material is required for pass selection."),
        _ => throw new NotSupportedException($"DX12 mesh material '{material.GetType().Name}' is not supported.")
    };

    /// <summary>
    ///     Creates the complete constant-buffer payload for one mesh draw.
    /// </summary>
    /// <param name="model">The existing per-model fields.</param>
    /// <param name="material">The existing material core, or <see langword="null" /> for pass-only drawing.</param>
    /// <returns>The complete cbMesh payload.</returns>
    internal static PhongPbrMaterialStruct Create(in ModelStruct model, MaterialCore? material) {
        var result = new PhongPbrMaterialStruct {
            Model = model,
            MinTessellationDistance = 1,
            MaxTessellationDistance = 100,
            MinDistanceTessellationFactor = 4,
            MaxDistanceTessellationFactor = 1,
            Diffuse = Color.White,
            UvTransformRow1 = new Vector4(1, 0, 0, 0),
            UvTransformRow2 = new Vector4(0, 1, 0, 0)
        };

        switch (material) {
            case DiffuseMaterialCore diffuse:
                ApplyDiffuse(ref result, diffuse);
                break;
            case PhongMaterialCore phong:
                ApplyPhong(ref result, phong);
                break;
            case PbrMaterialCore pbr:
                ApplyPbr(ref result, pbr);
                break;
            case ColorMaterialCore or NormalMaterialCore or PositionMaterialCore or NormalVectorMaterialCore:
            case null:
                break;
            case ColorStripeMaterialCore stripe:
                result.Diffuse = stripe.DiffuseColor;
                result.HasDiffuseMap = stripe.ColorStripeXEnabled && HasColors(stripe.ColorStripeX)
                    ? 1
                    : 0;
                result.HasAlphaOrRoughnessMetallicMap =
                    stripe.ColorStripeYEnabled && HasColors(stripe.ColorStripeY)
                        ? 1
                        : 0;
                break;
            default:
                throw new NotSupportedException($"DX12 mesh material '{material.GetType().Name}' is not supported.");
        }
        return result;
    }

    /// <summary>
    ///     Applies the diffuse-only material fields.
    /// </summary>
    /// <param name="result">The destination payload.</param>
    /// <param name="material">The source material.</param>
    private static void ApplyDiffuse(ref PhongPbrMaterialStruct result, DiffuseMaterialCore material) {
        result.Diffuse = material.DiffuseColor;
        result.HasDiffuseMap = Flag(material.RenderDiffuseMap && material.DiffuseMap is not null);
        result.HasNormalMap = Flag(material.EnableUnLit);
        result.RenderFlatFlag = Flag(material.EnableFlatShading);
        result.VertexColorBlendingAndPadding.X = material.VertexColorBlendingFactor;
        ApplyUvTransform(ref result, material.UvTransform);
    }

    /// <summary>
    ///     Applies the Blinn-Phong material fields.
    /// </summary>
    /// <param name="result">The destination payload.</param>
    /// <param name="material">The source material.</param>
    private static void ApplyPhong(ref PhongPbrMaterialStruct result, PhongMaterialCore material) {
        result.MinTessellationDistance = material.MinTessellationDistance;
        result.MaxTessellationDistance = material.MaxTessellationDistance;
        result.MinDistanceTessellationFactor = material.MinDistanceTessellationFactor;
        result.MaxDistanceTessellationFactor = material.MaxDistanceTessellationFactor;
        result.Diffuse = material.DiffuseColor;
        result.Ambient = material.AmbientColor;
        result.Emissive = material.EmissiveColor;
        result.SpecularOrPbrFactors = material.SpecularColor;
        result.ReflectOrClearCoat = material.ReflectiveColor;
        result.HasDiffuseMap = Flag(material.RenderDiffuseMap && material.DiffuseMap is not null);
        result.HasNormalMap = Flag(material.RenderNormalMap && material.NormalMap is not null);
        result.HasEmissiveMap = Flag(material.RenderEmissiveMap && material.EmissiveMap is not null);
        result.HasAlphaOrRoughnessMetallicMap = Flag(material.RenderDiffuseAlphaMap &&
                                                      material.DiffuseAlphaMap is not null);
        result.HasSpecularOrIrradianceMap = Flag(material.RenderSpecularColorMap &&
                                                 material.SpecularColorMap is not null);
        result.EnableAutoTangentFlag = Flag(material.EnableAutoTangent);
        result.HasDisplacementMap = Flag(material.RenderDisplacementMap && material.DisplacementMap is not null);
        result.RenderFlatFlag = Flag(material.EnableFlatShading);
        result.Shininess = material.SpecularShininess;
        result.DisplacementMapScaleMask = material.DisplacementMapScaleMask;
        result.VertexColorBlendingAndPadding.X = material.VertexColorBlendingFactor;
        ApplyUvTransform(ref result, material.UvTransform);
    }

    /// <summary>
    ///     Applies the physically based material fields.
    /// </summary>
    /// <param name="result">The destination payload.</param>
    /// <param name="material">The source material.</param>
    private static void ApplyPbr(ref PhongPbrMaterialStruct result, PbrMaterialCore material) {
        result.MinTessellationDistance = material.MinTessellationDistance;
        result.MaxTessellationDistance = material.MaxTessellationDistance;
        result.MinDistanceTessellationFactor = material.MinDistanceTessellationFactor;
        result.MaxDistanceTessellationFactor = material.MaxDistanceTessellationFactor;
        result.Diffuse = material.AlbedoColor;
        result.Emissive = material.EmissiveColor;
        result.SpecularOrPbrFactors = new Vector4(material.AmbientOcclusionFactor,
            material.RoughnessFactor,
            material.MetallicFactor,
            material.ReflectanceFactor);
        result.ReflectOrClearCoat = new Vector4(material.ClearCoatStrength,
            material.ClearCoatRoughness,
            0,
            Flag(material.RenderAmbientOcclusionMap && material.AmbientOcculsionMap is not null));
        result.HasDiffuseMap = Flag(material.RenderAlbedoMap && material.AlbedoMap is not null);
        result.HasNormalMap = Flag(material.RenderNormalMap && material.NormalMap is not null);
        result.HasEmissiveMap = Flag(material.RenderEmissiveMap && material.EmissiveMap is not null);
        result.HasAlphaOrRoughnessMetallicMap = Flag(material.RenderRoughnessMetallicMap &&
                                                      material.RoughnessMetallicMap is not null);
        result.HasSpecularOrIrradianceMap = Flag(material.RenderIrradianceMap && material.IrradianceMap is not null);
        result.EnableAutoTangentFlag = Flag(material.EnableAutoTangent);
        result.HasDisplacementMap = Flag(material.RenderDisplacementMap && material.DisplacementMap is not null);
        result.RenderPbrFlag = 1;
        result.RenderFlatFlag = Flag(material.EnableFlatShading);
        result.DisplacementMapScaleMask = material.DisplacementMapScaleMask;
        result.VertexColorBlendingAndPadding.X = material.VertexColorBlendingFactor;
        ApplyUvTransform(ref result, material.UvTransform);
    }

    /// <summary>
    ///     Converts the existing compact UV transform into the two HLSL rows.
    /// </summary>
    /// <param name="result">The destination payload.</param>
    /// <param name="transform">The source transform.</param>
    private static void ApplyUvTransform(ref PhongPbrMaterialStruct result, UvTransform transform) {
        Matrix matrix = transform;
        result.UvTransformRow1 = matrix.Column1;
        result.UvTransformRow2 = matrix.Column2;
    }

    /// <summary>
    ///     Tests the runtime-compatible optional color-list contract.
    /// </summary>
    /// <param name="colors">The optional color list.</param>
    /// <returns>Whether the list contains colors.</returns>
    private static bool HasColors(IList<Color4>? colors) => colors is { Count: > 0 };

    /// <summary>
    ///     Converts a managed Boolean to the 32-bit HLSL Boolean representation.
    /// </summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>One for true; otherwise zero.</returns>
    private static int Flag(bool value) => value ? 1 : 0;
}
