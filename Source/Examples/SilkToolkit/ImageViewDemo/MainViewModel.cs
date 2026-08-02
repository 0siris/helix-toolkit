// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace ImageViewDemo;

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DemoCore;
using ExifLib;
using HelixToolkit.Wpf;
using HelixToolkit.Wpf.SharpDX;
using Color = HelixToolkit.SharpDX.Core.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Plane {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.Plane)); }
    }

    public LineGeometry3D Grid {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.Grid)); }
    }

    public PhongMaterial PlaneMaterial {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.PlaneMaterial)); }
    }

    public Color GridColor {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.GridColor)); }
    }

    public Media3D.Transform3D PlaneTransform {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.PlaneTransform)); }
    }

    public Media3D.Transform3D GridTransform {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.GridTransform)); }
    }

    public Vector3D DirectionalLightDirection {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.DirectionalLightDirection)); }
    }

    public Color4 DirectionalLightColor {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.DirectionalLightColor)); }
    }

    public Color4 AmbientLightColor {
        get { return field; }
        set { this.SetValue(ref field, value, nameof(this.AmbientLightColor)); }
    }

    public ICommand OpenCommand { get; private set; }

    public ExifReader ExifReader { get; private set; }

    public MainViewModel() {
        this.Title = "ImageViewDemo";
        this.SubTitle = "WPF & SharpDX";

        EffectsManager = new DefaultEffectsManager();

        this.OpenCommand = new RelayCommand((x) => this.OnOpenClick());

        // camera setup
        this.defaultPerspectiveCamera = new PerspectiveCamera {
            Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5),
            UpDirection = new Vector3D(0, 1, 0), NearPlaneDistance = 0.5, FarPlaneDistance = 150
        };
        this.defaultOrthographicCamera = new OrthographicCamera {
            Position = new Point3D(0, 0, 5), LookDirection = new Vector3D(0, 0, -5),
            UpDirection = new Vector3D(0, 1, 0), NearPlaneDistance = 0, FarPlaneDistance = 100
        };
        this.Camera = this.defaultPerspectiveCamera;

        // setup lighting
        this.AmbientLightColor = new Color4(0f, 0f, 0f, 0f);
        this.DirectionalLightColor = Color.White;
        this.DirectionalLightDirection = new Vector3D(-0, -0, -10);

        // floor plane grid
        this.Grid = LineBuilder.GenerateGrid(Vector3.UnitZ, -5, 5, -5, 5);
        this.GridColor = Color.Black;
        this.GridTransform = new Media3D.TranslateTransform3D(0, 0, 0);

        // plane
        var b2 = new MeshBuilder();
        b2.AddBox(new Vector3(0, 0, 0), 10, 10, 0, BoxFaces.PositiveZ);
        this.Plane = b2.ToMeshGeometry3D();
        this.PlaneMaterial = PhongMaterials.Blue;
        this.PlaneTransform = new Media3D.TranslateTransform3D(-0, -0, -0);
        //this.PlaneMaterial.ReflectiveColor = Color.Black;
        this.PlaneTransform = new Media3D.TranslateTransform3D(0, 0, 0);
    }

    private void SetImages(BitmapSource img) {
        var ratio = img.PixelWidth / (double)img.PixelHeight;
        var transform = Media3D.Transform3D.Identity;
        ushort orientation = 1;
        if (this.ExifReader != null && this.ExifReader.GetTagValue(ExifTags.Orientation, out orientation)) {
            switch (orientation) {
                default:
                case 1: //
                    transform = transform.AppendTransform(new Media3D.ScaleTransform3D(ratio, 1.0, 1.0));
                    break;
                case 2: //"-flip horizontal";;
                    //transform = Media3D.Transform3D.Identity;
                    break;
                case 3: //"-rotate 180";;
                    transform = transform.AppendTransform(new Media3D.ScaleTransform3D(ratio, 1.0, 1.0));
                    transform = transform.AppendTransform(
                        new Media3D.RotateTransform3D(new Media3D.AxisAngleRotation3D(new Vector3D(0, 0, 1), -180)));
                    break;
                case 4: //"-flip vertical";;
                    //transform = Media3D.Transform3D.Identity;
                    break;
                case 5: //"-transpose";;
                    //transform = Media3D.Transform3D.Identity;
                    break;
                case 6: //"-rotate 90";;
                    transform = transform.AppendTransform(new Media3D.ScaleTransform3D(1.0, 1.0 / ratio, 1.0));
                    transform = transform.AppendTransform(
                        new Media3D.RotateTransform3D(new Media3D.AxisAngleRotation3D(new Vector3D(0, 0, 1), -90)));
                    break;
                case 7: //"-transverse";;
                    // transform = Media3D.Transform3D.Identity;
                    break;
                case 8: //"-rotate 270";;
                    transform = transform.AppendTransform(new Media3D.ScaleTransform3D(1.0, 1.0 / ratio, 1.0));
                    transform = transform.AppendTransform(
                        new Media3D.RotateTransform3D(new Media3D.AxisAngleRotation3D(new Vector3D(0, 0, 1), -270)));
                    break;
            }

            this.PlaneTransform = transform;
            this.GridTransform = transform;
        } else {
            if (ratio > 1) {
                transform = transform.AppendTransform(new Media3D.ScaleTransform3D(ratio, 1.0, 1.0));
                this.PlaneTransform = transform;
                this.GridTransform = this.PlaneTransform;
            } else {
                transform = transform.AppendTransform(new Media3D.ScaleTransform3D(1.0, 1.0 / ratio, 1.0));
                this.PlaneTransform = transform;
                this.GridTransform = this.PlaneTransform;
            }
        }

        var white = new PhongMaterial() {
            DiffuseColor = Color.White,
            AmbientColor = Color.Black,
            ReflectiveColor = Color.Black,
            EmissiveColor = Color.Black,
            SpecularColor = Color.Black,
            DiffuseMap = new MemoryStream(img.ToByteArray()),
        };

        this.PlaneMaterial = white;
    }

    private void TryGetExif(string filename) {
        try {
            this.ExifReader = new ExifReader(filename);
            DateTime dateTime;
            this.ExifReader.GetTagValue(ExifTags.DateTime, out dateTime);
        } catch (Exception ex) {
            this.ExifReader = null;
        }
    }

    private void OnOpenClick() {
        try {
            var d = new Microsoft.Win32.OpenFileDialog() {
                Filter = "image files|*.jpg; *.png; *.bmp; *.gif",
            };
            if (d.ShowDialog().Value) {
                if (File.Exists(d.FileName)) {
                    var img = new BitmapImage(new Uri(d.FileName, UriKind.RelativeOrAbsolute));
                    this.TryGetExif(d.FileName);
                    this.SetImages(img);
                    this.Title = d.FileName;
                }
            }
        } catch (Exception ex) {
            MessageBox.Show(ex.Message, "File open error!", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
