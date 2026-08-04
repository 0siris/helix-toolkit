/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
///     Screen Spaced node uses a fixed camera to render model (Mainly used for view box and coordinate system rendering)
///     onto screen which is separated from viewport camera.
///     <para>
///         Default fix camera is perspective camera with FOV 45 degree and camera distance = 20. Look direction is always
///         looking at (0,0,0).
///     </para>
///     <para>
///         User must properly scale the model to fit into the camera frustum. The usual maximum size is from (5,5,5) to
///         (-5,-5,-5) bounding box.
///     </para>
///     <para>
///         User can use <see cref="ScreenSpacedNode.SizeScale" /> to scale the size of the rendering.
///     </para>
/// </summary>
public class ScreenSpacedNode : GroupNode {
    private readonly ScreenSpacedContext screenSpacedContext = new();

    private List<HitTestResult> screenSpaceHits = [];

    public ScreenSpacedNode() {
        AffectsGlobalVariable = true;
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [need clear depth buffer].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [need clear depth buffer]; otherwise, <c>false</c>.
    /// </value>
    protected bool NeedClearDepthBuffer { get; set; } = true;

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() {
        var core = new ScreenSpacedMeshRenderCore();
        core.OnCoordinateSystemChanged += Core_OnCoordinateSystemChanged;
        return core;
    }

    private void Core_OnCoordinateSystemChanged(object sender, BoolArgs e) {
        OnCoordinateSystemChanged(e.Value);
    }

    protected virtual void OnCoordinateSystemChanged(bool e) { }

    protected override bool OnAttach(IEffectsManager effectsManager) {
        RenderCore.Attach(EffectTechnique);
        var screenSpaceCore = RenderCore as ScreenSpacedMeshRenderCore;
        screenSpaceCore.RelativeScreenLocationX = RelativeScreenLocationX;
        screenSpaceCore.RelativeScreenLocationY = RelativeScreenLocationY;
        screenSpaceCore.SizeScale = SizeScale;
        return base.OnAttach(effectsManager);
    }

    /// <summary>
    ///     Called when [detach].
    /// </summary>
    protected override void OnDetach() {
        RenderCore.Detach();
        base.OnDetach();
    }


    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        screenSpacedContext.RenderHost = context.RenderMatrices.RenderHost;
        var newRay = new Ray();
        var hitSP = context.HitPointSP;
        var preHit = false;
        screenSpacedContext.NearPlane = NearPlane;
        screenSpacedContext.FarPlane = FarPlane;
        switch (Mode) {
            case ScreenSpacedMode.RelativeScreenSpaced:
                preHit = CreateRelativeScreenModeRay(context, out newRay, out hitSP);
                break;
            case ScreenSpacedMode.AbsolutePosition3D:
                preHit = CreateAbsoluteModeRay(context, out newRay, out hitSP);
                break;
        }

        if (!preHit) return false;
        screenSpacedContext.Update();
        screenSpaceHits.Clear();
        var spHitContext = new HitTestContext(screenSpacedContext, newRay, hitSP);
        if (base.OnHitTest(spHitContext, totalModelMatrix, ref screenSpaceHits)) {
            hits ??= [];
            hits.Clear();
            hits.AddRange(screenSpaceHits);
            return true;
        }

        return false;
    }

