/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Shaders;
/// <summary>
/// </summary>
public static class DefaultBlendStateDescriptions {
    public static readonly BlendStateDescription BSAlphaBlend;
    public static readonly BlendStateDescription BSSourceAlways;
    public static readonly BlendStateDescription NoBlend;
    public static readonly BlendStateDescription BSOverlayBlending;
    public static readonly BlendStateDescription AdditiveBlend;
    public static readonly BlendStateDescription BSScreenDupCursorBlend;
    public static readonly BlendStateDescription BSOITBlend = new() { IndependentBlendEnable = true };
    public static readonly BlendStateDescription BSOTISortingBlend;
    public static readonly BlendStateDescription BSMeshOITBlendQuad;
    public static readonly BlendStateDescription VolumeBlending;
    public static readonly BlendStateDescription BSGlowBlending;
    public static readonly BlendStateDescription BSOITDP;
    public static readonly BlendStateDescription BSOITDPMaxBlending;
    public static readonly BlendStateDescription BSOITDPFinal;

    static DefaultBlendStateDescriptions() {
        BSAlphaBlend.RenderTarget[0] = new RenderTargetBlendDescription {
            AlphaBlendOperation = BlendOperation.Add,
            BlendOperation = BlendOperation.Add,
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,

            SourceAlphaBlend = BlendOption.SourceAlpha,
            DestinationAlphaBlend = BlendOption.DestinationAlpha,
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        BSSourceAlways.RenderTarget[0] = new RenderTargetBlendDescription {
            AlphaBlendOperation = BlendOperation.Add,
            BlendOperation = BlendOperation.Add,
            DestinationBlend = BlendOption.Zero,
            SourceBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.Zero,
            SourceAlphaBlend = BlendOption.One,
            IsBlendEnabled = false,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        NoBlend.RenderTarget[0] = new RenderTargetBlendDescription { IsBlendEnabled = false };
        BSOverlayBlending.RenderTarget[0] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.One,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.Zero,
            DestinationAlphaBlend = BlendOption.One,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        AdditiveBlend.RenderTarget[0] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.One,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.One,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        BSScreenDupCursorBlend.RenderTarget[0] = new RenderTargetBlendDescription {
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.Zero,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.All,
            IsBlendEnabled = true
        };

        BSOITBlend.RenderTarget[0] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.One,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.One,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };
        BSOITBlend.RenderTarget[1] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            SourceBlend = BlendOption.Zero,
            DestinationBlend = BlendOption.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.Zero,
            DestinationAlphaBlend = BlendOption.InverseSourceAlpha,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.Alpha
        };

        BSMeshOITBlendQuad.RenderTarget[0] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            SourceBlend = BlendOption.InverseSourceAlpha,
            DestinationBlend = BlendOption.SourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.InverseSourceAlpha,
            DestinationAlphaBlend = BlendOption.DestinationAlpha,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask =
                ColorWriteMaskFlags.Red | ColorWriteMaskFlags.Green | ColorWriteMaskFlags.Blue
        };

        VolumeBlending.RenderTarget[0] = new RenderTargetBlendDescription {
            AlphaBlendOperation = BlendOperation.Add,
            BlendOperation = BlendOperation.Add,
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,

            SourceAlphaBlend = BlendOption.InverseSourceAlpha,
            DestinationAlphaBlend = BlendOption.Zero,
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        BSGlowBlending.RenderTarget[0] = new RenderTargetBlendDescription {
            BlendOperation = BlendOperation.Add,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.InverseSourceAlpha,

            SourceAlphaBlend = BlendOption.SourceAlpha,
            DestinationAlphaBlend = BlendOption.DestinationAlpha,
            AlphaBlendOperation = BlendOperation.Add,
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        BSOITDP.IndependentBlendEnable = true;
        BSOITDP.AlphaToCoverageEnable = false;
        // Max blending
        BSOITDP.RenderTarget[0] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.One,
            BlendOperation = BlendOperation.Maximum,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.One,
            AlphaBlendOperation = BlendOperation.Maximum
        };
        // Front to back blending
        BSOITDP.RenderTarget[1] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All,
            SourceBlend = BlendOption.DestinationAlpha,
            DestinationBlend = BlendOption.One,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.Zero,
            DestinationAlphaBlend = BlendOption.InverseSourceAlpha,
            AlphaBlendOperation = BlendOperation.Add
        };
        // Back to front blending
        BSOITDP.RenderTarget[2] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All,
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.Zero,
            DestinationAlphaBlend = BlendOption.One,
            AlphaBlendOperation = BlendOperation.Add
        };
        BSOITDPMaxBlending.AlphaToCoverageEnable = false;
        BSOITDPMaxBlending.IndependentBlendEnable = true;
        // Max blending
        for (var i = 0; i < 3; ++i)
            BSOITDPMaxBlending.RenderTarget[i] = new RenderTargetBlendDescription {
                IsBlendEnabled = true,
                RenderTargetWriteMask = ColorWriteMaskFlags.All,
                SourceBlend = BlendOption.One,
                DestinationBlend = BlendOption.One,
                BlendOperation = BlendOperation.Maximum,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.One,
                AlphaBlendOperation = BlendOperation.Maximum
            };

        BSOITDPFinal = BSSourceAlways;
        BSOITDPFinal.AlphaToCoverageEnable = false;
    }
}

