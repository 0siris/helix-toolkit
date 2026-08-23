/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.DefaultShaders;
/// <summary>
/// </summary>
public static class DefaultPsShaderByteCodes {
    /// <summary>
    /// </summary>
    public static string PsMeshBinnPhong { get; } = "psMeshBlinnPhong";

    /// <summary>
    ///     Gets the ps mesh binn phong order independent transparent shader.
    /// </summary>
    /// <value>
    ///     The ps mesh binn phong order independent transparent shader.
    /// </value>
    public static string PsMeshBinnPhongOit { get; } = "psMeshBlinnPhongOIT";

    /// <summary>
    ///     Gets the ps mesh binn phong oit quad.
    /// </summary>
    /// <value>
    ///     The ps mesh binn phong oit quad.
    /// </value>
    public static string PsMeshBinnPhongOitQuad { get; } = "psMeshBlinnPhongOITQuad";

    public static string PsMeshOitdpFirst { get; } = "psMeshOITDPFirst";

    public static string PsMeshBlinnPhongOitdp { get; } = "psMeshBlinnPhongOITDP";

    public static string PsMeshOitdpFinal { get; } = "psMeshOITDPFinal";

    /// <summary>
    ///     Gets the ps mesh diffuse map oit.
    /// </summary>
    /// <value>
    ///     The ps mesh diffuse map oit.
    /// </value>
    public static string PsMeshDiffuseMapOit { get; } = "psMeshDiffuseMapOIT";

    public static string PsMeshDiffuseMapOitdp { get; } = "psMeshDiffuseMapOITDP";

    /// <summary>
    /// </summary>
    public static string PsMeshVertColor { get; } = "psColor";

    /// <summary>
    /// </summary>
    public static string PsMeshVertPosition { get; } = "psPositions";

    /// <summary>
    /// </summary>
    public static string PsMeshNormal { get; } = "psNormals";

    public static string PsMeshDiffuseMap { get; } = "psDiffuseMap";

    public static string PsMeshColorStripe { get; } = "psMeshColorStripe";

    public static string PsMeshViewCube { get; } = "psViewCube";

    /// <summary>
    /// </summary>
    public static string PsShadow { get; } = "psShadow";

    /// <summary>
    /// </summary>
    public static string PsPoint { get; } = "psPoint";

    /// <summary>
    /// </summary>
    public static string PsLine { get; } = "psLine";

    /// <summary>
    /// </summary>
    public static string PsLineColor { get; } = "psLineColor";

    /// <summary>
    /// </summary>
    public static string PsBillboardText { get; } = "psBillboardText";

    /// <summary>
    ///     Gets the ps billboard text order independent transparent shader.
    /// </summary>
    /// <value>
    ///     The ps billboard text order independent transparent shader.
    /// </value>
    public static string PsBillboardTextOit { get; } = "psBillboardTextOIT";

    public static string PsBillboardTextOitdp { get; } = "psBillboardTextOITDP";

    /// <summary>
    /// </summary>
    public static string PsMeshXRay { get; } = "psMeshXRay";

    /// <summary>
    /// </summary>
    public static string PsMeshClipPlaneBackface { get; } = "psMeshClipPlaneBackface";

    /// <summary>
    /// </summary>
    public static string PsMeshClipPlaneQuad { get; } = "psMeshClipPlaneQuad";

    /// <summary>
    /// </summary>
    public static string PsParticle { get; } = "psParticle";

    /// <summary>
    ///     Gets the ps particle order independent transparent shader.
    /// </summary>
    public static string PsParticleOit { get; } = "psParticleOIT";

    public static string PsParticleOitdp { get; } = "psParticleOITDP";

    /// <summary>
    /// </summary>
    public static string PsSkybox { get; } = "psSkybox";

    /// <summary>
    /// </summary>
    public static string PsMeshWireframe { get; } = "psWireframe";

    /// <summary>
    ///     Gets the ps mesh wireframe oit.
    /// </summary>
    /// <value>
    ///     The ps mesh wireframe oit.
    /// </value>
    public static string PsMeshWireframeOit { get; } = "psWireframeOIT";

    public static string PsMeshWireframeOitdp { get; } = "psWireframeOITDP";

