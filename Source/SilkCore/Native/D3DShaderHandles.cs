/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Native;

public enum FeatureLevel {
    LevelDefault = 0,
    Level91 = 0x9100,
    Level92 = 0x9200,
    Level93 = 0x9300,
    Level100 = 0xa000,
    Level101 = 0xa100,
    Level110 = 0xb000,
    Level111 = 0xb100
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
