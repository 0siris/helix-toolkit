//Flag.jpg image is created by Luis_molinero - Freepik.com

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cyotek.Drawing.BitmapFont;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.Wpf.SharpDX;
using Color = BillboardDemo.BillboardColors;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using D2DFontStyle = HelixToolkit.SharpDX.Core.FontStyle;
using D2DFontWeight = HelixToolkit.SharpDX.Core.FontWeight;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace BillboardDemo;

internal static class BillboardColors {
    public static readonly Color4 Black = System.Windows.Media.Colors.Black.ToColor4();
    public static readonly Color4 Blue = System.Windows.Media.Colors.Blue.ToColor4();
    public static readonly Color4 DarkBlue = System.Windows.Media.Colors.DarkBlue.ToColor4();
    public static readonly Color4 DarkGray = System.Windows.Media.Colors.DarkGray.ToColor4();
    public static readonly Color4 DarkRed = System.Windows.Media.Colors.DarkRed.ToColor4();
    public static readonly Color4 DarkSeaGreen = System.Windows.Media.Colors.DarkSeaGreen.ToColor4();
    public static readonly Color4 DarkSlateBlue = System.Windows.Media.Colors.DarkSlateBlue.ToColor4();
    public static readonly Color4 Green = System.Windows.Media.Colors.Green.ToColor4();
    public static readonly Color4 Indigo = System.Windows.Media.Colors.Indigo.ToColor4();
    public static readonly Color4 Lavender = System.Windows.Media.Colors.Lavender.ToColor4();
    public static readonly Color4 LightCoral = System.Windows.Media.Colors.LightCoral.ToColor4();
    public static readonly Color4 LightCyan = System.Windows.Media.Colors.LightCyan.ToColor4();
    public static readonly Color4 LightSalmon = System.Windows.Media.Colors.LightSalmon.ToColor4();
    public static readonly Color4 Orchid = System.Windows.Media.Colors.Orchid.ToColor4();
    public static readonly Color4 PaleGoldenrod = System.Windows.Media.Colors.PaleGoldenrod.ToColor4();
    public static readonly Color4 Red = System.Windows.Media.Colors.Red.ToColor4();
    public static readonly Color4 Transparent = System.Windows.Media.Colors.Transparent.ToColor4();
    public static readonly Color4 White = System.Windows.Media.Colors.White.ToColor4();
    public static readonly Color4 Yellow = System.Windows.Media.Colors.Yellow.ToColor4();
}

public class MainViewModel : DemoCore.BaseViewModel {
    public Geometry3D SphereModel { get; }
    public PhongMaterial EarthMaterial { get; }
    public BillboardImage3D FlagsBillboard { get; }

    public Geometry3D AxisLines { private set; get; }

    public BillboardImage3D AxisLabels { private set; get; }

    public BillboardSingleText3D SelectedFlagBillboard { get; } = new() {
        FontColor = Color.Blue,
        FontWeight = D2DFontWeight.Bold,
        BackgroundColor = new Color4(0.8f, 0.8f, 0.8f, 0.8f),
        Padding = new HelixToolkit.SharpDX.Core.Model.Scene2D.Thickness(2),
        IsDynamic = true // Mark dynamic because it will change frequently
    };

    public BillboardText3D LandmarkBillboards { get; } = new() { IsDynamic = true }; // Mark dynamic because it will change frequently

    public BillboardText3D LandmarkBillboards2 { get; }
    public BillboardImage3D BatchedText { private set; get; }
    public Stream BackgroundTexture { private set; get; }

    public Flag[] Flags => FlagsCollection.Flags;

    public bool FixedSize {
        set => SetValue(ref field, value);
        get => field;
    } = true;

    public Flag SelectedFlag {
        set {
            SetValue(ref field, value);
            UpdateSelectedFlagBillboard(value);
        }
        get => field;
    }

    private Color4 prevLocColor, prevLocColor2;
    private Color4 prevLocBackColor, prevLocBackColor2;
    private TextInfo highlightedLoc, highlightedLoc2;

