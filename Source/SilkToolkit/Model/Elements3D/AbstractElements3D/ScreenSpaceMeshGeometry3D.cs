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

namespace HelixToolkit.Wpf.SharpDX {
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
                                     ((d as Element3DCore).SceneNode as ScreenSpacedNode).RelativeScreenLocationX =
                                         (float) (double) e.NewValue;
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
                                     ((d as Element3DCore).SceneNode as ScreenSpacedNode).RelativeScreenLocationY =
                                         (float) (double) e.NewValue;
                                 }));

        /// <summary>
        ///     <see cref="SizeScale" />
        /// </summary>
        public static readonly DependencyProperty SizeScaleProperty = DependencyProperty.Register("SizeScale",
            typeof(double),
            typeof(ScreenSpacedElement3D),
            new PropertyMetadata(1.0,
                                 (d, e) => {
                                     ((d as Element3DCore).SceneNode as ScreenSpacedNode).SizeScale =
                                         (float) (double) e.NewValue;
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
                                                                 ((d as Element3DCore).SceneNode as ScreenSpacedNode)
                                                                     .Mode = (ScreenSpacedMode) e.NewValue;
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
                                                                 ((d as Element3DCore).SceneNode as ScreenSpacedNode)
                                                                     .AbsolutePosition3D =
                                                                     ((Point3D) e.NewValue).ToVector3();
                                                             }));

        /// <summary>
        ///     Gets or sets a value indicating whether [enable mover].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [enable mover]; otherwise, <c>false</c>.
        /// </value>
        public bool EnableMover {
            get => (bool) GetValue(EnableMoverProperty);
            set => SetValue(EnableMoverProperty, value);
        }

        /// <summary>
        ///     Relative Location X on screen. Range from -1~1
        /// </summary>
        public double RelativeScreenLocationX {
            get => (double) GetValue(RelativeScreenLocationXProperty);
            set => SetValue(RelativeScreenLocationXProperty, value);
        }

        /// <summary>
        ///     Relative Location Y on screen. Range from -1~1
        /// </summary>
        public double RelativeScreenLocationY {
            get => (double) GetValue(RelativeScreenLocationYProperty);
            set => SetValue(RelativeScreenLocationYProperty, value);
        }

        /// <summary>
        ///     Size scaling
        /// </summary>
        public double SizeScale {
            get => (double) GetValue(SizeScaleProperty);
            set => SetValue(SizeScaleProperty, value);
        }

        /// <summary>
        ///     Gets or sets the mode.
        /// </summary>
        /// <value>
        ///     The mode.
        /// </value>
        public ScreenSpacedMode Mode {
            get => (ScreenSpacedMode) GetValue(ModeProperty);
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
            get => (Point3D) GetValue(AbsolutePosition3DProperty);
            set => SetValue(AbsolutePosition3DProperty, value);
        }

        protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
            if (node is ScreenSpacedNode n) {
                n.RelativeScreenLocationX = (float) RelativeScreenLocationX;
                n.RelativeScreenLocationY = (float) RelativeScreenLocationY;
                n.SizeScale = (float) SizeScale;
                n.AbsolutePosition3D = AbsolutePosition3D.ToVector3();
                n.Mode = Mode;
            }

            base.AssignDefaultValuesToSceneNode(node);
            InitializeMover();
        }

    #region 2D stuffs

        public RelativePositionCanvas2D MoverCanvas { get; }
            = new() {HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch};

        private ScreenSpacePositionMoverBase mover;

        private bool isMoverInitialized;

        private void InitializeMover() {
            if (isMoverInitialized) return;
            mover = OnCreateMover();
            MoverCanvas.Children.Add(mover);
            SetBinding(nameof(RelativeScreenLocationX), mover, RelativePositionCanvas2D.RelativeXProperty, this);
            SetBinding(nameof(RelativeScreenLocationY), mover, RelativePositionCanvas2D.RelativeYProperty, this);
            SetBinding(nameof(IsRendering),
                       mover,
                       Element2D.VisibilityProperty,
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

        protected virtual ScreenSpacePositionMoverBase OnCreateMover() {
            return new ScreenSpacePositionMover();
        }

        private void Mover_OnMoveClicked(object sender, ScreenSpaceMoveDirArgs e) {
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
            IValueConverter converter = null
        ) {
            var binding = new Binding(path);
            binding.Source = viewModel;
            binding.Mode = mode;
            if (converter != null) binding.Converter = converter;
            BindingOperations.SetBinding(dobj, property, binding);
        }

    #endregion
    }
}


namespace HelixToolkit.Wpf.SharpDX {
    namespace Elements2D {
        using HelixToolkit.SharpDX.Core;
        using HelixToolkit.SharpDX.Core.Model.Scene2D;
        using HorizontalAlignment = HorizontalAlignment;
        using VerticalAlignment = VerticalAlignment;
        using Thickness = Thickness;
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
                                                                     ((d as Element2D).SceneNode as Node2DMoverBase)
                                                                         .EnableMover = (bool) e.NewValue;
                                                                 }));

            /// <summary>
            ///     Gets or sets a value indicating whether [enable mover].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [enable mover]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableMover {
                get => (bool) GetValue(EnableMoverProperty);
                set => SetValue(EnableMoverProperty, value);
            }

            /// <summary>
            ///     Occurs when [on move clicked].
            /// </summary>
            public event EventHandler<ScreenSpaceMoveDirArgs> OnMoveClicked;

            /// <summary>
            ///     Raises the on move click.
            /// </summary>
            /// <param name="direction">The direction.</param>
            protected void RaiseOnMoveClick(ScreenSpaceMoveDirection direction) {
                OnMoveClicked?.Invoke(this, new ScreenSpaceMoveDirArgs(direction));
            }

            public abstract class Node2DMoverBase : PanelNode2D {
                public bool EnableMover { get; set; } = true;

                protected override bool CanRender(RenderContext2D context) {
                    return base.CanRender(context) && EnableMover;
                }
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

            protected override SceneNode2D OnCreateSceneNode() {
                return new Node2DMover {Buttons = buttons};
            }

            public sealed class Node2DMover : Node2DMoverBase {
                public Button2D[] Buttons { get; set; }

                /// <summary>
                ///     Called when [hit test].
                /// </summary>
                /// <param name="mousePoint">The mouse point.</param>
                /// <param name="hitResult">The hit result.</param>
                /// <returns></returns>
                protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult hitResult) {
                    hitResult = null;
                    if (!EnableMover) return false;
                    if (LayoutBoundWithTransform.Contains(mousePoint)) {
                        foreach (var b in Buttons) b.Visibility = System.Windows.Visibility.Visible;
                        return base.OnHitTest(ref mousePoint, out hitResult);
                    }

                    foreach (var b in Buttons) b.Visibility = System.Windows.Visibility.Hidden;
                    return false;
                }
            }
        }
    }
}
