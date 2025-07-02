/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using SharpDX;
using System;
using System.Collections.Generic;
#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Model.Scene
    {
        using Core;
        /// <summary>
        /// 
        /// </summary>
        public class NodePostEffectXRay : SceneNode
        {
            #region Properties
            /// <summary>
            /// Gets or sets the name of the effect.
            /// </summary>
            /// <value>
            /// The name of the effect.
            /// </value>
            public string EffectName
            {
                set => ((IPostEffectMeshXRay) RenderCore).EffectName = value;
                get => ((IPostEffectMeshXRay) RenderCore).EffectName;
            }
            /// <summary>
            /// Gets or sets the color.
            /// </summary>
            /// <value>
            /// The color.
            /// </value>
            public Color4 Color
            {
                set => ((IPostEffectMeshXRay) RenderCore).Color = value;
                get => ((IPostEffectMeshXRay) RenderCore).Color;
            }
            /// <summary>
            /// Gets or sets the outline fading factor.
            /// </summary>
            /// <value>
            /// The outline fading factor.
            /// </value>
            public float OutlineFadingFactor
            {
                set => ((IPostEffectMeshXRay) RenderCore).OutlineFadingFactor = value;
                get => ((IPostEffectMeshXRay) RenderCore).OutlineFadingFactor;
            }
            /// <summary>
            /// Gets or sets a value indicating whether [enable double pass].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [enable double pass]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableDoublePass
            {
                set => ((IPostEffectMeshXRay) RenderCore).EnableDoublePass = value;
                get => ((IPostEffectMeshXRay) RenderCore).EnableDoublePass;
            }
            #endregion

            /// <summary>
            /// Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() 
                => new PostEffectMeshXRayCore();

            public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) 
                => false;

            protected sealed override bool OnHitTest(HitTestContext context, Matrix totalModelMatrix, ref List<HitTestResult> hits) 
                => false;
        }
    }
}
