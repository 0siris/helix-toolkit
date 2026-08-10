/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using Cyotek.Drawing.BitmapFont;


namespace HelixToolkit.SharpDX.Core;

public class TextInfoExt : TextInfo {
    public Vector4 Padding = Vector4.Zero;
    public string FontFamily { get; set; } = "Arial";
    public FontWeight FontWeight { get; set; } = FontWeight.Normal;
    public FontStyle FontStyle { get; set; } = FontStyle.Normal;
    public int Size { get; set; } = 12;
}

public class TextInfo {
    public TextInfo() { }

    public TextInfo(string text, Vector3 origin) {
        Text = text;
        Origin = origin;
    }

    public string Text { get; set; }

    public Vector3 Origin { get; set; }

    public Color4 Foreground { get; set; } = Color.Black;

    public Color4 Background { get; set; } = Color.Transparent;

    public float ActualWidth { get; protected set; }

    public float ActualHeight { get; protected set; }

    public float Scale { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the rotation angle in radians.
    /// </summary>
    /// <value>
    ///     The angle in radians.
    /// </value>
    public float Angle { get; set; } = 0;

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

    public virtual void UpdateTextInfo(float actualWidth, float actualHeight) {
        ActualWidth = actualWidth;
        ActualHeight = actualHeight;
        BoundSphere = new BoundingSphere(Origin, Math.Max(actualWidth, actualHeight) / 2);
    }
}

public class BillboardText3D : BillboardBase {
    private const float TextureScale = 0.66f;
    private const string FontName = "arial";
    private static readonly BitmapFont BmpFont;

    private ObservableCollection<TextInfo> textInfo = [];

    static BillboardText3D() {
        var assembly = typeof(BillboardText3D).GetTypeInfo().Assembly;
        var fontInfo = assembly.GetManifestResourceStream($"SilkCore.Resources.{FontName}.fnt");
        BmpFont = new BitmapFont();
        BmpFont.Load(fontInfo);
        var font = assembly.GetManifestResourceStream($"SilkCore.Resources.{FontName}.dds");
        TextureStatic = font;
    }

    public BillboardText3D() {
        textInfo.CollectionChanged += CollectionChanged;
        Texture = TextureStatic;
        BitmapFont = BmpFont;
    }

    public BillboardText3D(BitmapFont bitmapFont, Stream fontTexture) {
        textInfo.CollectionChanged += CollectionChanged;
        Texture = fontTexture;
        BitmapFont = bitmapFont;
    }

    public BillboardText3D(BitmapFont bitmapFont, TextureModel fontTexture) {
        textInfo.CollectionChanged += CollectionChanged;
        Texture = fontTexture;
        BitmapFont = bitmapFont;
    }

    public static Stream TextureStatic { get; }

    public override BillboardType Type => BillboardType.MultipleText;

    public BitmapFont BitmapFont { get; }

    public ObservableCollection<TextInfo> TextInfo {
        get => textInfo;
        set {
            var old = textInfo;
            if (Set(ref textInfo, value)) {
                old.CollectionChanged -= CollectionChanged;
                IsInitialized = false;
                value?.CollectionChanged += CollectionChanged;
            }
        }
    }

    protected override void OnAssignTo(Geometry3D target) {
        base.OnAssignTo(target);
        if (target is BillboardText3D billboard) billboard.TextInfo = TextInfo;
    }

    private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
        IsInitialized = false;
    }