/// <summary>
/// </summary>
public static class DefaultDepthStencilDescriptions {
    /// <summary>
    ///     The DSS depth less
    /// </summary>
    public static readonly DepthStencilStateDescription DSSDepthLess = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.All,
        DepthComparison = Comparison.Less,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS depth less equal
    /// </summary>
    public static readonly DepthStencilStateDescription DSSDepthLessEqual = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.All,
        DepthComparison = Comparison.LessEqual,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS less no write
    /// </summary>
    public static readonly DepthStencilStateDescription DSSLessNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Less,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS less equal no write
    /// </summary>
    public static readonly DepthStencilStateDescription DSSLessEqualNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.LessEqual,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS greater no write
    /// </summary>
    public static readonly DepthStencilStateDescription DSSGreaterNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Greater
    };

    /// <summary>
    ///     The DSS equal no write
    /// </summary>
    public static readonly DepthStencilStateDescription DSSEqualNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Equal
    };

    /// <summary>
    ///     The DSS clip plane backface
    /// </summary>
    public static readonly DepthStencilStateDescription DSSClipPlaneBackface = new() {
        IsDepthEnabled = true,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Less,
        StencilWriteMask = 0xFF,
        StencilReadMask = 0,
        BackFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Replace,
            Comparison = Comparison.Always,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        },
        FrontFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Never,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        }
    };

    /// <summary>
    ///     The DSS mesh outline pass1
    /// </summary>
    public static readonly DepthStencilStateDescription DSSMeshOutlineP1 = new() {
        IsDepthEnabled = false,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Always,
        StencilWriteMask = 0xFF,
        StencilReadMask = 0,
        BackFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Replace,
            Comparison = Comparison.Always,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        },
        FrontFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Replace,
            Comparison = Comparison.Always,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        }
    };

    /// <summary>
    ///     The DSS mesh outline pass1
    /// </summary>
    public static readonly DepthStencilStateDescription DSSEffectMeshXRayP1 = new() {
        IsDepthEnabled = false,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Always,
        StencilWriteMask = 0xFF,
        StencilReadMask = 0,
        BackFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Never,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        },
        FrontFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Increment,
            Comparison = Comparison.Always,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        }
    };

    public static readonly DepthStencilStateDescription DSSEffectMeshXRayGridP1 = new() {
        IsDepthEnabled = false,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Always,
        StencilWriteMask = 0xFF,
        StencilReadMask = 0,
        BackFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Never,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        },
        FrontFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Replace,
            Comparison = Comparison.Always,
            DepthFailOperation = StencilOperation.Zero,
            FailOperation = StencilOperation.Zero
        }
    };

    public static readonly DepthStencilStateDescription DSSEffectMeshXRayGridP2 = new() {
        IsDepthEnabled = true,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.LessEqual,
        StencilWriteMask = 0xFF,
        StencilReadMask = 0,
        BackFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Never,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        },
        FrontFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Zero,
            Comparison = Comparison.Equal,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Zero
        }
    };

    public static readonly DepthStencilStateDescription DSSEffectMeshXRayGridP3 = new() {
        IsDepthEnabled = false,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.NotEqual,
        StencilWriteMask = 0,
        StencilReadMask = 0xFF,
        BackFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Never,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        },
        FrontFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Equal,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        }
    };

    /// <summary>
    ///     The DSS mesh outline pass1
    /// </summary>
    public static readonly DepthStencilStateDescription DSSEffectMeshXRayP2 = new() {
        IsDepthEnabled = true,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Greater,
        StencilWriteMask = 0,
        StencilReadMask = 0xFF,
        BackFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Never,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        },
        FrontFace = new DepthStencilOperationDescription {
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Equal,
            DepthFailOperation = StencilOperation.Keep,
            FailOperation = StencilOperation.Keep
        }
    };

    /// <summary>
    ///     The DSS clip plane fill quad
    /// </summary>
    public static readonly DepthStencilStateDescription DSSOutlineFillQuad = new() {
        IsDepthEnabled = false,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Always,
        FrontFace = new DepthStencilOperationDescription {
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Equal
        },
        BackFace = new DepthStencilOperationDescription {
            Comparison = Comparison.Never,
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Keep
        },
        StencilReadMask = 0xFF,
        StencilWriteMask = 0
    };

    /// <summary>
    ///     The DSS clip plane fill quad
    /// </summary>
    public static readonly DepthStencilStateDescription DSSClipPlaneFillQuad = new() {
        IsDepthEnabled = false,
        IsStencilEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Less,
        FrontFace = new DepthStencilOperationDescription {
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Equal
        },
        BackFace = new DepthStencilOperationDescription {
            Comparison = Comparison.Never,
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Keep
        },
        StencilReadMask = 0xFF,
        StencilWriteMask = 0
    };

    /// <summary>
    ///     The DSS depth always no stencil
    /// </summary>
    public static readonly DepthStencilStateDescription DSSDepthAlwaysNoStencil = new() {
        IsDepthEnabled = true,
        DepthComparison = Comparison.Always,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS no depth no stencil
    /// </summary>
    public static readonly DepthStencilStateDescription DSSNoDepthNoStencil = new() {
        IsDepthEnabled = false,
        IsStencilEnabled = false,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Always,
        FrontFace = new DepthStencilOperationDescription {
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Always
        },
        BackFace = new DepthStencilOperationDescription {
            Comparison = Comparison.Always,
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Keep
        },
        StencilReadMask = 0,
        StencilWriteMask = 0
    };

    public static readonly DepthStencilStateDescription DSSVolumeBackFace = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.All,
        DepthComparison = Comparison.Less,
        IsStencilEnabled = true,
        FrontFace = new DepthStencilOperationDescription {
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Zero,
            Comparison = Comparison.Always
        },
        BackFace = new DepthStencilOperationDescription {
            Comparison = Comparison.Always,
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Zero
        },
        StencilReadMask = 0x00,
        StencilWriteMask = 0xFF
    };

    public static readonly DepthStencilStateDescription DSSVolumeFrontFace = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.All,
        DepthComparison = Comparison.Less,
        IsStencilEnabled = true,
        FrontFace = new DepthStencilOperationDescription {
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Keep,
            Comparison = Comparison.Equal
        },
        BackFace = new DepthStencilOperationDescription {
            Comparison = Comparison.Never,
            FailOperation = StencilOperation.Keep,
            DepthFailOperation = StencilOperation.Keep,
            PassOperation = StencilOperation.Zero
        },
        StencilReadMask = 0xFF,
        StencilWriteMask = 0x00
    };
}

