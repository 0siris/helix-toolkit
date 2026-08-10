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
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsMeshDefault, DefaultInputLayout.VsInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ViewCube) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshViewCube
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultGsShaderDescriptions.GsMeshNormalVector,
                        DefaultPsShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBoneSkinnedBasic,
                        DefaultGsShaderDescriptions.GsMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVsShaderByteCodes.VsMeshBoneSkinningBasic,
                        DefaultInputLayout.VsInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDepth,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.MeshSsaoPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshSsao,
                        DefaultPsShaderDescriptions.Psssaop1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellation) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOitdp) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPbrTriTessellation) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPbrTriTessellationOit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPbrTriTessellationOitdp) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshShadow,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDefault,
                        DefaultPsShaderDescriptions.PsEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderMeshBatched = new TechniqueDescription(DefaultRenderTechniqueNames.MeshBatched) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVsShaderByteCodes.VsMeshBatched,
                                                                DefaultInputLayout.VsMeshBatchedInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultGsShaderDescriptions.GsMeshNormalVector,
                        DefaultPsShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBoneSkinnedBasic,
                        DefaultGsShaderDescriptions.GsMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVsShaderByteCodes.VsMeshBoneSkinningBasic,
                        DefaultInputLayout.VsInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDepth,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.MeshSsaoPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedSsao,
                        DefaultPsShaderDescriptions.Psssaop1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedShadow,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBatched,
                        DefaultPsShaderDescriptions.PsEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderMeshInstancing = new TechniqueDescription(DefaultRenderTechniqueNames.InstancingMesh) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVsShaderByteCodes.VsMeshInstancing,
                                                                DefaultInputLayout.VsInputInstancing),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultGsShaderDescriptions.GsMeshNormalVector,
                        DefaultPsShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshBoneSkinnedBasic,
                        DefaultGsShaderDescriptions.GsMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVsShaderByteCodes.VsMeshBoneSkinningBasic,
                        DefaultInputLayout.VsInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDepth,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.MeshSsaoPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshSsao,
                        DefaultPsShaderDescriptions.Psssaop1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellation) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOitdp) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshShadow,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshWireframe,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshInstancing,
                        DefaultPsShaderDescriptions.PsEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderPoint = new TechniqueDescription(DefaultRenderTechniqueNames.Points) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsPoint,
                        DefaultPsShaderDescriptions.PsPoint
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsPoint,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPointShadow,
                        DefaultGsShaderDescriptions.GsPoint,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsPoint,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                }
            ]
        };

        var renderLine = new TechniqueDescription(DefaultRenderTechniqueNames.Lines) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPointShadow,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLine,
                        DefaultPsShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderLineArrowHead = new TechniqueDescription(DefaultRenderTechniqueNames.LinesArrowHead) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPointShadow,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHead,
                        DefaultPsShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderLineArrowHeadTail = new TechniqueDescription(DefaultRenderTechniqueNames.LinesArrowHeadTail) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPointShadow,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsPoint,
                        DefaultGsShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPsShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderBillboardText = new TechniqueDescription(DefaultRenderTechniqueNames.BillboardText) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVsShaderByteCodes.VsBillboard,
                                                                DefaultInputLayout.VsInputBillboard),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardText,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsBillboardText
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardText,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsBillboardTextOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardText,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardText,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsBillboardTextOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                }
            ]
        };

        var renderBillboardInstancing = new TechniqueDescription(DefaultRenderTechniqueNames.BillboardInstancing) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVsShaderByteCodes.VsBillboardInstancing,
                                                                DefaultInputLayout.VsInputBillboardInstancing),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardInstancing,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsBillboardText
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardInstancing,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsBillboardTextOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardInstancing,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsBillboardInstancing,
                        DefaultGsShaderDescriptions.GsBillboard,
                        DefaultPsShaderDescriptions.PsBillboardTextOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                }
            ]
        };

        var renderMeshBlinnClipPlane = new TechniqueDescription(DefaultRenderTechniqueNames.CrossSection) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsMeshDefault, DefaultInputLayout.VsInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultGsShaderDescriptions.GsMeshNormalVector,
                        DefaultPsShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshDepth,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Backface) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshClipBackface
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssClipPlaneBackface,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsFullScreenQuad,
                        DefaultPsShaderDescriptions.PsMeshClipScreenQuad
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssClipPlaneFillQuad,
                    StencilRef = 1,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshClipPlane,
                        DefaultPsShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderParticle = new TechniqueDescription(DefaultRenderTechniqueNames.ParticleStorm) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsParticle, DefaultInputLayout.VsInputParticle),
            PassDescriptions = [
                new ShaderPassDescription(DefaultParticlePassNames.Insert) {
                    ShaderList = [
                        DefaultComputeShaderDescriptions.CsParticleInsert
                    ]
                },
                new ShaderPassDescription(DefaultParticlePassNames.Update) {
                    ShaderList = [
                        DefaultComputeShaderDescriptions.CsParticleUpdate
                    ]
                },
                new ShaderPassDescription(DefaultParticlePassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsParticle,
                        DefaultGsShaderDescriptions.GsParticle,
                        DefaultPsShaderDescriptions.PsParticle
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    RasterStateDescription = DefaultRasterDescriptions.RsSolidNoMsaa,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsParticle,
                        DefaultGsShaderDescriptions.GsParticle,
                        DefaultPsShaderDescriptions.PsParticleOit
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    RasterStateDescription = DefaultRasterDescriptions.RsSolidNoMsaa
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsParticle,
                        DefaultGsShaderDescriptions.GsParticle,
                        DefaultPsShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsParticle,
                        DefaultGsShaderDescriptions.GsParticle,
                        DefaultPsShaderDescriptions.PsParticleOitdp
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    RasterStateDescription = DefaultRasterDescriptions.RsSolidNoMsaa
                }
            ]
        };

        var renderSkybox = new TechniqueDescription(DefaultRenderTechniqueNames.Skybox) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsSkybox, DefaultInputLayout.VsInputSkybox),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsSkybox,
                        DefaultPsShaderDescriptions.PsSkybox
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsSkybox
                }
            ]
        };

        var meshOitQuad = new TechniqueDescription(DefaultRenderTechniqueNames.MeshOitQuad) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsMeshBlinnPhongOitQuad
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsMeshOitBlendQuad,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var meshOitDepthPeeling = new TechniqueDescription(DefaultRenderTechniqueNames.MeshOitDepthPeeling) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingFinal) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsMeshOitdpFinal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpFinal,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsSkybox,
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
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsMeshOutlineScreenQuad
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssOutlineFillQuad,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectFullScreenBlurVertical
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectFullScreenBlurHorizontal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadFinal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsGlowBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
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
                            DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPsShaderDescriptions.PsMeshOutlineScreenQuad
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssOutlineFillQuad,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                        ShaderList = [
                            DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPsShaderDescriptions.PsEffectMeshBorderHighlight
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                        ShaderList = [
                            DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPsShaderDescriptions.PsEffectMeshBorderHighlight
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                        ShaderList = [
                            DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPsShaderDescriptions.PsMeshOutlineQuadFinal
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    }
                ]
            };

        var bloomPostEffect = new TechniqueDescription(DefaultRenderTechniqueNames.PostEffectBloom) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectBloomExtract
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.ScreenQuadCopy) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsMeshOutlineQuadFinal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectBloomVerticalBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectBloomHorizontalBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectBloomCombine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.AdditiveBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var fxaaPostEffect = new TechniqueDescription(DefaultRenderTechniqueNames.PostEffectFxaa) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.LumaPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectLuma
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.FxaaPass) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsEffectFxaa
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
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
                        DefaultVsShaderDescriptions.VsPlaneGrid,
                        DefaultPsShaderDescriptions.PsPlaneGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    RasterStateDescription = DefaultRasterDescriptions.RsPlaneGrid,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var screenQuad = new TechniqueDescription(DefaultRenderTechniqueNames.ScreenQuad) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsScreenQuad,
                        DefaultPsShaderDescriptions.PsScreenDup
                    ],
                    Topology = PrimitiveTopology.TriangleStrip,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual,
                    RasterStateDescription = DefaultRasterDescriptions.RsSkybox
                }
            ]
        };

        var sprite2D = new TechniqueDescription(DefaultRenderTechniqueNames.Sprite2D) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsSprite2D, DefaultInputLayout.VsInputSprite2D),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsSprite2D,
                        DefaultPsShaderDescriptions.PsSprite2D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsSpriteCw
                }
            ]
        };

        var volume3D = new TechniqueDescription(DefaultRenderTechniqueNames.Volume3D) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVsShaderByteCodes.VsVolume3D, DefaultInputLayout.VsInputVolume3D),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsVolume3D,
                        DefaultPsShaderDescriptions.PsVolume3D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsVolumeCubeBack
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsVolume3D,
                        DefaultPsShaderDescriptions.PsVolumeDiffuse3D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsVolumeCubeBack
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsVolume3D,
                        DefaultPsShaderDescriptions.PsVolumeCube
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssVolumeFrontFace,
                    RasterStateDescription = DefaultRasterDescriptions.RsVolumeCubeFront
                },
                new ShaderPassDescription(DefaultPassNames.Backface) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsVolume3D,
                        DefaultPsShaderDescriptions.PsVolumeCube
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssVolumeBackFace,
                    RasterStateDescription = DefaultRasterDescriptions.RsVolumeCubeBack,
                    StencilRef = 1
                }
            ]
        };

        var ssao = new TechniqueDescription(DefaultRenderTechniqueNames.Ssao) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.Vsssao,
                        DefaultPsShaderDescriptions.Psssao
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPsShaderDescriptions.PsssaoBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
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
        yield return meshOitQuad;
        yield return planeGrid;
        yield return screenQuad;
        yield return sprite2D;
        yield return volume3D;
        yield return ssao;
        yield return meshOitDepthPeeling;
    }
}
