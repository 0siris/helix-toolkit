/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;


namespace HelixToolkit.SharpDX.Core;

public abstract class BillboardBase : Geometry3D, IBillboardText {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    private bool isInitialized;

    public float Height { get; protected set; }

    public abstract BillboardType Type { get; }

    public TextureModel? Texture { get; protected set; }

    public float Width { get; protected set; }

    public bool IsInitialized {
        get => isInitialized;
        protected set {
            isInitialized = value;
            if (!isInitialized)
                //Notify to rebuild the texture or billboard vertices
                RaisePropertyChanged(VertexBuffer);
        }
    }

    public IList<BillboardVertex> BillboardVertices { get; } = [];

    /// <summary>
    ///     Draws the texture and fill the billboardverties. Called during initialize vertex buffer.
    /// </summary>
    /// <param name="deviceResources">The device resources.</param>
    public void DrawTexture(IDeviceResources deviceResources) {
        if (!isInitialized) {
            if (Logger.IsEnabled(LogLevel.Trace)) Logger.Verbose("Billboard update texture and verts");
            BillboardVertices.Clear();
            OnUpdateTextureAndBillboardVertices(deviceResources);
            UpdateBounds();
        }

        isInitialized = true;
    }

    protected abstract void OnUpdateTextureAndBillboardVertices(IDeviceResources deviceResources);

    protected override void OnAssignTo(Geometry3D target) {
        base.OnAssignTo(target);
        if (target is BillboardBase billboard) {
            billboard.Texture = Texture;
            billboard.IsInitialized = false;
        }
    }

    public void Invalidate() {
        IsInitialized = false;
    }

    #region HitTest

    /// <summary>
    ///     Hits the test.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="modelMatrix">The model matrix.</param>
    /// <param name="hits">The hits.</param>
    /// <param name="originalSource">The original source.</param>
    /// <param name="fixedSize">if set to <c>true</c> [fixed size].</param>
    /// <returns></returns>
    public abstract bool HitTest(
        HitTestContext context,
        Matrix modelMatrix,
        ref List<HitTestResult> hits,
        object originalSource,
        bool fixedSize
    );


    /// <summary>
    ///     Hits the size of the test fixed.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="modelMatrix">The model matrix.</param>
    /// <param name="hits">The hits.</param>
    /// <param name="originalSource">The original source.</param>
    /// <param name="count">The count of vertices in <see cref="BillboardBase.BillboardVertices" />.</param>
    /// <returns></returns>
    protected bool HitTestFixedSize(
        HitTestContext context,
        ref Matrix modelMatrix,
        ref List<HitTestResult> hits,
        object originalSource,
        int count
    ) {
        if (BillboardVertices.Count == 0) return false;
        var h = false;
        var result = new BillboardHitResult {
            Distance = double.MaxValue
        };
        var visualToScreen = context.RenderMatrices.ScreenViewProjectionMatrix;
        var screenPoint = context.HitPointSp * context.RenderMatrices.DpiScale;
        if (screenPoint.X < 0 || screenPoint.Y < 0) return false;

        for (var i = 0; i < count; ++i) {
            var vert = BillboardVertices[i];
            var pos = vert.Position.ToVector3();
            var c = SilkMath.TransformCoordinate(pos, modelMatrix);
            var dir = c - context.RayWs.Position;
            if (SilkMath.Dot(dir, context.RayWs.Direction) < 0) continue;
            var quad = GetScreenQuad(ref c,
                ref vert.OffTL,
                ref vert.OffTR,
                ref vert.OffBL,
                ref vert.OffBR,
                ref visualToScreen,
                context.RenderMatrices.DpiScale);
            if (quad.IsPointInQuad2D(ref screenPoint)) {
                var v = c - context.RayWs.Position;
                var dist = SilkMath.Dot(context.RayWs.Direction, v);
                if (dist > result.Distance) continue;
                h = true;

                result.ModelHit = originalSource;
                result.IsValid = true;
                result.PointHit = context.RayWs.Position + context.RayWs.Direction * dist;
                result.Distance = dist;
                result.Geometry = this;
                AssignResultAdditional(result, i);
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.Verbose("Hit; HitPoint:{Value0}; Text={Value1}", [
                        result.PointHit,
                        result.TextInfo == null
                            ? Type.ToString()
                            : result.TextInfo.Text
                    ]);
            }
        }

        if (h) hits.Add(result);
        return h;
    }

