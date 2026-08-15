/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Collections.Concurrent;
using Assimp;
using Assimp.Unmanaged;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using TextureType = Assimp.TextureType;

namespace HelixToolkit.SharpDX.Core.Assimp;

public partial class Importer {
    private readonly ConcurrentDictionary<string, TextureModel> textureDict = new();

    /// <summary>
    ///     To the phong material.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <returns></returns>
    protected virtual PhongMaterialCore OnCreatePhongMaterial(Material material) {
        var phong = new PhongMaterialCore {
            AmbientColor = material.HasColorAmbient && !configuration.IgnoreAmbientColor
                               ? material.ColorAmbient.ToSharpDXColor4()
                               : new Color4(0, 0, 0, 1),
            DiffuseColor = material.HasColorDiffuse
                               ? material.ColorDiffuse.ToSharpDXColor4()
                               : new Color4(1, 1, 1, 1),
            SpecularColor = material.HasColorSpecular
                                ? material.ColorSpecular.ToSharpDXColor4()
                                : new Color4(0, 0, 0, 1),
            EmissiveColor = material.HasColorEmissive && !configuration.IgnoreEmissiveColor
                                ? material.ColorEmissive.ToSharpDXColor4()
                                : new Color4(0, 0, 0, 1),
            ReflectiveColor = material.HasColorReflective
                                  ? material.ColorReflective.ToSharpDXColor4()
                                  : new Color4(0, 0, 0, 1),
            SpecularShininess = material.Shininess
        };
        if (material.HasOpacity) {
            var c = phong.DiffuseColor;
            c.W = material.Opacity;
            phong.DiffuseColor = c;
        }

        if (material.HasTextureDiffuse) {
            phong.DiffuseMap = LoadTexture(material.TextureDiffuse.FilePath);
            phong.DiffuseMapFilePath = material.TextureDiffuse.FilePath;
            var desc = DefaultSamplers.LinearSamplerClampAni1;
            desc.AddressU = ToDXAddressMode(material.TextureDiffuse.WrapModeU);
            desc.AddressV = ToDXAddressMode(material.TextureDiffuse.WrapModeV);
            phong.DiffuseMapSampler = desc;
        }

        if (material.HasTextureNormal) {
            phong.NormalMap = LoadTexture(material.TextureNormal.FilePath);
            phong.NormalMapFilePath = material.TextureNormal.FilePath;
        } else if (material.HasTextureHeight) {
            phong.NormalMap = LoadTexture(material.TextureHeight.FilePath);
            phong.NormalMapFilePath = material.TextureHeight.FilePath;
        }

        if (material.HasTextureSpecular) {
            phong.SpecularColorMap = LoadTexture(material.TextureSpecular.FilePath);
            phong.SpecularColorMapFilePath = material.TextureSpecular.FilePath;
        }

        if (material.HasTextureDisplacement) {
            phong.DisplacementMap = LoadTexture(material.TextureDisplacement.FilePath);
            phong.DisplacementMapFilePath = material.TextureDisplacement.FilePath;
        }

        if (material.HasTextureOpacity) {
            phong.DiffuseAlphaMap = LoadTexture(material.TextureOpacity.FilePath);
            phong.DiffuseAlphaMapFilePath = material.TextureOpacity.FilePath;
        }

        if (material.HasTextureEmissive) {
            phong.EmissiveMap = LoadTexture(material.TextureEmissive.FilePath);
            phong.EmissiveMapFilePath = material.TextureEmissive.FilePath;
        }

        if (material.HasNonTextureProperty(AiMatKeys.UVTRANSFORM_BASE)) {
            var values = material.GetNonTextureProperty(AiMatKeys.UVTRANSFORM_BASE).GetFloatArrayValue();
            if (values != null && values.Length == 5)
                phong.UvTransform = new UvTransform(values[0],
                                                    new Vector2(values[1], values[2]),
                                                    new Vector2(values[3], values[4]));
        }

        return phong;
    }