    /// <summary>
    /// </summary>
    public static string PsDepthStencilTestOnly { get; } = "psDepthStencilOnly";

    /// <summary>
    ///     Gets the ps mesh outline screen quad.
    /// </summary>
    /// <value>
    ///     The ps mesh outline screen quad.
    /// </value>
    public static string PsEffectOutlineScreenQuad { get; } = "psEffectOutlineQuad";

    /// <summary>
    ///     Gets the ps effect full screen blur vertical.
    /// </summary>
    /// <value>
    ///     The ps effect full screen blur vertical.
    /// </value>
    public static string PsEffectFullScreenBlurVertical { get; } = "psEffectGaussianBlurVertical";

    /// <summary>
    ///     Gets the ps effect full screen blur horizontal.
    /// </summary>
    /// <value>
    ///     The ps effect full screen blur horizontal.
    /// </value>
    public static string PsEffectFullScreenBlurHorizontal { get; } = "psEffectGaussianBlurHorizontal";

    /// <summary>
    ///     Gets the ps mesh border highlight
    /// </summary>
    /// <value>
    ///     The ps mesh mesh border highlight
    /// </value>
    public static string PsEffectMeshBorderHighlight { get; } = "psEffectMeshBorderHighlight";

    /// <summary>
    ///     Gets the ps effect outline smooth.
    /// </summary>
    /// <value>
    ///     The ps effect outline smooth.
    /// </value>
    public static string PsEffectOutlineSmooth { get; } = "psEffectOutlineSmooth";

    /// <summary>
    ///     Gets the ps mesh outline screen quad stencil.
    /// </summary>
    /// <value>
    ///     The ps mesh outline screen quad stencil.
    /// </value>
    public static string PsEffectOutlineScreenQuadStencil { get; } = "psEffectOutlineQuadStencil";

    /// <summary>
    ///     Gets the ps mesh outline quad final.
    /// </summary>
    /// <value>
    ///     The ps mesh outline quad final.
    /// </value>
    public static string PsEffectOutlineQuadFinal { get; } = "psEffectOutlineQualFinal";

    /// <summary>
    /// </summary>
    /// <value>
    /// </value>
    public static string PsEffectMeshXRay { get; } = "psEffectMeshXRay";

    /// <summary>
    ///     Gets the ps effect bloom extract.
    /// </summary>
    /// <value>
    ///     The ps effect bloom extract.
    /// </value>
    public static string PsEffectBloomExtract { get; } = "psEffectBloomExtract";

    /// <summary>
    ///     Gets the ps effect bloom vertical blur.
    /// </summary>
    /// <value>
    ///     The ps effect bloom vertical blur.
    /// </value>
    public static string PsEffectBloomVerticalBlur { get; } = "psEffectBloomBlurVertical";

    /// <summary>
    ///     Gets the ps effect bloom horizontal blur.
    /// </summary>
    /// <value>
    ///     The ps effect bloom horizontal blur.
    /// </value>
    public static string PsEffectBloomHorizontalBlur { get; } = "psEffectBloomBlurHorizontal";

    /// <summary>
    ///     Gets the ps effect bloom combine.
    /// </summary>
    /// <value>
    ///     The ps effect bloom combine.
    /// </value>
    public static string PsEffectBloomCombine { get; } = "psEffectBloomCombine";

    /// <summary>
    ///     Gets the ps effect fxaa.
    /// </summary>
    /// <value>
    ///     The ps effect fxaa.
    /// </value>
    public static string PsEffectFxaa { get; } = "psFXAA";

    /// <summary>
    ///     Gets the ps effect luma.
    /// </summary>
    /// <value>
    ///     The ps effect luma.
    /// </value>
    public static string PsEffectLuma { get; } = "psLuma";

    /// <summary>
    ///     Gets the ps effect x ray grid. This is based on BlinnPhong
    /// </summary>
    /// <value>
    ///     The ps effect x ray grid.
    /// </value>
    public static string PsEffectXRayGrid { get; } = "psEffectMeshXRayGrid";

    /// <summary>
    ///     Gets the ps effect diffuse x ray grid.
    /// </summary>
    /// <value>
    ///     The ps effect diffuse x ray grid.
    /// </value>
    public static string PsEffectDiffuseXRayGrid { get; } = "psEffectMeshDiffuseXRayGrid";

