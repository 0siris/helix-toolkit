using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public sealed class EmptyRenderCore : RenderCore {
    /// <summary>
    ///     Initializes a new instance of the <see cref="EmptyRenderCore" /> class.
    /// </summary>
    public EmptyRenderCore() : base(RenderType.None) { }

}
