/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.ShaderManager;

/// <summary>
/// </summary>
public struct DefaultRenderTechniqueNames {
    /// <summary>
    /// </summary>
    public const string Mesh = "RenderMesh";

    public const string MeshBatched = "RenderMeshBatch";

    /// <summary>
    /// </summary>
    public const string Lines = "RenderLines";

    /// <summary>
    /// </summary>
    public const string LinesArrowHead = "RenderLinesArrowHead";

    /// <summary>
    /// </summary>
    public const string LinesArrowHeadTail = "RenderLinesArrowHeadTail";

    /// <summary>
    /// </summary>
    public const string Points = "RenderPoints";

    /// <summary>
    /// </summary>
    public const string CubeMap = "RenderCubeMap";

    /// <summary>
    /// </summary>
    public const string BillboardText = "RenderBillboard";

    /// <summary>
    /// </summary>
    public const string BillboardInstancing = "RenderBillboardInstancing";

    /// <summary>
    /// </summary>
    public const string InstancingMesh = "RenderInstancingMesh";

    /// <summary>
    /// </summary>
    public const string ParticleStorm = "ParticleStorm";

    /// <summary>
    /// </summary>
    public const string CrossSection = "RenderCrossSectionBlinn";

    /// <summary>
    /// </summary>
    public const string ViewCube = "RenderViewCube";

    /// <summary>
    /// </summary>
    public const string Skybox = "Skybox";

    /// <summary>
    /// </summary>
    public const string PostEffectMeshBorderHighlight = "PostEffectMeshBorderHighlight";

    /// <summary>
    ///     The post effect mesh x ray
    /// </summary>
    public const string PostEffectMeshXRay = "PostEffectMeshXRay";

    /// <summary>
    /// </summary>
    public const string PostEffectMeshOutlineBlur = "PostEffectMeshOutlineBlur";

    /// <summary>
    /// </summary>
    public const string PostEffectBloom = "PostEffectBloom";

    /// <summary>
    /// </summary>
    public const string MeshOitQuad = "MeshOITQuad";

    /// <summary>
    /// </summary>
    public const string MeshOitSortRender = "MeshOITSortRender";

    public const string MeshOitDepthPeeling = "MeshOITDeepPeeling";

    /// <summary>
    ///     The post effect fxaa
    /// </summary>
    public const string PostEffectFxaa = "PostEffectFXAA";

    /// <summary>
    ///     The post effect mesh x ray grid
    /// </summary>
    public const string PostEffectMeshXRayGrid = "PostEffectMeshXRayGrid";

    /// <summary>
    ///     The plane grid
    /// </summary>
    public const string PlaneGrid = "PlaneGrid";

    public const string ScreenQuad = "ScreenQuad";

    public const string Sprite2D = "Sprite2D";

    public const string Volume3D = "Volume3D";

    public const string Ssao = "SSAO";

    /// <summary>
    /// </summary>
    public const string ScreenDuplication = "ScreenDup";
}

/// <summary>
/// </summary>
public struct DefaultPassNames {
    /// <summary>
    /// </summary>
    public const string Default = "Default";

    /// <summary>
    ///     The Physics Based Rendering
    /// </summary>
    public const string Pbr = "PhysicsBasedRendering";

    /// <summary>
    ///     The PBR face normal pass
    /// </summary>
    public const string PbrFaceNormal = "PhysicsBasedRenderingFaceNormal";

    /// <summary>
    /// </summary>
    public const string Diffuse = "RenderDiffuse";

    /// <summary>
    ///     The diffuse oit
    /// </summary>
    public const string DiffuseOit = "RenderDiffuseOIT";

    public const string DiffuseOitdp = "RenderDiffuseOITDepthPeeling";

    /// <summary>
    /// </summary>
    public const string Colors = "RenderColors";

    /// <summary>
    /// </summary>
    public const string Positions = "RenderPositions";

    /// <summary>
    /// </summary>
    public const string Normals = "RenderNormals";

    /// <summary>
    /// </summary>
    public const string NormalVector = "RenderNormalVector";

    /// <summary>
    /// </summary>
    public const string PerturbedNormals = "RenderPerturbedNormals";

    /// <summary>
    /// </summary>
    public const string Tangents = "RenderTangents";

    /// <summary>
    /// </summary>
    public const string TexCoords = "RenderTexCoords";

    /// <summary>
    /// </summary>
    public const string ViewCube = "RenderViewCube";

    /// <summary>
    /// </summary>
    public const string ColorStripe1D = "ColorStripe1D";

    /// <summary>
    ///     The mesh transparent
    /// </summary>
    public const string OitPass = "MeshOITPass";

    #region Deep peeling

    public const string OitDepthPeelingInit = "OITDepthPeelingFirst";

    public const string OitDepthPeeling = "OITDepthPeeling";

    public const string OitDepthPeelingBlending = "OITDepthPeelingBlending";

