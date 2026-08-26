using System;
using System.IO;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace CustomShaderDemo;

public static class CustomShaderNames {
    public static readonly string DataSampling = "DataSampling";
    public static readonly string NoiseMesh = "NoiseMesh";
    public static readonly string TexData = "texData";
    public static readonly string TexDataSampler = "texDataSampler";
}

public static class ShaderHelper {
    public static byte[] LoadShaderCode(string path) {
        if (File.Exists(path)) {
            return File.ReadAllBytes(path);
        } else {
            throw new ArgumentException($"Shader File not found: {path}");
        }
    }
}

/// <summary>
/// Build using Nuget Micorsoft.HLSL.Microsoft.HLSL.CSharpVB automatically during project build
/// </summary>
public static class CustomVsShaderDescription {
    public static byte[] VsMeshDataSamplerByteCode =>
        ShaderHelper.LoadShaderCode(@"DX12\Shaders\vsMeshDataSampling.main.dxil");

    public static ShaderDescription VsDataSampling = new(nameof(VsDataSampling),
                                                                           ShaderStage.Vertex,
                                                                           new ShaderReflector(),
                                                                           VsMeshDataSamplerByteCode);
}

/// <summary>
/// Build using Nuget Micorsoft.HLSL.Microsoft.HLSL.CSharpVB automatically during project build
/// </summary>
public static class CustomPsShaderDescription {
    public static ShaderDescription PsDataSampling = new(nameof(PsDataSampling),
                                                                           ShaderStage.Pixel,
                                                                           new ShaderReflector(),
                                                                           ShaderHelper.LoadShaderCode(
                                                                               @"DX12\Shaders\psMeshDataSampling.main.dxil"));

    public static ShaderDescription PsNoiseMesh = new(nameof(PsNoiseMesh),
                                                                        ShaderStage.Pixel,
                                                                        new ShaderReflector(),
                                                                        ShaderHelper.LoadShaderCode(
                                                                            @"DX12\Shaders\psMeshNoiseBlinnPhong.main.dxil"));


    public static ShaderDescription PsCustomPoint = new(nameof(PsCustomPoint),
                                                                          ShaderStage.Pixel,
                                                                          new ShaderReflector(),
                                                                          ShaderHelper.LoadShaderCode(
                                                                              @"DX12\Shaders\psCustomPoint.main.dxil"));
}

public class CustomEffectsManager : DefaultEffectsManager {
    public CustomEffectsManager() {
        LoadCustomTechniqueDescriptions();
    }


    private void LoadCustomTechniqueDescriptions() {
        var dataSampling = new TechniqueDescription(CustomShaderNames.DataSampling) {
            InputLayoutDescription =
                new InputLayoutDescription(CustomVsShaderDescription.VsMeshDataSamplerByteCode,
                                           DefaultInputLayout.VsInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        CustomVsShaderDescription.VsDataSampling,
                        //DefaultVSShaderDescriptions.VSMeshDefault,
                        CustomPsShaderDescription.PsDataSampling
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        CustomVsShaderDescription.VsDataSampling,
                        DefaultPsShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                }
            ]
        };
        var noiseMesh = new TechniqueDescription(CustomShaderNames.NoiseMesh) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsMeshDefault, DefaultInputLayout.VsInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        CustomPsShaderDescription.PsNoiseMesh
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                }
            ]
        };

        AddTechnique(dataSampling);
        AddTechnique(noiseMesh);

        var points = GetTechnique(DefaultRenderTechniqueNames.Points);
        points.AddPass(new ShaderPassDescription("CustomPointPass") {
            ShaderList = [
                DefaultVsShaderDescriptions.VsPoint,
                DefaultGsShaderDescriptions.GsPoint,
                CustomPsShaderDescription.PsCustomPoint
            ],
            BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
            DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
        });
    }
}
