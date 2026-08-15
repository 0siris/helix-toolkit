/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
///     Do a depth prepass before rendering.
///     <para>
///         Must customize the DefaultEffectsManager and set DepthStencilState to
///         DefaultDepthStencilDescriptions.DSSDepthEqualNoWrite in default ShaderPass from EffectsManager to achieve best
///         performance.
///     </para>
/// </summary>
public sealed class DepthPrepassNode : SceneNode {
    protected override RenderCore OnCreateRenderCore() => new DepthPrepassCore();

    public override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}