    /// <summary>
    ///     Gets the ps plane grid.
    /// </summary>
    /// <value>
    ///     The ps plane grid.
    /// </value>
    public static string PsPlaneGrid { get; } = "psPlaneGrid";

    /// <summary>
    ///     Gets the ps mesh PBR.
    /// </summary>
    /// <value>
    ///     The ps mesh PBR.
    /// </value>
    public static string PsMeshPbr { get; } = "psMeshPBR";

    /// <summary>
    ///     Gets the ps mesh PBR OIT.
    /// </summary>
    /// <value>
    ///     The ps mesh PBR.
    /// </value>
    public static string PsMeshPbroit { get; } = "psMeshPBROIT";

    public static string PsMeshPbroitdp { get; } = "psMeshPBROITDP";

    /// <summary>
    ///     Gets the ps sprite.
    /// </summary>
    /// <value>
    ///     The ps sprite.
    /// </value>
    public static string PsSprite2D { get; } = "psSprite";


    /// <summary>
    /// </summary>
    public static string PsScreenDup { get; } = "psScreenDup";

    public static string PsVolume3D { get; } = "psVolume";

    public static string PsVolumeCube { get; } = "psVolumeCube";

    public static string PsVolumeDiffuse { get; } = "psVolumeDiffuse";

    public static string Psssaop1 { get; } = "psSSAOP1";

    public static string Psssao { get; } = "psSSAO";

    public static string PsssaoBlur { get; } = "psSSAOBlur";
}


/// <summary>
///     Default Pixel Shaders
/// </summary>
public static class DefaultPsShaderDescriptions {
    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshBlinnPhong = new(nameof(PsMeshBlinnPhong),
                                                                    ShaderStage.Pixel,
                                                                    new ShaderReflector(),
                                                                    DefaultPsShaderByteCodes.PsMeshBinnPhong);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshBlinnPhongOit = new(nameof(PsMeshBlinnPhongOit),
                                                                       ShaderStage.Pixel,
                                                                       new ShaderReflector(),
                                                                       DefaultPsShaderByteCodes
                                                                           .PsMeshBinnPhongOit);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshBlinnPhongOitQuad = new(nameof(PsMeshBlinnPhongOitQuad),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsMeshBinnPhongOitQuad);

    public static readonly ShaderDescription PsMeshOitdpInit = new(nameof(PsMeshOitdpInit),
                                                                   ShaderStage.Pixel,
                                                                   new ShaderReflector(),
                                                                   DefaultPsShaderByteCodes.PsMeshOitdpFirst);

    public static readonly ShaderDescription PsMeshBlinnPhongOitdp = new(nameof(PsMeshBlinnPhongOitdp),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsMeshBlinnPhongOitdp);

    public static readonly ShaderDescription PsMeshOitdpFinal = new(nameof(PsMeshOitdpFinal),
                                                                    ShaderStage.Pixel,
                                                                    new ShaderReflector(),
                                                                    DefaultPsShaderByteCodes.PsMeshOitdpFinal);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshVertColor = new(nameof(PsMeshVertColor),
                                                                   ShaderStage.Pixel,
                                                                   new ShaderReflector(),
                                                                   DefaultPsShaderByteCodes.PsMeshVertColor);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshVertNormal = new(nameof(PsMeshVertNormal),
                                                                    ShaderStage.Pixel,
                                                                    new ShaderReflector(),
                                                                    DefaultPsShaderByteCodes.PsMeshNormal);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshVertPosition = new(nameof(PsMeshVertPosition),
                                                                      ShaderStage.Pixel,
                                                                      new ShaderReflector(),
                                                                      DefaultPsShaderByteCodes
                                                                          .PsMeshVertPosition);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshDiffuseMap = new(nameof(PsMeshDiffuseMap),
                                                                    ShaderStage.Pixel,
                                                                    new ShaderReflector(),
                                                                    DefaultPsShaderByteCodes.PsMeshDiffuseMap);

