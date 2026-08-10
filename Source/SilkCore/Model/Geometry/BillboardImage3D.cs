/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace HelixToolkit.SharpDX.Core;

public class BillboardImage3D : BillboardBase {
    private ObservableCollection<ImageInfo> imageInfos = [];

    private Color4 maskColor = Color.Transparent;

    public BillboardImage3D(Stream imageStream) {
        Texture = imageStream;
        imageInfos.CollectionChanged += CollectionChanged;
    }

    public BillboardImage3D(TextureModel texture) {
        Texture = texture;
        imageInfos.CollectionChanged += CollectionChanged;
    }

    public override BillboardType Type => BillboardType.Image;

    /// <summary>
    ///     If color in image is equal to the mask color, the color will set to transparent in image.
    ///     Default color is Transparent, which did not mask any color.
    /// </summary>
    public Color4 MaskColor {
        get => maskColor;
        set {
            if (Set(ref maskColor, value)) IsInitialized = false;
        }
    }

    public ObservableCollection<ImageInfo> ImageInfos {
        get => imageInfos;
        set {
            var old = imageInfos;
            if (Set(ref imageInfos, value)) {
                old.CollectionChanged -= CollectionChanged;
                IsInitialized = false;
                value?.CollectionChanged += CollectionChanged;
            }
        }
    }

    private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        IsInitialized = false;
    }

    protected override void OnUpdateTextureAndBillboardVertices(IDeviceResources deviceResources) {
        foreach (var img in ImageInfos) {
            img.UpdateImage();
            DrawImageVertex(img);
        }
    }

    private void DrawImageVertex(ImageInfo info) {
        GetQuadOffset(info.Width,
                      info.Height,
                      info.HorizontalAlignment,
                      info.VerticalAlignment,
                      out var tl,
                      out var br);

        var transform = info.Angle != 0 ? Matrix3X2.Rotation(info.Angle) : Matrix3X2.Identity;
        var offTl = tl * info.Scale;
        var offBr = br * info.Scale;
        var offTr = new Vector2(offBr.X, offTl.Y);
        var offBl = new Vector2(offTl.X, offBr.Y);
        BillboardVertices.Add(new BillboardVertex {
            Position = info.Position.ToVector4(),
            Foreground = Color.White,
            Background = maskColor,
            TexTL = info.UvTopLeft,
            TexBR = info.UvBottomRight,
            OffTL = Matrix3X2.TransformPoint(transform, offTl) + info.Offset,
            OffBL = Matrix3X2.TransformPoint(transform, offBl) + info.Offset,
            OffBR = Matrix3X2.TransformPoint(transform, offBr) + info.Offset,
            OffTR = Matrix3X2.TransformPoint(transform, offTr) + info.Offset
        });
    }

    public override bool HitTest(
        HitTestContext context,
        Matrix modelMatrix,
        ref List<HitTestResult> hits,
        object originalSource,
        bool fixedSize
    ) {
        var rayWs = context.RayWs;
        if (!IsInitialized || context == null ||
            (!fixedSize && !BoundingSphere.TransformBoundingSphere(modelMatrix).Intersects(ref rayWs))) return false;

        return fixedSize
                   ? HitTestFixedSize(context, ref modelMatrix, ref hits, originalSource, imageInfos.Count)
                   : HitTestNonFixedSize(context, ref modelMatrix, ref hits, originalSource, imageInfos.Count);
    }

    protected override void OnAssignTo(Geometry3D target) {
        base.OnAssignTo(target);
        if (target is BillboardImage3D t) {
            t.ImageInfos = new ObservableCollection<ImageInfo>(ImageInfos);
            t.IsInitialized = false;
        }
    }

    public override void UpdateBounds() {
        if (ImageInfos.Count == 0) {
            Bound = new BoundingBox();
            BoundingSphere = new BoundingSphere();
        } else {
            var sphere = ImageInfos[0].BoundSphere;
            var bound = BoundingBox.FromSphere(sphere);
            foreach (var info in ImageInfos) {
                sphere = BoundingSphereExtensions.Merge(sphere, info.BoundSphere);
                bound = BoundingBox.Merge(bound, BoundingBox.FromSphere(info.BoundSphere));
            }

            BoundingSphere = sphere;
            Bound = bound;
        }
    }
}

public class ImageInfo {
    public Vector2 UvTopLeft { get; set; }

    public Vector2 UvBottomRight { get; set; }

    public Vector3 Position { get; set; }

    public float Width { get; set; } = 1;
    public float Height { get; set; } = 1;
    public float Angle { get; set; } = 0;
    public float Scale { get; set; } = 1;

    /// <summary>
    ///     Sets or gets the horizontal alignment. Default = <see cref="BillboardHorizontalAlignment.Center" />
    ///     <para>
    ///         For example, when sets horizontal and vertical alignment to top/left,
    ///         billboard's bottom/right point will be anchored at the billboard origin.
    ///     </para>
    /// </summary>
    /// <value>
    ///     The horizontal alignment.
    /// </value>
    public BillboardHorizontalAlignment HorizontalAlignment { get; set; } = BillboardHorizontalAlignment.Center;

    /// <summary>
    ///     Sets or gets the vertical alignment. Default = <see cref="BillboardVerticalAlignment.Center" />
    ///     <para>
    ///         For example, when sets horizontal and vertical alignment to top/left,
    ///         billboard's bottom/right point will be anchored at the billboard origin.
    ///     </para>
    /// </summary>
    /// <value>
    ///     The vertical alignment.
    /// </value>
    public BillboardVerticalAlignment VerticalAlignment { get; set; } = BillboardVerticalAlignment.Center;

    /// <summary>
    ///     Additional offset for billboard display location.
    ///     Behavior depends on whether billboard is fixed sized or not.
    ///     When billboard is fixed sized, the offset is screen spaced.
    /// </summary>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    public BoundingSphere BoundSphere { get; private set; }

    public virtual void UpdateImage() {
        BoundSphere = new BoundingSphere(Position, Math.Max(Width * Scale, Height * Scale) / 2);
    }
}