    protected virtual void AssignResultAdditional(BillboardHitResult result, int index) {
        result.TextInfoIndex = index;
        result.Type = Type;
    }

    protected override void OnClearAllGeometryData() {
        base.OnClearAllGeometryData();
        BillboardVertices.Clear();
        (BillboardVertices as FastList<BillboardVertex>)?.TrimExcess();
    }

    /// <summary>
    ///     Hits the size of the test non fixed.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="modelMatrix">The model matrix.</param>
    /// <param name="hits">The hits.</param>
    /// <param name="originalSource">The original source.</param>
    /// <param name="count">The count of vertices in <see cref="BillboardBase.BillboardVertices" />.</param>
    /// <returns></returns>
    protected bool HitTestNonFixedSize(
        HitTestContext context,
        ref Matrix modelMatrix,
        ref List<HitTestResult> hits,
        object originalSource,
        int count
    ) {
        if (BillboardVertices.Count == 0) return false;
        var h = false;
        var result = new BillboardHitResult {
            Distance = double.MaxValue
        };
        var viewMatrix = context.RenderMatrices.ViewMatrix;
        var viewMatrixInv = viewMatrix.PsudoInvert();
        var rayWs = context.RayWs;
        for (var i = 0; i < count; ++i) {
            var vert = BillboardVertices[i];
            var pos = vert.Position.ToVector3();
            var c = SilkMath.TransformCoordinate(pos, modelMatrix);
            var dir = c - rayWs.Position;
            if (SilkMath.Dot(dir, rayWs.Direction) < 0) continue;
            var quad = GetHitTestQuad(ref c,
                ref vert.OffTL,
                ref vert.OffTR,
                ref vert.OffBL,
                ref vert.OffBR,
                ref viewMatrix,
                ref viewMatrixInv);
            if (Collision.RayIntersectsTriangle(ref rayWs, ref quad.Tl, ref quad.Tr, ref quad.Br, out Vector3 hitPoint)
                || Collision.RayIntersectsTriangle(ref rayWs, ref quad.Tl, ref quad.Br, ref quad.Bl, out hitPoint)) {
                var dist = (rayWs.Position - hitPoint).Length;
                if (dist > result.Distance) continue;
                h = true;
                result.ModelHit = originalSource;
                result.IsValid = true;
                result.PointHit = hitPoint;
                result.Distance = dist;
                result.Geometry = this;
                AssignResultAdditional(result, i);
                if (Logger.IsEnabled(LogLevel.Trace))
                    Logger.Verbose("Hit; HitPoint:{Value0}; Text={Value1}", [
                        result.PointHit,
                        result.TextInfo == null
                            ? Type.ToString()
                            : result.TextInfo.Text
                    ]);
            }
        }

        if (h) hits.Add(result);
        return h;
    }

    protected static void GetQuadOffset(
        float width,
        float height,
        BillboardHorizontalAlignment horizontalAlignment,
        BillboardVerticalAlignment verticalAlignment,
        out Vector2 topLeft,
        out Vector2 bottomRight
    ) {
        float top = 0;
        float bottom = 0;
        float left = 0;
        float right = 0;
        switch (horizontalAlignment) {
            case BillboardHorizontalAlignment.Center:
                left = -width / 2;
                right = width / 2;
                break;
            case BillboardHorizontalAlignment.Left:
                left = -width;
                right = 0;
                break;
            case BillboardHorizontalAlignment.Right:
                left = 0;
                right = width;
                break;
        }

        switch (verticalAlignment) {
            case BillboardVerticalAlignment.Center:
                top = height / 2;
                bottom = -height / 2;
                break;
            case BillboardVerticalAlignment.Top:
                top = height;
                bottom = 0;
                break;
            case BillboardVerticalAlignment.Bottom:
                top = 0;
                bottom = -height;
                break;
        }

        topLeft = new Vector2(left, top);
        bottomRight = new Vector2(right, bottom);
    }

    private struct Quad {
        public Vector3 Tl;
        public Vector3 Tr;
        public Vector3 Bl;
        public Vector3 Br;

