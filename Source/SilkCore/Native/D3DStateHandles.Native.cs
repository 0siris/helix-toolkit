/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core;
using Silk.NET.Direct3D11;

namespace HelixToolkit.SharpDX.Core.Native;
internal static class D3DStateConversions {
    public static BlendDesc ToSilkDesc(this BlendStateDescription description) {
        var desc = new BlendDesc {
            AlphaToCoverageEnable = new Bool32(description.AlphaToCoverageEnable),
            IndependentBlendEnable = new Bool32(description.IndependentBlendEnable)
        };

        var renderTargets = description.RenderTarget;
        for (var i = 0; i < 8; i++) {
            var renderTarget = i < renderTargets.Length ? renderTargets[i] : default;
            desc.RenderTarget[i] = new RenderTargetBlendDesc {
                BlendEnable = new Bool32(renderTarget.IsBlendEnabled),
                SrcBlend = (Blend)renderTarget.SourceBlend,
                DestBlend = (Blend)renderTarget.DestinationBlend,
                BlendOp = (BlendOp)renderTarget.BlendOperation,
                SrcBlendAlpha = (Blend)renderTarget.SourceAlphaBlend,
                DestBlendAlpha = (Blend)renderTarget.DestinationAlphaBlend,
                BlendOpAlpha = (BlendOp)renderTarget.AlphaBlendOperation,
                RenderTargetWriteMask = (byte)renderTarget.RenderTargetWriteMask
            };
        }

        return desc;
    }

    public static DepthStencilDesc ToSilkDesc(this DepthStencilStateDescription description) => new() {
        DepthEnable = new Bool32(description.IsDepthEnabled),
        DepthWriteMask = (Silk.NET.Direct3D11.DepthWriteMask)description.DepthWriteMask,
        DepthFunc = (ComparisonFunc)description.DepthComparison,
        StencilEnable = new Bool32(description.IsStencilEnabled),
        StencilReadMask = description.StencilReadMask,
        StencilWriteMask = description.StencilWriteMask,
        FrontFace = description.FrontFace.ToSilkDesc(),
        BackFace = description.BackFace.ToSilkDesc()
    };

    public static DepthStencilopDesc ToSilkDesc(this DepthStencilOperationDescription description) => new() {
        StencilFailOp = (StencilOp)description.FailOperation,
        StencilDepthFailOp = (StencilOp)description.DepthFailOperation,
        StencilPassOp = (StencilOp)description.PassOperation,
        StencilFunc = (ComparisonFunc)description.Comparison
    };

    public static RasterizerDesc ToSilkDesc(this RasterizerStateDescription description) => new() {
        FillMode = (Silk.NET.Direct3D11.FillMode)description.FillMode,
        CullMode = (Silk.NET.Direct3D11.CullMode)description.CullMode,
        FrontCounterClockwise = new Bool32(description.IsFrontCounterClockwise),
        DepthBias = description.DepthBias,
        DepthBiasClamp = description.DepthBiasClamp,
        SlopeScaledDepthBias = description.SlopeScaledDepthBias,
        DepthClipEnable = new Bool32(description.IsDepthClipEnabled),
        ScissorEnable = new Bool32(description.IsScissorEnabled),
        MultisampleEnable = new Bool32(description.IsMultisampleEnabled),
        AntialiasedLineEnable = new Bool32(description.IsAntialiasedLineEnabled)
    };

    public static unsafe SamplerDesc ToSilkDesc(this SamplerStateDescription description) {
        var desc = new SamplerDesc {
            Filter = (Silk.NET.Direct3D11.Filter)description.Filter,
            AddressU = (Silk.NET.Direct3D11.TextureAddressMode)description.AddressU,
            AddressV = (Silk.NET.Direct3D11.TextureAddressMode)description.AddressV,
            AddressW = (Silk.NET.Direct3D11.TextureAddressMode)description.AddressW,
            MipLODBias = description.MipLodBias,
            MaxAnisotropy = (uint)description.MaximumAnisotropy,
            ComparisonFunc = (ComparisonFunc)description.ComparisonFunction,
            MinLOD = description.MinimumLod,
            MaxLOD = description.MaximumLod
        };
        desc.BorderColor[0] = description.BorderColor.X;
        desc.BorderColor[1] = description.BorderColor.Y;
        desc.BorderColor[2] = description.BorderColor.Z;
        desc.BorderColor[3] = description.BorderColor.W;
        return desc;
    }
}
