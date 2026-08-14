/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Utilities;
using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;

namespace HelixToolkit.Wpf.SharpDX;

public class TransformManipulator3D : GroupElement3D {
    private static readonly Geometry3D TranslationXGeometry;
    private static readonly Geometry3D RotationXGeometry;
    private static readonly Geometry3D ScalingGeometry;

    private ManipulationType manipulationType = ManipulationType.None;

    static TransformManipulator3D() {
        var bd = new MeshBuilder();
        var arrowLength = 1.5f;
        bd.AddArrow(Vector3.UnitX * arrowLength, new Vector3(1.2f * arrowLength, 0, 0), 0.08, 4, 12);
        bd.AddCylinder(Vector3.Zero, Vector3.UnitX * arrowLength, 0.04, 12);
        TranslationXGeometry = bd.ToMesh();

        bd = new MeshBuilder();
        var circle = MeshBuilder.GetCircle(32);
        var path = circle.Select(x => new Vector3(0, x.X, x.Y)).ToArray();
        bd.AddTube(path, 0.06, 8, true);
        RotationXGeometry = bd.ToMesh();

        bd = new MeshBuilder();
        bd.AddBox(Vector3.UnitX * 0.8f, 0.15, 0.15, 0.15);
        bd.AddCylinder(Vector3.Zero, Vector3.UnitX * 0.8f, 0.02, 4);
        ScalingGeometry = bd.ToMesh();

        TranslationXGeometry.OctreeParameter.MinimumOctantSize = 0.01f;
        TranslationXGeometry.UpdateOctree();

        RotationXGeometry.OctreeParameter.MinimumOctantSize = 0.01f;
        RotationXGeometry.UpdateOctree();

        ScalingGeometry.OctreeParameter.MinimumOctantSize = 0.01f;
        ScalingGeometry.UpdateOctree();
    }

