/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;

using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
///     Default shader technique manager, includes all internal shaders
/// </summary>
public class DefaultEffectsManager : EffectsManager {
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
    internal static IEnumerable<TechniqueDescription> LoadTechniqueDescriptions() {
        var renderMesh = new TechniqueDescription(DefaultRenderTechniqueNames.Mesh) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsMeshDefault, DefaultInputLayout.VsInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ViewCube) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshViewCube
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultGSShaderDescriptions.GsMeshNormalVector,
                        DefaultPSShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBoneSkinnedBasic,
                        DefaultGSShaderDescriptions.GsMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVSShaderByteCodes.VsMeshBoneSkinningBasic,
                        DefaultInputLayout.VsInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDepth,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VsMeshDepth,
                        DefaultInputLayout.VsInputDepth)
                },
                new ShaderPassDescription(DefaultPassNames.MeshSsaoPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshSsao,
                        DefaultPSShaderDescriptions.Psssaop1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellation) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOitdp) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPbrTriTessellation) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPbrTriTessellationOit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshPbrTriTessellationOitdp) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshShadow,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsWireframe,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDefault,
                        DefaultPSShaderDescriptions.PsEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderMeshBatched = new TechniqueDescription(DefaultRenderTechniqueNames.MeshBatched) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VsMeshBatched,
                DefaultInputLayout.VsMeshBatchedInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultGSShaderDescriptions.GsMeshNormalVector,
                        DefaultPSShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBoneSkinnedBasic,
                        DefaultGSShaderDescriptions.GsMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVSShaderByteCodes.VsMeshBoneSkinningBasic,
                        DefaultInputLayout.VsInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDepth,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VsMeshDepth,
                        DefaultInputLayout.VsInputDepth)
                },
                new ShaderPassDescription(DefaultPassNames.MeshSsaoPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedSsao,
                        DefaultPSShaderDescriptions.Psssaop1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedShadow,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsWireframe,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatchedWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBatched,
                        DefaultPSShaderDescriptions.PsEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderMeshInstancing = new TechniqueDescription(DefaultRenderTechniqueNames.InstancingMesh) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VsMeshInstancing,
                DefaultInputLayout.VsInputInstancing),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultGSShaderDescriptions.GsMeshNormalVector,
                        DefaultPSShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.PreComputeMeshBoneSkinned) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshBoneSkinnedBasic,
                        DefaultGSShaderDescriptions.GsMeshBoneSkinnedOut
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    Topology = PrimitiveTopology.PointList,
                    InputLayoutDescription = new InputLayoutDescription(
                        DefaultVSShaderByteCodes.VsMeshBoneSkinningBasic,
                        DefaultInputLayout.VsInputBoneSkinnedBasic)
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDepth,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VsMeshDepth,
                        DefaultInputLayout.VsInputDepth)
                },
                new ShaderPassDescription(DefaultPassNames.MeshSsaoPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshSsao,
                        DefaultPSShaderDescriptions.Psssaop1
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellation) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.MeshTriTessellationOitdp) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancingTessellation,
                        DefaultHullShaderDescriptions.HsMeshTessellation,
                        DefaultDomainShaderDescriptions.DsMeshTessellation,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    Topology = PrimitiveTopology.PatchListWith3ControlPoints
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshShadow,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsWireframe,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite,
                    Topology = PrimitiveTopology.TriangleList
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsEffectMeshXRay
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshWireframe,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshDiffuseXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshInstancing,
                        DefaultPSShaderDescriptions.PsEffectDiffuseXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderPoint = new TechniqueDescription(DefaultRenderTechniqueNames.Points) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsPoint,
                        DefaultPSShaderDescriptions.PsPoint
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsPoint,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPointShadow,
                        DefaultGSShaderDescriptions.GsPoint,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsPoint,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                }
            ]
        };

        var renderLine = new TechniqueDescription(DefaultRenderTechniqueNames.Lines) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPointShadow,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLine,
                        DefaultPSShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderLineArrowHead = new TechniqueDescription(DefaultRenderTechniqueNames.LinesArrowHead) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPointShadow,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHead,
                        DefaultPSShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderLineArrowHeadTail = new TechniqueDescription(DefaultRenderTechniqueNames.LinesArrowHeadTail) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsPoint, DefaultInputLayout.VsInputPoint),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.ShadowPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPointShadow,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsShadow
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsPoint,
                        DefaultGSShaderDescriptions.GsLineArrowHeadTail,
                        DefaultPSShaderDescriptions.PsLine
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsOverlayBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP2,
                    StencilRef = 1
                }
            ]
        };

        var renderBillboardText = new TechniqueDescription(DefaultRenderTechniqueNames.BillboardText) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VsBillboard,
                DefaultInputLayout.VsInputBillboard),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardText,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsBillboardText
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardText,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsBillboardTextOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardText,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardText,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsBillboardTextOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                }
            ]
        };

        var renderBillboardInstancing = new TechniqueDescription(DefaultRenderTechniqueNames.BillboardInstancing) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVSShaderByteCodes.VsBillboardInstancing,
                DefaultInputLayout.VsInputBillboardInstancing),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardInstancing,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsBillboardText
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardInstancing,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsBillboardTextOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardInstancing,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsBillboardInstancing,
                        DefaultGSShaderDescriptions.GsBillboard,
                        DefaultPSShaderDescriptions.PsBillboardTextOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                }
            ]
        };

        var renderMeshBlinnClipPlane = new TechniqueDescription(DefaultRenderTechniqueNames.CrossSection) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsMeshDefault, DefaultInputLayout.VsInput),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhong
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Pbr) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshPbr
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Colors) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshVertColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Normals) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshVertNormal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshVertPosition
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMap
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.ColorStripe1D) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshColorStripe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLessEqual
                },
                new ShaderPassDescription(DefaultPassNames.NormalVector) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultGSShaderDescriptions.GsMeshNormalVector,
                        DefaultPSShaderDescriptions.PsLineColor
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshPbroit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.PbroitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshPbroitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DiffuseOitdp) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshDiffuseMapOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.DepthPrepass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshDepth,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssDepthLess
                },
                new ShaderPassDescription(DefaultPassNames.Backface) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshClipBackface
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssClipPlaneBackface,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsFullScreenQuad,
                        DefaultPSShaderDescriptions.PsMeshClipScreenQuad
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssClipPlaneFillQuad,
                    StencilRef = 1,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.Wireframe) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshWireframe
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsWireframe,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshWireframeOit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.WireframeOitdpPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshWireframeOitdp
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessEqualNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.EffectOutlineP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadStencil
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssMeshOutlineP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayP1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP1) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP1,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP2) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsDepthStencilOnly
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.NoBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP2,
                    StencilRef = 1
                },
                new ShaderPassDescription(DefaultPassNames.EffectMeshXRayGridP3) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshClipPlane,
                        DefaultPSShaderDescriptions.PsEffectXRayGrid
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssEffectMeshXRayGridP3,
                    StencilRef = 1
                }
            ]
        };

        var renderParticle = new TechniqueDescription(DefaultRenderTechniqueNames.ParticleStorm) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsParticle, DefaultInputLayout.VsInputParticle),
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
                        DefaultVSShaderDescriptions.VsParticle,
                        DefaultGSShaderDescriptions.GsParticle,
                        DefaultPSShaderDescriptions.PsParticle
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    RasterStateDescription = DefaultRasterDescriptions.RsSolidNoMsaa,
                    Topology = PrimitiveTopology.PointList
                },
                new ShaderPassDescription(DefaultPassNames.OitPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsParticle,
                        DefaultGSShaderDescriptions.GsParticle,
                        DefaultPSShaderDescriptions.PsParticleOit
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitBlend,
                    RasterStateDescription = DefaultRasterDescriptions.RsSolidNoMsaa
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeelingInit) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsParticle,
                        DefaultGSShaderDescriptions.GsParticle,
                        DefaultPSShaderDescriptions.PsMeshOitdpInit
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsoitdpMaxBlending,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite
                },
                new ShaderPassDescription(DefaultPassNames.OitDepthPeeling) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsParticle,
                        DefaultGSShaderDescriptions.GsParticle,
                        DefaultPSShaderDescriptions.PsParticleOitdp
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssLessNoWrite,
                    BlendStateDescription = DefaultBlendStateDescriptions.Bsoitdp,
                    RasterStateDescription = DefaultRasterDescriptions.RsSolidNoMsaa
                }
            ]
        };

        var renderSkybox = new TechniqueDescription(DefaultRenderTechniqueNames.Skybox) {
            InputLayoutDescription =
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsSkybox, DefaultInputLayout.VsInputSkybox),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsSkybox,
                        DefaultPSShaderDescriptions.PsSkybox
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
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsMeshBlinnPhongOitQuad
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
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsMeshOitdpFinal
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

        var meshOutlineBlurPostEffect =
            new TechniqueDescription(DefaultRenderTechniqueNames.PostEffectMeshOutlineBlur) {
                InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
                PassDescriptions = [
                    new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsMeshOutlineScreenQuad
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssOutlineFillQuad,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsEffectFullScreenBlurVertical
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsEffectFullScreenBlurHorizontal
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsMeshOutlineQuadFinal
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
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsMeshOutlineScreenQuad
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssOutlineFillQuad,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsEffectMeshBorderHighlight
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsEffectMeshBorderHighlight
                        ],
                        BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                        DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                        RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                        Topology = PrimitiveTopology.TriangleStrip
                    },
                    new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                        ShaderList = [
                            DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                            DefaultPSShaderDescriptions.PsMeshOutlineQuadFinal
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
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsEffectBloomExtract
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.ScreenQuadCopy) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsMeshOutlineQuadFinal
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurVertical) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsEffectBloomVerticalBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsEffectBloomHorizontalBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.MeshOutline) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsEffectBloomCombine
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
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsEffectLuma
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.FxaaPass) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsEffectFxaa
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
                        DefaultVSShaderDescriptions.VsPlaneGrid,
                        DefaultPSShaderDescriptions.PsPlaneGrid
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
                        DefaultVSShaderDescriptions.VsScreenQuad,
                        DefaultPSShaderDescriptions.PsScreenDup
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
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsSprite2D, DefaultInputLayout.VsInputSprite2D),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsSprite2D,
                        DefaultPSShaderDescriptions.PsSprite2D
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
                new InputLayoutDescription(DefaultVSShaderByteCodes.VsVolume3D, DefaultInputLayout.VsInputVolume3D),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsVolume3D,
                        DefaultPSShaderDescriptions.PsVolume3D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsVolumeCubeBack
                },
                new ShaderPassDescription(DefaultPassNames.Diffuse) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsVolume3D,
                        DefaultPSShaderDescriptions.PsVolumeDiffuse3D
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsVolumeCubeBack
                },
                new ShaderPassDescription(DefaultPassNames.Positions) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsVolume3D,
                        DefaultPSShaderDescriptions.PsVolumeCube
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssVolumeFrontFace,
                    RasterStateDescription = DefaultRasterDescriptions.RsVolumeCubeFront
                },
                new ShaderPassDescription(DefaultPassNames.Backface) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsVolume3D,
                        DefaultPSShaderDescriptions.PsVolumeCube
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
                        DefaultVSShaderDescriptions.Vsssao,
                        DefaultPSShaderDescriptions.Psssao
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.EffectBlurHorizontal) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsMeshOutlineScreenQuad,
                        DefaultPSShaderDescriptions.PsssaoBlur
                    ],
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsOutline,
                    Topology = PrimitiveTopology.TriangleStrip
                }
            ]
        };

        var renderScreenDup = new TechniqueDescription(DefaultRenderTechniqueNames.ScreenDuplication) {
            InputLayoutDescription = InputLayoutDescription.EmptyInputLayout,
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsScreenDup,
                        DefaultPSShaderDescriptions.PsScreenDup
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsSourceAlways,
                    RasterStateDescription = DefaultRasterDescriptions.RsScreenDuplication,
                    Topology = PrimitiveTopology.TriangleStrip
                },
                new ShaderPassDescription(DefaultPassNames.ScreenQuad) {
                    ShaderList = [
                        DefaultVSShaderDescriptions.VsScreenDupCursor,
                        DefaultPSShaderDescriptions.PsScreenDup
                    ],
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsScreenDupCursorBlend,
                    BlendFactor = new(0, 0, 0, 0),
                    RasterStateDescription = DefaultRasterDescriptions.RsScreenDuplication,
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
        yield return new TechniqueDescription("MeshIntersect") {
            PassDescriptions = renderLine.PassDescriptions!.Where(source => source.Name == DefaultPassNames.Default ||
                source.Name == DefaultPassNames.EffectMeshXRayP1 || source.Name == DefaultPassNames.EffectMeshXRayP2)
                .Select(source => new ShaderPassDescription(source.Name!) {
                ShaderList = source.ShaderList!.Select(shader => shader.ShaderType == ShaderStage.Vertex
                    ? new ShaderDescription("VsCollisionSegmentLine", ShaderStage.Vertex, new ShaderReflector(), "vsCollisionSegmentLine")
                    : shader).ToList(),
                Topology = PrimitiveTopology.LineList,
                BlendStateDescription = source.BlendStateDescription,
                DepthStencilStateDescription = source.DepthStencilStateDescription,
                RasterStateDescription = source.RasterStateDescription ?? new RasterizerStateDescription {
                    CullMode = CullMode.None, FillMode = FillMode.Solid,
                    IsFrontCounterClockwise = true, IsDepthClipEnabled = true
                },
                StencilRef = source.StencilRef,
                SampleMask = source.SampleMask
            }).Concat([
                new ShaderPassDescription("Compute") { ShaderList = [DefaultComputeShaderDescriptions.CsMeshIntersect] },
                new ShaderPassDescription("ComputeSharedRenderBuffer") {
                    ShaderList = [DefaultComputeShaderDescriptions.CsMeshIntersectSharedRenderBuffer]
                }
            ]).ToList()
        };
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
        yield return renderScreenDup;
    }
}
