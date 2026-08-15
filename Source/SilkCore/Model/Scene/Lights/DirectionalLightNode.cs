/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Lights;

namespace HelixToolkit.SharpDX.Core.Model.Scene.Lights;
/// <summary>
/// </summary>
public sealed class DirectionalLightNode : LightNode {
    public Vector3 Direction {
        get => (RenderCore as DirectionalLightCore
                ?? throw new InvalidOperationException("Directional light render core was not created.")).Direction;
        set => (RenderCore as DirectionalLightCore
                ?? throw new InvalidOperationException("Directional light render core was not created.")).Direction = value;
    }

    protected override RenderCore OnCreateRenderCore() => new DirectionalLightCore();
}