    protected override void OnUpdateTextureAndBillboardVertices(IDeviceResources deviceResources) {
        Width = 0;
        Height = 0;
        // http://www.cyotek.com/blog/angelcode-bitmap-font-parsing-using-csharp
        var tempList = new List<BillboardVertex>(100);

        foreach (var textInfo in TextInfo) {
            var tempPrevCount = tempList.Count;
            var x = 0;
            var y = 0;
            var w = BitmapFont.TextureSize.Width;
            var h = BitmapFont.TextureSize.Height;
            char previousCharacter;

            previousCharacter = ' ';
            var normalizedText = textInfo.Text;
            var rect = new RectangleF(textInfo.Origin.X, textInfo.Origin.Y, 0, 0);
            foreach (var character in normalizedText) {
                switch (character) {
                    case '\n':
                        x = 0;
                        y -= BitmapFont.LineHeight;
                        break;
                    default:
                        var data = BitmapFont[character];
                        var kerning = BitmapFont.GetKerning(previousCharacter, character);
                        tempList.Add(DrawCharacter(data,
                                                   new Vector3(x + data.XOffset, y - data.YOffset, 0),
                                                   w,
                                                   h,
                                                   kerning,
                                                   textInfo));

                        x += data.XAdvance + kerning;
                        break;
                }

                previousCharacter = character;
                if (tempList.Count > 0) {
                    rect.Width = Math.Max(rect.Width, x * textInfo.Scale * TextureScale);
                    rect.Height = Math.Max(rect.Height, Math.Abs(tempList.Last().OffBR.Y));
                }
            }

            var transform = textInfo.Angle != 0 ? Matrix3X2.Rotation(textInfo.Angle) : Matrix3X2.Identity;
            GetQuadOffset(rect.Width,
                          rect.Height,
                          textInfo.HorizontalAlignment,
                          textInfo.VerticalAlignment,
                          out var tl,
                          out var br);
            var tr = new Vector2(br.X, tl.Y);
            var bl = new Vector2(tl.X, br.Y);
            //Add backbround vertex first. This is also used for hit test
            BillboardVertices.Add(new BillboardVertex {
                Position = textInfo.Origin.ToVector4(),
                Background = textInfo.Background,
                TexTL = Vector2.Zero,
                TexBR = Vector2.Zero,
                OffTL = Matrix3X2.TransformPoint(transform, tl) + textInfo.Offset,
                OffBR = Matrix3X2.TransformPoint(transform, br) + textInfo.Offset,
                OffTR = Matrix3X2.TransformPoint(transform, tr) + textInfo.Offset,
                OffBL = Matrix3X2.TransformPoint(transform, bl) + textInfo.Offset
            });

            textInfo.UpdateTextInfo(rect.Width, rect.Height);
            var halfW = rect.Width / 2;
            var halfH = rect.Height / 2;
            for (var k = tempPrevCount; k < tempList.Count; ++k) {
                var v = tempList[k];
                v.OffTL = Matrix3X2.TransformPoint(transform, v.OffTL + tl) + textInfo.Offset;
                v.OffBR = Matrix3X2.TransformPoint(transform, v.OffBR + tl) + textInfo.Offset;
                v.OffTR = Matrix3X2.TransformPoint(transform, v.OffTR + tl) + textInfo.Offset;
                v.OffBL = Matrix3X2.TransformPoint(transform, v.OffBL + tl) + textInfo.Offset;
                tempList[k] = v;
            }

            Width += rect.Width;
            Height += rect.Height;
        }

        foreach (var v in tempList) BillboardVertices.Add(v);
    }

    public override void UpdateBounds() {
        if (TextInfo.Count == 0) {
            Bound = new BoundingBox();
            BoundingSphere = new BoundingSphere();
        } else {
            var sphere = TextInfo[0].BoundSphere;
            var bound = BoundingBox.FromSphere(sphere);
            foreach (var info in TextInfo) {
                sphere = BoundingSphereExtensions.Merge(sphere, info.BoundSphere);
                bound = BoundingBox.Merge(bound, BoundingBox.FromSphere(info.BoundSphere));
            }

            BoundingSphere = sphere;
            Bound = bound;
        }
    }

    private BillboardVertex DrawCharacter(
        Character character,
        Vector3 origin,
        float w,
        float h,
        float kerning,
        TextInfo info
    ) {
        var cw = character.Width;
        var ch = character.Height;
        var cu = character.X;
        var cv = character.Y;
        var tl = new Vector2(origin.X + kerning, origin.Y);
        var br = new Vector2(origin.X + cw + kerning, origin.Y - ch);
        var offTl = tl * info.Scale * TextureScale;
        var offBr = br * info.Scale * TextureScale;
        var offTr = new Vector2(offBr.X, offTl.Y);
        var offBl = new Vector2(offTl.X, offBr.Y);
        var uvTl = new Vector2(cu / w, cv / h);
        var uvBr = new Vector2((cu + cw) / w, (cv + ch) / h);

        return new BillboardVertex {
            Position = info.Origin.ToVector4(),
            Foreground = info.Foreground,
            Background = Color.Transparent,
            TexTL = uvTl,
            TexBR = uvBr,
            OffTL = offTl,
            OffBL = offBl,
            OffBR = offBr,
            OffTR = offTr
        };
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
                   ? HitTestFixedSize(context, ref modelMatrix, ref hits, originalSource, textInfo.Count)
                   : HitTestNonFixedSize(context, ref modelMatrix, ref hits, originalSource, textInfo.Count);
    }

    protected override void AssignResultAdditional(BillboardHitResult result, int index) {
        base.AssignResultAdditional(result, index);
        result.TextInfo = textInfo[index];
        result.TextInfoIndex = index;
    }
}
