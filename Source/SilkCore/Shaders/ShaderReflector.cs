/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Shaders;
public sealed unsafe class ShaderReflector : IShaderReflector {
    private static readonly Guid ShaderReflectionGuid = new("8d536ca1-0cca-4956-a837-786963755584");

    public FeatureLevel FeatureLevel { get; private set; }

    public Dictionary<string, ConstantBufferMapping> ConstantBufferMappings { get; } = [];

    public Dictionary<string, TextureMapping> TextureMappings { get; } = [];

    public Dictionary<string, UavMapping> UavMappings { get; } = [];

    public Dictionary<string, SamplerMapping> SamplerMappings { get; } = [];

    public void Parse(byte[] byteCode, ShaderStage stage) {
        ConstantBufferMappings.Clear();
        TextureMappings.Clear();
        UavMappings.Clear();
        SamplerMappings.Clear();

        if (byteCode.Length == 0) {
            FeatureLevel = FeatureLevel.LevelDefault;
            throw new ArgumentException("Shader bytecode cannot be empty.", nameof(byteCode));
        }

        fixed (byte* byteCodePtr = byteCode) {
            void* reflectionPtr = null;
            var shaderReflectionGuid = ShaderReflectionGuid;
            var result = D3DReflect(byteCodePtr,
                                    (nuint)byteCode.Length,
                                    ref shaderReflectionGuid,
                                    &reflectionPtr);
            if (result < 0)
                throw new InvalidDataException($"Invalid {stage} shader bytecode.",
                                               Marshal.GetExceptionForHR(result));
            var reflection = (Id3D11ShaderReflection*)reflectionPtr;
            try {
                ShaderDesc shaderDesc = default;
                Marshal.ThrowExceptionForHR(reflection->LpVtbl->GetDesc(reflection, &shaderDesc));
                FeatureLevel = GetFeatureLevel(shaderDesc.Version);

                for (var i = 0u; i < shaderDesc.BoundResources; ++i) {
                    ShaderInputBindDesc resourceDesc = default;
                    Marshal.ThrowExceptionForHR(
                        reflection->LpVtbl->GetResourceBindingDesc(reflection, i, &resourceDesc));
                    var name = PtrToString(resourceDesc.Name);
                    switch (resourceDesc.Type) {
                        case ShaderInputType.ConstantBuffer:
                            var cb = reflection->LpVtbl->GetConstantBufferByName(reflection, resourceDesc.Name);
                            var cbDesc =
                                CreateConstantBufferDescription(cb, stage, (int)resourceDesc.BindPoint);
                            ConstantBufferMappings.Add(name,
                                                       cbDesc.CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.Texture:
                            TextureMappings.Add(name,
                                                new TextureDescription(name, stage, TextureType.Texture)
                                                    .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.Structured:
                            TextureMappings.Add(name,
                                                new TextureDescription(name, stage, TextureType.Structured)
                                                    .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.ByteAddress:
                            TextureMappings.Add(name,
                                                new TextureDescription(name, stage, TextureType.ByteAddress)
                                                    .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.TextureBuffer:
                            TextureMappings.Add(name,
                                                new TextureDescription(name, stage, TextureType.TextureBuffer)
                                                    .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.UnorderedAccessViewAppendStructured:
                            UavMappings.Add(name,
                                            new UavDescription(name,
                                                               stage,
                                                               UnorderedAccessViewType.AppendStructured)
                                                .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.UnorderedAccessViewConsumeStructured:
                            UavMappings.Add(name,
                                            new UavDescription(name,
                                                               stage,
                                                               UnorderedAccessViewType.ConsumeStructured)
                                                .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.UnorderedAccessViewRwByteAddress:
                            UavMappings.Add(name,
                                            new UavDescription(name,
                                                               stage,
                                                               UnorderedAccessViewType.RwByteAddress)
                                                .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.UnorderedAccessViewRwStructuredWithCounter:
                            UavMappings.Add(name,
                                            new UavDescription(name,
                                                               stage,
                                                               UnorderedAccessViewType.RwStructuredWithCounter)
                                                .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.UnorderedAccessViewRwTyped:
                            UavMappings.Add(name,
                                            new UavDescription(name, stage, UnorderedAccessViewType.RwTyped)
                                                .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.UnorderedAccessViewRwStructured:
                            UavMappings.Add(name,
                                            new UavDescription(name,
                                                               stage,
                                                               UnorderedAccessViewType.RwStructured)
                                                .CreateMapping((int)resourceDesc.BindPoint));
                            break;
                        case ShaderInputType.Sampler:
                            SamplerMappings.Add(name,
                                                new SamplerMapping((int)resourceDesc.BindPoint, name, stage));
                            break;
                    }
                }
            } finally {
                if (reflection != null) reflection->LpVtbl->Release(reflection);
            }
        }
    }

    private static ConstantBufferDescription CreateConstantBufferDescription(
        Id3D11ShaderReflectionConstantBuffer* buffer,
        ShaderStage stage,
        int slot
    ) {
        ShaderBufferDesc desc = default;
        Marshal.ThrowExceptionForHR(buffer->LpVtbl->GetDesc(buffer, &desc));

        var variables = new List<ConstantBufferVariable>((int)desc.Variables);
        for (var i = 0u; i < desc.Variables; i++) {
            var variable = buffer->LpVtbl->GetVariableByIndex(buffer, i);
            ShaderVariableDesc variableDesc = default;
            Marshal.ThrowExceptionForHR(variable->LpVtbl->GetDesc(variable, &variableDesc));
            variables.Add(new ConstantBufferVariable {
                Name = PtrToString(variableDesc.Name),
                StartOffset = (int)variableDesc.StartOffset,
                Size = (int)variableDesc.Size
            });
        }

        return new ConstantBufferDescription(PtrToString(desc.Name), (int)desc.Size, variables) {
            Stage = stage,
            Slot = slot
        };
    }

    private static string PtrToString(byte* value) => value == null ? string.Empty : Marshal.PtrToStringAnsi((nint)value) ?? string.Empty;

    private static FeatureLevel GetFeatureLevel(uint shaderVersion) {
        var major = (shaderVersion >> 4) & 0xf;
        var minor = shaderVersion & 0xf;
        if (major >= 5) return FeatureLevel.Level110;

        if (major == 4 && minor >= 1) return FeatureLevel.Level101;

        if (major == 4) return FeatureLevel.Level100;

        return FeatureLevel.Level91;
    }

    [DllImport("d3dcompiler_47.dll", ExactSpelling = true)]
    private static extern int D3DReflect(
        void* pSrcData,
        nuint srcDataSize,
        ref Guid pInterface,
        void** ppReflector
    );

    private enum ShaderInputType : uint {
        ConstantBuffer = 0,
        TextureBuffer = 1,
        Texture = 2,
        Sampler = 3,
        UnorderedAccessViewRwTyped = 4,
        Structured = 5,
        UnorderedAccessViewRwStructured = 6,
        ByteAddress = 7,
        UnorderedAccessViewRwByteAddress = 8,
        UnorderedAccessViewAppendStructured = 9,
        UnorderedAccessViewConsumeStructured = 10,
        UnorderedAccessViewRwStructuredWithCounter = 11
    }

    private struct Id3D11ShaderReflection {
        public Id3D11ShaderReflectionVtbl* LpVtbl;
    }

    private struct Id3D11ShaderReflectionVtbl {
        public void* QueryInterface;
        public void* AddRef;
        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflection*, uint> Release;
        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflection*, ShaderDesc*, int> GetDesc;

        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflection*, uint, Id3D11ShaderReflectionConstantBuffer*
            > GetConstantBufferByIndex;

        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflection*, byte*, Id3D11ShaderReflectionConstantBuffer
            *> GetConstantBufferByName;

        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflection*, uint, ShaderInputBindDesc*, int>
            GetResourceBindingDesc;
    }

    private struct Id3D11ShaderReflectionConstantBuffer {
        public Id3D11ShaderReflectionConstantBufferVtbl* LpVtbl;
    }

    private struct Id3D11ShaderReflectionConstantBufferVtbl {
        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflectionConstantBuffer*, ShaderBufferDesc*, int>
            GetDesc;

        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflectionConstantBuffer*, uint,
            Id3D11ShaderReflectionVariable*> GetVariableByIndex;

        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflectionConstantBuffer*, byte*,
            Id3D11ShaderReflectionVariable*> GetVariableByName;
    }

    private struct Id3D11ShaderReflectionVariable {
        public Id3D11ShaderReflectionVariableVtbl* LpVtbl;
    }

    private struct Id3D11ShaderReflectionVariableVtbl {
        public delegate* unmanaged[Stdcall]<Id3D11ShaderReflectionVariable*, ShaderVariableDesc*, int> GetDesc;
    }

    private struct ShaderInputBindDesc {
        public byte* Name;
        public ShaderInputType Type;
        public uint BindPoint;
        public uint BindCount;
        public uint Flags;
        public uint ReturnType;
        public uint Dimension;
        public uint NumSamples;
    }

    private struct ShaderBufferDesc {
        public byte* Name;
        public uint Type;
        public uint Variables;
        public uint Size;
        public uint Flags;
    }

    private struct ShaderVariableDesc {
        public byte* Name;
        public uint StartOffset;
        public uint Size;
        public uint Flags;
        public void* DefaultValue;
        public uint StartTexture;
        public uint TextureSize;
        public uint StartSampler;
        public uint SamplerSize;
    }

    private struct ShaderDesc {
        public uint Version;
        public byte* Creator;
        public uint Flags;
        public uint ConstantBuffers;
        public uint BoundResources;
        public uint InputParameters;
        public uint OutputParameters;
        public uint InstructionCount;
        public uint TempRegisterCount;
        public uint TempArrayCount;
        public uint DefCount;
        public uint DclCount;
        public uint TextureNormalInstructions;
        public uint TextureLoadInstructions;
        public uint TextureCompInstructions;
        public uint TextureBiasInstructions;
        public uint TextureGradientInstructions;
        public uint FloatInstructionCount;
        public uint IntInstructionCount;
        public uint UintInstructionCount;
        public uint StaticFlowControlCount;
        public uint DynamicFlowControlCount;
        public uint MacroInstructionCount;
        public uint ArrayInstructionCount;
        public uint CutInstructionCount;
        public uint EmitInstructionCount;
        public uint GsOutputTopology;
        public uint GsMaxOutputVertexCount;
        public uint InputPrimitive;
        public uint PatchConstantParameters;
        public uint GsInstanceCount;
        public uint ControlPoints;
        public uint HsOutputPrimitive;
        public uint HsPartitioning;
        public uint TessellatorDomain;
        public uint BarrierInstructions;
        public uint InterlockedInstructions;
        public uint TextureStoreInstructions;
    }
}
