/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Utilities;
public sealed class HitTestContext {
        /// <summary>
        ///     Initializes a new instance of the <see cref="HitTestContext" /> class.
        /// </summary>
        /// <param name="metrices">The render metrices.</param>
        /// <param name="rayWs">The ray in world space.</param>
        /// <param name="hitSp">
        ///     The hit point on screen space. Pass in the hit point on viewport region directly.
        ///     <para>Do not scale with DpiScale factor.</para>
        /// </param>
    public HitTestContext(IRenderMatrices metrices, ref Ray rayWs, ref Vector2 hitSp) {
        RenderMatrices = metrices;
        RayWs = rayWs;
        HitPointSp = hitSp;
    }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HitTestContext" /> class.
        /// </summary>
        /// <param name="metrices">The render metrices.</param>
        /// <param name="rayWs">The ray in world space.</param>
        /// <param name="hitSp">
        ///     The hit point on screen space. Pass in the hit point on viewport region directly.
        ///     <para>Do not scale with DpiScale factor.</para>
        /// </param>
    public HitTestContext(IRenderMatrices metrices, Ray rayWs, Vector2 hitSp)
        : this(metrices, ref rayWs, ref hitSp) { }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HitTestContext" /> class.
        ///     This calculates screen hit point automatically from metrices and world space ray.
        /// </summary>
        /// <param name="metrices">The render metrices.</param>
        /// <param name="rayWs">The ray in world space.</param>
    public HitTestContext(IRenderMatrices metrices, ref Ray rayWs) {
        RenderMatrices = metrices;
        RayWs = rayWs;
        HitPointSp = metrices.Project(rayWs.Position);
    }

        /// <summary>
        ///     Initializes a new instance of the <see cref="HitTestContext" /> class.
        ///     This calculates ray in world space automatically from metrices and hit point.
        /// </summary>
        /// <param name="metrices">The render metrices.</param>
        /// <param name="hitSp">
        ///     Screen hit point. Pass in the hit point on viewport region directly.
        ///     <para>Do not scale with DpiScale factor.</para>
        /// </param>
    public HitTestContext(IRenderMatrices metrices, ref Vector2 hitSp) {
        RenderMatrices = metrices;
        HitPointSp = hitSp;
        metrices.UnProject(hitSp, out var ray);
        RayWs = ray;
    }

        /// <summary>
        ///     Gets or sets the render matrices. This is only needed for line/point hit test.
        /// </summary>
        /// <value>
        ///     The render matrices.
        /// </value>
    public IRenderMatrices RenderMatrices { get; set; }

        /// <summary>
        ///     Gets or sets the ray in world space.
        /// </summary>
        /// <value>
        ///     The ray.
        /// </value>
    public Ray RayWs { get; set; }

        /// <summary>
        ///     Gets or sets the hit point on screen space. This is the hit point on viewport region without DpiScaled coordinate.
        /// </summary>
        /// <value>
        ///     The screen hit point.
        /// </value>
    public Vector2 HitPointSp { get; set; }
}