    private bool CreateRelativeScreenModeRay(HitTestContext context, out Ray newRay, out Vector2 hitSP) {
        var p = context.HitPointSP *
                context.RenderMatrices
                       .DpiScale; //SilkMath.TransformCoordinate(context.RayWS.Position, context.RenderMatrices.ScreenViewProjectionMatrix);
        var screenSpaceCore = RenderCore as ScreenSpacedMeshRenderCore;
        screenSpacedContext.IsPerspective = screenSpaceCore.IsPerspective;
        var viewportSize = screenSpaceCore.Size * screenSpaceCore.SizeScale * context.RenderMatrices.DpiScale;

        var offx = context.RenderMatrices.ActualWidth / 2 * (1 + screenSpaceCore.RelativeScreenLocationX) -
                   viewportSize / 2;
        var offy = context.RenderMatrices.ActualHeight / 2 * (1 - screenSpaceCore.RelativeScreenLocationY) -
                   viewportSize / 2;
        offx = Math.Max(0, Math.Min(offx, (int)(context.RenderMatrices.ActualWidth - viewportSize)));
        offy = Math.Max(0, Math.Min(offy, (int)(context.RenderMatrices.ActualHeight - viewportSize)));

        var px = p.X - offx;
        var py = p.Y - offy;

        if (px < 0 || py < 0 || px > viewportSize || py > viewportSize) {
            newRay = new Ray();
            hitSP = Vector2.Zero;
            return false;
        }

        hitSP = new Vector2(px, py) / context.RenderMatrices.DpiScale;
        var viewMatrix = screenSpaceCore.GlobalTransform.View;
        var projMatrix = screenSpaceCore.GlobalTransform.Projection;
        newRay = new Vector2(px, py).UnProject(ref viewMatrix,
                                               ref projMatrix,
                                               screenSpaceCore.NearPlane,
                                               viewportSize,
                                               viewportSize,
                                               screenSpaceCore.IsPerspective);
        screenSpacedContext.ViewMatrix = viewMatrix;
        screenSpacedContext.ViewMatrixInv = viewMatrix.PsudoInvert();
        screenSpacedContext.ProjectionMatrix = projMatrix;
        screenSpacedContext.ActualWidth = screenSpacedContext.ActualHeight = viewportSize;
        return true;
    }

    private bool CreateAbsoluteModeRay(HitTestContext context, out Ray newRay, out Vector2 hitSP) {
        var screenSpaceCore = RenderCore as ScreenSpacedMeshRenderCore;
        screenSpacedContext.IsPerspective = screenSpaceCore.IsPerspective;
        var viewMatrix = screenSpaceCore.GlobalTransform.View;
        var projMatrix = screenSpaceCore.GlobalTransform.Projection;
        screenSpacedContext.ViewMatrix = viewMatrix;
        screenSpacedContext.ViewMatrixInv = viewMatrix.PsudoInvert();
        screenSpacedContext.ProjectionMatrix = projMatrix;
        if (context.RenderMatrices.IsPerspective) {
            hitSP = context.HitPointSP;
            newRay = (hitSP * context.RenderMatrices.DpiScale).UnProject(ref viewMatrix,
                ref projMatrix,
                screenSpaceCore.NearPlane,
                context.RenderMatrices.ActualWidth,
                context.RenderMatrices.ActualHeight,
                screenSpaceCore.IsPerspective);
            screenSpacedContext.ActualWidth = context.RenderMatrices.ActualWidth;
            screenSpacedContext.ActualHeight = context.RenderMatrices.ActualHeight;
            return true;
        }

        var p = context.HitPointSP * context.RenderMatrices.DpiScale;
        var viewportSize = screenSpaceCore.Size * screenSpaceCore.SizeScale * context.RenderMatrices.DpiScale;

        var abs = SilkMath.TransformCoordinate(AbsolutePosition3D,
                                               context.RenderMatrices.ScreenViewProjectionMatrix);
        var offx = abs.X - viewportSize / 2;
        var offy = abs.Y - viewportSize / 2;

        var px = p.X - offx;
        var py = p.Y - offy;

        if (px < 0 || py < 0 || px > viewportSize || py > viewportSize) {
            newRay = new Ray();
            hitSP = Vector2.Zero;
            return false;
        }

        hitSP = new Vector2(px, py) / context.RenderMatrices.DpiScale;
        newRay = new Vector2(px, py).UnProject(ref viewMatrix,
                                               ref projMatrix,
                                               screenSpaceCore.NearPlane,
                                               viewportSize,
                                               viewportSize,
                                               screenSpaceCore.IsPerspective);
        screenSpacedContext.ActualWidth = viewportSize;
        screenSpacedContext.ActualHeight = viewportSize;
        screenSpacedContext.ViewMatrix = viewMatrix;
        screenSpacedContext.ViewMatrixInv = viewMatrix.PsudoInvert();
        screenSpacedContext.ProjectionMatrix = projMatrix;
        return true;
    }

    private sealed class ScreenSpacedContext : IRenderMatrices {
        public float NearPlane { get; set; }

        public float FarPlane { get; set; }

        public CameraCore Camera => RenderHost?.RenderContext.Camera;

        public Matrix ViewMatrix { get; set; }

