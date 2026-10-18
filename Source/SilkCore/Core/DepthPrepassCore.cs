using HelixToolkit.SharpDX.Core;
/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define TEST

using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
///     Do a depth prepass before rendering.
///     <para>
///         Must customize the DefaultEffectsManager and set DepthStencilState to
///         DefaultDepthStencilDescriptions.DSSDepthEqualNoWrite in default ShaderPass from EffectsManager to achieve best
///         performance.
///     </para>
/// </summary>
public sealed class DepthPrepassCore : RenderCore {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DepthPrepassCore" /> class.
    /// </summary>
    public DepthPrepassCore() : base(RenderType.PreProc) { }

}