    public TransformManipulator3D() {
        var rotationYMatrix = SilkMath.RotationZ((float)Math.PI / 2);
        var rotationZMatrix = SilkMath.RotationY(-(float)Math.PI / 2);
        ctrlGroup = new GroupModel3D();

        #region Translation Models

        translationX = new MeshGeometryModel3D {
            Geometry = TranslationXGeometry, Material = DiffuseMaterials.Red, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        translationY = new MeshGeometryModel3D {
            Geometry = TranslationXGeometry, Material = DiffuseMaterials.Green, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        translationZ = new MeshGeometryModel3D {
            Geometry = TranslationXGeometry, Material = DiffuseMaterials.Blue, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        translationY.Transform = new Media3D.MatrixTransform3D(rotationYMatrix.ToMatrix3D());
        translationZ.Transform = new Media3D.MatrixTransform3D(rotationZMatrix.ToMatrix3D());
        translationX.Mouse3DDown += Translation_Mouse3DDown;
        translationY.Mouse3DDown += Translation_Mouse3DDown;
        translationZ.Mouse3DDown += Translation_Mouse3DDown;
        translationX.Mouse3DMove += Translation_Mouse3DMove;
        translationY.Mouse3DMove += Translation_Mouse3DMove;
        translationZ.Mouse3DMove += Translation_Mouse3DMove;
        translationX.Mouse3DUp += Manipulation_Mouse3DUp;
        translationY.Mouse3DUp += Manipulation_Mouse3DUp;
        translationZ.Mouse3DUp += Manipulation_Mouse3DUp;

        translationGroup = new GroupModel3D();
        translationGroup.Children.Add(translationX);
        translationGroup.Children.Add(translationY);
        translationGroup.Children.Add(translationZ);
        ctrlGroup.Children.Add(translationGroup);

        #endregion

        #region Rotation Models

        rotationX = new MeshGeometryModel3D {
            Geometry = RotationXGeometry, Material = DiffuseMaterials.Red, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        rotationY = new MeshGeometryModel3D {
            Geometry = RotationXGeometry, Material = DiffuseMaterials.Green, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        rotationZ = new MeshGeometryModel3D {
            Geometry = RotationXGeometry, Material = DiffuseMaterials.Blue, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        rotationY.Transform = new Media3D.MatrixTransform3D(rotationYMatrix.ToMatrix3D());
        rotationZ.Transform = new Media3D.MatrixTransform3D(rotationZMatrix.ToMatrix3D());
        rotationX.Mouse3DDown += Rotation_Mouse3DDown;
        rotationY.Mouse3DDown += Rotation_Mouse3DDown;
        rotationZ.Mouse3DDown += Rotation_Mouse3DDown;
        rotationX.Mouse3DMove += Rotation_Mouse3DMove;
        rotationY.Mouse3DMove += Rotation_Mouse3DMove;
        rotationZ.Mouse3DMove += Rotation_Mouse3DMove;
        rotationX.Mouse3DUp += Manipulation_Mouse3DUp;
        rotationY.Mouse3DUp += Manipulation_Mouse3DUp;
        rotationZ.Mouse3DUp += Manipulation_Mouse3DUp;

        rotationGroup = new GroupModel3D();
        rotationGroup.Children.Add(rotationX);
        rotationGroup.Children.Add(rotationY);
        rotationGroup.Children.Add(rotationZ);
        ctrlGroup.Children.Add(rotationGroup);

        #endregion

        #region Scaling Models

        scaleX = new MeshGeometryModel3D {
            Geometry = ScalingGeometry, Material = DiffuseMaterials.Red, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        scaleY = new MeshGeometryModel3D {
            Geometry = ScalingGeometry, Material = DiffuseMaterials.Green, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        scaleZ = new MeshGeometryModel3D {
            Geometry = ScalingGeometry, Material = DiffuseMaterials.Blue, CullMode = CullMode.Back,
            PostEffects = "ManipulatorXRayGrid"
        };
        scaleY.Transform = new Media3D.MatrixTransform3D(rotationYMatrix.ToMatrix3D());
        scaleZ.Transform = new Media3D.MatrixTransform3D(rotationZMatrix.ToMatrix3D());
        scaleX.Mouse3DDown += Scaling_Mouse3DDown;
        scaleY.Mouse3DDown += Scaling_Mouse3DDown;
        scaleZ.Mouse3DDown += Scaling_Mouse3DDown;
        scaleX.Mouse3DMove += Scaling_Mouse3DMove;
        scaleY.Mouse3DMove += Scaling_Mouse3DMove;
        scaleZ.Mouse3DMove += Scaling_Mouse3DMove;
        scaleX.Mouse3DUp += Manipulation_Mouse3DUp;
        scaleY.Mouse3DUp += Manipulation_Mouse3DUp;
        scaleZ.Mouse3DUp += Manipulation_Mouse3DUp;

        scaleGroup = new GroupModel3D();
        scaleGroup.Children.Add(scaleX);
        scaleGroup.Children.Add(scaleY);
        scaleGroup.Children.Add(scaleZ);
        ctrlGroup.Children.Add(scaleGroup);

        #endregion

        Children.Add(ctrlGroup);
        xrayEffect = new PostEffectMeshXRayGrid {
            EffectName = "ManipulatorXRayGrid",
            DimmingFactor = 0.5,
            BlendingFactor = 0.8,
            GridDensity = 4,
#if WINUI
                GridColor = Microsoft.UI.Colors.Gray
#else
            GridColor = Media.Colors.Gray
#endif
        };
        ((NodePostEffectXRayGrid)xrayEffect.SceneNode).XRayDrawingPassName =
            DefaultPassNames.EffectMeshDiffuseXRayGridP3;
        Children.Add(xrayEffect);
        SceneNode.Attached += SceneNode_OnAttached;
        SceneNode.Detached += SceneNode_OnDetached;
    }

    private void SceneNode_OnDetached(object? sender, EventArgs e) {
        //if (target != null)
        //{
        //    target.SceneNode.OnTransformChanged -= SceneNode_OnTransformChanged;
        //}
    }

    private void SceneNode_OnAttached(object? sender, EventArgs e) {
        OnTargetChanged(target);
    }

    protected virtual bool CanBeginTransform(MouseDown3DEventArgs e) => true;

    private void Manipulation_Mouse3DUp(object? sender, MouseUp3DEventArgs e) {
        if (isCaptured && e.HitTestResult?.ModelHit is MeshGeometryModel3D { Material: DiffuseMaterial material })
            material.DiffuseColor = currentColor;

        manipulationType = ManipulationType.None;
        isCaptured = false;
    }

    private void ResetTransforms() {
        scaleMatrix = rotationMatrix = targetMatrix = Matrix.Identity;
        translationVector = Vector3.Zero;
        OnUpdateSelfTransform();
    }

    /// <summary>
    ///     Called when [target changed]. Use target boundingbox center as Manipulator center
    /// </summary>
    /// <param name="target">The target.</param>
    private void OnTargetChanged(Element3D? target) {
        Debug.WriteLine("OnTargetChanged");
        //if(target != null)
        //{
        //    target.SceneNode.OnTransformChanged -= SceneNode_OnTransformChanged;
        //}
        this.target = target;
        if (target == null)
            ResetTransforms();
        else
            //target.SceneNode.OnTransformChanged += SceneNode_OnTransformChanged;
            SceneNode_OnTransformChanged(target.SceneNode, new TransformArgs(target.SceneNode.ModelMatrix));
    }

    private void SceneNode_OnTransformChanged(object? sender, TransformArgs e) {
        var m = e.Transform;
        m.Decompose(out var scale, out var rotation, out var translation);
        scaleMatrix = SilkMath.Scaling(scale);
        rotationMatrix = SilkMath.RotationQuaternion(rotation);
        if (centerOffset != Vector3.Zero) {
            var org = SilkMath.Translation(-centerOffset) * scaleMatrix * rotationMatrix *
                      SilkMath.Translation(centerOffset);
            translationVector = translation - new Vector3(org.M41, org.M42, org.M43);
        } else {
            translationVector = new Vector3(m.M41, m.M42, m.M43);
        }

        OnUpdateSelfTransform();
        //OnUpdateTargetMatrix();
    }

    private void OnUpdateTargetMatrix() {
        if (target == null) return;
        targetMatrix = SilkMath.Translation(-centerOffset) * scaleMatrix * rotationMatrix *
                       SilkMath.Translation(centerOffset) * SilkMath.Translation(translationVector);
        target.Transform = new Media3D.MatrixTransform3D(targetMatrix.ToMatrix3D());
    }

    private void OnUpdateSelfTransform() {
        var m = SilkMath.Translation(centerOffset + translationVector);
        m.M11 = m.M22 = m.M33 = (float)sizeScale;
        ctrlGroup.Transform = new Media3D.MatrixTransform3D(m.ToMatrix3D());
    }

    protected override SceneNode OnCreateSceneNode() => new AlwaysHitGroupNode(this);

    private enum ManipulationType {
        None,
        TranslationX,
        TranslationY,
        TranslationZ,
        RotationX,
        RotationY,
        RotationZ,
        ScaleX,
        ScaleY,
        ScaleZ
    }

    private sealed class AlwaysHitGroupNode : GroupNode {
        private readonly TransformManipulator3D manipulator;
        private readonly HashSet<object> models = [];

        public AlwaysHitGroupNode(TransformManipulator3D manipulator) {
            this.manipulator = manipulator;
        }

        protected override bool OnAttach(IEffectsManager effectsManager) {
            models.Add(manipulator.translationX);
            models.Add(manipulator.translationY);
            models.Add(manipulator.translationZ);
            models.Add(manipulator.rotationX);
            models.Add(manipulator.rotationY);
            models.Add(manipulator.rotationZ);
            models.Add(manipulator.scaleX);
            models.Add(manipulator.scaleY);
            models.Add(manipulator.scaleZ);
            return base.OnAttach(effectsManager);
        }

        protected override bool OnHitTest(
            HitTestContext context,
            Matrix totalModelMatrix,
            ref List<HitTestResult> hits
        ) {
            //Set hit distance to 0 so event manipulator is inside the model, hit test still works
            if (base.OnHitTest(context, totalModelMatrix, ref hits)) {
                if (hits.Count > 0) {
                    var res = new HitTestResult { Distance = float.MaxValue };
                    foreach (var hit in hits)
                        if (hit.ModelHit is { } modelHit && models.Contains(modelHit))
                            if (hit.Distance < res.Distance)
                                res = hit;

                    res.Distance = 0;
                }

                return true;
            }

            return false;
        }
    }

    #region Dependency Properties

    public Element3D? Target {
        get => (Element3D?)GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }


    public static readonly DependencyProperty TargetProperty =
        DependencyProperty.Register("Target",
                                    typeof(Element3D),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((TransformManipulator3D)d).OnTargetChanged(
                                                                 (Element3D?)e.NewValue);
                                                         }));


    public bool EnableScaling {
        get => (bool)GetValue(EnableScalingProperty);
        set => SetValue(EnableScalingProperty, value);
    }

    public static readonly DependencyProperty EnableScalingProperty =
        DependencyProperty.Register("EnableScaling",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.scaleX.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableScalingX;
                                                             manipulator.scaleY.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableScalingY;
                                                             manipulator.scaleZ.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableScalingZ;
                                                         }));

    public bool EnableScalingX {
        get => (bool)GetValue(EnableScalingXProperty);
        set => SetValue(EnableScalingXProperty, value);
    }

    public static readonly DependencyProperty EnableScalingXProperty =
        DependencyProperty.Register("EnableScalingX",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.scaleX.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableScalingX;
                                                         }));

    public bool EnableScalingY {
        get => (bool)GetValue(EnableScalingYProperty);
        set => SetValue(EnableScalingYProperty, value);
    }

    public static readonly DependencyProperty EnableScalingYProperty =
        DependencyProperty.Register("EnableScalingY",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.scaleY.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableScaling;
                                                         }));