    public MainViewModel() {
        Title = "HelixToolkit Billboard Demo";
        SubTitle = "Wpf SharpDX";
        EffectsManager = new DefaultEffectsManager();
        Camera = new OrthographicCamera() {
            Position = new System.Windows.Media.Media3D.Point3D(0, -10, 0),
            LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 10, 0),
            UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, 1),
            FarPlaneDistance = 1000,
            NearPlaneDistance = 0.1,
            Width = 10
        };
        var builder = new MeshBuilder();
        builder.AddSphere(Vector3.Zero, 4, 16, 16);
        SphereModel = builder.ToMesh();
        SphereModel.UpdateOctree();
        EarthMaterial = PhongMaterials.White;
        EarthMaterial.SpecularShininess = 10;
        EarthMaterial.SpecularColor = new Color4(0.3f, 0.3f, 0.3f, 1);
        EarthMaterial.DiffuseMap = TextureModel.Create("earthmap.jpg");
        EarthMaterial.SpecularColorMap = TextureModel.Create("earthspec.jpg");
        EarthMaterial.DisplacementMap = TextureModel.Create("earthbump.jpg");
        EarthMaterial.NormalMap = TextureModel.Create("earthNormal.jpg");
        EarthMaterial.DisplacementMapScaleMask = new Vector4(0.2f, 0.2f, 0.2f, 0);
        EarthMaterial.EnableTessellation = true;
        EarthMaterial.MaxDistanceTessellationFactor = 1;
        EarthMaterial.MinDistanceTessellationFactor = 3;
        EarthMaterial.MaxTessellationDistance = 500;
        EarthMaterial.MinDistanceTessellationFactor = 2;
        EarthMaterial.EnableAutoTangent = true;
        BackgroundTexture =
            BitmapExtensions.CreateLinearGradientBitmapStream(EffectsManager,
                                                              128,
                                                              128,
                                                              Direct2DImageFormat.Bmp,
                                                              new Vector2(0, 0),
                                                              new Vector2(0, 128),
                                                              [
                                                                  new GradientStop()
                                                                      {Color = Color.DarkBlue, Position = 0f},
                                                                  new GradientStop()
                                                                      {Color = Color.Black, Position = 1f}
                                                              ]);

        FlagsBillboard = new BillboardImage3D(TextureModel.Create("Flags.jpg"));
        foreach (var info in FlagsCollection.Flags.Where(x => x.Position != Vector3.Zero)) {
            FlagsBillboard.ImageInfos.Add(info);
        }

        var segoeFont = new BitmapFont();
        segoeFont.Load(@"Fonts\SegoeScript.fnt");
        LandmarkBillboards2 = new BillboardText3D(segoeFont, TextureModel.Create(@"Fonts\SegoeScript.dds"));
        AddLocations();
        AddBatchedText();
        CreateCoordinateSystem();
    }

    private void CreateCoordinateSystem() {
        var linebuilder = new LineBuilder();
        linebuilder.AddLine(Vector3.Zero, Vector3.UnitX * 6);
        linebuilder.AddLine(Vector3.Zero, Vector3.UnitY * 6);
        linebuilder.AddLine(Vector3.Zero, Vector3.UnitZ * 6);
        AxisLines = linebuilder.ToLineGeometry3D();
        AxisLines.Colors = [Color.Red, Color.Red, Color.Green, Color.Green, Color.Blue, Color.Blue];
        var texts = new TextInfoExt[] {
            new() {
                Text = "右", Origin = Vector3.UnitX * 8, Foreground = Color.Red, Size = 16,
                FontWeight = D2DFontWeight.SemiBold
            },
            new() {
                Text = "前", Origin = Vector3.UnitY * 8, Foreground = Color.Green, Size = 16,
                FontWeight = D2DFontWeight.SemiBold
            },
            new() {
                Text = "上", Origin = Vector3.UnitZ * 8, Foreground = Color.Blue, Size = 16,
                FontWeight = D2DFontWeight.SemiBold
            }
        };
        AxisLabels = texts.ToBillboardImage3D(EffectsManager);
    }

    private void AddLocations() {
        float offset = 4.5f;
        float scale = 0.8f;
        LandmarkBillboards.TextInfo.Add(new TextInfo("Arctic", Vector3.UnitZ * offset) {
            Foreground = Color.Red,
            Scale = scale * 2,
            VerticalAlignment = BillboardVerticalAlignment.Top
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Antarctica", Vector3.UnitZ * -offset) {
            Foreground = Color.Blue,
            Scale = scale * 2,
            VerticalAlignment = BillboardVerticalAlignment.Bottom
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Equator",
                                                     new Vector3(0, offset, 0)) {
            Foreground = Color.White,
            Scale = scale
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Equator",
                                                     new Vector3((float)Math.Cos(Math.PI / 6) * offset,
                                                                 -(float)Math.Sin(Math.PI / 6) * offset,
                                                                 0)) {
            Foreground = Color.White,
            Scale = scale
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Equator",
                                                     new Vector3(-(float)Math.Cos(Math.PI / 6) * offset,
                                                                 -(float)Math.Sin(Math.PI / 6) * offset,
                                                                 0)) {
            Foreground = Color.White,
            Scale = scale
        });

        LandmarkBillboards.TextInfo.Add(new TextInfo("Pacific", new Vector3(3.922917f, 0.2635128f, 2.084114f)) {
            Foreground = Color.White,
            Background = Color.Green,
            Scale = scale * 1.4f
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Indian", new Vector3(-0.8591346f, -4.321474f, -0.2010482f)) {
            Foreground = Color.White,
            Background = Color.Green,
            Scale = scale * 1.4f
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Atlantic", new Vector3(-2.595731f, 1.984212f, 3.021353f)) {
            Foreground = Color.White,
            Background = Color.Green,
            Scale = scale * 1.4f
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Southern", new Vector3(0.08587439f, -2.127402f, -3.936893f)) {
            Foreground = Color.White,
            Background = Color.Green,
            Scale = scale * 1.4f
        });
        LandmarkBillboards.TextInfo.Add(new TextInfo("Arctic Ocean", new Vector3(-0.7553688f, -0.6352348f, 4.379822f)) {
            Foreground = Color.White,
            Background = Color.Green,
            Scale = scale * 1.4f,
            VerticalAlignment = BillboardVerticalAlignment.Top
        });

        LandmarkBillboards2.TextInfo.Add(new TextInfo("Asia", new Vector3(-0.8280244f, -3.665166f, 2.356252f)) {
            Foreground = Color.Red,
            Background = Color.DarkGray,
            Scale = 1
        });
        LandmarkBillboards2.TextInfo.Add(new TextInfo("Europe", new Vector3(-2.531648f, -1.158762f, 3.501563f)) {
            Foreground = Color.Blue,
            Background = Color.DarkGray,
            Scale = 1
        });
        LandmarkBillboards2.TextInfo.Add(new TextInfo("North America", new Vector3(0.1436681f, 3.24761f, 3.080247f)) {
            Foreground = Color.Red,
            Background = Color.DarkGray,
            Scale = 1
        });
        LandmarkBillboards2.TextInfo.Add(new TextInfo("South America", new Vector3(-1.96404f, 3.929909f, -0.6905473f)) {
            Foreground = Color.Orchid,
            Background = Color.DarkGray,
            Scale = 1
        });
        LandmarkBillboards2.TextInfo.Add(new TextInfo("Oceania", new Vector3(2.306917f, -3.398433f, -1.661219f)) {
            Foreground = Color.Green,
            Background = Color.DarkGray,
            Scale = 1
        });
    }

    private void AddBatchedText() {
        var texts = new TextInfoExt[] {
            new() {
                Text = "English",
                Foreground = Color.Indigo,
                Background = Color.LightCoral,
                FontWeight = D2DFontWeight.Light,
                FontFamily = "Segoe UI",
                Padding = new Vector4(4),
                Origin = new Vector3(-10, 0, -4),
                Size = 18, HorizontalAlignment = BillboardHorizontalAlignment.Left
            },
            new() {
                Text = "中文",
                Foreground = Color.Green,
                Background = Color.White,
                FontStyle = D2DFontStyle.Italic,
                Origin = new Vector3(-10, 0, -2),
                Padding = new Vector4(4, 2, 4, 2),
                FontFamily = "Microsoft YaHei",
                Size = 16, HorizontalAlignment = BillboardHorizontalAlignment.Right
            },
            new() {
                Text = "日本語",
                Foreground = Color.Blue,
                Background = Color.Green,
                FontWeight = D2DFontWeight.Bold,
                Origin = new Vector3(-10, 0, 0),
                Padding = new Vector4(2, 4, 2, 4),
                Size = 18
            },
            new() {
                Text = "Français",
                Foreground = Color.White,
                Background = Color.Black,
                Origin = new Vector3(-10, 0, 2),
                Padding = new Vector4(8, 4, 2, 4),
                FontFamily = "Calibri",
                Size = 20
            },
            new() {
                Text = "Español",
                Foreground = Color.DarkSeaGreen,
                Background = Color.LightCyan,
                Origin = new Vector3(-10, 0, 4),
                Padding = new Vector4(6),
                FontFamily = "Times New Roman",
                Size = 22
            },
            new() {
                Text = "繁體中文",
                Foreground = Color.Red,
                Background = Color.Blue,
                Padding = new Vector4(2, 2, 2, 2),
                Origin = new Vector3(-14, 0, -4),
                FontStyle = D2DFontStyle.Oblique,
                Size = 14
            },
            new() {
                Text = "한국어",
                Foreground = Color.LightSalmon,
                Background = Color.DarkSlateBlue,

                Origin = new Vector3(-14, 0, -2),
                Padding = new Vector4(4, 2, 4, 2),
                Size = 16
            },
            new() {
                Text = "Deutsch",
                Foreground = Color.Blue,
                Background = Color.White,
                FontWeight = D2DFontWeight.Bold,
                Origin = new Vector3(-14, 0, 0),
                Padding = new Vector4(2, 4, 2, 4),
                FontFamily = "Garamond",
                Size = 18
            },
            new() {
                Text = "Português",
                Foreground = Color.DarkRed,
                Background = Color.Lavender,
                Origin = new Vector3(-14, 0, 2),
                Padding = new Vector4(8, 4, 2, 4),
                FontFamily = "Tahoma",
                Size = 20
            },
            new() {
                Text = "Below are batched \ntexts rendering \nwith different styles",
                Foreground = Color.PaleGoldenrod,
                Background = Color.Transparent,
                Origin = new Vector3(-12, 0, 8),
                Padding = new Vector4(6),
                FontFamily = "Consolas",
                Size = 24
            }
        };
        BatchedText = texts.ToBillboardImage3D(EffectsManager);
    }

    private void UpdateSelectedFlagBillboard(Flag flag) {
        if (flag.Position != Vector3.Zero) {
            SelectedFlagBillboard.TextInfo = new TextInfo(flag.Name, flag.Position) { Scale = 0.015f };
        } else {
            SelectedFlagBillboard.TextInfo = null;
        }
    }

    public void OnMouseUpHandler(object sender, MouseUp3DEventArgs e) {
        if (e.HitTestResult != null && e.HitTestResult.ModelHit is BillboardTextModel3D model
                                    && e.HitTestResult is BillboardHitResult res) {
            if (model.Geometry == FlagsBillboard) {
                SelectedFlag = FlagsBillboard.ImageInfos[res.TextInfoIndex] as Flag;
            } else if (model.Geometry == LandmarkBillboards) {
                RestoreLocColor();
                BackupLocColor(LandmarkBillboards.TextInfo[res.TextInfoIndex]);
                highlightedLoc.Background = Color.Yellow;
                highlightedLoc.Foreground = Color.Black;
                LandmarkBillboards.Invalidate();
            } else if (model.Geometry == LandmarkBillboards2) {
                RestoreLocColor2();
                BackupLocColor2(LandmarkBillboards2.TextInfo[res.TextInfoIndex]);
                highlightedLoc2.Background = Color.Yellow;
                highlightedLoc2.Foreground = Color.Black;
                LandmarkBillboards2.Invalidate();
            }
        }
    }

    private void BackupLocColor(TextInfo loc) {
        highlightedLoc = loc;
        prevLocBackColor = highlightedLoc.Background;
        prevLocColor = highlightedLoc.Foreground;
    }

    private void RestoreLocColor() {
        if (highlightedLoc != null) {
            highlightedLoc.Background = prevLocBackColor;
            highlightedLoc.Foreground = prevLocColor;
            LandmarkBillboards.Invalidate();
        }

        highlightedLoc = null;
    }


    private void BackupLocColor2(TextInfo loc) {
        highlightedLoc2 = loc;
        prevLocBackColor2 = highlightedLoc2.Background;
        prevLocColor2 = highlightedLoc2.Foreground;
    }

    private void RestoreLocColor2() {
        if (highlightedLoc2 != null) {
            highlightedLoc2.Background = prevLocBackColor2;
            highlightedLoc2.Foreground = prevLocColor2;
            LandmarkBillboards2.Invalidate();
        }

        highlightedLoc2 = null;
    }

    public void OnFlag_Drop(object sender, DragEventArgs e) {
        if (e.Data.GetData("Flag") is Flag flag && sender is Viewport3DX viewport) {
            var point = e.GetPosition(sender as IInputElement);
            var hits = viewport.FindHits(point);
            if (hits.Count == 0) {
                return;
            }

            if (hits[0].ModelHit is GeometryModel3D model && model.Geometry == SphereModel) {
                var pos = hits[0].PointHit;
                var normal = hits[0].NormalAtHit;
                flag.Position = pos + normal * 0.5f;
                FlagsBillboard.ImageInfos.Remove(flag);
                FlagsBillboard.ImageInfos.Add(flag);
            }
        }
    }

    //private DataObject dragData;
    private ListBox dragSource;

    public void ListBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        dragSource = sender as ListBox;
    }

    public void ListBox_MouseMove(object sender, MouseEventArgs e) {
        if (e.MouseDevice.LeftButton == MouseButtonState.Pressed) {
            var parent = sender as ListBox;
            if (parent == null || parent != dragSource) {
                return;
            }

            if (e.OriginalSource is FrameworkElement dp && dp.DataContext is Flag flag) {
                DataObject dragData = new DataObject("Flag", flag);
                DragDrop.DoDragDrop(dragSource, dragData, DragDropEffects.Move);
                dragSource = null;
            }
        }
    }
}