    /// <summary>
    ///     To the PBR material.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <returns></returns>
    protected virtual PbrMaterialCore OnCreatePBRMaterial(Material material) {
        var pbr = new PbrMaterialCore {
            AlbedoColor = material.HasColorDiffuse
                              ? material.ColorDiffuse.ToSharpDXColor4()
                              : new Color4(0, 0, 0, 1),
            EmissiveColor = material.HasColorEmissive && !Configuration.IgnoreEmissiveColor
                                ? material.ColorEmissive.ToSharpDXColor4()
                                : new Color4(0, 0, 0, 1)
        };
        if (material.HasNonTextureProperty(GltfMatKeys.AiMatkeyGltfBasecolorFactor))
            pbr.AlbedoColor = material.GetNonTextureProperty(GltfMatKeys.AiMatkeyGltfBasecolorFactor)
                                      .GetColor4DValue().ToSharpDXColor4();
        if (material.HasNonTextureProperty(GltfMatKeys.AiMatkeyGltfMetallicFactor))
            pbr.MetallicFactor = material.GetNonTextureProperty(GltfMatKeys.AiMatkeyGltfMetallicFactor)
                                         .GetFloatValue();
        if (material.HasColorAmbient) pbr.AmbientOcclusionFactor = material.ColorAmbient.R;
        if (material.HasNonTextureProperty(GltfMatKeys.AiMatkeyGltfRoughnessFactor)) {
            pbr.RoughnessFactor = material.GetNonTextureProperty(GltfMatKeys.AiMatkeyGltfMetallicFactor)
                                          .GetFloatValue();
        } else if (material.HasColorSpecular && material.HasShininess) {
            //Ref https://github.com/assimp/assimp/blob/master/code/glTF2Exporter.cpp
            var specularIntensity = material.ColorSpecular.R * 0.2125f
                                    + material.ColorSpecular.G * 0.7154f + material.ColorSpecular.B * 0.0721f;
            var normalizedShininess = (float)Math.Sqrt(material.Shininess / 1000);
            normalizedShininess = Math.Min(Math.Max(normalizedShininess, 0), 1f);
            normalizedShininess *= specularIntensity;
            pbr.RoughnessFactor = 1 - normalizedShininess;
        }

        if (material.HasNonTextureProperty(GltfMatKeys.AiMatkeyGltfPbrspecularglossiness)) {
            var hasGlossiness = material.GetNonTextureProperty(GltfMatKeys.AiMatkeyGltfPbrspecularglossiness)
                                        .GetBooleanValue();
            if (hasGlossiness) {
                if (material.HasNonTextureProperty(GltfMatKeys
                                                       .AiMatkeyGltfPbrspecularglossinessGlossinessFactor))
                    pbr.ReflectanceFactor = material
                                            .GetNonTextureProperty(GltfMatKeys
                                                                       .AiMatkeyGltfPbrspecularglossinessGlossinessFactor)
                                            .GetFloatValue();
                else if (material.HasShininess) pbr.ReflectanceFactor = material.Shininess / 1000;
            }
        }

        if (material.HasOpacity) {
            var c = pbr.AlbedoColor;
            c.W = material.Opacity;
            pbr.AlbedoColor = c;
        }

        if (material.HasTextureDiffuse) {
            pbr.AlbedoMap = LoadTexture(material.TextureDiffuse.FilePath);
            pbr.AlbedoMapFilePath = material.TextureDiffuse.FilePath;
            var desc = DefaultSamplers.LinearSamplerClampAni1;
            desc.AddressU = ToDXAddressMode(material.TextureDiffuse.WrapModeU);
            desc.AddressV = ToDXAddressMode(material.TextureDiffuse.WrapModeV);
            pbr.SurfaceMapSampler = desc;
        }

        if (material.HasTextureNormal) {
            pbr.NormalMap = LoadTexture(material.TextureNormal.FilePath);
            pbr.NormalMapFilePath = material.TextureNormal.FilePath;
        } else if (material.HasTextureHeight) {
            pbr.NormalMap = LoadTexture(material.TextureHeight.FilePath);
            pbr.NormalMapFilePath = material.TextureHeight.FilePath;
        }

        if (material.HasProperty(GltfMatKeys.AiMatkeyGltfMetallicroughnessaoTexture,
                                 TextureType.Unknown,
                                 0)) {
            var t = material.GetProperty(GltfMatKeys.AiMatkeyGltfMetallicroughnessaoTexture,
                                         TextureType.Unknown,
                                         0);
            pbr.RoughnessMetallicMap = LoadTexture(t.GetStringValue());
            pbr.RoughnessMetallicMapFilePath = t.GetStringValue();
        } else if (material.HasTextureSpecular) {
            pbr.RoughnessMetallicMap = LoadTexture(material.TextureSpecular.FilePath);
            pbr.RoughnessMetallicMapFilePath = material.TextureSpecular.FilePath;
        }

        if (material.HasTextureDisplacement) {
            pbr.DisplacementMap = LoadTexture(material.TextureDisplacement.FilePath);
            pbr.DisplacementMapFilePath = material.TextureDisplacement.FilePath;
        }

        if (material.HasTextureLightMap) {
            pbr.AmbientOcculsionMap = LoadTexture(material.TextureLightMap.FilePath);
            pbr.AmbientOcculsionMapFilePath = material.TextureLightMap.FilePath;
        }

        if (material.HasTextureEmissive) {
            pbr.EmissiveMap = LoadTexture(material.TextureEmissive.FilePath);
            pbr.EmissiveMapFilePath = material.TextureEmissive.FilePath;
        }

        if (material.HasNonTextureProperty(AiMatKeys.UVTRANSFORM_BASE)) {
            var values = material.GetNonTextureProperty(AiMatKeys.UVTRANSFORM_BASE).GetFloatArrayValue();
            if (values != null && values.Length == 5)
                pbr.UvTransform = new UvTransform(values[0],
                                                  new Vector2(values[1], values[2]),
                                                  new Vector2(values[3], values[4]));
        }

        return pbr;
    }

