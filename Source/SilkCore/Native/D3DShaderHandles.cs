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

namespace HelixToolkit.SharpDX.Core
{
    public enum FeatureLevel
    {
        Level_DEFAULT = 0,
        Level_9_1 = 0x9100,
        Level_9_2 = 0x9200,
        Level_9_3 = 0x9300,
        Level_10_0 = 0xa000,
        Level_10_1 = 0xa100,
        Level_11_0 = 0xb000,
        Level_11_1 = 0xb100
    }

    public enum InputClassification
    {
        PerVertexData = 0,
        PerInstanceData = 1
    }

    public struct InputElement
    {
        public const int AppendAligned = -1;

        public InputElement(string semanticName, int semanticIndex, Format format, int alignedByteOffset, int slot,
            InputClassification classification = InputClassification.PerVertexData, int instanceDataStepRate = 0)
        {
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

    public struct StreamOutputElement
    {
        public StreamOutputElement(int stream, string semanticName, int semanticIndex, byte startComponent,
            byte componentCount, byte outputSlot)
        {
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

    namespace Native
    {
        internal unsafe interface IShaderHandle
        {
            int StageIndex { get; }

            void* NativeHandle { get; }
        }

        internal sealed unsafe class VertexShaderHandle : IDisposable, IShaderHandle
        {
            private SilkD3D11VertexShaderPtr shader;

            public VertexShaderHandle(SilkD3D11VertexShaderPtr shader)
            {
                this.shader = shader;
            }

            public ID3D11VertexShader* Handle => shader.Handle;

            public void Dispose()
            {
                if (shader.Handle != null)
                {
                    shader.Dispose();
                    shader = default;
                }
            }

            public void* NativeHandle => shader.Handle;

            public int StageIndex => Constants.VertexIdx;
        }

        internal sealed unsafe class PixelShaderHandle : IDisposable, IShaderHandle
        {
            private SilkD3D11PixelShaderPtr shader;

            public PixelShaderHandle(SilkD3D11PixelShaderPtr shader)
            {
                this.shader = shader;
            }

            public ID3D11PixelShader* Handle => shader.Handle;

            public void Dispose()
            {
                if (shader.Handle != null)
                {
                    shader.Dispose();
                    shader = default;
                }
            }

            public void* NativeHandle => shader.Handle;

            public int StageIndex => Constants.PixelIdx;
        }

        internal sealed unsafe class ComputeShaderHandle : IDisposable, IShaderHandle
        {
            private SilkD3D11ComputeShaderPtr shader;

            public ComputeShaderHandle(SilkD3D11ComputeShaderPtr shader)
            {
                this.shader = shader;
            }

            public ID3D11ComputeShader* Handle => shader.Handle;

            public void Dispose()
            {
                if (shader.Handle != null)
                {
                    shader.Dispose();
                    shader = default;
                }
            }

            public void* NativeHandle => shader.Handle;

            public int StageIndex => Constants.ComputeIdx;
        }

        internal sealed unsafe class DomainShaderHandle : IDisposable, IShaderHandle
        {
            private SilkD3D11DomainShaderPtr shader;

            public DomainShaderHandle(SilkD3D11DomainShaderPtr shader)
            {
                this.shader = shader;
            }

            public ID3D11DomainShader* Handle => shader.Handle;

            public void Dispose()
            {
                if (shader.Handle != null)
                {
                    shader.Dispose();
                    shader = default;
                }
            }

            public void* NativeHandle => shader.Handle;

            public int StageIndex => Constants.DomainIdx;
        }

        internal sealed unsafe class HullShaderHandle : IDisposable, IShaderHandle
        {
            private SilkD3D11HullShaderPtr shader;

            public HullShaderHandle(SilkD3D11HullShaderPtr shader)
            {
                this.shader = shader;
            }

            public ID3D11HullShader* Handle => shader.Handle;

            public void Dispose()
            {
                if (shader.Handle != null)
                {
                    shader.Dispose();
                    shader = default;
                }
            }

            public void* NativeHandle => shader.Handle;

            public int StageIndex => Constants.HullIdx;
        }

        internal sealed unsafe class GeometryShaderHandle : IDisposable, IShaderHandle
        {
            private SilkD3D11GeometryShaderPtr shader;

            public GeometryShaderHandle(SilkD3D11GeometryShaderPtr shader)
            {
                this.shader = shader;
            }

            public ID3D11GeometryShader* Handle => shader.Handle;

            public void Dispose()
            {
                if (shader.Handle != null)
                {
                    shader.Dispose();
                    shader = default;
                }
            }

            public void* NativeHandle => shader.Handle;

            public int StageIndex => Constants.GeometryIdx;
        }

        internal sealed unsafe class InputLayout : IDisposable
        {
            private SilkD3D11InputLayoutPtr layout;

            public InputLayout(SilkD3D11InputLayoutPtr layout)
            {
                this.layout = layout;
            }

            public ID3D11InputLayout* Handle => layout.Handle;

            public void Dispose()
            {
                if (layout.Handle != null)
                {
                    layout.Dispose();
                    layout = default;
                }
            }
        }

        internal static unsafe class D3DShaderConversions
        {
            public static FeatureLevel ToFeatureLevel(this SilkFeatureLevel featureLevel)
            {
                return featureLevel switch
                {
                    SilkFeatureLevel.Level_11_1 => FeatureLevel.Level_11_1,
                    SilkFeatureLevel.Level_11_0 => FeatureLevel.Level_11_0,
                    SilkFeatureLevel.Level_10_1 => FeatureLevel.Level_10_1,
                    SilkFeatureLevel.Level_10_0 => FeatureLevel.Level_10_0,
                    SilkFeatureLevel.Level_9_3 => FeatureLevel.Level_9_3,
                    SilkFeatureLevel.Level_9_2 => FeatureLevel.Level_9_2,
                    SilkFeatureLevel.Level_9_1 => FeatureLevel.Level_9_1,
                    _ => FeatureLevel.Level_DEFAULT
                };
            }

            public static InputElementDesc ToSilkDesc(this InputElement element, nint semanticName)
            {
                return new InputElementDesc
                {
                    SemanticName = (byte*) semanticName,
                    SemanticIndex = (uint) element.SemanticIndex,
                    Format = element.Format,
                    InputSlot = (uint) element.Slot,
                    AlignedByteOffset = element.AlignedByteOffset == InputElement.AppendAligned
                        ? unchecked((uint) -1)
                        : (uint) element.AlignedByteOffset,
                    InputSlotClass = (Silk.NET.Direct3D11.InputClassification) element.Classification,
                    InstanceDataStepRate = (uint) element.InstanceDataStepRate
                };
            }

            public static SODeclarationEntry ToSilkDesc(this StreamOutputElement element, nint semanticName)
            {
                return new SODeclarationEntry
                {
                    Stream = (uint) element.Stream,
                    SemanticName = (byte*) semanticName,
                    SemanticIndex = (uint) element.SemanticIndex,
                    StartComponent = element.StartComponent,
                    ComponentCount = element.ComponentCount,
                    OutputSlot = element.OutputSlot
                };
            }
        }
    }
}