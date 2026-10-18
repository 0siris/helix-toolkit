using HelixToolkit.SharpDX.Core.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/



namespace HelixToolkit.SharpDX.Core.Model.Scene;
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
