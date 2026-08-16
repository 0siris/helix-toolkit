// <copyright file="ScreenSpaceMeshGeometry3D.cs" company="Helix Toolkit">
//   Copyright (c) 2017 Helix Toolkit contributors
//   Author: Lunci Hua
// </copyright>

using System;
using System.Windows;
using System.Windows.Data;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Converters;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements2D;
using Point3D = System.Windows.Media.Media3D.Point3D;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;
/// <summary>
///     Base class for screen space rendering, such as Coordinate System or ViewBox
/// </summary>
public abstract class ScreenSpacedElement3D : GroupModel3D {
    /// <summary>
    ///     <see cref="RelativeScreenLocationX" />
    /// </summary>
    public static readonly DependencyProperty RelativeScreenLocationXProperty = DependencyProperty.Register(
        "RelativeScreenLocationX",
        typeof(double),
        typeof(ScreenSpacedElement3D),
        new PropertyMetadata(-0.8,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: ScreenSpacedNode node })
                                     node.RelativeScreenLocationX = (float)(double)e.NewValue;
                             }));

    /// <summary>
    ///     <see cref="RelativeScreenLocationY" />
    /// </summary>
    public static readonly DependencyProperty RelativeScreenLocationYProperty = DependencyProperty.Register(
        "RelativeScreenLocationY",
        typeof(double),
        typeof(ScreenSpacedElement3D),
        new PropertyMetadata(-0.8,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: ScreenSpacedNode node })
                                     node.RelativeScreenLocationY = (float)(double)e.NewValue;
                             }));

    /// <summary>
    ///     <see cref="SizeScale" />
    /// </summary>
    public static readonly DependencyProperty SizeScaleProperty = DependencyProperty.Register("SizeScale",
        typeof(double),
        typeof(ScreenSpacedElement3D),
        new PropertyMetadata(1.0,
                             (d, e) => {
                                 if (d is Element3DCore { SceneNode: ScreenSpacedNode node })
                                     node.SizeScale = (float)(double)e.NewValue;
                             }));


    /// <summary>
    ///     The enable mover property
    /// </summary>
    public static readonly DependencyProperty EnableMoverProperty =
        DependencyProperty.Register("EnableMover",
                                    typeof(bool),
                                    typeof(ScreenSpacedElement3D),
                                    new PropertyMetadata(true));

    /// <summary>
    ///     The mode property
    /// </summary>
    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register("Mode",
                                    typeof(ScreenSpacedMode),
                                    typeof(ScreenSpacedElement3D),
                                    new PropertyMetadata(ScreenSpacedMode.RelativeScreenSpaced,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: ScreenSpacedNode node })
                                                                 node.Mode = (ScreenSpacedMode)e.NewValue;
                                                         }));


    /// <summary>
    ///     The absolute position3 d property
    /// </summary>
    public static readonly DependencyProperty AbsolutePosition3DProperty =
        DependencyProperty.Register("AbsolutePosition3D",
                                    typeof(Point3D),
                                    typeof(ScreenSpacedElement3D),
                                    new PropertyMetadata(new Point3D(),
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: ScreenSpacedNode node })
                                                                 node.AbsolutePosition3D = ((Point3D)e.NewValue).ToVector3();
                                                         }));

    /// <summary>
    ///     Gets or sets a value indicating whether [enable mover].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable mover]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableMover {
        get => (bool)GetValue(EnableMoverProperty);
        set => SetValue(EnableMoverProperty, value);
    }

    /// <summary>
    ///     Relative Location X on screen. Range from -1~1
    /// </summary>
    public double RelativeScreenLocationX {
        get => (double)GetValue(RelativeScreenLocationXProperty);
        set => SetValue(RelativeScreenLocationXProperty, value);
    }

    /// <summary>
    ///     Relative Location Y on screen. Range from -1~1
    /// </summary>
    public double RelativeScreenLocationY {
        get => (double)GetValue(RelativeScreenLocationYProperty);
        set => SetValue(RelativeScreenLocationYProperty, value);
    }

    /// <summary>
    ///     Size scaling
    /// </summary>
    public double SizeScale {
        get => (double)GetValue(SizeScaleProperty);
        set => SetValue(SizeScaleProperty, value);
    }

    /// <summary>
    ///     Gets or sets the mode.
    /// </summary>
    /// <value>
    ///     The mode.
    /// </value>
    public ScreenSpacedMode Mode {
        get => (ScreenSpacedMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>
    ///     Gets or sets the absolute position in 3d. Use by <see cref="Mode" /> =
    ///     <see cref="ScreenSpacedMode.AbsolutePosition3D" />
    /// </summary>
    /// <value>
    ///     The absolute position3 d.
    /// </value>
    public Point3D AbsolutePosition3D {
        get => (Point3D)GetValue(AbsolutePosition3DProperty);
        set => SetValue(AbsolutePosition3DProperty, value);
    }

    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        if (node is ScreenSpacedNode n) {
            n.RelativeScreenLocationX = (float)RelativeScreenLocationX;
            n.RelativeScreenLocationY = (float)RelativeScreenLocationY;
            n.SizeScale = (float)SizeScale;
            n.AbsolutePosition3D = AbsolutePosition3D.ToVector3();
            n.Mode = Mode;
        }

        base.AssignDefaultValuesToSceneNode(node);
        InitializeMover();
    }

    #region 2D stuffs

    public RelativePositionCanvas2D MoverCanvas { get; }
        = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };

    private ScreenSpacePositionMoverBase? mover;

    private bool isMoverInitialized;

    private void InitializeMover() {
        if (isMoverInitialized) 
            return;
        
        mover = OnCreateMover();
        MoverCanvas.Children.Add(mover);
        SetBinding(nameof(RelativeScreenLocationX), mover, RelativePositionCanvas2D.RelativeXProperty, this);
        SetBinding(nameof(RelativeScreenLocationY), mover, RelativePositionCanvas2D.RelativeYProperty, this);
        SetBinding(nameof(IsRendering),
                   mover,
                   Elements2D.Abstract.Element2D.VisibilityProperty,
                   this,
                   BindingMode.OneWay,
                   new BoolToVisibilityConverter());
        SetBinding(nameof(EnableMover),
                   mover,
                   ScreenSpacePositionMoverBase.EnableMoverProperty,
                   this,
                   BindingMode.OneWay);
        mover.OnMoveClicked += Mover_OnMoveClicked;
        isMoverInitialized = true;
    }

    protected virtual ScreenSpacePositionMoverBase OnCreateMover() => new ScreenSpacePositionMover();

    private void Mover_OnMoveClicked(object? sender, ScreenSpaceMoveDirArgs e) {
        switch (e.Direction) {
            case ScreenSpaceMoveDirection.LeftTop:
                RelativeScreenLocationX = -Math.Abs(RelativeScreenLocationX);
                RelativeScreenLocationY = Math.Abs(RelativeScreenLocationY);
                break;
            case ScreenSpaceMoveDirection.LeftBottom:
                RelativeScreenLocationX = -Math.Abs(RelativeScreenLocationX);
                RelativeScreenLocationY = -Math.Abs(RelativeScreenLocationY);
                break;
            case ScreenSpaceMoveDirection.RightTop:
                RelativeScreenLocationX = Math.Abs(RelativeScreenLocationX);
                RelativeScreenLocationY = Math.Abs(RelativeScreenLocationY);
                break;
            case ScreenSpaceMoveDirection.RightBottom:
                RelativeScreenLocationX = Math.Abs(RelativeScreenLocationX);
                RelativeScreenLocationY = -Math.Abs(RelativeScreenLocationY);
                break;
        }
    }

    private static void SetBinding(
        string path,
        DependencyObject dobj,
        DependencyProperty property,
        object viewModel,
        BindingMode mode = BindingMode.TwoWay,
        IValueConverter? converter = null
    ) {
        var binding = new Binding(path) {
            Source = viewModel,
            Mode = mode
        };
        if (converter != null) binding.Converter = converter;
        BindingOperations.SetBinding(dobj, property, binding);
    }

    #endregion
}
