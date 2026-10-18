using HelixToolkit.SharpDX.Core.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public sealed class AmbientLightNode : LightNode {
    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() => new AmbientLightCore();
}
