/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core
{
    namespace Model.Scene
    {
        public class ScreenQuadNode : SceneNode
        {
            private float depth = 1f;

            public ScreenQuadNode()
            {
                IsHitTestVisible = false;
            }

            /// <summary>
            ///     Gets or sets the texture.
            /// </summary>
            /// <value>
            ///     The texture.
            /// </value>
            public TextureModel Texture
            {
                get => (RenderCore as DrawScreenQuadCore).Texture;
                set => (RenderCore as DrawScreenQuadCore).Texture = value;
            }

            /// <summary>
            ///     Gets or sets the sampler.
            /// </summary>
            /// <value>
            ///     The sampler.
            /// </value>
            public SamplerStateDescription Sampler
            {
                get => (RenderCore as DrawScreenQuadCore).SamplerDescription;
                set => (RenderCore as DrawScreenQuadCore).SamplerDescription = value;
            }

            public float Depth
            {
                get => depth;
                set
                {
                    if (SetAffectsRender(ref depth, value))
                    {
                        var core = RenderCore as DrawScreenQuadCore;
                        core.ModelStruct.TopLeft.Z = core.ModelStruct.TopRight.Z =
                            core.ModelStruct.BottomLeft.Z = core.ModelStruct.BottomRight.Z = value;
                    }
                }
            }

            protected override RenderCore OnCreateRenderCore()
            {
                return new DrawScreenQuadCore();
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager)
            {
                return effectsManager[DefaultRenderTechniqueNames.ScreenQuad];
            }

            public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits)
            {
                return false;
            }

            protected sealed override bool OnHitTest(HitTestContext context, Matrix totalModelMatrix,
                ref List<HitTestResult> hits)
            {
                return false;
            }
        }
    }
}