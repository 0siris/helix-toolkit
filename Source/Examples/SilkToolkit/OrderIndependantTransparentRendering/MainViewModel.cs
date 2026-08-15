// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;

namespace OrderIndependentTransparentRendering;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using DemoCore;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Media3D = System.Windows.Media.Media3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

public enum MaterialType { BlinnPhong, Pbr, Diffuse };

public class MainViewModel : BaseViewModel {
    private const string OpenFileFilter = "3D model files (*.obj;*.3ds;*.stl|*.obj;*.3ds;*.stl;";

    public ObservableElement3DCollection ModelGeometry { get; private set; }

    public ObservableElement3DCollection PlaneGeometry { private set; get; } = [];

    public LineGeometry3D GridModel { private set; get; } = new();

    public Media3D.Transform3D GridTransform { private set; get; } = Media3D.Transform3D.Identity;

    public bool ShowWireframe {
        set {
            if (SetValue(ref field, value)) {
                foreach (var model in ModelGeometry) {
                    if (model is MeshGeometryModel3D mesh)
                        mesh.RenderWireframe = value;
                }
            }
        }
        get;
    } = false;

    public OutlineMode DrawMode { set; get; } = OutlineMode.Merged;

    public bool HighlightSeparated {
        set {
            if (SetValue(ref field, value)) {
                DrawMode = value
                    ? OutlineMode.Separated
                    : OutlineMode.Merged;
                OnPropertyChanged(nameof(DrawMode));
            }
        }
        get;
    } = false;

    public bool OitWeightedModeEnabled {
        set => SetValue(ref field, value);
        get;
    } = false;

    private bool oitDepthPeelModeEnabled = true;

    public bool OitDepthPeelModeEnabled {
        set => SetValue(ref oitDepthPeelModeEnabled, value);
        get => oitDepthPeelModeEnabled;
    }

    public OitRenderType OitRenderType {
        set {
            if (SetValue(ref field, value)) {
                switch (value) {
                    case OitRenderType.None:
                        OitDepthPeelModeEnabled = OitWeightedModeEnabled = false;
                        break;
                    case OitRenderType.DepthPeeling:
                        OitDepthPeelModeEnabled = true;
                        OitWeightedModeEnabled = false;
                        break;
                    case OitRenderType.SinglePassWeighted:
                        oitDepthPeelModeEnabled = false;
                        OitWeightedModeEnabled = true;
                        break;
                }
            }
        }
        get;
    } = OitRenderType.DepthPeeling;

    private MaterialType materialType = MaterialType.BlinnPhong;

    public MaterialType MaterialType {
        set {
            if (SetValue(ref materialType, value)) {
                UpdateMaterials();
            }
        }
        get => materialType;
    }

    public OitWeightMode[] OitWeights { get; } =
        [OitWeightMode.Linear0, OitWeightMode.Linear1, OitWeightMode.Linear2, OitWeightMode.NonLinear];

    public OitRenderType[] OitRenderTypes { get; } =
        [OitRenderType.None, OitRenderType.DepthPeeling, OitRenderType.SinglePassWeighted];

    public MaterialType[] MaterialTypes { get; } = [MaterialType.BlinnPhong, MaterialType.Pbr, MaterialType.Diffuse];

    public int RedPlaneOpacity {
        set {
            if (SetValue(ref field, value)) {
                if (PlaneGeometry.Count > 0 && PlaneGeometry[0] is MeshGeometryModel3D {
                        Material: PhongMaterial material
                    })
                    material.DiffuseColor = new Color4(1, 0, 0, value / 100f);
            }
        }
        get;
    } = 60;

    public int GreenPlaneOpacity {
        set {
            if (SetValue(ref field, value)) {
                if (PlaneGeometry.Count > 1 && PlaneGeometry[1] is MeshGeometryModel3D {
                        Material: PhongMaterial material
                    })
                    material.DiffuseColor = new Color4(0, 1, 0, value / 100f);
            }
        }
        get;
    } = 60;

    public int BluePlaneOpacity {
        set {
            if (SetValue(ref field, value)) {
                if (PlaneGeometry.Count > 2 && PlaneGeometry[2] is MeshGeometryModel3D {
                        Material: PhongMaterial material
                    })
                    material.DiffuseColor = new Color4(0, 0, 1, value / 100f);
            }
        }
        get;
    } = 60;

    public ICommand ResetCameraCommand { set; get; }

    private readonly SynchronizationContext context = SynchronizationContext.Current
                                                      ?? throw new InvalidOperationException(
                                                          "The transparent rendering demo requires a synchronization context.");


    private readonly Random rnd = new();

    public MainViewModel() {
        ModelGeometry = [];
        EffectsManager = new DefaultEffectsManager();
        Camera = new OrthographicCamera() {
            LookDirection = new Media3D.Vector3D(0, -50, -50),
            Position = new Media3D.Point3D(0, 50, 50),
            FarPlaneDistance = 500,
            NearPlaneDistance = 0.1,
            Width = 100
        };
        ResetCameraCommand = new RelayCommand((_) => { Camera.Reset(); });
        Task.Run(() => Load3Ds("NITRO_ENGINE.3ds"));

        BuildGrid();
        BuildPlanes();
    }

