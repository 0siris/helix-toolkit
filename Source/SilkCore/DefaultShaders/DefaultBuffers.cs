/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Shaders;
/// <summary>
///     Default buffer names from shader code. Name must match shader code to bind proper buffer
///     <para>Note: Constant buffer must match both name and struct size</para>
/// </summary>
public static class DefaultBufferNames {
    public const string GlobalTransformCb = "cbTransforms";
    public const string ModelCb = "cbMesh";
    public const string SimpleMeshCb = "cbMeshSimple";
    public const string PointLineModelCb = "cbPointLineModel";
    public const string ParticleModelCb = "cbParticleModel";
    public const string PlaneGridModelCb = "cbPlaneGridModel";
    public const string LightCb = "cbLights";
    public const string ClipParamsCb = "cbClipping";
    public const string BorderEffectCb = "cbBorderEffect";
    public const string DynamicCubeMapCb = "cbDynamicCubeMap";
    public const string ScreenQuadCb = "cbScreenQuad";
    public const string VolumeModelCb = "cbVolumeModel";
    public const string Ssaocb = "cbSSAO";
    public const string ScreenDuplicationCb = "cbScreenClone";

    public const string MorphTargetCb = "cbMorphTarget";

    //-----------Materials--------------------
    public const string DiffuseMapTb = "texDiffuseMap";
    public const string AlphaMapTb = "texAlphaMap";
    public const string NormalMapTb = "texNormalMap";
    public const string DisplacementMapTb = "texDisplacementMap";
    public const string CubeMapTb = "texCubeMap";
    public const string ShadowMapTb = "texShadowMap";
    public const string SpecularTb = "texSpecularMap";
    public const string BillboardTb = "billboardTexture";
    public const string ColorStripe1Dxtb = "texColorStripe1DX";
    public const string ColorStripe1Dytb = "texColorStripe1DY";
    public const string RmMapTb = "texRMMap";
    public const string AoMapTb = "texAOMap";
    public const string EmissiveTb = "texEmissiveMap";

    public const string IrradianceMap = "texIrradianceMap";

    //----------Particle--------------
    public const string ParticleFrameCb = "cbParticleFrame";
    public const string ParticleCreateParameters = "cbParticleCreateParameters";
    public const string ParticleMapTb = "texParticle";
    public const string CurrentSimulationStateUb = "CurrentSimulationState";
    public const string NewSimulationStateUb = "NewSimulationState";

    public const string SimulationStateTb = "SimulationState";

    //----------ShadowMap---------------
    public const string ShadowParamCb = "cbShadow";

    //----------Order Independent Transparent-----------
    public const string OitColorTb = "texOITColor";
    public const string OitAlphaTb = "texOITAlpha";

    public const string OitSortCb = "cbOITSortRender";

    //----------Bone Skin--------------
    public const string BoneSkinSb = "skinMatrices"; // Structured Buffer

    public const string MtWeightsB = "morphTargetWeights"; //Buffer<float>
    public const string MtDeltasB = "morphTargetDeltas";   //Buffer<float3>
    public const string MtOffsetsB = "morphTargetOffsets"; //Buffer<int>

    public const string SpriteTb = "texSprite";

    public const string VolumeTb = "texVolume";
    public const string VolumeFront = "texVolumeFront";
    public const string VolumeBack = "texVolumeBack";

    public const string SsaoMapTb = "texSSAOMap";
    public const string SsaoNoiseTb = "texSSAONoise";
    public const string SsaoDepthTb = "texSSAODepth";
}

public static class DefaultSamplerStateNames {
    public const string SurfaceSampler = "samplerSurface";
    public const string IblSampler = "samplerIBL";
    public const string DisplacementMapSampler = "samplerDisplace";
    public const string CubeMapSampler = "samplerCube";
    public const string ShadowMapSampler = "samplerShadow";
    public const string ParticleTextureSampler = "samplerParticle";
    public const string BillboardTextureSampler = "samplerBillboard";
    public const string SpriteSampler = "samplerSprite";
    public const string VolumeSampler = "samplerVolume";

    public const string NoiseSampler = "samplerNoise";
}
