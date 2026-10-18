using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2021 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
///     Clears the depth buffer and reset global transform.
/// </summary>
public class TopMostMeshRenderCore : RenderCore {
    public TopMostMeshRenderCore() : base(RenderType.ScreenSpaced) { }

}
