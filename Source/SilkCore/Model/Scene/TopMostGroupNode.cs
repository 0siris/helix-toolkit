/*
The MIT License (MIT)
Copyright (c) 2021 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        ///     Provides a way to render child elements always on top of other elements.
        ///     This is rendered at the same level of screen spaced group items.
        ///     Child items do not support post effects.
        /// </summary>
        public class TopMostGroupNode : GroupNode {
            private bool enableTopMost = true;

            public TopMostGroupNode() {
                AffectsGlobalVariable = true;
            }

            public bool EnableTopMost {
                get => enableTopMost;
                set {
                    if (SetAffectsRender(ref enableTopMost, value))
                        RenderType = value ? RenderType.ScreenSpaced : RenderType.Opaque;
                }
            }

            protected override RenderCore OnCreateRenderCore() {
                var core = new TopMostMeshRenderCore();
                return core;
            }
        }
    }
}
