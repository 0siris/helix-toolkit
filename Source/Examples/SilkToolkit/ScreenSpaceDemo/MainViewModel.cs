// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace ScreenSpaceDemo;

using System;
using System.Linq;
using DemoCore;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public ObservableElement3DCollection ModelGeometry { get; private set; }
    public PhongMaterial DefaultMaterial { get; private set; }
    public Color GridColor { get; private set; }

    public Transform3D ModelTransform { get; private set; }

    public Vector3 DirectionalLightDirection1 { get; private set; }
    public Vector3 DirectionalLightDirection2 { get; private set; }
    public Color4 DirectionalLightColor { get; private set; }
    public Color4 AmbientLightColor { get; private set; }
    public Color4 BackgroundColor { get; private set; }
    public IRenderTechnique RenderTechnique { get; private set; }

    [Obsolete]
    public MainViewModel() {
        // ----------------------------------------------
        // titles
        Title = "Screen Space Ambient Occlusion Demo";
        SubTitle = "WPF & SharpDX";

        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(1.5, 2.5, 2.5),
            LookDirection = new Vector3D(-1.5, -2.5, -2.5),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // default render technique
        EffectsManager = new DefaultEffectsManager();
        RenderTechnique = EffectsManager[DefaultRenderTechniqueNames.Mesh];

        // background
        BackgroundColor = new Color4(1, 1, 1, 1);

        // setup lighting
        AmbientLightColor = new Color4(0.1f, 0.1f, 0.1f, 1.0f);
        DirectionalLightColor = new Color4(1, 1, 1, 1);
        DirectionalLightDirection1 = new Vector3(-2, -5, -2);
        DirectionalLightDirection2 = new Vector3(+2, +5, +5);

        // model materials
        DefaultMaterial = PhongMaterials.DefaultVrml;

        //load model
        var reader = new ObjReader();
        var objModel = reader.Read(@"./Media/CornellBox-Glossy.obj");

        ModelGeometry = [
            .. objModel.Select(x => new MeshGeometryModel3D() {
                Geometry = x.Geometry as MeshGeometry3D,
                Material = GetMaterialFromMaterialCore(x.Material as PhongMaterialCore),
            }),
        ];

        // model trafos
        ModelTransform = new TranslateTransform3D(0, 0, 0);
    }

    private static Material GetMaterialFromMaterialCore(PhongMaterialCore? material) {
        if (material is null) {
            return PhongMaterials.DefaultVrml;
        }

        var mat = new PhongMaterial {
            Name = material.Name,
            SpecularColor = material.SpecularColor,
            SpecularShininess = material.SpecularShininess,
            AmbientColor = material.AmbientColor,
            DiffuseColor = material.DiffuseColor,
            DiffuseMap = material.DiffuseMap,
            DiffuseMapSampler = material.DiffuseMapSampler,
            DiffuseAlphaMap = material.DiffuseAlphaMap,
            DisplacementMap = material.DisplacementMap,
            DisplacementMapSampler = material.DisplacementMapSampler,
            DisplacementMapScaleMask = material.DisplacementMapScaleMask,
            EmissiveColor = material.EmissiveColor,
            EnableTessellation = material.EnableTessellation,
            MaxDistanceTessellationFactor = material.MaxDistanceTessellationFactor,
            MaxTessellationDistance = material.MaxTessellationDistance,
            MinDistanceTessellationFactor = material.MinDistanceTessellationFactor,
            MinTessellationDistance = material.MinTessellationDistance,
            NormalMap = material.NormalMap,
            ReflectiveColor = material.ReflectiveColor,
            RenderDiffuseAlphaMap = material.RenderDiffuseAlphaMap,
            RenderDiffuseMap = material.RenderDiffuseMap,
            RenderDisplacementMap = material.RenderDisplacementMap,
            RenderNormalMap = material.RenderNormalMap,
        };

        return mat;
    }
}