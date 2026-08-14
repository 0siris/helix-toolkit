// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UICompositeManipulator3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   The can rotate x property.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf.SharpDX.Model;

namespace HelixToolkit.Wpf.SharpDX;

using Transform3D = Transform3D;

public class UiCompositeManipulator3D : CompositeModel3D {
    /// <summary>
    ///     The can rotate x property.
    /// </summary>
    public static readonly DependencyProperty CanRotateXProperty = DependencyProperty.Register("CanRotateX",
        typeof(bool),
        typeof(UiCompositeManipulator3D),
        new PropertyMetadata(true,
                             (d, e) => { (d as UiCompositeManipulator3D).rotateX.IsRendering = (bool)e.NewValue; }));

    /// <summary>
    ///     The can rotate y property.
    /// </summary>
    public static readonly DependencyProperty CanRotateYProperty = DependencyProperty.Register("CanRotateY",
        typeof(bool),
        typeof(UiCompositeManipulator3D),
        new PropertyMetadata(true,
                             (d, e) => { (d as UiCompositeManipulator3D).rotateY.IsRendering = (bool)e.NewValue; }));

    /// <summary>
    ///     The can rotate z property.
    /// </summary>
    public static readonly DependencyProperty CanRotateZProperty = DependencyProperty.Register("CanRotateZ",
        typeof(bool),
        typeof(UiCompositeManipulator3D),
        new PropertyMetadata(true,
                             (d, e) => { (d as UiCompositeManipulator3D).rotateZ.IsRendering = (bool)e.NewValue; }));

