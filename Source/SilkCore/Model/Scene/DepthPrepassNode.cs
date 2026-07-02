/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using System.Collections.Generic;

namespace HelixToolkit.SharpDX.Core
{
    namespace Model.Scene
    {
        using Core;
        /// <summary>
        /// Do a depth prepass before rendering.
        /// <para>Must customize the DefaultEffectsManager and set DepthStencilState to DefaultDepthStencilDescriptions.DSSDepthEqualNoWrite in default ShaderPass from EffectsManager to achieve best performance.</para>
        /// </summary>
        public sealed class DepthPrepassNode : SceneNode
        {
            protected override RenderCore OnCreateRenderCore()
            {
                return new DepthPrepassCore();
            }

            public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits)
            {
                return false;
            }

            protected sealed override bool OnHitTest(HitTestContext context, Matrix totalModelMatrix, ref List<HitTestResult> hits)
            {
                return false;
            }
        }
    }
}
