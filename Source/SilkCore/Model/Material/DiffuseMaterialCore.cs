/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        [DataContract]
        public class DiffuseMaterialCore : MaterialCore {
            private Color4 diffuseColor = Color.White;
            private TextureModel diffuseMap;
            private SamplerStateDescription diffuseMapSampler = DefaultSamplers.LinearSamplerWrapAni4;

            private bool enableFlatShading;

            private bool enableUnLit;

            private bool renderDiffuseMap = true;

            private UVTransform uvTransform = UVTransform.Identity;

            private float vertexColorBlendingFactor;

            /// <summary>
            ///     Gets or sets the color of the diffuse.
            /// </summary>
            /// <value>
            ///     The color of the diffuse.
            /// </value>
            public Color4 DiffuseColor {
                get => diffuseColor;
                set => Set(ref diffuseColor, value);
            }

            /// <summary>
            ///     Gets or sets the diffuse map.
            /// </summary>
            /// <value>
            ///     The diffuse map.
            /// </value>
            public TextureModel DiffuseMap {
                get => diffuseMap;
                set => Set(ref diffuseMap, value);
            }

            /// <summary>
            ///     Gets or sets the diffuse map file path. Only for export
            /// </summary>
            /// <value>
            ///     The diffuse map file path.
            /// </value>
            public string DiffuseMapFilePath { get; set; }

            /// <summary>
            ///     Gets or sets the uv transform.
            /// </summary>
            /// <value>
            ///     The uv transform.
            /// </value>
            public UVTransform UVTransform {
                get => uvTransform;
                set => Set(ref uvTransform, value);
            }

            /// <summary>
            ///     Gets or sets the DiffuseMapSampler.
            /// </summary>
            /// <value>
            ///     DiffuseMapSampler
            /// </value>
            public SamplerStateDescription DiffuseMapSampler {
                get => diffuseMapSampler;
                set => Set(ref diffuseMapSampler, value);
            }

            /// <summary>
            /// </summary>
            public bool RenderDiffuseMap {
                get => renderDiffuseMap;
                set => Set(ref renderDiffuseMap, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether disable lighting. Directly render diffuse color and diffuse map
            /// </summary>
            /// <value>
            ///     <c>true</c> if [enable un lit]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableUnLit {
                get => enableUnLit;
                set => Set(ref enableUnLit, value);
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [enable flat shading].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [enable flat shading]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableFlatShading {
                get => enableFlatShading;
                set => Set(ref enableFlatShading, value);
            }

            /// <summary>
            ///     Gets or sets the vert color blending factor.
            ///     Diffuse = (1- <see cref="VertexColorBlendingFactor" />) * Diffuse + <see cref="VertexColorBlendingFactor" /> *
            ///     Vertex Color
            /// </summary>
            /// <value>
            ///     The vert color blending factor.
            /// </value>
            public float VertexColorBlendingFactor {
                get => vertexColorBlendingFactor;
                set => Set(ref vertexColorBlendingFactor, value);
            }

            public override MaterialVariable CreateMaterialVariables(
                IEffectsManager manager,
                IRenderTechnique technique
            ) {
                return new DiffuseMaterialVariables(DefaultPassNames.Diffuse, manager, technique, this);
            }
        }

        public sealed class ViewCubeMaterialCore : DiffuseMaterialCore {
            public override MaterialVariable CreateMaterialVariables(
                IEffectsManager manager,
                IRenderTechnique technique
            ) {
                return new DiffuseMaterialVariables(DefaultPassNames.ViewCube, manager, technique, this);
            }
        }
    }
}
