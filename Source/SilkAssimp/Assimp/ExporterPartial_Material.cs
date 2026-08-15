/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using Assimp;
using Assimp.Unmanaged;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Assimp;

public partial class Exporter {
    /// <summary>
    ///     Gets the material from node. Currently only supports <see cref="MaterialGeometryNode" />
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="material">The material.</param>
    /// <returns></returns>
    protected virtual bool GetMaterialFromNode(SceneNode node, out MaterialCore? material) {
        if (node is MaterialGeometryNode geo) {
            material = geo.Material;
            return material is not null;
        }

        material = null;
        return false;
    }

    /// <summary>
    ///     Adds the properties.
    /// </summary>
    /// <param name="phong">The phong.</param>
    /// <param name="assimpMaterial">The assimp material.</param>
    protected virtual void AddProperties(PhongMaterialCore phong, Material assimpMaterial) {
        assimpMaterial.ShadingMode = ShadingMode.Blinn;
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_DIFFUSE_BASE,
                                                        phong.DiffuseColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_SPECULAR_BASE,
                                                        phong.SpecularColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_REFLECTIVE_BASE,
                                                        phong.ReflectiveColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_EMISSIVE_BASE,
                                                        phong.EmissiveColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_AMBIENT_BASE,
                                                        phong.AmbientColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.SHININESS_BASE, phong.SpecularShininess));

        if (phong.DiffuseColor.W < 1f)
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.OPACITY_BASE, phong.DiffuseColor.W));
        if (phong.DiffuseMap != null && !string.IsNullOrEmpty(phong.DiffuseMapFilePath)) {
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            phong.DiffuseMapFilePath,
                                                            TextureType.Diffuse,
                                                            0));
            assimpMaterial.AddProperty(new MaterialProperty(
                                           AiMatKeys.GetFullTextureName(AiMatKeys.MAPPINGMODE_U_BASE,
                                                                        TextureType.Diffuse,
                                                                        0),
                                           (int)ToAssimpAddressMode(phong.DiffuseMapSampler.AddressU)));
            assimpMaterial.AddProperty(new MaterialProperty(
                                           AiMatKeys.GetFullTextureName(AiMatKeys.MAPPINGMODE_V_BASE,
                                                                        TextureType.Diffuse,
                                                                        0),
                                           (int)ToAssimpAddressMode(phong.DiffuseMapSampler.AddressV)));
        }

        if (phong.EmissiveMap != null && !string.IsNullOrEmpty(phong.EmissiveMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            phong.EmissiveMapFilePath,
                                                            TextureType.Emissive,
                                                            0));
        if (phong.NormalMap != null && !string.IsNullOrEmpty(phong.NormalMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            phong.NormalMapFilePath,
                                                            TextureType.Normals,
                                                            0));
        if (phong.DisplacementMap != null && !string.IsNullOrEmpty(phong.DisplacementMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            phong.DisplacementMapFilePath,
                                                            TextureType.Displacement,
                                                            0));
        if (phong.SpecularColorMap != null && !string.IsNullOrEmpty(phong.SpecularColorMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            phong.SpecularColorMapFilePath,
                                                            TextureType.Specular,
                                                            0));
        if (phong.DiffuseAlphaMap != null && !string.IsNullOrEmpty(phong.DiffuseAlphaMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            phong.DiffuseAlphaMapFilePath,
                                                            TextureType.Opacity,
                                                            0));
        if (phong.UvTransform.HasUvTransform)
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.UVTRANSFORM_BASE,
                                                            phong.UvTransform.ToArray()));
    }

    /// <summary>
    ///     Adds the properties.
    /// </summary>
    /// <param name="pbr">The PBR.</param>
    /// <param name="assimpMaterial">The assimp material.</param>
    protected virtual void AddProperties(PbrMaterialCore pbr, Material assimpMaterial) {
        assimpMaterial.ShadingMode = ShadingMode.Fresnel;
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_DIFFUSE_BASE,
                                                        pbr.AlbedoColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(GltfMatKeys.AiMatkeyGltfBasecolorFactor,
                                                        pbr.AlbedoColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_EMISSIVE_BASE,
                                                        pbr.EmissiveColor.ToAssimpColor4D()));
        assimpMaterial.AddProperty(new MaterialProperty(GltfMatKeys.AiMatkeyGltfMetallicFactor,
                                                        pbr.MetallicFactor));
        assimpMaterial.AddProperty(new MaterialProperty(GltfMatKeys.AiMatkeyGltfRoughnessFactor,
                                                        pbr.RoughnessFactor));
        if (pbr.AmbientOcclusionFactor != 1)
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_AMBIENT,
                                                            new Color4D(pbr.AmbientOcclusionFactor)));
        if (pbr.ReflectanceFactor != 0) {
            assimpMaterial.AddProperty(new MaterialProperty(GltfMatKeys.AiMatkeyGltfPbrspecularglossiness,
                                                            true));
            assimpMaterial.AddProperty(new MaterialProperty(
                                           GltfMatKeys.AiMatkeyGltfPbrspecularglossinessGlossinessFactor,
                                           pbr.ReflectanceFactor));
        }

        if (pbr.AlbedoColor.W < 1)
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.OPACITY, pbr.AlbedoColor.W));
        if (pbr.AlbedoMap != null && !string.IsNullOrEmpty(pbr.AlbedoMapFilePath)) {
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            pbr.AlbedoMapFilePath,
                                                            TextureType.Diffuse,
                                                            0));
            assimpMaterial.AddProperty(new MaterialProperty(
                                           AiMatKeys.GetFullTextureName(AiMatKeys.MAPPINGMODE_U_BASE,
                                                                        TextureType.Diffuse,
                                                                        0),
                                           (int)ToAssimpAddressMode(pbr.SurfaceMapSampler.AddressU)));
            assimpMaterial.AddProperty(new MaterialProperty(
                                           AiMatKeys.GetFullTextureName(AiMatKeys.MAPPINGMODE_V_BASE,
                                                                        TextureType.Diffuse,
                                                                        0),
                                           (int)ToAssimpAddressMode(pbr.SurfaceMapSampler.AddressV)));
        }

        if (pbr.RoughnessMetallicMap != null && !string.IsNullOrEmpty(pbr.RoughnessMetallicMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(GltfMatKeys.AiMatkeyGltfMetallicroughnessaoTexture,
                                                            pbr.RoughnessMetallicMapFilePath,
                                                            TextureType.Unknown,
                                                            0));
        if (pbr.EmissiveMap != null && !string.IsNullOrEmpty(pbr.EmissiveMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            pbr.EmissiveMapFilePath,
                                                            TextureType.Emissive,
                                                            0));
        if (pbr.NormalMap != null && !string.IsNullOrEmpty(pbr.NormalMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            pbr.NormalMapFilePath,
                                                            TextureType.Normals,
                                                            0));
        if (pbr.DisplacementMap != null && !string.IsNullOrEmpty(pbr.DisplacementMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            pbr.DisplacementMapFilePath,
                                                            TextureType.Displacement,
                                                            0));
        if (pbr.IrradianceMap != null && !string.IsNullOrEmpty(pbr.IrradianceMapFilePath))
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.TEXTURE_BASE,
                                                            pbr.IrradianceMapFilePath,
                                                            TextureType.Lightmap,
                                                            0));
        if (pbr.UvTransform.HasUvTransform)
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.UVTRANSFORM_BASE,
                                                            pbr.UvTransform.ToArray()));
    }

    /// <summary>
    ///     Adds the properties.
    /// </summary>
    /// <param name="diffuse">The diffuse.</param>
    /// <param name="assimpMaterial">The assimp material.</param>
    protected virtual void AddProperties(DiffuseMaterialCore diffuse, Material assimpMaterial) {
        assimpMaterial.ShadingMode = ShadingMode.Gouraud;
        assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_DIFFUSE_BASE,
                                                        diffuse.DiffuseColor.ToAssimpColor4D()));
    }

    /// <summary>
    ///     Called when [create assimp material].
    /// </summary>
    /// <param name="material">The material.</param>
    /// <returns></returns>
    protected virtual Material OnCreateAssimpMaterial(MaterialCore material) {
        var assimpMaterial = new Material {
            Name = string.IsNullOrEmpty(material.Name)
                       ? $"MAT_{Interlocked.Increment(ref materialIndexForNoName)}"
                       : material.Name
        };
        if (material is PhongMaterialCore phong)
            AddProperties(phong, assimpMaterial);
        else if (material is PbrMaterialCore pbr)
            AddProperties(pbr, assimpMaterial);
        else if (material is DiffuseMaterialCore diffuse)
            AddProperties(diffuse, assimpMaterial);
        else if (material is ColorMaterialCore)
            assimpMaterial.ShadingMode = ShadingMode.Flat;
        else if (material is LineMaterialCore line)
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_DIFFUSE_BASE,
                                                            line.LineColor.ToAssimpColor4D()));
        else if (material is PointMaterialCore point)
            assimpMaterial.AddProperty(new MaterialProperty(AiMatKeys.COLOR_DIFFUSE_BASE,
                                                            point.PointColor.ToAssimpColor4D()));
        return assimpMaterial;
    }

    private static TextureWrapMode ToAssimpAddressMode(TextureAddressMode mode) {
        switch (mode) {
            case TextureAddressMode.Clamp:
                return TextureWrapMode.Clamp;
            case TextureAddressMode.Mirror:
                return TextureWrapMode.Mirror;
            case TextureAddressMode.Wrap:
                return TextureWrapMode.Wrap;
            default:
                return TextureWrapMode.Wrap;
        }
    }
}