    /// <summary>
    ///     The ps mesh diffuse map oit
    /// </summary>
    public static readonly ShaderDescription PsMeshDiffuseMapOit = new(nameof(PsMeshDiffuseMapOit),
                                                                       ShaderStage.Pixel,
                                                                       new ShaderReflector(),
                                                                       DefaultPsShaderByteCodes
                                                                           .PsMeshDiffuseMapOit);

    public static readonly ShaderDescription PsMeshDiffuseMapOitdp = new(nameof(PsMeshDiffuseMapOitdp),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsMeshDiffuseMapOitdp);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshColorStripe = new(nameof(PsMeshColorStripe),
                                                                     ShaderStage.Pixel,
                                                                     new ShaderReflector(),
                                                                     DefaultPsShaderByteCodes
                                                                         .PsMeshColorStripe);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshViewCube = new(nameof(PsMeshViewCube),
                                                                  ShaderStage.Pixel,
                                                                  new ShaderReflector(),
                                                                  DefaultPsShaderByteCodes.PsMeshViewCube);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsPoint = new(nameof(PsPoint),
                                                           ShaderStage.Pixel,
                                                           new ShaderReflector(),
                                                           DefaultPsShaderByteCodes.PsPoint);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsLine = new(nameof(PsLine),
                                                          ShaderStage.Pixel,
                                                          new ShaderReflector(),
                                                          DefaultPsShaderByteCodes.PsLine);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsLineColor = new(nameof(PsLineColor),
                                                               ShaderStage.Pixel,
                                                               new ShaderReflector(),
                                                               DefaultPsShaderByteCodes.PsLineColor);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsBillboardText = new(nameof(PsBillboardText),
                                                                   ShaderStage.Pixel,
                                                                   new ShaderReflector(),
                                                                   DefaultPsShaderByteCodes.PsBillboardText);

    /// <summary>
    ///     The ps billboard text oit
    /// </summary>
    public static readonly ShaderDescription PsBillboardTextOit = new(nameof(PsBillboardTextOit),
                                                                      ShaderStage.Pixel,
                                                                      new ShaderReflector(),
                                                                      DefaultPsShaderByteCodes
                                                                          .PsBillboardTextOit);

    public static readonly ShaderDescription PsBillboardTextOitdp = new(nameof(PsBillboardTextOitdp),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsBillboardTextOitdp);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshXRay = new(nameof(PsMeshXRay),
                                                              ShaderStage.Pixel,
                                                              new ShaderReflector(),
                                                              DefaultPsShaderByteCodes.PsMeshXRay);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsShadow = new(nameof(PsShadow),
                                                            ShaderStage.Pixel,
                                                            new ShaderReflector(),
                                                            DefaultPsShaderByteCodes.PsShadow);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsParticle = new(nameof(PsParticle),
                                                              ShaderStage.Pixel,
                                                              new ShaderReflector(),
                                                              DefaultPsShaderByteCodes.PsParticle);

    /// <summary>
    ///     The ps particle oit
    /// </summary>
    public static readonly ShaderDescription PsParticleOit = new(nameof(PsParticleOit),
                                                                 ShaderStage.Pixel,
                                                                 new ShaderReflector(),
                                                                 DefaultPsShaderByteCodes.PsParticleOit);

    public static readonly ShaderDescription PsParticleOitdp = new(nameof(PsParticleOitdp),
                                                                   ShaderStage.Pixel,
                                                                   new ShaderReflector(),
                                                                   DefaultPsShaderByteCodes.PsParticleOitdp);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsSkybox = new(nameof(PsSkybox),
                                                            ShaderStage.Pixel,
                                                            new ShaderReflector(),
                                                            DefaultPsShaderByteCodes.PsSkybox);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshWireframe = new(nameof(PsMeshWireframe),
                                                                   ShaderStage.Pixel,
                                                                   new ShaderReflector(),
                                                                   DefaultPsShaderByteCodes.PsMeshWireframe);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshWireframeOit = new(nameof(PsMeshWireframeOit),
                                                                      ShaderStage.Pixel,
                                                                      new ShaderReflector(),
                                                                      DefaultPsShaderByteCodes
                                                                          .PsMeshWireframeOit);

    public static readonly ShaderDescription PsMeshWireframeOitdp = new(nameof(PsMeshWireframeOitdp),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsMeshWireframeOitdp);

