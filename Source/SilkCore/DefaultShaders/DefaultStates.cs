/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Shaders;
/// <summary>
/// </summary>
public static class DefaultBlendStateDescriptions {
    public static readonly BlendStateDescription BsAlphaBlend;
    public static readonly BlendStateDescription BsSourceAlways;
    public static readonly BlendStateDescription NoBlend;
    public static readonly BlendStateDescription BsOverlayBlending;
    public static readonly BlendStateDescription AdditiveBlend;
    public static readonly BlendStateDescription BsScreenDupCursorBlend;
    public static readonly BlendStateDescription BsoitBlend = new() { IndependentBlendEnable = true };
    public static readonly BlendStateDescription BsotiSortingBlend;
    public static readonly BlendStateDescription BsMeshOitBlendQuad;
    public static readonly BlendStateDescription VolumeBlending;
    public static readonly BlendStateDescription BsGlowBlending;
    public static readonly BlendStateDescription Bsoitdp;
    public static readonly BlendStateDescription BsoitdpMaxBlending;
    public static readonly BlendStateDescription BsoitdpFinal;

    static DefaultBlendStateDescriptions() {
        BsAlphaBlend.RenderTarget[0] = new RenderTargetBlendDescription {
            AlphaBlendOperation = BlendOperation.Add,
            BlendOperation = BlendOperation.Add,
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,

            SourceAlphaBlend = BlendOption.SourceAlpha,
            DestinationAlphaBlend = BlendOption.DestinationAlpha,
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        BsSourceAlways.RenderTarget[0] = new RenderTargetBlendDescription {
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
        BsOverlayBlending.RenderTarget[0] = new RenderTargetBlendDescription {
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

        BsScreenDupCursorBlend.RenderTarget[0] = new RenderTargetBlendDescription {
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.Zero,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.All,
            IsBlendEnabled = true
        };

        BsoitBlend.RenderTarget[0] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.One,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.One,
            DestinationAlphaBlend = BlendOption.One,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };
        BsoitBlend.RenderTarget[1] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            SourceBlend = BlendOption.Zero,
            DestinationBlend = BlendOption.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.Zero,
            DestinationAlphaBlend = BlendOption.InverseSourceAlpha,
            AlphaBlendOperation = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteMaskFlags.Alpha
        };

        BsMeshOitBlendQuad.RenderTarget[0] = new RenderTargetBlendDescription {
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

        BsGlowBlending.RenderTarget[0] = new RenderTargetBlendDescription {
            BlendOperation = BlendOperation.Add,
            SourceBlend = BlendOption.One,
            DestinationBlend = BlendOption.InverseSourceAlpha,

            SourceAlphaBlend = BlendOption.SourceAlpha,
            DestinationAlphaBlend = BlendOption.DestinationAlpha,
            AlphaBlendOperation = BlendOperation.Add,
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All
        };

        Bsoitdp.IndependentBlendEnable = true;
        Bsoitdp.AlphaToCoverageEnable = false;
        // Max blending
        Bsoitdp.RenderTarget[0] = new RenderTargetBlendDescription {
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
        Bsoitdp.RenderTarget[1] = new RenderTargetBlendDescription {
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
        Bsoitdp.RenderTarget[2] = new RenderTargetBlendDescription {
            IsBlendEnabled = true,
            RenderTargetWriteMask = ColorWriteMaskFlags.All,
            SourceBlend = BlendOption.SourceAlpha,
            DestinationBlend = BlendOption.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceAlphaBlend = BlendOption.Zero,
            DestinationAlphaBlend = BlendOption.One,
            AlphaBlendOperation = BlendOperation.Add
        };
        BsoitdpMaxBlending.AlphaToCoverageEnable = false;
        BsoitdpMaxBlending.IndependentBlendEnable = true;
        // Max blending
        for (var i = 0; i < 3; ++i)
            BsoitdpMaxBlending.RenderTarget[i] = new RenderTargetBlendDescription {
                IsBlendEnabled = true,
                RenderTargetWriteMask = ColorWriteMaskFlags.All,
                SourceBlend = BlendOption.One,
                DestinationBlend = BlendOption.One,
                BlendOperation = BlendOperation.Maximum,
                SourceAlphaBlend = BlendOption.One,
                DestinationAlphaBlend = BlendOption.One,
                AlphaBlendOperation = BlendOperation.Maximum
            };

        BsoitdpFinal = BsSourceAlways;
        BsoitdpFinal.AlphaToCoverageEnable = false;
    }
}

/// <summary>
/// </summary>
public static class DefaultDepthStencilDescriptions {
    /// <summary>
    ///     The DSS depth less
    /// </summary>
    public static readonly DepthStencilStateDescription DssDepthLess = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.All,
        DepthComparison = Comparison.Less,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS depth less equal
    /// </summary>
    public static readonly DepthStencilStateDescription DssDepthLessEqual = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.All,
        DepthComparison = Comparison.LessEqual,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS less no write
    /// </summary>
    public static readonly DepthStencilStateDescription DssLessNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Less,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS less equal no write
    /// </summary>
    public static readonly DepthStencilStateDescription DssLessEqualNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.LessEqual,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS greater no write
    /// </summary>
    public static readonly DepthStencilStateDescription DssGreaterNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Greater
    };

    /// <summary>
    ///     The DSS equal no write
    /// </summary>
    public static readonly DepthStencilStateDescription DssEqualNoWrite = new() {
        IsDepthEnabled = true,
        DepthWriteMask = DepthWriteMask.Zero,
        DepthComparison = Comparison.Equal
    };

    /// <summary>
    ///     The DSS clip plane backface
    /// </summary>
    public static readonly DepthStencilStateDescription DssClipPlaneBackface = new() {
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
    public static readonly DepthStencilStateDescription DssMeshOutlineP1 = new() {
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
    public static readonly DepthStencilStateDescription DssEffectMeshXRayP1 = new() {
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

    public static readonly DepthStencilStateDescription DssEffectMeshXRayGridP1 = new() {
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

    public static readonly DepthStencilStateDescription DssEffectMeshXRayGridP2 = new() {
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

    public static readonly DepthStencilStateDescription DssEffectMeshXRayGridP3 = new() {
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
    public static readonly DepthStencilStateDescription DssEffectMeshXRayP2 = new() {
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
    public static readonly DepthStencilStateDescription DssOutlineFillQuad = new() {
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
    public static readonly DepthStencilStateDescription DssClipPlaneFillQuad = new() {
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
    public static readonly DepthStencilStateDescription DssDepthAlwaysNoStencil = new() {
        IsDepthEnabled = true,
        DepthComparison = Comparison.Always,
        IsStencilEnabled = false
    };

    /// <summary>
    ///     The DSS no depth no stencil
    /// </summary>
    public static readonly DepthStencilStateDescription DssNoDepthNoStencil = new() {
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

    public static readonly DepthStencilStateDescription DssVolumeBackFace = new() {
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

    public static readonly DepthStencilStateDescription DssVolumeFrontFace = new() {
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
    public static readonly RasterizerStateDescription RsSolidNoMsaa = new() {
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
    public static readonly RasterizerStateDescription RsSkybox = new() {
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

    public static readonly RasterizerStateDescription RsSkyDome = new() {
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

    public static readonly RasterizerStateDescription RsOutline = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };

    public static readonly RasterizerStateDescription RsPlaneGrid = new() {
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

    public static readonly RasterizerStateDescription RsSpriteCw = new() {
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

    public static readonly RasterizerStateDescription RsVolume = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };

    public static readonly RasterizerStateDescription RsVolumeCubeFront = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.Back,
        DepthBias = 0,
        DepthBiasClamp = 0,
        SlopeScaledDepthBias = +0,
        IsFrontCounterClockwise = true,
        IsMultisampleEnabled = false,
        IsAntialiasedLineEnabled = false
    };

    public static readonly RasterizerStateDescription RsVolumeCubeBack = new() {
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
    public static readonly RasterizerStateDescription RsScreenDuplication = new() {
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