    /// <summary>
    ///     The can translate x property.
    /// </summary>
    public static readonly DependencyProperty CanTranslateXProperty = DependencyProperty.Register("CanTranslateX",
        typeof(bool),
        typeof(UiCompositeManipulator3D),
        new PropertyMetadata(true,
                             (d, e) => {
                                 (d as UiCompositeManipulator3D).translateX.IsRendering = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The can translate y property.
    /// </summary>
    public static readonly DependencyProperty CanTranslateYProperty = DependencyProperty.Register("CanTranslateY",
        typeof(bool),
        typeof(UiCompositeManipulator3D),
        new PropertyMetadata(true,
                             (d, e) => {
                                 (d as UiCompositeManipulator3D).translateY.IsRendering = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The can translate z property.
    /// </summary>
    public static readonly DependencyProperty CanTranslateZProperty = DependencyProperty.Register("CanTranslateZ",
        typeof(bool),
        typeof(UiCompositeManipulator3D),
        new PropertyMetadata(true,
                             (d, e) => {
                                 (d as UiCompositeManipulator3D).translateZ.IsRendering = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The diameter property.
    /// </summary>
    public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
        "Diameter",
        typeof(double),
        typeof(UiCompositeManipulator3D),
        new PropertyMetadata(2.0, ChildrenChanged));

    /// <summary>
    ///     The target transform property.
    /// </summary>
    public static readonly DependencyProperty TargetTransformProperty = DependencyProperty.Register("TargetTransform",
        typeof(Transform3D),
        typeof(UiCompositeManipulator3D),
        new FrameworkPropertyMetadata(Transform3D.Identity,
                                      FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                      (d, e) => { (d as Element3DCore).InvalidateRender(); }));

    private readonly UiRotateManipulator3D rotateX;
    private readonly UiRotateManipulator3D rotateY;
    private readonly UiRotateManipulator3D rotateZ;
    private readonly UiTranslateManipulator3D translateX;
    private readonly UiTranslateManipulator3D translateY;
    private readonly UiTranslateManipulator3D translateZ;

    /// <summary>
    /// </summary>
    public UiCompositeManipulator3D() {
        OnSceneNodeCreated += (s, e) => { e.Node.Attached += SceneNode_OnAttached; };
        translateX = new UiTranslateManipulator3D { Direction = new Vector3(1, 0, 0), Material = DiffuseMaterials.Red };
        translateY = new UiTranslateManipulator3D { Direction = new Vector3(0, 1, 0), Material = DiffuseMaterials.Green };
        translateZ = new UiTranslateManipulator3D { Direction = new Vector3(0, 0, 1), Material = DiffuseMaterials.Blue };
        rotateX = new UiRotateManipulator3D { Axis = new Vector3(1, 0, 0), Length = 0.05, Material = DiffuseMaterials.Red };
        rotateY = new UiRotateManipulator3D { Axis = new Vector3(0, 1, 0), Length = 0.05, Material = DiffuseMaterials.Green };
        rotateZ = new UiRotateManipulator3D { Axis = new Vector3(0, 0, 1), Length = 0.05, Material = DiffuseMaterials.Blue };

        // bind UITranslateManipulators3D.TargetTransform to this.Transform            
        BindingOperations.SetBinding(translateX,
                                     UiManipulator3D.TargetTransformProperty,
                                     new Binding("TargetTransform") { Source = this });
        BindingOperations.SetBinding(translateY,
                                     UiManipulator3D.TargetTransformProperty,
                                     new Binding("TargetTransform") { Source = this });
        BindingOperations.SetBinding(translateZ,
                                     UiManipulator3D.TargetTransformProperty,
                                     new Binding("TargetTransform") { Source = this });

        BindingOperations.SetBinding(rotateX,
                                     UiManipulator3D.TargetTransformProperty,
                                     new Binding("TargetTransform") { Source = this });
        BindingOperations.SetBinding(rotateY,
                                     UiManipulator3D.TargetTransformProperty,
                                     new Binding("TargetTransform") { Source = this });
        BindingOperations.SetBinding(rotateZ,
                                     UiManipulator3D.TargetTransformProperty,
                                     new Binding("TargetTransform") { Source = this });

        //BindingOperations.SetBinding(this.translateX, UIManipulator3D.TransformProperty, new Binding("TargetTransform") { Source = this });
        //BindingOperations.SetBinding(this.translateY, UIManipulator3D.TransformProperty, new Binding("TargetTransform") { Source = this });
        //BindingOperations.SetBinding(this.translateZ, UIManipulator3D.TransformProperty, new Binding("TargetTransform") { Source = this });

        //BindingOperations.SetBinding(this.rotateX, UIManipulator3D.TransformProperty, new Binding("TargetTransform") { Source = this });
        //BindingOperations.SetBinding(this.rotateY, UIManipulator3D.TransformProperty, new Binding("TargetTransform") { Source = this });
        //BindingOperations.SetBinding(this.rotateZ, UIManipulator3D.TransformProperty, new Binding("TargetTransform") { Source = this });

        // bind this.Transform to this.TargetTransform (TwoWay)
        BindingOperations.SetBinding(this,
                                     TransformProperty,
                                     new Binding("TargetTransform") { Source = this, Mode = BindingMode.TwoWay });

        Children.Clear();
        Children.Add(translateX);
        Children.Add(translateY);
        Children.Add(translateZ);
        Children.Add(rotateX);
        Children.Add(rotateY);
        Children.Add(rotateZ);
    }

    /// <summary>
    ///     Gets or sets TargetTransform.
    /// </summary>
    public Transform3D TargetTransform {
        get => (Transform3D)GetValue(TargetTransformProperty);
        set => SetValue(TargetTransformProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance can rotate X.
    /// </summary>
    /// <value> <c>true</c> if this instance can rotate X; otherwise, <c>false</c> . </value>
    public bool CanRotateX {
        get => (bool)GetValue(CanRotateXProperty);
        set => SetValue(CanRotateXProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance can rotate Y.
    /// </summary>
    /// <value> <c>true</c> if this instance can rotate Y; otherwise, <c>false</c> . </value>
    public bool CanRotateY {
        get => (bool)GetValue(CanRotateYProperty);
        set => SetValue(CanRotateYProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance can rotate Z.
    /// </summary>
    /// <value> <c>true</c> if this instance can rotate Z; otherwise, <c>false</c> . </value>
    public bool CanRotateZ {
        get => (bool)GetValue(CanRotateZProperty);
        set => SetValue(CanRotateZProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance can translate X.
    /// </summary>
    /// <value> <c>true</c> if this instance can translate X; otherwise, <c>false</c> . </value>
    public bool CanTranslateX {
        get => (bool)GetValue(CanTranslateXProperty);
        set => SetValue(CanTranslateXProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance can translate Y.
    /// </summary>
    /// <value> <c>true</c> if this instance can translate Y; otherwise, <c>false</c> . </value>
    public bool CanTranslateY {
        get => (bool)GetValue(CanTranslateYProperty);
        set => SetValue(CanTranslateYProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance can translate Z.
    /// </summary>
    /// <value> <c>true</c> if this instance can translate Z; otherwise, <c>false</c> . </value>
    public bool CanTranslateZ {
        get => (bool)GetValue(CanTranslateZProperty);
        set => SetValue(CanTranslateZProperty, value);
    }

    /// <summary>
    ///     Gets or sets the diameter.
    /// </summary>
    /// <value> The diameter. </value>
    public double Diameter {
        get => (double)GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    private void SceneNode_OnAttached(object sender, EventArgs e) {
        OnChildrenChanged();
    }

    /// <summary>
    ///     Binds this manipulator to a given Model3D.
    /// </summary>
    /// <param name="source">
    ///     Source Visual3D which receives the manipulator transforms.
    /// </param>
    public void Bind(GeometryModel3D source) {
        BindingOperations.SetBinding(this, TargetTransformProperty, new Binding("Transform") { Source = source });
        BindingOperations.SetBinding(this, TransformProperty, new Binding("Transform") { Source = source });
    }

    /// <summary>
    ///     Releases the binding of this manipulator.
    /// </summary>
    public void UnBind() {
        BindingOperations.ClearBinding(this, TargetTransformProperty);
        BindingOperations.ClearBinding(this, TransformProperty);
    }

    ///// <summary>
    ///// 
    ///// </summary>
    ///// <param name="context"></param>
    ///// <param name="totalModelMatrix"></param>
    ///// <param name="ray"></param>
    ///// <param name="hits"></param>
    ///// <returns></returns>
    //protected override bool OnHitTest(IRenderContext context, Matrix totalModelMatrix, ref Ray ray, ref List<HitTestResult> hits)
    //{
    //    bool hit = false;
    //    foreach (var c in this.Children)
    //    {
    //        var hc = c as IHitable;
    //        if (hc != null)
    //        {
    //            if (hc.HitTest(context, ray, ref hits))
    //            {
    //                hit = true;
    //            }
    //        }
    //    }
    //    return hit;
    //}

    /// <summary>
    ///     The on children changed.
    /// </summary>
    protected virtual void OnChildrenChanged() {
        var diameter = Diameter;

        translateX.Length = diameter;
        translateY.Length = diameter;
        translateZ.Length = diameter;

        rotateX.InnerDiameter = diameter;
        rotateY.InnerDiameter = diameter;
        rotateZ.InnerDiameter = diameter;

        rotateY.InnerDiameter += 0.01;
        rotateZ.InnerDiameter += 0.02;
        rotateX.OuterDiameter = rotateX.InnerDiameter + 0.25;
        rotateY.OuterDiameter = rotateY.InnerDiameter + 0.25;
        rotateZ.OuterDiameter = rotateZ.InnerDiameter + 0.25;
    }

    /// <summary>
    ///     The children changed.
    /// </summary>
    /// <param name="d">
    ///     The d.
    /// </param>
    /// <param name="e">
    ///     The event arguments.
    /// </param>
    private static void ChildrenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        var model = d as UiCompositeManipulator3D;
        if (model.SceneNode.IsAttached) model.OnChildrenChanged();
    }
}