/// <summary>
/// </summary>
public static class DefaultRasterDescriptions {
    /// <summary>
    ///     The solid no msaa RasterizerState
    /// </summary>
    public static readonly RasterizerStateDescription RSSolidNoMSAA = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.Back,
        DepthBias = -5,
        DepthBiasClamp = -10,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };

    /// <summary>
    ///     The skybox RasterizerState
    /// </summary>
    public static readonly RasterizerStateDescription RSSkybox = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false,
        IsDepthClipEnabled = false
    };

    public static readonly RasterizerStateDescription RSSkyDome = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = false,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false,
        IsDepthClipEnabled = false
    };

    public static readonly RasterizerStateDescription RSOutline = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };

    public static readonly RasterizerStateDescription RSPlaneGrid = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 10,
        DepthBiasClamp = 1000,
        SlopeScaledDepthBias = 0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false,
        IsDepthClipEnabled = true,
        IsScissorEnabled = true
    };

    public static readonly RasterizerStateDescription RSSpriteCW = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = 0,
        IsFrontCounterClockwise = false,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false,
        IsDepthClipEnabled = false,
        IsScissorEnabled = true
    };

    public static readonly RasterizerStateDescription RSVolume = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };

    public static readonly RasterizerStateDescription RSVolumeCubeFront = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.Back,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };

    public static readonly RasterizerStateDescription RSVolumeCubeBack = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.Front,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };
#if !WINDOWS_UWP
    /// <summary>
    ///     The screen duplication RasterizerState
    /// </summary>
    public static readonly RasterizerStateDescription RSScreenDuplication = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false,
        IsScissorEnabled = true
    };
#endif
}
