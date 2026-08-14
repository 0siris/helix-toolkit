/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Assimp;

public static class GltfMatKeys {
    /// <summary>
    ///     The ai matkey GLTF basecolor factor for PBR material
    /// </summary>
    public const string AiMatkeyGltfBasecolorFactor = @"$mat.gltf.pbrMetallicRoughness.baseColorFactor";

    /// <summary>
    ///     The ai matkey GLTF metallic factor for PBR material
    /// </summary>
    public const string AiMatkeyGltfMetallicFactor = @"$mat.gltf.pbrMetallicRoughness.metallicFactor";

    /// <summary>
    ///     The ai matkey GLTF metallic, roughness, ambient occlusion texture
    /// </summary>
    public const string AiMatkeyGltfMetallicroughnessaoTexture = @"$tex.file";

    /// <summary>
    ///     The ai matkey GLTF roughness factor for PBR material
    /// </summary>
    public const string AiMatkeyGltfRoughnessFactor = @"$mat.gltf.pbrMetallicRoughness.roughnessFactor";

    /// <summary>
    ///     The ai matkey GLTF pbrspecularglossiness
    /// </summary>
    public const string AiMatkeyGltfPbrspecularglossiness = @"$mat.gltf.pbrSpecularGlossiness";

    /// <summary>
    ///     The ai matkey GLTF pbrspecularglossiness glossiness factor
    /// </summary>
    public const string AiMatkeyGltfPbrspecularglossinessGlossinessFactor =
        @"$mat.gltf.pbrMetallicRoughness.glossinessFactor";
}
