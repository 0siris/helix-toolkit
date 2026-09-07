/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Logger;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class ViewBoxNode : ScreenSpacedNode {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    static ViewBoxNode() {
        var builder = new MeshBuilder(true, false);
        var cornerSize = Size / 5;
        builder.AddBox(Vector3.Zero, cornerSize, cornerSize, cornerSize);
        CornerGeometry = builder.ToMesh();

        builder = new MeshBuilder(true, false);
        var halfSize = Size / 2;
        var edgeSize = halfSize * 1.5f;
        builder.AddBox(Vector3.Zero, cornerSize, edgeSize, cornerSize);
        EdgeGeometry = builder.ToMesh();

        CornerInstances = new Matrix[CornerPoints.Length];
        for (var i = 0; i < CornerPoints.Length; ++i)
            CornerInstances[i] = SilkMath.Translation(CornerPoints[i] * Size / 2 * 0.95f);
        var count = XAligned.Length;
        EdgeInstances = new Matrix[count * 3];

        for (var i = 0; i < count; ++i)
            EdgeInstances[i] = SilkMath.RotationZ((float)Math.PI / 2) *
                               SilkMath.Translation(XAligned[i] * halfSize * 0.95f);
        for (var i = count; i < count * 2; ++i)
            EdgeInstances[i] = SilkMath.Translation(YAligned[i % count] * halfSize * 0.95f);
        for (var i = count * 2; i < count * 3; ++i)
            EdgeInstances[i] = SilkMath.RotationX((float)Math.PI / 2) *
                               SilkMath.Translation(ZAligned[i % count] * halfSize * 0.95f);
    }

    public ViewBoxNode() {
        CameraType = ScreenSpacedCameraType.Perspective;
        RelativeScreenLocationX = 0.8f;
        viewBoxMeshModel = new MeshNode { EnableViewFrustumCheck = false, CullMode = CullMode.Back };
        var sampler = DefaultSamplers.LinearSamplerWrapAni1;
        sampler.BorderColor = Color.Gray;
        sampler.AddressU = sampler.AddressV = sampler.AddressW = TextureAddressMode.Border;
        AddChildNode(viewBoxMeshModel);
        viewBoxMeshModel.Material = new ViewCubeMaterialCore {
            DiffuseColor = Color.White,
            DiffuseMapSampler = sampler
        };
        viewBoxMeshModel.OnSetRenderTechnique = effectsManager => {
            EnsureViewBoxTexture(effectsManager);
            return effectsManager[DefaultRenderTechniqueNames.Mesh];
        };

        cornerModel = new InstancingMeshNode {
            EnableViewFrustumCheck = false,
            Material = new DiffuseMaterialCore { DiffuseColor = Color.Yellow },
            Geometry = CornerGeometry,
            Instances = CornerInstances,
            Visible = false
        };
        AddChildNode(cornerModel);

        edgeModel = new InstancingMeshNode {
            EnableViewFrustumCheck = false,
            Material = new DiffuseMaterialCore { DiffuseColor = Color.Silver },
            Geometry = EdgeGeometry,
            Instances = EdgeInstances,
            Visible = false
        };
        AddChildNode(edgeModel);
        UpdateModel(UpDirection);
    }

    protected override bool OnAttach(IEffectsManager effectsManager) {
        if (base.OnAttach(effectsManager)) {
            if (viewBoxMeshModel.Material is not ViewCubeMaterialCore)
                return false;

            EnsureViewBoxTexture(effectsManager);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Creates the default labelled view-box texture when no explicit texture was supplied.
    /// </summary>
    /// <param name="effectsManager">The active effects manager.</param>
    private void EnsureViewBoxTexture(IEffectsManager effectsManager) {
        if (viewBoxMeshModel.Material is not ViewCubeMaterialCore material)
            return;

        material.DiffuseMap ??= ViewBoxTexture ?? BitmapExtensions.CreateViewBoxTextureModel(
                                  effectsManager,
                                  "F",
                                  "B",
                                  "L",
                                  "R",
                                  "U",
                                  "D",
                                  Color.Red,
                                  Color.Red,
                                  Color.Blue,
                                  Color.Blue,
                                  Color.Green,
                                  Color.Green,
                                  Color.White,
                                  Color.White,
                                  Color.White,
                                  Color.White,
                                  Color.White,
                                  Color.White);
    }

    protected override void OnCoordinateSystemChanged(bool e) {
        if (isRightHanded != e) {
            isRightHanded = e;
            UpdateModel(UpDirection);
        }
    }

    private void UpdateTexture(TextureModel? texture) {
        if (viewBoxMeshModel.Material is ViewCubeMaterialCore material)
            material.DiffuseMap = texture;
    }

    protected void UpdateModel(Vector3 up) {
        var left = new Vector3(up.Y, up.Z, up.X);
        var front = SilkMath.Cross(left, up);
        if (!isRightHanded) {
            front *= -1;
            left *= -1;
        }

        var builder = new MeshBuilder(true);
        builder.AddCubeFace(new Vector3(0, 0, 0), front, up, Size, Size, Size);
        builder.AddCubeFace(new Vector3(0, 0, 0), -front, up, Size, Size, Size);
        builder.AddCubeFace(new Vector3(0, 0, 0), left, up, Size, Size, Size);
        builder.AddCubeFace(new Vector3(0, 0, 0), -left, up, Size, Size, Size);
        builder.AddCubeFace(new Vector3(0, 0, 0), up, left, Size, Size, Size);
        builder.AddCubeFace(new Vector3(0, 0, 0), -up, -left, Size, Size, Size);

        var mesh = builder.ToMesh();
        CreateTextureCoordinates(mesh);

        var pts = new List<Vector3>();

        var center = up * -Size / 2 * 1.1f;
        var phi = 24;
        for (var i = 0; i < phi; i++) {
            double angle = 0 + 360 * i / (phi - 1);
            var angleRad = angle / 180 * Math.PI;
            var dir = left * (float)Math.Cos(angleRad) + front * (float)Math.Sin(angleRad);
            pts.Add(center + dir * (Size - 0.75f));
            pts.Add(center + dir * (Size + 1.1f));
        }

        builder = new MeshBuilder(false, false);
        builder.AddTriangleStrip(pts);
        var pie = builder.ToMesh();
        var newMesh = MeshGeometry3D.Merge(pie, mesh);
        if (pie.Indices is not { } pieIndices || pie.Positions is not { } piePositions ||
            newMesh.Positions is not { } newPositions || mesh.TextureCoordinates is not { } meshTextureCoordinates ||
            mesh.Positions is not { } meshPositions)
            return;

        var count = pieIndices.Count;
        for (var i = 0; i < count;) {
            var v1 = pieIndices[i++];
            var v2 = pieIndices[i++];
            var v3 = pieIndices[i++];
            pieIndices.Add(v1);
            pieIndices.Add(v3);
            pieIndices.Add(v2);
        }

        if (!isRightHanded)
            for (var i = 0; i < newPositions.Count; ++i) {
                var p = newPositions[i];
                p.Z *= -1;
                newPositions[i] = p;
            }

        var textureCoordinates = new Vector2Collection(
            [.. Enumerable.Repeat(new Vector2(-1, -1), piePositions.Count)]);
        textureCoordinates.AddRange(meshTextureCoordinates);
        newMesh.TextureCoordinates = textureCoordinates;
        newMesh.Colors = [.. Enumerable.Repeat(new Color4(1f, 1f, 1f, 1f), piePositions.Count)];
        newMesh.Colors?.AddRange(Enumerable.Repeat(new Color4(1, 1, 1, 1), meshPositions.Count));
        newMesh.Normals = newMesh.CalculateNormals();
        viewBoxMeshModel.Geometry = newMesh;
    }

    private static void CreateTextureCoordinates(MeshGeometry3D mesh) {
        var faces = 6;
        var segment = 4;
        var inc = 1f / faces;
        if (mesh.TextureCoordinates is not { } textureCoordinates)
            return;

        for (var i = 0; i < textureCoordinates.Count; ++i)
            textureCoordinates[i] = new Vector2(textureCoordinates[i].X * inc + inc * (i / segment),
                                                textureCoordinates[i].Y);
    }

    protected override bool CanHitTest(HitTestContext? context) => context != null;

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    ) {
        if (base.OnHitTest(context, totalModelMatrix, ref hitsInternal)) {
            if (Logger.IsEnabled(LogLevel.Debug)) Logger.Debug("View box hit.");
            var hit = hitsInternal.OrderBy(x => x.Distance).FirstOrDefault();
            if (hit == null) return false;
            var normal = Vector3.Zero;
            var inv = isRightHanded ? 1 : -1;
            if (hit.ModelHit == viewBoxMeshModel) {
                normal = -hit.NormalAtHit * inv;
                //Fix the normal if returned normal is reversed
                if (SilkMath.Dot(normal, context.RenderMatrices.CameraParams.LookAtDir) < 0) normal *= -1;
            } else if (hit.Tag is int index) {
                if (hit.ModelHit == edgeModel && index < EdgeInstances.Length) {
                    var transform = EdgeInstances[index];
                    normal = -new Vector3(transform.M41, transform.M42, transform.M43);
                } else if (hit.ModelHit == cornerModel && index < CornerInstances.Length) {
                    var transform = CornerInstances[index];
                    normal = -new Vector3(transform.M41, transform.M42, transform.M43);
                } else {
                    return false;
                }
            } else {
                return false;
            }

            normal.Normalize();
            hit.NormalAtHit = normal;
            hit.ModelHit = this;
            hit.Tag = Tag;
            hits.Add(hit);
            hitsInternal.Clear();
            return true;
        }

        return false;
    }

    #region Properties

    /// <summary>
    ///     Gets or sets the view box texture.
    /// </summary>
    /// <value>
    ///     The view box texture.
    /// </value>
    public TextureModel? ViewBoxTexture {
        get;
        set {
            if (Set(ref field, value)) UpdateTexture(value);
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable edge click].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable edge click]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableEdgeClick {
        get => cornerModel.Visible;
        set => cornerModel.Visible = edgeModel.Visible = value;
    }

    /// <summary>
    ///     Gets or sets up direction.
    /// </summary>
    /// <value>
    ///     Up direction.
    /// </value>
    public Vector3 UpDirection {
        get;
        set {
            if (Set(ref field, value)) UpdateModel(value);
        }
    } = new(0, 1, 0);

#endregion

    #region Fields

    private const float Size = 5;

    private static readonly Vector3[] XAligned =
        [new(0, -1, -1), new(0, 1, -1), new(0, -1, 1), new(0, 1, 1)]; //x

    private static readonly Vector3[] YAligned =
        [new(-1, 0, -1), new(1, 0, -1), new(-1, 0, 1), new(1, 0, 1)]; //y

    private static readonly Vector3[] ZAligned =
        [new(-1, -1, 0), new(-1, 1, 0), new(1, -1, 0), new(1, 1, 0)]; //z

    private static readonly Vector3[] CornerPoints = [
        new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
        new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1)
    ];

    private static readonly Matrix[] CornerInstances;
    private static readonly Matrix[] EdgeInstances;
    private static readonly Geometry3D CornerGeometry;
    private static readonly Geometry3D EdgeGeometry;

    private readonly MeshNode viewBoxMeshModel;
    private readonly InstancingMeshNode edgeModel;
    private readonly InstancingMeshNode cornerModel;

    private bool isRightHanded = true;
    private List<HitTestResult> hitsInternal = [];

    #endregion
}
