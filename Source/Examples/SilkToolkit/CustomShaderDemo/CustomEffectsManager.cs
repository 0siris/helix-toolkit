using System;
using System.Collections.Generic;
using System.IO;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.Wpf.SharpDX;

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
public static class CustomVSShaderDescription {
    public static byte[] VSMeshDataSamplerByteCode => ShaderHelper.LoadShaderCode(@"Shaders\vsMeshDataSampling.cso");

    public static ShaderDescription VSDataSampling = new ShaderDescription(nameof(VSDataSampling),
                                                                           ShaderStage.Vertex,
                                                                           new ShaderReflector(),
                                                                           VSMeshDataSamplerByteCode);
}

/// <summary>
/// Build using Nuget Micorsoft.HLSL.Microsoft.HLSL.CSharpVB automatically during project build
/// </summary>
public static class CustomPSShaderDescription {
    public static ShaderDescription PSDataSampling = new ShaderDescription(nameof(PSDataSampling),
                                                                           ShaderStage.Pixel,
                                                                           new ShaderReflector(),
                                                                           ShaderHelper.LoadShaderCode(
                                                                               @"Shaders\psMeshDataSampling.cso"));

    public static ShaderDescription PSNoiseMesh = new ShaderDescription(nameof(PSNoiseMesh),
                                                                        ShaderStage.Pixel,
                                                                        new ShaderReflector(),
                                                                        ShaderHelper.LoadShaderCode(
                                                                            @"Shaders\psMeshNoiseBlinnPhong.cso"));


    public static ShaderDescription PSCustomPoint = new ShaderDescription(nameof(PSCustomPoint),
                                                                          ShaderStage.Pixel,
                                                                          new ShaderReflector(),
                                                                          ShaderHelper.LoadShaderCode(
                                                                              @"Shaders\psCustomPoint.cso"));
}

public class CustomEffectsManager : DefaultEffectsManager {
    public CustomEffectsManager() {
        LoadCustomTechniqueDescriptions();
    }


    private void LoadCustomTechniqueDescriptions() {
        var dataSampling = new TechniqueDescription(CustomShaderNames.DataSampling) {
            InputLayoutDescription =
                new InputLayoutDescription(CustomVSShaderDescription.VSMeshDataSamplerByteCode,
                                           DefaultInputLayout.VsInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        CustomVSShaderDescription.VSDataSampling,
                        //DefaultVSShaderDescriptions.VSMeshDefault,
                        CustomPSShaderDescription.PSDataSampling
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        CustomVSShaderDescription.VSDataSampling,
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
                        CustomPSShaderDescription.PSNoiseMesh
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
                CustomPSShaderDescription.PSCustomPoint
            ],
            BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
            DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
        });
    }
}