    /// <summary>
    ///     To the helix material.
    /// </summary>
    /// <param name="material">The material.</param>
    /// <returns></returns>
    /// <exception cref="System.NotSupportedException">Shading Mode {material.ShadingMode}</exception>
    protected virtual KeyValuePair<Material, MaterialCore> OnCreateHelixMaterial(Material material) {
        MaterialCore? core = null;
        if (!material.HasShadingMode) {
            if (material.HasNonTextureProperty(GltfMatKeys.AiMatkeyGltfMetallicFactor)
                || material.HasNonTextureProperty(GltfMatKeys.AiMatkeyGltfRoughnessFactor)
                || material.HasNonTextureProperty(GltfMatKeys.AiMatkeyGltfBasecolorFactor))
                material.ShadingMode = ShadingMode.Fresnel;
            else if (material.HasColorSpecular || material.HasColorDiffuse || material.HasTextureDiffuse)
                material.ShadingMode = ShadingMode.Blinn;
            else
                material.ShadingMode = ShadingMode.Gouraud;
        }

        var mode = material.ShadingMode;
        if (Configuration.ImportMaterialType != MaterialType.Auto)
            switch (Configuration.ImportMaterialType) {
                case MaterialType.BlinnPhong:
                    mode = ShadingMode.Blinn;
                    break;
                case MaterialType.Diffuse:
                    mode = ShadingMode.Gouraud;
                    break;
                case MaterialType.Pbr:
                    mode = ShadingMode.Fresnel;
                    break;
                case MaterialType.VertexColor:
                    core = new ColorMaterialCore();
                    break;
                case MaterialType.Normal:
                    core = new NormalMaterialCore();
                    break;
                case MaterialType.Position:
                    core = new PositionMaterialCore();
                    break;
            }

        if (core == null)
            switch (mode) {
                case ShadingMode.Blinn:
                case ShadingMode.Phong:
                case ShadingMode.None:
                    core = OnCreatePhongMaterial(material);
                    break;
                case ShadingMode.CookTorrance:
                case ShadingMode.Fresnel:
                case ShadingMode.OrenNayar:
                    core = OnCreatePBRMaterial(material);
                    break;
                case ShadingMode.Gouraud:
                    var diffuse = new DiffuseMaterialCore {
                        DiffuseColor = material.ColorDiffuse.ToSharpDXColor4()
                    };
                    if (material.HasOpacity) {
                        var c = diffuse.DiffuseColor;
                        c.W = material.Opacity;
                        diffuse.DiffuseColor = c;
                    }

                    if (material.HasTextureDiffuse) {
                        diffuse.DiffuseMap = LoadTexture(material.TextureDiffuse.FilePath);
                        diffuse.DiffuseMapFilePath = material.TextureDiffuse.FilePath;
                    }

                    if (material.ShadingMode == ShadingMode.Flat) diffuse.EnableFlatShading = true;
                    core = diffuse;
                    break;
                case ShadingMode.Flat:
                    core = OnCreatePhongMaterial(material);
                    if (core is PhongMaterialCore p) p.EnableFlatShading = true;
                    break;
                default:
                    switch (Configuration.ImportMaterialType) {
                        case MaterialType.Position:
                            core = new PositionMaterialCore();
                            break;
                        case MaterialType.Normal:
                            core = new NormalMaterialCore();
                            break;
                        default:
                            Logger.Warn("Shading Mode is not supported: {Value0}", material.ShadingMode);
                            core = new DiffuseMaterialCore { DiffuseColor = new Color4(1, 0, 0, 1), EnableUnLit = true };
                            break;
                    }

                    break;
            }

        if (core is null) throw new InvalidOperationException($"Unsupported shading mode: {material.ShadingMode}");
        core.Name = string.IsNullOrEmpty(material.Name)
                        ? $"Material_{Interlocked.Increment(ref materialIndexForNoName)}"
                        : material.Name;
        return new KeyValuePair<Material, MaterialCore>(material, core);
    }