    public bool EnableScalingZ {
        get => (bool)GetValue(EnableScalingZProperty);
        set => SetValue(EnableScalingZProperty, value);
    }

    public static readonly DependencyProperty EnableScalingZProperty =
        DependencyProperty.Register("EnableScalingZ",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.scaleZ.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableScaling;
                                                         }));


    public bool EnableTranslation {
        get => (bool)GetValue(EnableTranslationProperty);
        set => SetValue(EnableTranslationProperty, value);
    }

    public static readonly DependencyProperty EnableTranslationProperty =
        DependencyProperty.Register("EnableTranslation",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.translationX.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableTranslationX;
                                                             manipulator.translationY.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableTranslationY;
                                                             manipulator.translationZ.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableTranslationZ;
                                                         }));

    public bool EnableTranslationX {
        get => (bool)GetValue(EnableTranslationXProperty);
        set => SetValue(EnableTranslationXProperty, value);
    }

    public static readonly DependencyProperty EnableTranslationXProperty =
        DependencyProperty.Register("EnableTranslationX",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.translationX.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableTranslation;
                                                         }));

    public bool EnableTranslationY {
        get => (bool)GetValue(EnableTranslationYProperty);
        set => SetValue(EnableTranslationYProperty, value);
    }

    public static readonly DependencyProperty EnableTranslationYProperty =
        DependencyProperty.Register("EnableTranslationY",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.translationY.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableTranslation;
                                                         }));

    public bool EnableTranslationZ {
        get => (bool)GetValue(EnableTranslationZProperty);
        set => SetValue(EnableTranslationZProperty, value);
    }

    public static readonly DependencyProperty EnableTranslationZProperty =
        DependencyProperty.Register("EnableTranslationZ",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.translationZ.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableTranslation;
                                                         }));


    public bool EnableRotation {
        get => (bool)GetValue(EnableRotationProperty);
        set => SetValue(EnableRotationProperty, value);
    }

    public static readonly DependencyProperty EnableRotationProperty =
        DependencyProperty.Register("EnableRotation",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.rotationX.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableRotationX;
                                                             manipulator.rotationY.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableRotationY;
                                                             manipulator.rotationZ.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableRotationZ;
                                                         }));

    public bool EnableRotationX {
        get => (bool)GetValue(EnableRotationXProperty);
        set => SetValue(EnableRotationXProperty, value);
    }

    public static readonly DependencyProperty EnableRotationXProperty =
        DependencyProperty.Register("EnableRotationX",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.rotationX.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableRotation;
                                                         }));

    public bool EnableRotationY {
        get => (bool)GetValue(EnableRotationYProperty);
        set => SetValue(EnableRotationYProperty, value);
    }

    public static readonly DependencyProperty EnableRotationYProperty =
        DependencyProperty.Register("EnableRotationY",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.rotationY.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableRotation;
                                                         }));

    public bool EnableRotationZ {
        get => (bool)GetValue(EnableRotationZProperty);
        set => SetValue(EnableRotationZProperty, value);
    }

    public static readonly DependencyProperty EnableRotationZProperty =
        DependencyProperty.Register("EnableRotationZ",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.rotationZ.IsRendering =
                                                                 (bool)e.NewValue && manipulator.EnableRotation;
                                                         }));


    public bool EnableXRayGrid {
        get => (bool)GetValue(EnableXRayGridProperty);
        set => SetValue(EnableXRayGridProperty, value);
    }

    public static readonly DependencyProperty EnableXRayGridProperty =
        DependencyProperty.Register("EnableXRayGrid",
                                    typeof(bool),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                              ((TransformManipulator3D)d).xrayEffect.IsRendering =
                                                                 (bool)e.NewValue;
                                                         }));


    [TypeConverter(typeof(Vector3Converter))]
    public Vector3 CenterOffset {
        get => (Vector3)GetValue(CenterOffsetProperty);
        set => SetValue(CenterOffsetProperty, value);
    }

    public static readonly DependencyProperty CenterOffsetProperty =
        DependencyProperty.Register("CenterOffset",
                                    typeof(Vector3),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(Vector3.Zero,
                                                         (d, e) => {
                                                             var manipulator = (TransformManipulator3D)d;
                                                             manipulator.centerOffset = (Vector3)e.NewValue;
                                                             manipulator.OnUpdateSelfTransform();
                                                         }));

    public double SizeScale {
        get => (double)GetValue(SizeScaleProperty);
        set => SetValue(SizeScaleProperty, value);
    }

    public static readonly DependencyProperty SizeScaleProperty =
        DependencyProperty.Register("SizeScale",
                                    typeof(double),
                                    typeof(TransformManipulator3D),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((TransformManipulator3D)d).sizeScale =
                                                                 (double)e.NewValue;
                                                         }));

    #endregion

    #region Variables

    private readonly MeshGeometryModel3D translationX, translationY, translationZ;
    private readonly MeshGeometryModel3D rotationX, rotationY, rotationZ;
    private readonly MeshGeometryModel3D scaleX, scaleY, scaleZ;
    private readonly GroupModel3D translationGroup, rotationGroup, scaleGroup, ctrlGroup;
    private readonly Element3D xrayEffect;
    private Vector3 centerOffset = Vector3.Zero;
    private Vector3 translationVector = Vector3.Zero;
    private Matrix rotationMatrix = Matrix.Identity;
    private Matrix scaleMatrix = Matrix.Identity;
    private Matrix targetMatrix = Matrix.Identity;

    private Element3D? target;
    private Viewport3DX? currentViewport;
    private Vector3 lastHitPosWs;
    private Vector3 normal;

    private Vector3 direction;
    private Vector3 currentHit;
    private bool isCaptured;
    private double sizeScale = 1;
    private Color4 currentColor;

    #endregion

    #region Handle Translation

    private void Translation_Mouse3DDown(object? sender, MouseDown3DEventArgs e) {
        if (target == null || !CanBeginTransform(e)) return;
        var hitResult = e.HitTestResult;
        if (hitResult?.ModelHit is not Element3D elem) {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        if (elem == translationX) {
            manipulationType = ManipulationType.TranslationX;
            direction = Vector3.UnitX;
        } else if (elem == translationY) {
            manipulationType = ManipulationType.TranslationY;
            direction = Vector3.UnitY;
        } else if (elem == translationZ) {
            manipulationType = ManipulationType.TranslationZ;
            direction = Vector3.UnitZ;
        } else {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        if (elem is not MeshGeometryModel3D { Material: DiffuseMaterial material } || e.Viewport is not { } viewport) {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        currentColor = material.DiffuseColor;
        material.DiffuseColor = Color.Yellow;
        currentViewport = viewport;
        var cameraNormal = SilkMath.Normalize(viewport.Camera.CameraInternal.LookDirection);
        lastHitPosWs = hitResult.PointHit;
        var up = SilkMath.Cross(cameraNormal, direction);
        normal = SilkMath.Cross(up, direction);
        if (viewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWs, normal, out var hit)) {
            currentHit = hit;
            isCaptured = true;
        }
    }

    private void Translation_Mouse3DMove(object? sender, MouseMove3DEventArgs e) {
        if (!isCaptured || currentViewport is not { } viewport) return;
        if (viewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWs, normal, out var hit)) {
            var moveDir = hit - currentHit;
            currentHit = hit;
            switch (manipulationType) {
                case ManipulationType.TranslationX:
                    translationVector += new Vector3(moveDir.X, 0, 0);
                    break;
                case ManipulationType.TranslationY:
                    translationVector += new Vector3(0, moveDir.Y, 0);
                    break;
                case ManipulationType.TranslationZ:
                    translationVector += new Vector3(0, 0, moveDir.Z);
                    break;
            }

            OnUpdateSelfTransform();
            OnUpdateTargetMatrix();
        }
    }

    #endregion

    #region Handle Rotation

    private void Rotation_Mouse3DDown(object? sender, MouseDown3DEventArgs e) {
        if (target == null || !CanBeginTransform(e)) return;
        var hitResult = e.HitTestResult;
        if (hitResult?.ModelHit is not Element3D elem) {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        if (elem == rotationX) {
            manipulationType = ManipulationType.RotationX;
            direction = new Vector3(1, 0, 0);
        } else if (elem == rotationY) {
            manipulationType = ManipulationType.RotationY;
            direction = new Vector3(0, 1, 0);
        } else if (elem == rotationZ) {
            manipulationType = ManipulationType.RotationZ;
            direction = new Vector3(0, 0, 1);
        } else {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        if (elem is not MeshGeometryModel3D { Material: DiffuseMaterial material } || e.Viewport is not { } viewport) {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        currentColor = material.DiffuseColor;
        material.DiffuseColor = Color.Yellow;
        currentViewport = viewport;
        normal = SilkMath.Normalize(viewport.Camera.CameraInternal.LookDirection);
        lastHitPosWs = hitResult.PointHit;
        //var up = SilkMath.Cross(cameraNormal, direction);
        //normal = SilkMath.Cross(up, direction);
        if (viewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWs, normal, out var hit)) {
            currentHit = hit;
            isCaptured = true;
        }
    }

    private void Rotation_Mouse3DMove(object? sender, MouseMove3DEventArgs e) {
        if (!isCaptured || currentViewport is not { } viewport) return;
        if (viewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWs, normal, out var hit)) {
            var position = translationVector + centerOffset;
            var v = SilkMath.Normalize(currentHit - position);
            var u = SilkMath.Normalize(hit - position);
            var currentAxis = SilkMath.Cross(u, v);
            var axis = Vector3.UnitX;
            currentHit = hit;
            switch (manipulationType) {
                case ManipulationType.RotationX:
                    axis = Vector3.UnitX;
                    break;
                case ManipulationType.RotationY:
                    axis = Vector3.UnitY;
                    break;
                case ManipulationType.RotationZ:
                    axis = Vector3.UnitZ;
                    break;
            }

            var sign = -SilkMath.Dot(axis, currentAxis);
            var theta = (float)(Math.Sign(sign) * Math.Asin(currentAxis.Length));
            switch (manipulationType) {
                case ManipulationType.RotationX:
                    rotationMatrix *= SilkMath.RotationX(theta);
                    break;
                case ManipulationType.RotationY:
                    rotationMatrix *= SilkMath.RotationY(theta);
                    break;
                case ManipulationType.RotationZ:
                    rotationMatrix *= SilkMath.RotationZ(theta);
                    break;
            }

            OnUpdateTargetMatrix();
        }
    }

    #endregion

    #region Handle Scaling

    private void Scaling_Mouse3DDown(object? sender, MouseDown3DEventArgs e) {
        if (target == null || !CanBeginTransform(e)) return;
        var hitResult = e.HitTestResult;
        if (hitResult?.ModelHit is not Element3D elem) {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        if (elem == scaleX) {
            manipulationType = ManipulationType.ScaleX;
            direction = Vector3.UnitX;
        } else if (elem == scaleY) {
            manipulationType = ManipulationType.ScaleY;
            direction = Vector3.UnitY;
        } else if (elem == scaleZ) {
            manipulationType = ManipulationType.ScaleZ;
            direction = Vector3.UnitZ;
        } else {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        if (elem is not MeshGeometryModel3D { Material: DiffuseMaterial material } || e.Viewport is not { } viewport) {
            manipulationType = ManipulationType.None;
            isCaptured = false;
            return;
        }

        currentColor = material.DiffuseColor;
        material.DiffuseColor = Color.Yellow;
        currentViewport = viewport;
        var cameraNormal = SilkMath.Normalize(viewport.Camera.CameraInternal.LookDirection);
        lastHitPosWs = hitResult.PointHit;
        var up = SilkMath.Cross(cameraNormal, direction);
        normal = SilkMath.Cross(up, direction);
        if (viewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWs, normal, out var hit)) {
            currentHit = hit;
            isCaptured = true;
        }
    }

    private void Scaling_Mouse3DMove(object? sender, MouseMove3DEventArgs e) {
        if (!isCaptured || currentViewport is not { } viewport) return;
        if (viewport.UnProjectOnPlane(e.Position.ToVector2(), lastHitPosWs, normal, out var hit)) {
            var moveDir = hit - currentHit;
            currentHit = hit;
            var orgAxis = Vector3.Zero;
            float scale = 1;
            switch (manipulationType) {
                case ManipulationType.ScaleX:
                    orgAxis = Vector3.UnitX;
                    scale = moveDir.X;
                    break;
                case ManipulationType.ScaleY:
                    orgAxis = Vector3.UnitY;
                    scale = moveDir.Y;
                    break;
                case ManipulationType.ScaleZ:
                    orgAxis = Vector3.UnitZ;
                    scale = moveDir.Z;
                    break;
            }

            var axisX = SilkMath.TransformNormal(Vector3.UnitX, rotationMatrix);
            var axisY = SilkMath.TransformNormal(Vector3.UnitY, rotationMatrix);
            var axisZ = SilkMath.TransformNormal(Vector3.UnitZ, rotationMatrix);
            var dotX = SilkMath.Dot(axisX, orgAxis);
            var dotY = SilkMath.Dot(axisY, orgAxis);
            var dotZ = SilkMath.Dot(axisZ, orgAxis);
            scaleMatrix.M11 += scale * Math.Abs(dotX);
            scaleMatrix.M22 += scale * Math.Abs(dotY);
            scaleMatrix.M33 += scale * Math.Abs(dotZ);
            OnUpdateTargetMatrix();
        }
    }

    #endregion
}
