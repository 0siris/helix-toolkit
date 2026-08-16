using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;

public partial class DeviceContextProxy {
    public const int ConstantBufferCount = 15;
    public const int SamplerStateCount = 16;
    public const int ShaderResourceViewCount = 128;
    public const int SimultaneousRenderTargetCount = 8;
    public const int StageCount = Constants.NumShaderStages;
    public const int UnorderedAcccesViewCount = 8;

    private readonly object?[] constantBufferCheck = new object?[ConstantBufferCount * StageCount];
    private readonly object?[] samplerStateCheck = new object?[SamplerStateCount * StageCount];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShader<TShader>(
        TShader shader,
        bool bindConstantBuffer = true
    )
        where TShader : ShaderBase, IShaderType
        => SetShader(TShader.Index, shader, bindConstantBuffer);
    
    #region Shader Resources

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResource<TShader>(
        int slot,
        ShaderResourceView? texture
    )
        where TShader : struct, IShaderResourceType
        => SetShaderResource(TShader.Index, slot, texture);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderResources<TShader>(
        int slot,
        ShaderResourceView?[] texture
    )
        where TShader : struct, IShaderResourceType
        => SetShaderResources(TShader.Index, slot, texture);

    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ShaderResourceView[] GetShaderResources<TShader>(
        int startSlot,
        int num
    ) where TShader : struct, IShaderResourceType
        => []; //TODO implement

    #endregion

    #region Unordered Access Views

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    public void SetUnorderedAccessView<TShader>(
        int slot,
        UnorderedAccessView? uav
    )
        where TShader : struct, IUnorderedAccessShaderType
        => NativeContext.SetUnorderedAccessView(slot, uav);

    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnorderedAccessView<TShader>(
        int slot,
        UnorderedAccessView[] uavs
    )
        where TShader : struct, IUnorderedAccessShaderType
        => NativeContext.SetUnorderedAccessViews(slot, uavs);
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetUnorderedAccessView<TShader>(
        int slot,
        UnorderedAccessView uav,
        int uavInitialCount
    ) where TShader : struct, IUnorderedAccessShaderType
        => NativeContext.SetUnorderedAccessView(slot, uav, uavInitialCount);

    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public UnorderedAccessView[] GetUnorderedAccessView<TShader>(int startSlot, int num)
        where TShader : struct, IUnorderedAccessShaderType
        => [];//TODO implement
    
    #endregion

    #region Sampler

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSampler<TShader>(
        int slot,
        SamplerStateProxy? sampler)
        where TShader : struct, ISamplerShaderType
        => TrackSampler(TShader.Index, slot, sampler);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetSamplers<TShader>(
        int slot,
        SamplerStateProxy?[] samplers)
        where TShader : struct, ISamplerShaderType
        => TrackSamplers(TShader.Index, slot, samplers);
    
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSampler<TShader>(int startSlot, int num) 
        where TShader : struct, ISamplerShaderType
        => []; //TODO implement
    
    [SuppressMessage("ReSharper", "UnusedTypeParameter")]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SamplerStateProxy[] GetSamplers<TShader>(
        int startSlot,
        int num
    )
        where TShader : struct, ISamplerShaderType
        => [];//TODO implement

    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetShaderPass(ShaderPass pass, bool bindConstantBuffer = true) {
        if (CurrShaderPass == pass || pass.IsNull) 
            return;

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

    private void SetShaderResources(int shaderStage, int slot, ShaderResourceView?[] textures) =>
        NativeContext.SetShaderResources(shaderStage, slot, textures);

    private void SetShader(int shaderStage, ShaderBase shader, bool bindConstantBuffer) {
        NativeContext.SetShader(shaderStage, shader.IsNull ? null : shader.NativeShader);
        if (!bindConstantBuffer || shader.IsNull) return;

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

    private void TrackSampler(int shaderStage, int slot, SamplerStateProxy? sampler) {
        if (slot < 0) return;

        var index = shaderStage * SamplerStateCount + slot;
        if (index >= samplerStateCheck.Length) return;

        if (AutoSkipRedundantStateSetting && samplerStateCheck[index] == sampler) return;

        NativeContext.SetSampler(shaderStage, slot, sampler?.State);
        samplerStateCheck[index] = sampler;
    }

    private void TrackSamplers(int shaderStage, int slot, SamplerStateProxy?[] samplers) {
        if (slot < 0) return;

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

        var nativeSamplers = new SamplerState?[count];
        for (var i = 0; i < count; i++) {
            nativeSamplers[i] = samplers[i]?.State;
            samplerStateCheck[start + i] = samplers[i];
        }

        NativeContext.SetSamplers(shaderStage, slot, nativeSamplers);
    }
}
