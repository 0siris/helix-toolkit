/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core.Core;

public class DirectionalLightCore : LightCoreBase {
    private Vector3 direction;

    public DirectionalLightCore() => LightType = LightType.Directional;

    public Vector3 Direction {
        get => direction;
        set => SetAffectsRender(ref direction, value);
    }

    protected override void OnRender(Light3DSceneShared lightScene, int index) {
        base.OnRender(lightScene, index);
        lightScene.LightModels.Lights[index].LightDir = -SilkMath.TransformNormal(direction, ModelMatrix)
                                                                 .Normalized()
                                                                 .ToVector4(0);
    }
}