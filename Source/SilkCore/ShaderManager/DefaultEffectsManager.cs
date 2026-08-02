/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Shaders;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
///     Default shader technique manager, includes all internal shaders
/// </summary>
public class DefaultEffectsManager : EffectsManager {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultEffectsManager" /> class.
    /// </summary>
    /// <param name="adapterIndex">Index of the adapter.</param>
    public DefaultEffectsManager(int adapterIndex) : base(adapterIndex) {
        AddDefaultTechniques();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultEffectsManager" /> class.
    /// </summary>
    public DefaultEffectsManager() {
        AddDefaultTechniques();
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DefaultEffectsManager" /> class.
    /// </summary>
    /// <param name="configuration"></param>
    public DefaultEffectsManager(EffectsManagerConfiguration configuration) : base(configuration) {
        AddDefaultTechniques();
    }


    private void AddDefaultTechniques() {
        foreach (var technique in LoadTechniqueDescriptions()) AddTechnique(technique);
    }

    /// <summary>
    ///     Loads the technique descriptions.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<TechniqueDescription> LoadTechniqueDescriptions() {
        var renderMesh = new TechniqueDescription(DefaultRenderTechniqueNames.Mesh) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSMeshDefault, DefaultInputLayout.VSInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.PBR) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshPBR
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ViewCube) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshViewCube
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultGSShaderDescriptions.GSMeshNormalVector,
                        DefaultPSShaderDescriptions.PSLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshOITDPInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshPBROIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshPBROITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOIT) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOITDP) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBoneSkinnedBasic,
                        DefaultGSShaderDescriptions.GSMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVSShaderByteCodes.VSMeshBoneSkinningBasic,
                        DefaultInputLayout.VSInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDepth,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.MeshSSAOPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshSSAO,
                        DefaultPSShaderDescriptions.PSSSAOP1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellation) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOIT) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOITDP) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPBRTriTessellation) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshPBR
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPBRTriTessellationOIT) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshPBROIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPBRTriTessellationOITDP) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshPBROITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshShadow,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframeOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframeOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDefault,
                        DefaultPSShaderDescriptions.PSEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderMeshBatched = new TechniqueDescription(DefaultRenderTechniqueNames.MeshBatched) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VSMeshBatched,
                                                                DefaultInputLayout.VSMeshBatchedInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.PBR) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshPBR
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultGSShaderDescriptions.GSMeshNormalVector,
                        DefaultPSShaderDescriptions.PSLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshPBROIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshPBROITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshOITDPInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBoneSkinnedBasic,
                        DefaultGSShaderDescriptions.GSMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVSShaderByteCodes.VSMeshBoneSkinningBasic,
                        DefaultInputLayout.VSInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOIT) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOITDP) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDepth,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.MeshSSAOPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedSSAO,
                        DefaultPSShaderDescriptions.PSSSAOP1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedShadow,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframeOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframeOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBatched,
                        DefaultPSShaderDescriptions.PSEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderMeshInstancing = new TechniqueDescription(DefaultRenderTechniqueNames.InstancingMesh) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VSMeshInstancing,
                                                                DefaultInputLayout.VSInputInstancing),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.PBR) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshPBR
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultGSShaderDescriptions.GSMeshNormalVector,
                        DefaultPSShaderDescriptions.PSLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshBoneSkinnedBasic,
                        DefaultGSShaderDescriptions.GSMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVSShaderByteCodes.VSMeshBoneSkinningBasic,
                        DefaultInputLayout.VSInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.OITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshPBROIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOIT) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshOITDPInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshPBROITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOITDP) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDepth,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.MeshSSAOPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshSSAO,
                        DefaultPSShaderDescriptions.PSSSAOP1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellation) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOIT) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOITDP) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HSMeshTessellation,
                        DefaultDomainShaderDescriptions.DSMeshTessellation,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshShadow,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframeOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshWireframeOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshWireframe,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshInstancing,
                        DefaultPSShaderDescriptions.PSEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderPoint = new TechniqueDescription(DefaultRenderTechniqueNames.Points) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSPoint, DefaultInputLayout.VSInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSPoint,
                        DefaultPSShaderDescriptions.PSPoint
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSPoint,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPointShadow,
                        DefaultGSShaderDescriptions.GSPoint,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSPoint,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                }
            ]
        };

        var renderLine = new TechniqueDescription(DefaultRenderTechniqueNames.Lines) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSPoint, DefaultInputLayout.VSInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPointShadow,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLine,
                        DefaultPSShaderDescriptions.PSLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderLineArrowHead = new TechniqueDescription(DefaultRenderTechniqueNames.LinesArrowHead) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSPoint, DefaultInputLayout.VSInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPointShadow,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHead,
                        DefaultPSShaderDescriptions.PSLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderLineArrowHeadTail = new TechniqueDescription(DefaultRenderTechniqueNames.LinesArrowHeadTail) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSPoint, DefaultInputLayout.VSInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPointShadow,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPoint,
                        DefaultGSShaderDescriptions.GSLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PSLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderBillboardText = new TechniqueDescription(DefaultRenderTechniqueNames.BillboardText) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VSBillboard,
                                                                DefaultInputLayout.VSInputBillboard),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardText,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSBillboardText
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.OITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardText,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSBillboardTextOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardText,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSMeshOITDPInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardText,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSBillboardTextOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                }
            ]
        };

        var renderBillboardInstancing = new TechniqueDescription(DefaultRenderTechniqueNames.BillboardInstancing) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VSBillboardInstancing,
                                                                DefaultInputLayout.VSInputBillboardInstancing),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardInstancing,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSBillboardText
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.OITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardInstancing,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSBillboardTextOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardInstancing,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSMeshOITDPInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSBillboardInstancing,
                        DefaultGSShaderDescriptions.GSBillboard,
                        DefaultPSShaderDescriptions.PSBillboardTextOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                }
            ]
        };

        var renderMeshBlinnClipPlane = new TechniqueDescription(DefaultRenderTechniqueNames.CrossSection) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSMeshDefault, DefaultInputLayout.VSInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.PBR) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshPBR
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultGSShaderDescriptions.GSMeshNormalVector,
                        DefaultPSShaderDescriptions.PSLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshPBROIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOIT) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshOITDPInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PBROITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshPBROITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOITDP) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshDiffuseMapOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshDepth,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Backface) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshClipBackface
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSClipPlaneBackface,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSFullScreenQuad,
                        DefaultPSShaderDescriptions.PSMeshClipScreenQuad
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSClipPlaneFillQuad,
                    StencilRef = 1,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshWireframeOIT
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOITDPPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshWireframeOITDP
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshClipPlane,
                        DefaultPSShaderDescriptions.PSEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderParticle = new TechniqueDescription(DefaultRenderTechniqueNames.ParticleStorm) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSParticle, DefaultInputLayout.VSInputParticle),
            PassDescriptions = [
                new ShaderPassDescription(DefaultParticlePassNames.Insert) {
                    ShaderList = [
                        DefaultComputeShaderDescriptions.CSParticleInsert
                    ]
                },
                new ShaderPassDescription(DefaultParticlePassNames.Update) {
                    ShaderList = [
                        DefaultComputeShaderDescriptions.CSParticleUpdate
                    ]
                },
                new ShaderPassDescription(DefaultParticlePassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSParticle,
                        DefaultGSShaderDescriptions.GSParticle,
                        DefaultPSShaderDescriptions.PSParticle
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    RasterStateDescription = DefaultRasterDescriptions.RSSolidNoMSAA,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OITPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSParticle,
                        DefaultGSShaderDescriptions.GSParticle,
                        DefaultPSShaderDescriptions.PSParticleOIT
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITBlend,
                    RasterStateDescription = DefaultRasterDescriptions.RSSolidNoMSAA
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSParticle,
                        DefaultGSShaderDescriptions.GSParticle,
                        DefaultPSShaderDescriptions.PSMeshOITDPInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OITDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSParticle,
                        DefaultGSShaderDescriptions.GSParticle,
                        DefaultPSShaderDescriptions.PSParticleOITDP
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDP,
                    RasterStateDescription = DefaultRasterDescriptions.RSSolidNoMSAA
                }
            ]
        };

        var renderSkybox = new TechniqueDescription(DefaultRenderTechniqueNames.Skybox) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSSkybox, DefaultInputLayout.VSInputSkybox),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSSkybox,
                        DefaultPSShaderDescriptions.PSSkybox
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessEqualNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RSSkybox
                }
            ]
        };

        var meshOITQuad = new TechniqueDescription(DefaultRenderTechniqueNames.MeshOITQuad) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSMeshBlinnPhongOITQuad
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSMeshOITBlendQuad,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var meshOITDepthPeeling = new TechniqueDescription(DefaultRenderTechniqueNames.MeshOITDepthPeeling) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.OITDepthPeelingFinal) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSMeshOITDPFinal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSOITDPFinal,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSSkybox,
                    StencilRef = 0,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        #region Post Effects

        var meshOutlineBlurPostEffect = new TechniqueDescription(DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSMeshOutlineScreenQuad
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSOutlineFillQuad,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectFullScreenBlurVertical
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectFullScreenBlurHorizontal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadFinal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSGlowBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var meshBorderHighlightPostEffect =
            new TechniqueDescription(DefaultRenderTechniqueNames.PostEffectMeshBorderHighlight) {
                InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
                PassDescriptions = [
                    new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PSMeshOutlineScreenQuad
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSOutlineFillQuad,
                        RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PSEffectMeshBorderHighlight
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PSEffectMeshBorderHighlight
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PSMeshOutlineQuadFinal
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    }
                ]
            };

        var bloomPostEffect = new TechniqueDescription(DefaultRenderTechniqueNames.PostEffectBloom) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectBloomExtract
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.ScreenQuadCopy) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSMeshOutlineQuadFinal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectBloomVerticalBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectBloomHorizontalBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectBloomCombine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.AdditiveBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var fxaaPostEffect = new TechniqueDescription(DefaultRenderTechniqueNames.PostEffectFXAA) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.LumaPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectLUMA
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.FXAAPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSEffectFXAA
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        #endregion

        var planeGrid = new TechniqueDescription(DefaultRenderTechniqueNames.PlaneGrid) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSPlaneGrid,
                        DefaultPSShaderDescriptions.PSPlaneGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSLessNoWrite,
                    RasterStateDescription = DefaultRasterDescriptions.RSPlaneGrid,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var screenQuad = new TechniqueDescription(DefaultRenderTechniqueNames.ScreenQuad) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSScreenQuad,
                        DefaultPSShaderDescriptions.PSScreenDup
                    ],
                    Topology = PrimitiveTopology.TriangleStrip,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSDepthLessEqual,
                    RasterStateDescription = DefaultRasterDescriptions.RSSkybox
                }
            ]
        };

        var sprite2D = new TechniqueDescription(DefaultRenderTechniqueNames.Sprite2D) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSSprite2D, DefaultInputLayout.VSInputSprite2D),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSSprite2D,
                        DefaultPSShaderDescriptions.PSSprite2D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSSpriteCW
                }
            ]
        };

        var volume3D = new TechniqueDescription(DefaultRenderTechniqueNames.Volume3D) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VSVolume3D, DefaultInputLayout.VSInputVolume3D),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSVolume3D,
                        DefaultPSShaderDescriptions.PSVolume3D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSVolumeCubeBack
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSVolume3D,
                        DefaultPSShaderDescriptions.PSVolumeDiffuse3D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSVolumeCubeBack
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSVolume3D,
                        DefaultPSShaderDescriptions.PSVolumeCube
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSVolumeFrontFace,
                    RasterStateDescription = DefaultRasterDescriptions.RSVolumeCubeFront
                },
                new ShaderPassDescription(DefaultPassNames.Backface) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSVolume3D,
                        DefaultPSShaderDescriptions.PSVolumeCube
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSVolumeBackFace,
                    RasterStateDescription = DefaultRasterDescriptions.RSVolumeCubeBack,
                    StencilRef = 1
                }
            ]
        };

        var ssao = new TechniqueDescription(DefaultRenderTechniqueNames.SSAO) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSSSAO,
                        DefaultPSShaderDescriptions.PSSSAO
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VSMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PSSSAOBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BSSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DSSNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RSOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        yield return renderMesh;
        yield return renderMeshBatched;
        yield return renderMeshInstancing;
        yield return renderPoint;
        yield return renderLine;
        yield return renderLineArrowHead;
        yield return renderLineArrowHeadTail;
        yield return renderBillboardText;
        yield return renderBillboardInstancing;
        yield return renderMeshBlinnClipPlane;
        yield return renderParticle;
        yield return renderSkybox;
        yield return meshOutlineBlurPostEffect;
        yield return meshBorderHighlightPostEffect;
        yield return bloomPostEffect;
        yield return fxaaPostEffect;
        yield return meshOITQuad;
        yield return planeGrid;
        yield return screenQuad;
        yield return sprite2D;
        yield return volume3D;
        yield return ssao;
        yield return meshOITDepthPeeling;
    }
}
