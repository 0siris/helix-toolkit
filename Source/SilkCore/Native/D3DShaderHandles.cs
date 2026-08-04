/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Direct3D11;
using SilkD3D11ComputeShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11ComputeShader>;
using SilkD3D11DomainShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DomainShader>;
using SilkD3D11GeometryShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11GeometryShader>;
using SilkD3D11HullShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11HullShader>;
using SilkD3D11InputLayoutPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11InputLayout>;
using SilkD3D11PixelShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11PixelShader>;
using SilkD3D11VertexShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11VertexShader>;

namespace HelixToolkit.SharpDX.Core;

public enum FeatureLevel {
    Level_DEFAULT = 0,
    Level_9_1 = 0x9100,
    Level_9_2 = 0x9200,
    Level_9_3 = 0x9300,
    Level_10_0 = 0xa000,
    Level_10_1 = 0xa100,
    Level_11_0 = 0xb000,
    Level_11_1 = 0xb100
}

public enum InputClassification {
    PerVertexData = 0,
    PerInstanceData = 1
}

public struct InputElement {
    public const int AppendAligned = -1;

    public InputElement(
        string semanticName,
        int semanticIndex,
        Format format,
        int alignedByteOffset,
        int slot,
        InputClassification classification = InputClassification.PerVertexData,
        int instanceDataStepRate = 0
    ) {
        SemanticName = semanticName;
        SemanticIndex = semanticIndex;
        Format = format;
        AlignedByteOffset = alignedByteOffset;
        Slot = slot;
        Classification = classification;
        InstanceDataStepRate = instanceDataStepRate;
    }

    public string SemanticName { get; set; }

    public int SemanticIndex { get; set; }

    public Format Format { get; set; }

    public int AlignedByteOffset { get; set; }

    public int Slot { get; set; }

    public InputClassification Classification { get; set; }

    public int InstanceDataStepRate { get; set; }
}

public struct StreamOutputElement {
    public StreamOutputElement(
        int stream,
        string semanticName,
        int semanticIndex,
        byte startComponent,
        byte componentCount,
        byte outputSlot
    ) {
        Stream = stream;
        SemanticName = semanticName;
        SemanticIndex = semanticIndex;
        StartComponent = startComponent;
        ComponentCount = componentCount;
        OutputSlot = outputSlot;
    }

    public int Stream { get; set; }

    public string SemanticName { get; set; }

    public int SemanticIndex { get; set; }

    public byte StartComponent { get; set; }

    public byte ComponentCount { get; set; }

    public byte OutputSlot { get; set; }
}
