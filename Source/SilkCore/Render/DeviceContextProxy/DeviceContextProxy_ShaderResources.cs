using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Render;

public partial class DeviceContextProxy {
    public const int ConstantBufferCount = 15;
    public const int SamplerStateCount = 16;
    public const int ShaderResourceViewCount = 128;
    public const int SimultaneousRenderTargetCount = 8;
    public const int StageCount = Constants.NumShaderStages;
    public const int UnorderedAcccesViewCount = 8;

    private readonly object[] constantBufferCheck = new object[ConstantBufferCount * StageCount];
    private readonly object[] samplerStateCheck = new object[SamplerStateCount * StageCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShader(VertexShader shader, bool bindConstantBuffer = true) =>
        SetShader(Constants.VertexIdx, shader, bindConstantBuffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShader(HullShader shader, bool bindConstantBuffer = true) =>
        SetShader(Constants.HullIdx, shader, bindConstantBuffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShader(DomainShader shader, bool bindConstantBuffer = true) =>
        SetShader(Constants.DomainIdx, shader, bindConstantBuffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShader(GeometryShader shader, bool bindConstantBuffer = true) =>
        SetShader(Constants.GeometryIdx, shader, bindConstantBuffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShader(PixelShader shader, bool bindConstantBuffer = true) =>
        SetShader(Constants.PixelIdx, shader, bindConstantBuffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShader(ComputeShader shader, bool bindConstantBuffer = true) =>
        SetShader(Constants.ComputeIdx, shader, bindConstantBuffer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResource(VertexShaderType shaderType, int slot, ShaderResourceView texture) =>
        SetShaderResource(Constants.VertexIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResources(VertexShaderType shaderType, int slot, ShaderResourceView[] texture) =>
        SetShaderResources(Constants.VertexIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceView[] GetShaderResources(VertexShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResource(DomainShaderType shaderType, int slot, ShaderResourceView texture) =>
        SetShaderResource(Constants.DomainIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResources(DomainShaderType shaderType, int slot, ShaderResourceView[] texture) =>
        SetShaderResources(Constants.DomainIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceView[] GetShaderResources(DomainShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResource(HullShaderType shaderType, int slot, ShaderResourceView texture) =>
        SetShaderResource(Constants.HullIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResources(HullShaderType shaderType, int slot, ShaderResourceView[] texture) =>
        SetShaderResources(Constants.HullIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceView[] GetShaderResources(HullShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResource(GeometryShaderType shaderType, int slot, ShaderResourceView texture) =>
        SetShaderResource(Constants.GeometryIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResources(GeometryShaderType shaderType, int slot, ShaderResourceView[] texture) =>
        SetShaderResources(Constants.GeometryIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceView[] GetShaderResources(GeometryShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResource(PixelShaderType shaderType, int slot, ShaderResourceView? texture) =>
        SetShaderResource(Constants.PixelIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResources(PixelShaderType shaderType, int slot, ShaderResourceView[] texture) =>
        SetShaderResources(Constants.PixelIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceView[] GetShaderResources(PixelShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResource(ComputeShaderType shaderType, int slot, ShaderResourceView texture) =>
        SetShaderResource(Constants.ComputeIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResources(ComputeShaderType shaderType, int slot, ShaderResourceView[] texture) =>
        SetShaderResources(Constants.ComputeIdx, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceView[] GetShaderResources(ComputeShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnorderedAccessView(ComputeShaderType shaderType, int slot, UnorderedAccessView uav) =>
        NativeContext.SetUnorderedAccessView(slot, uav);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnorderedAccessView(
        ComputeShaderType shaderType,
        int slot,
        UnorderedAccessView uav,
        int uavInitialCount
    ) {
        NativeContext.SetUnorderedAccessView(slot, uav, uavInitialCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnorderedAccessViews(ComputeShaderType shaderType, int slot, UnorderedAccessView[] uavs) {
        NativeContext.SetUnorderedAccessViews(slot, uavs);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnorderedAccessViews(
        ComputeShaderType shaderType,
        int slot,
        UnorderedAccessView[] uavs,
        int[] uavInitialCounts
    ) {
        NativeContext.SetUnorderedAccessViews(slot, uavs, uavInitialCounts);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnorderedAccessView[] GetUnorderedAccessView(ComputeShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(VertexShaderType shaderType, int slot, SamplerStateProxy sampler) =>
        TrackSampler(Constants.VertexIdx, slot, sampler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSamplers(VertexShaderType shaderType, int slot, SamplerStateProxy[] samplers) =>
        TrackSamplers(Constants.VertexIdx, slot, samplers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSampler(VertexShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(DomainShaderType shaderType, int slot, SamplerStateProxy sampler) =>
        TrackSampler(Constants.DomainIdx, slot, sampler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSamplers(DomainShaderType shaderType, int slot, SamplerStateProxy[] samplers) =>
        TrackSamplers(Constants.DomainIdx, slot, samplers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSampler(DomainShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(HullShaderType shaderType, int slot, SamplerStateProxy sampler) =>
        TrackSampler(Constants.HullIdx, slot, sampler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSamplers(HullShaderType shaderType, int slot, SamplerStateProxy[] samplers) =>
        TrackSamplers(Constants.HullIdx, slot, samplers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSampler(HullShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(GeometryShaderType shaderType, int slot, SamplerStateProxy sampler) =>
        TrackSampler(Constants.GeometryIdx, slot, sampler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSamplers(GeometryShaderType shaderType, int slot, SamplerStateProxy[] samplers) =>
        TrackSamplers(Constants.GeometryIdx, slot, samplers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSampler(GeometryShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(PixelShaderType shaderType, int slot, SamplerStateProxy sampler) =>
        TrackSampler(Constants.PixelIdx, slot, sampler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSamplers(PixelShaderType shaderType, int slot, SamplerStateProxy[] samplers) =>
        TrackSamplers(Constants.PixelIdx, slot, samplers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSampler(PixelShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler(ComputeShaderType shaderType, int slot, SamplerStateProxy sampler) =>
        TrackSampler(Constants.ComputeIdx, slot, sampler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSamplers(ComputeShaderType shaderType, int slot, SamplerStateProxy[] samplers) =>
        TrackSamplers(Constants.ComputeIdx, slot, samplers);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSampler(ComputeShaderType shaderType, int startSlot, int num) => [];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderPass(ShaderPass pass, bool bindConstantBuffer = true) {
        if (pass == null || CurrShaderPass == pass || pass.IsNull) return;

        SetShader(pass.VertexShader, bindConstantBuffer);
        SetShader(pass.PixelShader, bindConstantBuffer);
        SetShader(pass.ComputeShader, bindConstantBuffer);
        SetShader(pass.HullShader, bindConstantBuffer);
        SetShader(pass.DomainShader, bindConstantBuffer);
        SetShader(pass.GeometryShader, bindConstantBuffer);
        CurrShaderPass = pass;
    }

    private void SetShaderResource(int shaderStage, int slot, ShaderResourceView? texture) =>
        NativeContext.SetShaderResource(shaderStage, slot, texture);

    private void SetShaderResources(int shaderStage, int slot, ShaderResourceView[] textures) =>
        NativeContext.SetShaderResources(shaderStage, slot, textures);

    private void SetShader(int shaderStage, ShaderBase shader, bool bindConstantBuffer) {
        NativeContext.SetShader(shaderStage, shader == null || shader.IsNull ? null : shader.NativeShader);
        if (!bindConstantBuffer || shader == null || shader.IsNull) return;

        foreach (var mapping in shader.ConstantBufferMapping.Mappings)
            TrackConstantBuffer(shaderStage, mapping.Key, mapping.Value);
    }

    private void TrackConstantBuffer(int shaderStage, int slot, ConstantBufferProxy buffer) {
        if (slot < 0) return;

        var index = shaderStage * ConstantBufferCount + slot;
        if (index >= constantBufferCheck.Length) return;

        if (AutoSkipRedundantStateSetting && constantBufferCheck[index] == buffer) return;

        NativeContext.SetConstantBuffer(shaderStage, slot, buffer);
        constantBufferCheck[index] = buffer;
    }

    private void TrackSampler(int shaderStage, int slot, SamplerStateProxy sampler) {
        if (slot < 0) return;

        var index = shaderStage * SamplerStateCount + slot;
        if (index >= samplerStateCheck.Length) return;

        if (AutoSkipRedundantStateSetting && samplerStateCheck[index] == sampler) return;

        NativeContext.SetSampler(shaderStage, slot, sampler?.State);
        samplerStateCheck[index] = sampler;
    }

    private void TrackSamplers(int shaderStage, int slot, SamplerStateProxy[] samplers) {
        if (slot < 0 || samplers == null) return;

        var start = shaderStage * SamplerStateCount + slot;
        var count = Math.Min(samplers.Length, samplerStateCheck.Length - start);
        if (count <= 0) return;

        if (AutoSkipRedundantStateSetting) {
            var allSame = true;
            for (var i = 0; i < count; i++)
                if (samplerStateCheck[start + i] != samplers[i]) {
                    allSame = false;
                    break;
                }

            if (allSame) return;
        }

        var nativeSamplers = new SamplerState[count];
        for (var i = 0; i < count; i++) {
            nativeSamplers[i] = samplers[i]?.State;
            samplerStateCheck[start + i] = samplers[i];
        }

        NativeContext.SetSamplers(shaderStage, slot, nativeSamplers);
    }
}