    /// <summary>
    ///     The ps depth stencil only
    /// </summary>
    public static readonly ShaderDescription PsDepthStencilOnly = new(nameof(PsDepthStencilOnly),
                                                                      ShaderStage.Pixel,
                                                                      new ShaderReflector(),
                                                                      DefaultPsShaderByteCodes
                                                                          .PsDepthStencilTestOnly);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshOutlineScreenQuad = new(nameof(PsMeshOutlineScreenQuad),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectOutlineScreenQuad);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsEffectFullScreenBlurVertical = new(
        nameof(PsEffectFullScreenBlurVertical),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectFullScreenBlurVertical);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsEffectFullScreenBlurHorizontal = new(
        nameof(PsEffectFullScreenBlurHorizontal),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectFullScreenBlurHorizontal);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsEffectMeshBorderHighlight = new(
        nameof(PsEffectMeshBorderHighlight),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectMeshBorderHighlight);

    /// <summary>
    ///     The ps effect mesh border highlight
    /// </summary>
    public static readonly ShaderDescription PsEffectOutlineSmooth = new(nameof(PsEffectOutlineSmooth),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectOutlineSmooth);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshOutlineQuadStencil = new(nameof(PsMeshOutlineQuadStencil),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectOutlineScreenQuadStencil);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshOutlineQuadFinal = new(nameof(PsMeshOutlineQuadFinal),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectOutlineQuadFinal);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsEffectMeshXRay = new(nameof(PsEffectMeshXRay),
                                                                    ShaderStage.Pixel,
                                                                    new ShaderReflector(),
                                                                    DefaultPsShaderByteCodes.PsEffectMeshXRay);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsEffectBloomExtract = new(nameof(PsEffectBloomExtract),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectBloomExtract);

    /// <summary>
    ///     The ps effect bloom vertical blur
    /// </summary>
    public static readonly ShaderDescription PsEffectBloomVerticalBlur = new(nameof(PsEffectBloomVerticalBlur),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectBloomVerticalBlur);

    /// <summary>
    ///     The ps effect bloom horizontal blur
    /// </summary>
    public static readonly ShaderDescription PsEffectBloomHorizontalBlur = new(
        nameof(PsEffectBloomHorizontalBlur),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectBloomHorizontalBlur);

    /// <summary>
    ///     The ps effect bloom combine
    /// </summary>
    public static readonly ShaderDescription PsEffectBloomCombine = new(nameof(PsEffectBloomCombine),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectBloomCombine);

    /// <summary>
    ///     The ps effect FXAA
    /// </summary>
    public static readonly ShaderDescription PsEffectFxaa = new(nameof(PsEffectFxaa),
                                                                ShaderStage.Pixel,
                                                                new ShaderReflector(),
                                                                DefaultPsShaderByteCodes.PsEffectFxaa);

    /// <summary>
    ///     The ps effect luma
    /// </summary>
    public static readonly ShaderDescription PsEffectLuma = new(nameof(PsEffectLuma),
                                                                ShaderStage.Pixel,
                                                                new ShaderReflector(),
                                                                DefaultPsShaderByteCodes.PsEffectLuma);

    /// <summary>
    ///     The ps effect x ray grid, this is based on BlinnPhong
    /// </summary>
    public static readonly ShaderDescription PsEffectXRayGrid = new(nameof(PsEffectXRayGrid),
                                                                    ShaderStage.Pixel,
                                                                    new ShaderReflector(),
                                                                    DefaultPsShaderByteCodes.PsEffectXRayGrid);

    /// <summary>
    ///     The ps effect x ray grid, this is based on diffuse shading
    /// </summary>
    public static readonly ShaderDescription PsEffectDiffuseXRayGrid = new(nameof(PsEffectDiffuseXRayGrid),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsEffectDiffuseXRayGrid);

    /// <summary>
    ///     The ps plane grid
    /// </summary>
    public static readonly ShaderDescription PsPlaneGrid = new(nameof(PsPlaneGrid),
                                                               ShaderStage.Pixel,
                                                               new ShaderReflector(),
                                                               DefaultPsShaderByteCodes.PsPlaneGrid);

