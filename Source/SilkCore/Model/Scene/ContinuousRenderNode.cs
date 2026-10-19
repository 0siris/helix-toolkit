using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
///     Use this node to keep update rendering in each frame.
///     <para>
///         Default behavior for render host is lazy rendering.
///         Only property changes trigger a render inside render loop.
///         However, sometimes user want to keep updating rendering for each frame such as doing shader animation using
///         TimeStamp.
///         Use this node to invalidate rendering and keep render host busy.
///     </para>
/// </summary>
public sealed class ContinuousRenderNode : SceneNode {
    /// <inheritdoc />
    protected override RenderCore OnCreateRenderCore() => new InvalidRendererCore();

    /// <inheritdoc />
    protected override bool CanHitTest(HitTestContext? context) => false;

    /// <inheritdoc />
    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;

    /// <inheritdoc />
    public override void Update(RenderContext context) {
        base.Update(context);
        if (!IsRenderable) return;
        for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent)
            if (!ancestor.Visible || ancestor.IsDisposed) return;
        RenderHost?.InvalidateRender();
    }

    /// <summary>Supplies the attached global-effect marker used by continuous source nodes.</summary>
    private sealed class InvalidRendererCore : RenderCore {
        /// <summary>Initializes the global-effect marker without GPU draw resources.</summary>
        public InvalidRendererCore() : base(RenderType.GlobalEffect) { }

    }
}
