// <copyright file="ScreenSpaceMeshGeometry3D.cs" company="Helix Toolkit">
//   Copyright (c) 2017 Helix Toolkit contributors
//   Author: Lunci Hua
// </copyright>

using System;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX.Model.Elements2D;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Thickness = System.Windows.Thickness;
using VerticalAlignment = System.Windows.VerticalAlignment;
using Visibility = System.Windows.Visibility;

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
/// <seealso cref="Panel2D" />
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
                                                          ((Node2DMoverBase)((Elements2D.Abstract.Element2D)d).SceneNode)
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
/// <seealso cref="Button2D" />
public sealed class MoverButton2D : Button2D {
    static MoverButton2D() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MoverButton2D),
                                                 new FrameworkPropertyMetadata(typeof(MoverButton2D)));
    }
}

/// <summary>
/// </summary>
/// <seealso cref="ScreenSpacePositionMoverBase" />
public class ScreenSpacePositionMover : ScreenSpacePositionMoverBase {
    private readonly Button2D[] buttons = new Button2D[4];
    private readonly Button2D moveLeftBottom;
    private readonly Button2D moveLeftTop;
    private readonly Button2D moveRightBottom;
    private readonly Button2D moveRightTop;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ScreenSpacePositionMover" /> class.
    /// </summary>
    public ScreenSpacePositionMover() {
        moveLeftTop = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            BorderThickness = new Thickness(2, 0, 0, 2)
        };

        moveRightTop = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            BorderThickness = new Thickness(2, 2, 0, 0)
        };

        moveLeftBottom = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            BorderThickness = new Thickness(0, 0, 2, 2)
        };

        moveRightBottom = new MoverButton2D {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            BorderThickness = new Thickness(0, 2, 2, 0)
        };

        buttons[0] = moveLeftTop;
        buttons[1] = moveLeftBottom;
        buttons[2] = moveRightTop;
        buttons[3] = moveRightBottom;

        Width = 100;
        Height = 100;

        foreach (var b in buttons) {
            b.Visibility = Visibility.Hidden;
            Children.Add(b);
        }

        moveLeftTop.Clicked2D += (_, _) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.LeftTop); };
        moveLeftBottom.Clicked2D += (_, _) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.LeftBottom); };
        moveRightTop.Clicked2D += (_, _) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.RightTop); };
        moveRightBottom.Clicked2D += (_, _) => { RaiseOnMoveClick(ScreenSpaceMoveDirection.RightBottom); };
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