    /// <summary>
    ///     The ps mesh PBR
    /// </summary>
    public static readonly ShaderDescription PsMeshPbr = new(nameof(PsMeshPbr),
                                                             ShaderStage.Pixel,
                                                             new ShaderReflector(),
                                                             DefaultPsShaderByteCodes.PsMeshPbr);

    /// <summary>
    ///     The ps mesh PBR
    /// </summary>
    public static readonly ShaderDescription PsMeshPbroit = new(nameof(PsMeshPbroit),
                                                                ShaderStage.Pixel,
                                                                new ShaderReflector(),
                                                                DefaultPsShaderByteCodes.PsMeshPbroit);

    public static readonly ShaderDescription PsMeshPbroitdp = new(nameof(PsMeshPbroitdp),
                                                                  ShaderStage.Pixel,
                                                                  new ShaderReflector(),
                                                                  DefaultPsShaderByteCodes.PsMeshPbroitdp);

    /// <summary>
    ///     The ps sprite
    /// </summary>
    public static readonly ShaderDescription PsSprite2D = new(nameof(PsSprite2D),
                                                              ShaderStage.Pixel,
                                                              new ShaderReflector(),
                                                              DefaultPsShaderByteCodes.PsSprite2D);

    /// <summary>
    ///     The ps screen dup
    /// </summary>
    public static readonly ShaderDescription PsScreenDup = new(nameof(PsScreenDup),
                                                               ShaderStage.Pixel,
                                                               new ShaderReflector(),
                                                               DefaultPsShaderByteCodes.PsScreenDup);

    /// <summary>
    ///     The ps volume3d
    /// </summary>
    public static readonly ShaderDescription PsVolume3D = new(nameof(PsVolume3D),
                                                              ShaderStage.Pixel,
                                                              new ShaderReflector(),
                                                              DefaultPsShaderByteCodes.PsVolume3D);

    /// <summary>
    ///     The ps volume cube
    /// </summary>
    public static readonly ShaderDescription PsVolumeCube = new(nameof(PsVolumeCube),
                                                                ShaderStage.Pixel,
                                                                new ShaderReflector(),
                                                                DefaultPsShaderByteCodes.PsVolumeCube);

    /// <summary>
    ///     The ps volume3d
    /// </summary>
    public static readonly ShaderDescription PsVolumeDiffuse3D = new(nameof(PsVolumeDiffuse3D),
                                                                     ShaderStage.Pixel,
                                                                     new ShaderReflector(),
                                                                     DefaultPsShaderByteCodes.PsVolumeDiffuse);

    /// <summary>
    ///     The psssao p1
    /// </summary>
    public static readonly ShaderDescription Psssaop1 = new(nameof(Psssaop1),
                                                            ShaderStage.Pixel,
                                                            new ShaderReflector(),
                                                            DefaultPsShaderByteCodes.Psssaop1);

    /// <summary>
    ///     The psssao
    /// </summary>
    public static readonly ShaderDescription Psssao = new(nameof(Psssao),
                                                          ShaderStage.Pixel,
                                                          new ShaderReflector(),
                                                          DefaultPsShaderByteCodes.Psssao);

    /// <summary>
    ///     The ps ssao blur
    /// </summary>
    public static readonly ShaderDescription PsssaoBlur = new(nameof(PsssaoBlur),
                                                              ShaderStage.Pixel,
                                                              new ShaderReflector(),
                                                              DefaultPsShaderByteCodes.PsssaoBlur);

    #region Mesh Clipping

    /// <summary>
    ///     /
    /// </summary>
    public static readonly ShaderDescription PsMeshClipBackface = new(nameof(PsMeshClipBackface),
                                                                      ShaderStage.Pixel,
                                                                      new ShaderReflector(),
                                                                      DefaultPsShaderByteCodes
                                                                          .PsMeshClipPlaneBackface);

    /// <summary>
    /// </summary>
    public static readonly ShaderDescription PsMeshClipScreenQuad = new(nameof(PsMeshClipScreenQuad),
        ShaderStage.Pixel,
        new ShaderReflector(),
        DefaultPsShaderByteCodes.PsMeshClipPlaneQuad);

    #endregion
}
