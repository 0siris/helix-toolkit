// <copyright file="CrossSectionMeshGeometryModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2018 Helix Toolkit contributors
// </copyright>


using System.Collections.Generic;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
///     Do a depth prepass before rendering.
///     <para>
///         Must customize the DefaultEffectsManager and set DepthStencilState to
///         DefaultDepthStencilDescriptions.DSSDepthEqualNoWrite in default ShaderPass from EffectsManager to achieve best
///         performance.
///     </para>
/// </summary>
public sealed class DepthPrepassElement3D : Model.Elements3D.AbstractElements3D.Element3D {
    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode() => new DepthPrepassNode();

    /// <summary>
    ///     Hits the test.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="hits">The hits.</param>
    /// <returns></returns>
    public override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;
}
