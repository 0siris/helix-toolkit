/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Native;

public enum BlendOperation {
    Add = 1,
    Subtract = 2,
    ReverseSubtract = 3,
    Minimum = 4,
    Maximum = 5
}

public enum BlendOption {
    Zero = 1,
    One = 2,
    SourceColor = 3,
    InverseSourceColor = 4,
    SourceAlpha = 5,
    InverseSourceAlpha = 6,
    DestinationAlpha = 7,
    InverseDestinationAlpha = 8,
    DestinationColor = 9,
    InverseDestinationColor = 10,
    SourceAlphaSaturate = 11,
    BlendFactor = 14,
    InverseBlendFactor = 15,
    SecondarySourceColor = 16,
    InverseSecondarySourceColor = 17,
    SecondarySourceAlpha = 18,
    InverseSecondarySourceAlpha = 19
}

[Flags]
public enum ColorWriteMaskFlags {
    None = 0,
    Red = 1,
    Green = 2,
    Blue = 4,
    Alpha = 8,
    All = Red | Green | Blue | Alpha
}

public enum Comparison {
    Never = 1,
    Less = 2,
    Equal = 3,
    LessEqual = 4,
    Greater = 5,
    NotEqual = 6,
    GreaterEqual = 7,
    Always = 8
}

public enum DepthWriteMask {
    Zero = 0,
    All = 1
}

public enum StencilOperation {
    Keep = 1,
    Zero = 2,
    Replace = 3,
    IncrementSaturation = 4,
    DecrementSaturation = 5,
    Invert = 6,
    Increment = 7,
    Decrement = 8
}

public enum FillMode {
    Wireframe = 2,
    Solid = 3
}

public enum CullMode {
    None = 1,
    Front = 2,
    Back = 3
}

public enum Filter {
    MinMagMipPoint = 0,
    MinMagPointMipLinear = 0x1,
    MinPointMagLinearMipPoint = 0x4,
    MinPointMagMipLinear = 0x5,
    MinLinearMagMipPoint = 0x10,
    MinLinearMagPointMipLinear = 0x11,
    MinMagLinearMipPoint = 0x14,
    MinMagMipLinear = 0x15,
    Anisotropic = 0x55,
    ComparisonMinMagMipPoint = 0x80,
    ComparisonMinMagPointMipLinear = 0x81,
    ComparisonMinPointMagLinearMipPoint = 0x84,
    ComparisonMinPointMagMipLinear = 0x85,
    ComparisonMinLinearMagMipPoint = 0x90,
    ComparisonMinLinearMagPointMipLinear = 0x91,
    ComparisonMinMagLinearMipPoint = 0x94,
    ComparisonMinMagMipLinear = 0x95,
    ComparisonAnisotropic = 0xd5
}

public enum TextureAddressMode {
    Wrap = 1,
    Mirror = 2,
    Clamp = 3,
    Border = 4,
    MirrorOnce = 5
}

public enum PrimitiveTopology {
    Undefined = 0,
    PointList = 1,
    LineList = 2,
    LineStrip = 3,
    TriangleList = 4,
    TriangleStrip = 5,
    LineListWithAdjacency = 10,
    LineStripWithAdjacency = 11,
    TriangleListWithAdjacency = 12,
    TriangleStripWithAdjacency = 13,
    PatchListWith1ControlPoint = 33,
    PatchListWith2ControlPoints = 34,
    PatchListWith3ControlPoints = 35,
    PatchListWith4ControlPoints = 36,
    PatchListWith5ControlPoints = 37,
    PatchListWith6ControlPoints = 38,
    PatchListWith7ControlPoints = 39,
    PatchListWith8ControlPoints = 40,
    PatchListWith9ControlPoints = 41,
    PatchListWith10ControlPoints = 42,
    PatchListWith11ControlPoints = 43,
    PatchListWith12ControlPoints = 44,
    PatchListWith13ControlPoints = 45,
    PatchListWith14ControlPoints = 46,
    PatchListWith15ControlPoints = 47,
    PatchListWith16ControlPoints = 48,
    PatchListWith17ControlPoints = 49,
    PatchListWith18ControlPoints = 50,
    PatchListWith19ControlPoints = 51,
    PatchListWith20ControlPoints = 52,
    PatchListWith21ControlPoints = 53,
    PatchListWith22ControlPoints = 54,
    PatchListWith23ControlPoints = 55,
    PatchListWith24ControlPoints = 56,
    PatchListWith25ControlPoints = 57,
    PatchListWith26ControlPoints = 58,
    PatchListWith27ControlPoints = 59,
    PatchListWith28ControlPoints = 60,
    PatchListWith29ControlPoints = 61,
    PatchListWith30ControlPoints = 62,
    PatchListWith31ControlPoints = 63,
    PatchListWith32ControlPoints = 64
}

public struct RenderTargetBlendDescription {
    public bool IsBlendEnabled;
    public BlendOption SourceBlend;
    public BlendOption DestinationBlend;
    public BlendOperation BlendOperation;
    public BlendOption SourceAlphaBlend;
    public BlendOption DestinationAlphaBlend;
    public BlendOperation AlphaBlendOperation;
    public ColorWriteMaskFlags RenderTargetWriteMask;
}

public struct BlendStateDescription {
    public bool AlphaToCoverageEnable;
    public bool IndependentBlendEnable;

    public RenderTargetBlendDescription[] RenderTarget {
        get {
            if (field == null) {
                field = new RenderTargetBlendDescription[8];
                for (var i = 0; i < field.Length; i++)
                    field[i] = new RenderTargetBlendDescription {
                        SourceBlend = BlendOption.One,
                        DestinationBlend = BlendOption.Zero,
                        BlendOperation = BlendOperation.Add,
                        SourceAlphaBlend = BlendOption.One,
                        DestinationAlphaBlend = BlendOption.Zero,
                        AlphaBlendOperation = BlendOperation.Add,
                        RenderTargetWriteMask = ColorWriteMaskFlags.All
                    };
            }

            return field;
        }
        set;
    }
}

public struct DepthStencilOperationDescription {
    public StencilOperation FailOperation;
    public StencilOperation DepthFailOperation;
    public StencilOperation PassOperation;
    public Comparison Comparison;
}

public struct DepthStencilStateDescription {
    public bool IsDepthEnabled;
    public DepthWriteMask DepthWriteMask;
    public Comparison DepthComparison;
    public bool IsStencilEnabled;
    public byte StencilReadMask;
    public byte StencilWriteMask;
    public DepthStencilOperationDescription FrontFace;
    public DepthStencilOperationDescription BackFace;
}

public struct RasterizerStateDescription {
    public FillMode FillMode;
    public CullMode CullMode;
    public bool IsFrontCounterClockwise;
    public int DepthBias;
    public float DepthBiasClamp;
    public float SlopeScaledDepthBias;
    public bool IsDepthClipEnabled;
    public bool IsScissorEnabled;
    public bool IsMultisampleEnabled;
    public bool IsAntialiasedLineEnabled;
}

public struct SamplerStateDescription {
    public Filter Filter;
    public TextureAddressMode AddressU;
    public TextureAddressMode AddressV;
    public TextureAddressMode AddressW;
    public float MipLodBias;
    public int MaximumAnisotropy;
    public Comparison ComparisonFunction;
    public Color4 BorderColor;
    public float MinimumLod;
    public float MaximumLod;
}
