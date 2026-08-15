/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public sealed class EmptyRenderCore : RenderCore {
    /// <summary>
    ///     Initializes a new instance of the <see cref="EmptyRenderCore" /> class.
    /// </summary>
    public EmptyRenderCore() : base(RenderType.None) { }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    public override void Render(RenderContext context, DeviceContextProxy deviceContext) { }

    protected override bool OnAttach(IRenderTechnique technique) => true;

    protected override void OnDetach() { }
}