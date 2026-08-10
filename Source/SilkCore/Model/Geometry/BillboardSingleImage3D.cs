/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpDX.Toolkit.Graphics;



namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public class BillboardSingleImage3D : BillboardBase {
    /// <summary>
    ///     Billboard type, <see cref="BillboardType" />
    /// </summary>
    public override BillboardType Type => BillboardType.Image;

    /// <summary>
    ///     Billboard center location
    /// </summary>
    public Vector3 Center {
        get;
        set {
            if (Set(ref field, value)) IsInitialized = false;
        }
    } = Vector3.Zero;

    /// <summary>
    ///     If color in image is equal to the mask color, the color will set to transparent in image.
    ///     Default color is Transparent, which did not mask any color.
    /// </summary>
    public Color4 MaskColor {
        get;
        set {
            if (Set(ref field, value)) IsInitialized = false;
        }
    } = Color.Transparent;

    /// <summary>
    ///     Gets or sets the rotation angle in radians.
    /// </summary>
    /// <value>
    ///     The angle in radians.
    /// </value>
    public float Angle {
        get;
        set {
            if (Set(ref field, value)) IsInitialized = false;
        }
    }

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
    public BillboardHorizontalAlignment HorizontalAlignment {
        get;
        set {
            if (Set(ref field, value)) IsInitialized = false;
        }
    } = BillboardHorizontalAlignment.Center;

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
    public BillboardVerticalAlignment VerticalAlignment {
        get;
        set {
            if (Set(ref field, value)) {
                field = value;
                IsInitialized = false;
            }
        }
    } = BillboardVerticalAlignment.Center;

    /// <summary>
    ///     Additional offset for billboard display location.
    ///     Behavior depends on whether billboard is fixed sized or not.
    ///     When billboard is fixed sized, the offset is screen spaced.
    /// </summary>
    public Vector2 Offset {
        get;
        set {
            if (Set(ref field, value)) IsInitialized = false;
        }
    } = Vector2.Zero;

    /// <summary>
    ///     Initializes a new instance of the <see cref="BillboardSingleImage3D" /> class.
    /// </summary>
    /// <param name="imageStream">The image stream.</param>
    public BillboardSingleImage3D(Stream imageStream) {
        Texture = imageStream;
        using var image = Image.Load(imageStream);
        Width = image.Description.Width;
        Height = image.Description.Height;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BillboardSingleImage3D" /> class.
    /// </summary>
    /// <param name="texture">The image texture.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public BillboardSingleImage3D(TextureModel texture, float width, float height) {
        Texture = texture;
        Width = width;
        Height = height;
    }

    /// <summary>
    ///     Updates the bounds.
    /// </summary>
    public override void UpdateBounds() {
        BoundingSphere = new BoundingSphere(Center, (float)Math.Sqrt(Width * Width + Height * Height) / 2);
        Bound = BoundingBox.FromSphere(BoundingSphere);
    }

    protected override void OnAssignTo(Geometry3D target) {
        base.OnAssignTo(target);
        if (target is BillboardSingleImage3D billboard) {
            billboard.Center = Center;
            billboard.MaskColor = MaskColor;
        }
    }

    /// <summary>
    ///     Called when [draw texture].
    /// </summary>
    /// <param name="deviceResources">The device resources.</param>
    protected override void OnUpdateTextureAndBillboardVertices(IDeviceResources deviceResources) {
        GetQuadOffset(Width, Height, HorizontalAlignment, VerticalAlignment, out var tl, out var br);

        var uvTl = new Vector2(0, 0);
        var uvBr = new Vector2(1, 1);
        var transform = Angle != 0 ? Matrix3X2.Rotation(Angle) : Matrix3X2.Identity;
        var tr = new Vector2(br.X, tl.Y);
        var bl = new Vector2(tl.X, br.Y);
        BillboardVertices.Add(new BillboardVertex {
            Position = Center.ToVector4(),
            Foreground = Color.White,
            Background = MaskColor,
            TexTL = uvTl,
            TexBR = uvBr,
            OffTL = Matrix3X2.TransformPoint(transform, tl) + Offset,
            OffBR = Matrix3X2.TransformPoint(transform, br) + Offset,
            OffBL = Matrix3X2.TransformPoint(transform, bl) + Offset,
            OffTR = Matrix3X2.TransformPoint(transform, tr) + Offset
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
        if (!IsInitialized || context == null || Width == 0 || Height == 0
            || (!fixedSize && !BoundingSphere.TransformBoundingSphere(modelMatrix).Intersects(ref rayWs)))
            return false;

        return fixedSize
                   ? HitTestFixedSize(context, ref modelMatrix, ref hits, originalSource, BillboardVertices.Count)
                   : HitTestNonFixedSize(context, ref modelMatrix, ref hits, originalSource, BillboardVertices.Count);
    }
}