        public Quad(ref Vector3 tl, ref Vector3 tr, ref Vector3 bl, ref Vector3 br) {
            Tl = tl;
            Tr = tr;
            Bl = bl;
            Br = br;
        }

        public Quad(Vector3 tl, Vector3 tr, Vector3 bl, Vector3 br) {
            Tl = tl;
            Tr = tr;
            Bl = bl;
            Br = br;
        }
    }

    private struct Quad2D {
        public readonly Vector2 Tl;
        public readonly Vector2 Tr;
        public readonly Vector2 Bl;
        public readonly Vector2 Br;

        public Quad2D(ref Vector2 tl, ref Vector2 tr, ref Vector2 bl, ref Vector2 br) {
            Tl = tl;
            Tr = tr;
            Bl = bl;
            Br = br;
        }

        public Quad2D(Vector2 tl, Vector2 tr, Vector2 bl, Vector2 br) {
            Tl = tl;
            Tr = tr;
            Bl = bl;
            Br = br;
        }


        public bool IsPointInQuad2D(Vector2 point) => IsPointInQuad2D(ref point);


        public bool IsPointInQuad2D(ref Vector2 point) =>
            //var v1 = point - TL;
            //var t1 = BL - TL;
            //if(SilkMath.Dot(v1, t1) < 0)
            //{
            //    return false;
            //}
            //var v2 = point - BL;
            //var t2 = BR - BL;
            //if (SilkMath.Dot(v2, t2) < 0)
            //{
            //    return false;
            //}
            //var v3 = point - BR;
            //var t3 = TR - BR;
            //if (SilkMath.Dot(v3, t3) < 0)
            //{
            //    return false;
            //}
            //var v4 = point - TR;
            //var t4 = TL - TR;
            //if (SilkMath.Dot(v4, t4) < 0)
            //{
            //    return false;
            //}
            //return true;
            SilkMath.Dot(point - Tl, Bl - Tl) >= 0 && SilkMath.Dot(point - Bl, Br - Bl) >= 0
                                                   && SilkMath.Dot(point - Br, Tr - Br) >= 0 &&
                                                   SilkMath.Dot(point - Tr, Tl - Tr) >= 0;
    }

    private static Quad GetHitTestQuad(
        ref Vector3 center,
        ref Vector2 tl,
        ref Vector2 tr,
        ref Vector2 bl,
        ref Vector2 br,
        ref Matrix viewMatrix,
        ref Matrix viewMatrixInv
    ) {
        var vcenter = SilkMath.TransformCoordinate(center, viewMatrix);
        var vcX = vcenter.X;
        var vcY = vcenter.Y;

        var transformedBl = new Vector3(vcX + bl.X, vcY + bl.Y, vcenter.Z);
        var transformedBr = new Vector3(vcX + br.X, vcY + br.Y, vcenter.Z);
        var transformedTr = new Vector3(vcX + tr.X, vcY + tr.Y, vcenter.Z);
        var transformedTl = new Vector3(vcX + tl.X, vcY + tl.Y, vcenter.Z);

        transformedBl = SilkMath.TransformCoordinate(transformedBl, viewMatrixInv);
        transformedBr = SilkMath.TransformCoordinate(transformedBr, viewMatrixInv);
        transformedTr = SilkMath.TransformCoordinate(transformedTr, viewMatrixInv);
        transformedTl = SilkMath.TransformCoordinate(transformedTl, viewMatrixInv);
        return new Quad(ref transformedTl, ref transformedTr, ref transformedBl, ref transformedBr);
    }

    private static Quad2D GetScreenQuad(
        ref Vector3 center,
        ref Vector2 tl,
        ref Vector2 tr,
        ref Vector2 bl,
        ref Vector2 br,
        ref Matrix screenViewProjection,
        float scale
    ) {
        var vcenter = SilkMath.TransformCoordinate(center, screenViewProjection);
        var p = new Vector2(vcenter.X, vcenter.Y);
        var screenTl = p + new Vector2(tl.X, -tl.Y) * scale;
        var screenTr = p + new Vector2(tr.X, -tr.Y) * scale;
        var screenBl = p + new Vector2(bl.X, -bl.Y) * scale;
        var screenBr = p + new Vector2(br.X, -br.Y) * scale;
        return new Quad2D(ref screenTl, ref screenTr, ref screenBl, ref screenBr);
    }

    #endregion
}
