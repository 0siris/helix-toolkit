// <copyright file="ScreenSpaceMeshGeometry3D.cs" company="Helix Toolkit">
//   Copyright (c) 2017 Helix Toolkit contributors
//   Author: Lunci Hua
// </copyright>

using System;
using System.Windows;
using System.Windows.Data;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Converters;
using HelixToolkit.Wpf.SharpDX.Elements2D;
using HelixToolkit.Wpf.SharpDX.Model;
using Point3D = System.Windows.Media.Media3D.Point3D;

namespace HelixToolkit.Wpf.SharpDX.Elements2D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HorizontalAlignment = HorizontalAlignment;
using Thickness = Thickness;
using VerticalAlignment = VerticalAlignment;
using Visibility = Visibility;

/// <summary>
/// </summary>
public enum ScreenSpaceMoveDirection {
    LeftTop,
    LeftBottom,
    RightTop,
    RightBottom
}

public sealed class ScreenSpaceMoveDirArgs : EventArgs {
    public readonly ScreenSpaceMoveDirection Direction;

    public ScreenSpaceMoveDirArgs(ScreenSpaceMoveDirection direction) {
        Direction = direction;
    }
}

/// <summary>
/// </summary>
/// <seealso cref="HelixToolkit.Wpf.SharpDX.Elements2D.Panel2D" />
public abstract class ScreenSpacePositionMoverBase : Panel2D {
    /// <summary>
    ///     The enable mover property
    /// </summary>
    public static readonly DependencyProperty EnableMoverProperty =
        DependencyProperty.Register("EnableMover",
                                    typeof(bool),
                                    typeof(ScreenSpacePositionMover),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                          ((Node2DMoverBase)((Element2D)d).SceneNode)
                                                                 .EnableMover = (bool)e.NewValue;
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
    ///     Occurs when [on move clicked].
    /// </summary>
    public event EventHandler<ScreenSpaceMoveDirArgs>? OnMoveClicked;

    /// <summary>
    ///     Raises the on move click.
    /// </summary>
    /// <param name="direction">The direction.</param>
    protected void RaiseOnMoveClick(ScreenSpaceMoveDirection direction) {
        OnMoveClicked?.Invoke(this, new ScreenSpaceMoveDirArgs(direction));
    }

    public abstract class Node2DMoverBase : PanelNode2D {
        public bool EnableMover { get; set; } = true;

        protected override bool CanRender(RenderContext2D context) => base.CanRender(context) && EnableMover;
    }
}

/// <summary>
///     Use to apply style for mover button from Generic.xaml/>
/// </summary>
/// <seealso cref="HelixToolkit.Wpf.SharpDX.Elements2D.Button2D" />
public sealed class MoverButton2D : Button2D {
    static MoverButton2D() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MoverButton2D),
                                                 new FrameworkPropertyMetadata(typeof(MoverButton2D)));
    }
}

/// <summary>
/// </summary>
/// <seealso cref="HelixToolkit.Wpf.SharpDX.Elements2D.ScreenSpacePositionMoverBase" />
public class ScreenSpacePositionMover : ScreenSpacePositionMoverBase {
    private readonly Button2D[] buttons = new Button2D[4];
    private readonly Button2D MoveLeftBottom;
    private readonly Button2D MoveLeftTop;
    private readonly Button2D MoveRightBottom;
    private readonly Button2D MoveRightTop;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScreenSpacePositionMover" /> class.
    /// </summary>
    public ScreenSpacePositionMover() {
        MoveLeftTop = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            BorderThickness = new Thickness(2, 0, 0, 2)
        };

        MoveRightTop = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            BorderThickness = new Thickness(2, 2, 0, 0)
        };

        MoveLeftBottom = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            BorderThickness = new Thickness(0, 0, 2, 2)
        };

        MoveRightBottom = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            BorderThickness = new Thickness(0, 2, 2, 0)
        };

        buttons[0] = MoveLeftTop;
        buttons[1] = MoveLeftBottom;
        buttons[2] = MoveRightTop;
        buttons[3] = MoveRightBottom;

        Width = 100;
        Height = 100;

        foreach (var b in buttons) {
            b.Visibility = Visibility.Hidden;
            Children.Add(b);
        }

        MoveLeftTop.Clicked2D += (s, e) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.LeftTop); };
        MoveLeftBottom.Clicked2D += (s, e) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.LeftBottom); };
        MoveRightTop.Clicked2D += (s, e) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.RightTop); };
        MoveRightBottom.Clicked2D += (s, e) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.RightBottom); };
    }

    protected override SceneNode2D OnCreateSceneNode() => new Node2DMover { Buttons = buttons };

    public sealed class Node2DMover : Node2DMoverBase {
        public Button2D[]? Buttons { get; set; }

        /// <summary>
        ///     Called when [hit test].
        /// </summary>
        /// <param name="mousePoint">The mouse point.</param>
        /// <param name="hitResult">The hit result.</param>
        /// <returns></returns>
        protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
            hitResult = null;
            if (!EnableMover) return false;
            if (LayoutBoundWithTransform.Contains(mousePoint)) {
                foreach (var b in Buttons!) b.Visibility = System.Windows.Visibility.Visible;
                return base.OnHitTest(ref mousePoint, out hitResult);
            }

            foreach (var b in Buttons!) b.Visibility = System.Windows.Visibility.Hidden;
            return false;
        }
    }
}

