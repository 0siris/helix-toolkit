// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UITranslateManipulator3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   A translate manipulator.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX.Utilities;

namespace HelixToolkit.Wpf.SharpDX;

using TranslateTransform3D = TranslateTransform3D;
using MatrixTransform3D = MatrixTransform3D;

/// <summary>
///     A translate manipulator.
/// </summary>
public class UITranslateManipulator3D : UIManipulator3D
{
    /// <summary>
    ///     The diameter property.
    /// </summary>
    public static readonly DependencyProperty DiameterProperty =
        DependencyProperty.Register("Diameter", typeof(double), typeof(UITranslateManipulator3D),
            new PropertyMetadata(0.2, ModelChanged));

    /// <summary>
    ///     The direction property.
    /// </summary>
    public static readonly DependencyProperty DirectionProperty =
        DependencyProperty.Register("Direction", typeof(Vector3), typeof(UITranslateManipulator3D),
            new PropertyMetadata(new Vector3(0, 0, 1), ModelChanged));

    /// <summary>
    ///     The length property.
    /// </summary>
    public static readonly DependencyProperty LengthProperty =
        DependencyProperty.Register("Length", typeof(double), typeof(UITranslateManipulator3D),
            new PropertyMetadata(1.0, ModelChanged));

    /// <summary>
    ///     Initializes a new instance of the <see cref="UIManipulator3D" /> class.
    /// </summary>
    public UITranslateManipulator3D()
    {
        Material = PhongMaterials.Red;
        Transform = new TranslateTransform3D();
    }

    /// <summary>
    ///     Gets or sets the diameter of the manipulator arrow.
    /// </summary>
    /// <value> The diameter. </value>
    public double Diameter
    {
        get => (double) GetValue(DiameterProperty);
        set => SetValue(DiameterProperty, value);
    }

    /// <summary>
    ///     Gets or sets the direction of the translation.
    /// </summary>
    /// <value> The direction. </value>
    [TypeConverter(typeof(Vector3Converter))]
    public Vector3 Direction
    {
        get => (Vector3) GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    /// <summary>
    ///     Gets or sets the length of the manipulator arrow.
    /// </summary>
    /// <value> The length. </value>
    public double Length
    {
        get => (double) GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }

    /// <summary>
    ///     Called when geometry has been changed.
    /// </summary>
    protected override void OnModelChanged()
    {
        var mb = new MeshBuilder();
        var p0 = Offset; // new Vector3(0, 0, 0);
        var d = Direction;
        d.Normalize();
        var p1 = p0 + d * (float) Length;
        mb.AddArrow(p0, p1, Diameter, 2, 64);
        Geometry = mb.ToMeshGeometry3D();
    }

    /// <summary>
    /// </summary>
    protected override void UpdateManipulator(RoutedEventArgs e)
    {
        var args = e as Mouse3DEventArgs;

        // camera normal
        var normalWS = cameraNormal;
        // move directon
        var directionWS = ToWorldVec(Direction);
        // up direction
        var upWS = SilkMath.Cross(normalWS, directionWS);
        // the direction plane
        normalWS = SilkMath.Cross(upWS, directionWS);
        normalWS.Normalize();
        // find new hit on the camera-direction plane
        if (viewport.UnProjectOnPlane(args.Position.ToVector2(), lastHitPosWS, normalWS, out var newHit))
        {
            // project point on ray
            // a: vec to project on
            //b(a) = (a.b)/(a.a)*a;
            var b = newHit - lastHitPosWS;
            var ab = SilkMath.Dot(directionWS, b);
            var aa = SilkMath.Dot(directionWS, directionWS);
            var ba = ab / aa * directionWS;
            newHit = lastHitPosWS + ba;

            var delta = newHit - lastHitPosWS;
            Value += SilkMath.Dot(delta, directionWS);
            var deltaTranslateTrafo = new TranslateTransform3D(delta.ToVector3D());

            if (TargetTransform != null)
            {
                TargetTransform = new MatrixTransform3D(TargetTransform.AppendTransform(deltaTranslateTrafo).Value);
            }
            else
            {
                if (Transform == null)
                    Transform = deltaTranslateTrafo;
                else
                    Transform = new MatrixTransform3D(Transform.AppendTransform(deltaTranslateTrafo).Value);
            }

            lastHitPosWS = newHit;
        }
    }
}