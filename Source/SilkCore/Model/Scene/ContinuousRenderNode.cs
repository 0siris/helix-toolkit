/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Render;
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
    protected override RenderCore OnCreateRenderCore() => new InvalidRendererCore();

    protected override bool CanHitTest(HitTestContext? context) => false;

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;

    private sealed class InvalidRendererCore : RenderCore {
        public InvalidRendererCore() : base(RenderType.GlobalEffect) { }

    }
}
