// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PointLight3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;

namespace HelixToolkit.Wpf.SharpDX.Model.Lights3D;

public class PointLight3D : Light3D {
    public static readonly DependencyProperty AttenuationProperty =
        DependencyProperty.Register("Attenuation",
                                    typeof(Vector3D),
                                    typeof(PointLight3D),
                                    new PropertyMetadata(new Vector3D(1.0f, 0.0f, 0.0f),
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: PointLightNode node })
                                                                 node.Attenuation = ((Vector3D)e.NewValue).ToVector3();
                                                         }));

    public static readonly DependencyProperty RangeProperty =
        DependencyProperty.Register("Range",
                                    typeof(double),
                                    typeof(PointLight3D),
                                    new PropertyMetadata(100.0,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: PointLightNode node })
                                                                 node.Range = (float)(double)e.NewValue;
                                                         }));

    public static readonly DependencyProperty PositionProperty =
        DependencyProperty.Register("Position",
                                    typeof(Point3D),
                                    typeof(PointLight3D),
                                    new PropertyMetadata(new Point3D(),
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: PointLightNode node })
                                                                 node.Position = ((Point3D)e.NewValue).ToVector3();
                                                         }));

    /// <summary>
    ///     The position of the model in world space.
    /// </summary>
    public Point3D Position {
        get => (Point3D)GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>
    ///     Attenuation coefficients:
    ///     X = constant attenuation,
    ///     Y = linar attenuation,
    ///     Z = quadratic attenuation.
    ///     For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb172279(v=vs.85).aspx
    /// </summary>
    public Vector3D Attenuation {
        get => (Vector3D)GetValue(AttenuationProperty);
        set => SetValue(AttenuationProperty, value);
    }

    /// <summary>
    ///     Range of this light. This is the maximum distance
    ///     of a pixel being lit by this light.
    ///     For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb172279(v=vs.85).aspx
    /// </summary>
    public double Range {
        get => (double)GetValue(RangeProperty);
        set => SetValue(RangeProperty, value);
    }

    protected override SceneNode OnCreateSceneNode() => new PointLightNode();

    protected override void AssignDefaultValuesToSceneNode(SceneNode core) {
        base.AssignDefaultValuesToSceneNode(core);
        if (core is PointLightNode n) {
            n.Attenuation = Attenuation.ToVector3();
            n.Range = (float)Range;
            n.Position = Position.ToVector3();
        }
    }
}
