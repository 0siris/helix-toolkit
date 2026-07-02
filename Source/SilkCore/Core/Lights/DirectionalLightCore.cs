/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core
{
    namespace Core
    {
        using Model;
        public class DirectionalLightCore : LightCoreBase
        {
            private Vector3 direction;
            public Vector3 Direction
            {
                set
                {
                    SetAffectsRender(ref direction, value);
                }
                get
                {
                    return direction;
                }
            }

            public DirectionalLightCore()
            {
                LightType = LightType.Directional;
            }

            protected override void OnRender(Light3DSceneShared lightScene, int index)
            {
                base.OnRender(lightScene, index);
                lightScene.LightModels.Lights[index].LightDir = -SilkMath.TransformNormal(direction, ModelMatrix).Normalized().ToVector4(0);
            }
        }
    }
}
