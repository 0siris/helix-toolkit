using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model;
/// <summary>
///     Used to hold shared variables for Lights per scene
/// </summary>
public sealed class Light3DSceneShared : DisposeObject {
    public readonly LightsBufferModel LightModels = new();

}