    private void BuildGrid() {
        var builder = new LineBuilder();
        var zOff = -45;
        for (var i = 0; i < 10; ++i) {
            for (var j = 0; j < 10; ++j) {
                builder.AddLine(new Vector3(-i * 5, 0, j * 5), new Vector3(i * 5, 0, j * 5));
                builder.AddLine(new Vector3(-i * 5, 0, -j * 5), new Vector3(i * 5, 0, -j * 5));
                builder.AddLine(new Vector3(i * 5, 0, -j * 5), new Vector3(i * 5, 0, j * 5));
                builder.AddLine(new Vector3(-i * 5, 0, -j * 5), new Vector3(-i * 5, 0, j * 5));
                builder.AddLine(new Vector3(-i * 5, j * 5, zOff), new Vector3(i * 5, j * 5, zOff));
                builder.AddLine(new Vector3(i * 5, 0, zOff), new Vector3(i * 5, j * 5, zOff));
                builder.AddLine(new Vector3(-i * 5, 0, zOff), new Vector3(-i * 5, j * 5, zOff));
            }
        }

        GridModel = builder.ToLineGeometry3D();
        GridTransform = new Media3D.TranslateTransform3D(new Media3D.Vector3D(0, -10, 0));
    }

    private void BuildPlanes() {
        PlaneGeometry = [];
        var builder = new MeshBuilder(true);
        builder.AddBox(new Vector3(0, 0, 0), 15, 15, 0.5);
        var mesh = builder.ToMesh();

        var material = new PhongMaterial {
            DiffuseColor = new Color4(1, 0, 0, RedPlaneOpacity / 100f)
        };

        var model = new MeshGeometryModel3D() {
            Geometry = mesh,
            Material = material,
            Transform = new Media3D.TranslateTransform3D(-15, 0, 0),
            IsTransparent = true,
            CullMode = CullMode.Back
        };
        PlaneGeometry.Add(model);

        material = new PhongMaterial {
            DiffuseColor = new Color4(0, 1, 0, GreenPlaneOpacity / 100f)
        };

        model = new MeshGeometryModel3D() {
            Geometry = mesh,
            Material = material,
            Transform = new Media3D.TranslateTransform3D(-20, 5, -10),
            IsTransparent = true,
            CullMode = CullMode.Back
        };
        PlaneGeometry.Add(model);

        material = new PhongMaterial {
            DiffuseColor = new Color4(0, 0, 1, BluePlaneOpacity / 100f)
        };

        model = new MeshGeometryModel3D() {
            Geometry = mesh,
            Material = material,
            Transform = new Media3D.TranslateTransform3D(-25, 10, -20),
            IsTransparent = true,
            CullMode = CullMode.Back
        };
        PlaneGeometry.Add(model);
    }

    [Obsolete]
    public void Load3Ds(string path) {
        var reader = new StudioReader();
        var objCol = reader.Read(path);
        AttachModelList(objCol);
    }

    [Obsolete]
    public void LoadObj(string path) {
        var reader = new ObjReader();
        var objCol = reader.Read(path);
        AttachModelList(objCol);
    }

    [Obsolete]
    public void LoadStl(string path) {
        var reader = new StLReader();
        var objCol = reader.Read(path);
        AttachModelList(objCol);
    }

    public void AttachModelList(List<Object3D>? objs) {
        if (objs is null)
            return;

        foreach (var ob in objs) {
            if (ob.Geometry is not { } geometry)
                continue;
            geometry.UpdateOctree();
            Task.Delay(50)
                .Wait(); //Only for async loading demo
            context.Post((_) => {
                    var s = new MeshGeometryModel3D {
                        Geometry = geometry,
                        IsTransparent = true,
                        DepthBias = -100
                    };
                    UpdateMaterial(s);
                    ModelGeometry.Add(s);
                },
                null);
        }
    }

    private void UpdateMaterials() {
        foreach (var geo in ModelGeometry) {
            if (geo is MeshGeometryModel3D mesh) {
                UpdateMaterial(mesh);
            }
        }
    }

    private void UpdateMaterial(MeshGeometryModel3D mesh) {
        var diffuse = new Color4 {
            X = (float) rnd.NextDouble(),
            Y = (float) rnd.NextDouble(),
            Z = (float) rnd.NextDouble(),
            W = 0.6f
        };
        Material material = materialType switch {
            MaterialType.BlinnPhong => new PhongMaterial() {
                DiffuseColor = diffuse
            },
            MaterialType.Pbr => new PbrMaterial() {
                AlbedoColor = diffuse,
                MetallicFactor = 0.7f,
                RoughnessFactor = 0.6f,
                ReflectanceFactor = 0.2,
            },
            MaterialType.Diffuse => new DiffuseMaterial() {
                DiffuseColor = diffuse
            },
            _ => throw new ArgumentOutOfRangeException(nameof(materialType))
        };

        mesh.Material = material;
    }
}