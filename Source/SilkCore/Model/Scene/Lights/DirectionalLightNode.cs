/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public sealed class DirectionalLightNode : LightNode {
            public Vector3 Direction {
                get => (RenderCore as DirectionalLightCore).Direction;
                set => (RenderCore as DirectionalLightCore).Direction = value;
            }

            protected override RenderCore OnCreateRenderCore() {
                return new DirectionalLightCore();
            }
        }
    }
}