    public const string OitDepthPeelingFinal = "OITDepthPeelingFinal";

    #endregion

    /// <summary>
    ///     The oit pass PBR
    /// </summary>
    public const string PbroitPass = "MeshPhysicsBasedOITPass";

    public const string PbroitdpPass = "MeshPhysicsBasedOITDepthPeelingPass";

    /// <summary>
    /// </summary>
    public const string WireframeOitPass = "WireframeOIT";

    public const string WireframeOitdpPass = "WireframeOITDP";

    /// <summary>
    /// </summary>
    public const string MeshTriTessellation = "MeshTriTessellation";

    public const string MeshTriTessellationOit = "MeshTriTessellationOIT";

    public const string MeshTriTessellationOitdp = "MeshTriTessellationOITDP";

    /// <summary>
    /// </summary>
    public const string MeshPbrTriTessellation = "MeshPBRTriTessellation";

    public const string MeshPbrTriTessellationOit = "MeshPBRTriTessellationOIT";

    public const string MeshPbrTriTessellationOitdp = "MeshPBRTriTessellationOITDP";

    /// <summary>
    /// </summary>
    public const string MeshQuadTessellation = "MeshQuadTessellation";

    /// <summary>
    /// </summary>
    public const string MeshOutline = "RenderMeshOutline";

    /// <summary>
    ///     The mesh bone skinned
    /// </summary>
    public const string PreComputeMeshBoneSkinned = "MeshBoneSkinned";

    /// <summary>
    /// </summary>
    public const string EffectBlurVertical = "EffectBlurVertical";

    /// <summary>
    /// </summary>
    public const string EffectBlurHorizontal = "EffectBlurHorizontal";

    /// <summary>
    /// </summary>
    public const string EffectOutlineSmooth = "EffectOutlineSmooth";

    /// <summary>
    /// </summary>
    public const string EffectOutlineP1 = "RenderMeshOutlineP1";

    /// <summary>
    /// </summary>
    public const string EffectMeshXRayP1 = "EffectMeshXRayP1";

    /// <summary>
    /// </summary>
    public const string EffectMeshXRayP2 = "EffectMeshXRayP2";

    /// <summary>
    /// </summary>
    public const string EffectMeshXRayGridP1 = "EffectMeshXRayGridP1";

    /// <summary>
    /// </summary>
    public const string EffectMeshXRayGridP2 = "EffectMeshXRayGridP2";

    /// <summary>
    /// </summary>
    public const string EffectMeshXRayGridP3 = "EffectMeshXRayGridP3";

    /// <summary>
    ///     The effect mesh diffuse x ray grid p3
    /// </summary>
    public const string EffectMeshDiffuseXRayGridP3 = "EffectMeshDiffueXRayGridP3";

    /// <summary>
    /// </summary>
    public const string ShadowPass = "RenderShadow";

    /// <summary>
    /// </summary>
    public const string Backface = "RenderBackface";

    /// <summary>
    /// </summary>
    public const string ScreenQuad = "ScreenQuad";

    /// <summary>
    /// </summary>
    public const string ScreenQuadCopy = "ScreenQuadCopy";

    /// <summary>
    ///     The wireframe
    /// </summary>
    public const string Wireframe = "Wireframe";

    /// <summary>
    ///     The depth prepass
    /// </summary>
    public const string DepthPrepass = "DepthPrepass";

    /// <summary>
    ///     Calculate luma and set luma as Alpha channel before FXAA pass.
    /// </summary>
    public const string LumaPass = "LumaPass";

    /// <summary>
    /// </summary>
    public const string FxaaPass = "FXAAPass";

    /// <summary>
    ///     The ssao pass
    /// </summary>
    public const string MeshSsaoPass = "MeshSSAOPass";
}

/// <summary>
/// </summary>
public struct DefaultParticlePassNames {
    /// <summary>
    ///     The insert
    /// </summary>
    public const string Insert = "InsertParticle"; //For compute shader

    /// <summary>
    ///     The update
    /// </summary>
    public const string Update = "UpdateParticle"; //For compute shader

    /// <summary>
    ///     The default
    /// </summary>
    public const string Default = "Default"; //For rendering
}

//public struct TessellationRenderTechniqueNames
//{
//    public const string PNTriangles = "RenderPNTriangs";
//    public const string PNQuads = "RenderPNQuads";
//}
/// <summary>
/// </summary>
public struct DeferredRenderTechniqueNames {
    /// <summary>
    ///     The deferred
    /// </summary>
    public const string Deferred = "RenderDeferred";

    /// <summary>
    ///     The g buffer
    /// </summary>
    public const string GBuffer = "RenderGBuffer";

    /// <summary>
    ///     The deferred lighting
    /// </summary>
    public const string DeferredLighting = "RenderDeferredLighting";

    /// <summary>
    ///     The screen space
    /// </summary>
    public const string ScreenSpace = "RenderScreenSpace";
}