        public Matrix ViewMatrixInv { get; set; }

        public Matrix ProjectionMatrix { get; set; }

        public Matrix ViewportMatrix =>
            new(ActualWidth / 2,
                0,
                0,
                0,
                0,
                -(ActualHeight / 2),
                0,
                0,
                0,
                0,
                1,
                0,
                (ActualWidth - 1) / 2,
                (ActualHeight - 1) / 2,
                0,
                1);

        public Matrix ScreenViewProjectionMatrix { get; private set; }

        public bool IsPerspective { get; set; }

        public float ActualWidth { get; set; }

        public float ActualHeight { get; set; }

        public float DpiScale => RenderHost.DpiScale;

        public IRenderHost RenderHost { get; set; }

        public FrustumCameraParams CameraParams { get; private set; }

        public void Update() {
            ScreenViewProjectionMatrix = ViewMatrix * ProjectionMatrix * ViewportMatrix;
            if (Camera != null)
                CameraParams = Camera.CreateCameraParams(ActualWidth / ActualHeight, NearPlane, FarPlane);
        }
    }

    #region Properties

    /// <summary>
    ///     Gets or sets the relative screen location x.
    /// </summary>
    /// <value>
    ///     The relative screen location x.
    /// </value>
    public float RelativeScreenLocationX {
        get => (RenderCore as IScreenSpacedRenderParams).RelativeScreenLocationX;
        set => (RenderCore as IScreenSpacedRenderParams).RelativeScreenLocationX = value;
    }

    /// <summary>
    ///     Gets or sets the relative screen location y.
    /// </summary>
    /// <value>
    ///     The relative screen location y.
    /// </value>
    public float RelativeScreenLocationY {
        get => (RenderCore as IScreenSpacedRenderParams).RelativeScreenLocationY;
        set => (RenderCore as IScreenSpacedRenderParams).RelativeScreenLocationY = value;
    }

    /// <summary>
    ///     Gets or sets the size scale.
    /// </summary>
    /// <value>
    ///     The size scale.
    /// </value>
    public float SizeScale {
        get => (RenderCore as IScreenSpacedRenderParams).SizeScale;
        set => (RenderCore as IScreenSpacedRenderParams).SizeScale = value;
    }

    /// <summary>
    ///     Gets or sets the mode. Includes <see cref="ScreenSpacedMode.RelativeScreenSpaced" /> and
    ///     <see cref="ScreenSpacedMode.AbsolutePosition3D" />
    /// </summary>
    /// <value>
    ///     The mode.
    /// </value>
    public ScreenSpacedMode Mode {
        get => (RenderCore as IScreenSpacedRenderParams).Mode;
        set => (RenderCore as IScreenSpacedRenderParams).Mode = value;
    }

    /// <summary>
    ///     Gets or sets the absolute position. <see cref="ScreenSpacedMode.AbsolutePosition3D" />
    /// </summary>
    /// <value>
    ///     The absolute position.
    /// </value>
    public Vector3 AbsolutePosition3D {
        get => (RenderCore as IScreenSpacedRenderParams).AbsolutePosition3D;
        set => (RenderCore as IScreenSpacedRenderParams).AbsolutePosition3D = value;
    }

    /// <summary>
    ///     Only being used when <see cref="Mode" /> is RelativeScreenSpaced
    /// </summary>
    public ScreenSpacedCameraType CameraType {
        get => (RenderCore as IScreenSpacedRenderParams).CameraType;
        set => (RenderCore as IScreenSpacedRenderParams).CameraType = value;
    }

    /// <summary>
    ///     Gets or sets the far plane for screen spaced camera
    /// </summary>
    /// <value>
    ///     The far plane.
    /// </value>
    public float FarPlane {
        get => (RenderCore as IScreenSpacedRenderParams).FarPlane;
        set => (RenderCore as IScreenSpacedRenderParams).FarPlane = value;
    }

    /// <summary>
    ///     Gets or sets the near plane for screen spaced camera
    /// </summary>
    /// <value>
    ///     The near plane.
    /// </value>
    public float NearPlane {
        get => (RenderCore as IScreenSpacedRenderParams).NearPlane;
        set => (RenderCore as IScreenSpacedRenderParams).NearPlane = value;
    }

    #endregion
}
