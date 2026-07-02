// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UIRotateManipulator3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   A translate manipulator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX.Utilities;

namespace HelixToolkit.Wpf.SharpDX;

using MatrixTransform3D = MatrixTransform3D;

/// <summary>
///     A translate manipulator.
/// </summary>
public class UIRotateManipulator3D : UIManipulator3D
{
    /// <summary>
    ///     The axis property.
    /// </summary>
    public static readonly DependencyProperty AxisProperty = DependencyProperty.Register(
        "Axis", typeof(Vector3), typeof(UIRotateManipulator3D),
        new PropertyMetadata(new Vector3(0, 0, 1), ModelChanged));

    /// <summary>
    ///     The diameter property.
    /// </summary>
    public static readonly DependencyProperty OuterDiameterProperty = DependencyProperty.Register(
        "OuterDiameter", typeof(double), typeof(UIRotateManipulator3D), new PropertyMetadata(1.5, ModelChanged));

    /// <summary>
    ///     The inner diameter property.
    /// </summary>
    public static readonly DependencyProperty InnerDiameterProperty = DependencyProperty.Register(
        "InnerDiameter", typeof(double), typeof(UIRotateManipulator3D), new PropertyMetadata(1.0, ModelChanged));

    /// <summary>
    ///     The length property.
    /// </summary>
    public static readonly DependencyProperty LengthProperty = DependencyProperty.Register(
        "Length", typeof(double), typeof(UIRotateManipulator3D), new PropertyMetadata(0.1, ModelChanged));

    /// <summary>
    ///     The pivot point property.
    /// </summary>
    public static readonly DependencyProperty PivotProperty = DependencyProperty.Register(
        "Pivot", typeof(Vector3), typeof(UIRotateManipulator3D), new PropertyMetadata(new Vector3(0, 0, 0)));

    /// <summary>
    ///     Initializes a new instance of the <see cref="UIManipulator3D" /> class.
    /// </summary>
    public UIRotateManipulator3D()
    {
        Transform = new RotateTransform3D();
    }

    /// <summary>
    ///     Gets or sets the rotation axis.
    /// </summary>
    /// <value>The axis.</value>
    [TypeConverter(typeof(Vector3Converter))]
    public Vector3 Axis
    {
        get => (Vector3) GetValue(AxisProperty);
        set => SetValue(AxisProperty, value);
    }

    /// <summary>
    ///     Gets or sets the diameter of the manipulator arrow.
    /// </summary>
    /// <value> The diameter. </value>
    public double OuterDiameter
    {
        get => (double) GetValue(OuterDiameterProperty);
        set => SetValue(OuterDiameterProperty, value);
    }

    /// <summary>
    ///     Gets or sets the inner diameter.
    /// </summary>
    /// <value>The inner diameter.</value>
    public double InnerDiameter
    {
        get => (double) GetValue(InnerDiameterProperty);
        set => SetValue(InnerDiameterProperty, value);
    }

    /// <summary>
    ///     Gets or sets the length of the cylinder.
    /// </summary>
    /// <value>The length.</value>
    public double Length
    {
        get => (double) GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    /// <summary>
    ///     Gets or sets the pivot point of the manipulator.
    /// </summary>
    /// <value> The position. </value>
    [TypeConverter(typeof(Vector3Converter))]
    public Vector3 Pivot
    {
        get => (Vector3) GetValue(PivotProperty);
        set => SetValue(PivotProperty, value);
    }

    /// <summary>
    ///     Called when geometry has been changed.
    /// </summary>
    protected override void OnModelChanged()
    {
        var mb = new MeshBuilder();
        var p0 = Offset; //new Vector3(0, 0, 0);
        if (InnerDiameter >= OuterDiameter)
            OuterDiameter = InnerDiameter + 0.3;

        var d = Axis;
        d.Normalize();
        var p1 = p0 - d * (float) Length * 0.5f;
        var p2 = p0 + d * (float) Length * 0.5f;
        mb.AddPipe(p1, p2, InnerDiameter, OuterDiameter, 64);
        Geometry = mb.ToMeshGeometry3D();
    }

    /// <summary>
    /// </summary>
    protected override void UpdateManipulator(RoutedEventArgs e)
    {
        if (!isMouseCaptured)
            return;

        var args = e as Mouse3DEventArgs;

        // --- get the plane for translation (camera normal is a good choice)                     
        var normal = cameraNormal;
        var position = new Vector3(TotalModelMatrix.M41, TotalModelMatrix.M42, TotalModelMatrix.M43);

        // --- hit position 
        if (viewport.UnProjectOnPlane(args.Position.ToVector2(), lastHitPosWS, normal, out var newHitPos))
        {
            var v = lastHitPosWS - position;
            var u = newHitPos - position;
            v.Normalize();
            u.Normalize();

            var currentAxis = SilkMath.Cross(u, v);
            var mainAxis = ToWorldVec(Axis); // this.Transform.Transform(this.Axis.ToVector3D()).ToVector3();
            double sign = -SilkMath.Dot(mainAxis, currentAxis);
            var theta = Math.Sign(sign) * Math.Asin(currentAxis.Length) / Math.PI * 180;
            Value += theta;

            var rotateTransform =
                new RotateTransform3D(new AxisAngleRotation3D(Axis.ToVector3D(), theta), Pivot.ToPoint3D());

            // rotate target
            if (TargetTransform != null)
            {
                TargetTransform = new MatrixTransform3D(rotateTransform.AppendTransform(TargetTransform).Value);
            }
            else
            {
                if (Transform == null)
                    Transform = rotateTransform;
                else
                    Transform = new MatrixTransform3D(rotateTransform.AppendTransform(Transform).Value);
            }

            lastHitPosWS = newHitPos;
        }
    }

    /// <summary>
    /// </summary>
    protected override void OnMouse3DMove(object sender, RoutedEventArgs e)
    {
        if (IsHitTestVisible)
            base.OnMouse3DMove(sender, e);
    }
}