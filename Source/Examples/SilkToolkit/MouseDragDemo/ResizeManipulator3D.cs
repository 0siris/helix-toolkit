// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ResizeManipulator3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   The can translate x property.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace MouseDragDemo;

using System.Diagnostics.CodeAnalysis;
using System.Windows;
using HelixToolkit.Wpf.SharpDX;
using Colors = System.Windows.Media.Colors;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

public class ResizeManipulator3D : GroupElement3D //, IHitable, INotifyPropertyChanged
{
    private UiTranslateManipulator3D translateXl,
        translateYl,
        translateZl,
        translateXr,
        translateYr,
        translateZr;

    private LineGeometryModel3D selectionBounds;


    /// <summary>
    /// The can translate x property.
    /// </summary>
    public static readonly DependencyProperty CanTranslateXProperty = DependencyProperty.Register(
        "CanTranslateX",
        typeof(bool),
        typeof(ResizeManipulator3D),
        new UIPropertyMetadata(true, ChildrenChanged));

    /// <summary>
    /// The can translate y property.
    /// </summary>
    public static readonly DependencyProperty CanTranslateYProperty = DependencyProperty.Register(
        "CanTranslateY",
        typeof(bool),
        typeof(ResizeManipulator3D),
        new UIPropertyMetadata(true, ChildrenChanged));

    /// <summary>
    /// The can translate z property.
    /// </summary>
    public static readonly DependencyProperty CanTranslateZProperty = DependencyProperty.Register(
        "CanTranslateZ",
        typeof(bool),
        typeof(ResizeManipulator3D),
        new UIPropertyMetadata(true, ChildrenChanged));

    /// <summary>
    /// 
    /// </summary>
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        "Content",
        typeof(MeshGeometryModel3D),
        typeof(ResizeManipulator3D),
        new UIPropertyMetadata(null, ContentChanged));

    /// <summary>
    /// Gets or sets a value indicating whether this instance can translate X.
    /// </summary>
    /// <value> <c>true</c> if this instance can translate X; otherwise, <c>false</c> . </value>
    public bool CanTranslateX {
        get => (bool) GetValue(CanTranslateXProperty);
        set => SetValue(CanTranslateXProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can translate Y.
    /// </summary>
    /// <value> <c>true</c> if this instance can translate Y; otherwise, <c>false</c> . </value>
    public bool CanTranslateY {
        get => (bool) GetValue(CanTranslateYProperty);
        set => SetValue(CanTranslateYProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance can translate Z.
    /// </summary>
    /// <value> <c>true</c> if this instance can translate Z; otherwise, <c>false</c> . </value>
    public bool CanTranslateZ {
        get => (bool) GetValue(CanTranslateZProperty);
        set => SetValue(CanTranslateZProperty, value);
    }


    /// <summary>
    /// 
    /// </summary>
    public ResizeManipulator3D() {
        var red = PhongMaterials.Red;
        red.ReflectiveColor = Colors.Black.ToColor4();
        //red.SpecularShininess = 0f;
        translateXr = new UiTranslateManipulator3D {
            Direction = new Vector3(+1, 0, 0),
            IsThrowingShadow = false,
            Material = red,
        };
        translateYr = new UiTranslateManipulator3D {
            Direction = new Vector3(0, +1, 0),
            IsThrowingShadow = false,
            Material = PhongMaterials.Green
        };
        translateZr = new UiTranslateManipulator3D {
            Direction = new Vector3(0, 0, +1),
            IsThrowingShadow = false,
            Material = PhongMaterials.Blue
        };
        translateXl = new UiTranslateManipulator3D {
            Direction = new Vector3(-1, 0, 0),
            IsThrowingShadow = false,
            Material = red
        };
        translateYl = new UiTranslateManipulator3D {
            Direction = new Vector3(0, -1, 0),
            IsThrowingShadow = false,
            Material = PhongMaterials.Green
        };
        translateZl = new UiTranslateManipulator3D {
            Direction = new Vector3(0, 0, -1),
            IsThrowingShadow = false,
            Material = PhongMaterials.Blue
        };
        //this.rotateZ = new UIRotateManipulator3D { Axis = Vector3.UnitZ, InnerDiameter = 2, OuterDiameter = 2.15, Length = 0.05 };

        CanTranslateX = true;
        CanTranslateY = false;
        CanTranslateZ = false;
        IsRendering = false;

        OnChildrenChanged();
        // this.OnContentChanged();                       
    }

    ~ResizeManipulator3D() { }


    /// <summary>
    /// The children changed.
    /// </summary>
    /// <param name="d">
    /// The d.
    /// </param>
    /// <param name="e">
    /// The event arguments.
    /// </param>
    private static void ChildrenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((ResizeManipulator3D) d).OnChildrenChanged();
    }

    /// <summary>
    /// 
    /// </summary>
    private static void ContentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args) {
        //((ResizeManipulator3D)obj).OnContentChanged();
    }


    /// <summary>
    /// The on children changed.
    /// </summary>
    [MemberNotNull(nameof(selectionBounds))]
    protected virtual void OnChildrenChanged() {
        translateXl.Length = 0.5;
        translateYl.Length = 0.5;
        translateZl.Length = 0.5;
        translateXr.Length = 0.5;
        translateYr.Length = 0.5;
        translateZr.Length = 0.5;

        Children.Clear();

        if (CanTranslateX) {
            Children.Add(translateXl);
            Children.Add(translateXr);
        }

        if (CanTranslateY) {
            Children.Add(translateYl);
            Children.Add(translateYr);
        }

        if (CanTranslateZ) {
            Children.Add(translateZl);
            Children.Add(translateZr);
        }


        {
            var g = new LineBuilder();
            g.AddLine(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            g.AddLine(new Vector3(1, 0, 0), new Vector3(1, 1, 0));
            g.AddLine(new Vector3(1, 1, 0), new Vector3(0, 1, 0));
            g.AddLine(new Vector3(0, 1, 0), new Vector3(0, 0, 0));
            selectionBounds = new LineGeometryModel3D() {
                Thickness = 3,
                Smoothness = 2,
                Color = Colors.Red,
                IsThrowingShadow = false,
                Geometry = g.ToLineGeometry3D(),
            };
            Children.Add(selectionBounds);
        }
    }
}