    protected virtual TextureModel? OnLoadEmbeddedTexture(EmbeddedTexture texture) {
        if (texture.HasCompressedData) {
            Logger.Info("Loading Embedded Compressed Texture. Format: {Value0}",
                                  texture.CompressedFormatHint);
            if (!SupportedTextureFormatDict.Contains(texture.CompressedFormatHint.ToLowerInvariant())) {
                Logger.Info("Compressed Texture Format not supported. Format: {Value0}",
                                      texture.CompressedFormatHint);
                return null;
            }

            var data = texture.CompressedData.ToArray();
            var stream = new MemoryStream(data);
            return new TextureModel(stream);
        }

        if (texture.HasNonCompressedData) {
            Logger.Info("Loading Embedded NonCompressed Texture");
            var rawData = texture.NonCompressedData
                                 .Select(x => new Color4(x.R / 255f, x.G / 255f, x.B / 255f, x.A / 255f)).ToArray();
            return new TextureModel(rawData, texture.Width, texture.Height);
        }

        return null;
    }


    private TextureModel? LoadTexture(string texturePath) {
        if (textureDict.TryGetValue(texturePath, out var s)) return s;

        var texture = OnLoadTexture(texturePath, out var actualPath);
        if (texture != null) {
            if (!string.IsNullOrEmpty(actualPath)) {
                // If texture is a separate file, uses file path as key and recheck whether exists
                if (!textureDict.TryAdd(actualPath, texture)) texture = textureDict[actualPath];
            } else {
                textureDict.TryAdd(texturePath, texture);
            }
        }

        return texture;
    }

    protected virtual TextureModel? OnLoadTexture(string texturePath, out string? actualPath) {
        actualPath = texturePath;
        try {
            //Check if is embedded material
            if (texturePath.StartsWith("*") && int.TryParse(texturePath.Substring(1, texturePath.Length - 1),
                                                            out var idx)
                                            && embeddedTextures.Count > idx)
                return OnLoadEmbeddedTexture(embeddedTextures[idx]);

            if (embeddedTextureDict.TryGetValue(texturePath, out var embeddedTex))
                return OnLoadEmbeddedTexture(embeddedTex);

            var ext = Path.GetExtension(texturePath);
            if (string.IsNullOrEmpty(ext) ||
                !SupportedTextureFormats.Contains(ext.TrimStart('.').ToLowerInvariant())) {
                Logger.Warn("Load Texture Failed. Texture Format not supported = {Value0}.", ext);

                return null;
            }

            actualPath = configuration.TexturePathResolver.Resolve(path, texturePath);
            return string.IsNullOrEmpty(actualPath) ? null : new TextureModel(actualPath);
        } catch (Exception ex) {
            Logger.Warn(ex, "Load Texture Exception. Texture Path = {Value0}", (object)texturePath);
        }

        return null;
    }


    private static TextureAddressMode ToDXAddressMode(TextureWrapMode mode) {
        switch (mode) {
            case TextureWrapMode.Clamp:
                return TextureAddressMode.Clamp;
            case TextureWrapMode.Mirror:
                return TextureAddressMode.Mirror;
            case TextureWrapMode.Wrap:
                return TextureAddressMode.Wrap;
            default:
                return TextureAddressMode.Wrap;
        }
    }
}
