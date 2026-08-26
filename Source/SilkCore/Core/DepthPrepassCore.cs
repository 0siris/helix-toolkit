/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define TEST

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;

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

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
    }

    protected override bool OnAttach(IRenderTechnique technique) => true;

    protected override void OnDetach() { }
}
