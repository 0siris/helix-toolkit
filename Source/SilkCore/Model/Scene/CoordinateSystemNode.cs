/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


namespace HelixToolkit.SharpDX.Core
{
    namespace Model.Scene
    {
        /// <summary>
        /// </summary>
        public class CoordinateSystemNode : ScreenSpacedNode
        {
            private static readonly float arrowSize = 5.5f;
            private static readonly float arrowWidth = 0.6f;
            private static readonly float arrowHead = 1.7f;
            private readonly MeshNode arrowMeshModel = new() {EnableViewFrustumCheck = false};

            private readonly BillboardNode axisBillboard = new() {Material = new BillboardMaterialCore()};
            private Color4 axisXColor = Color.Red;

            private Color4 axisYColor = Color.Green;

            private Color4 axisZColor = Color.Blue;

            private Color4 labelColor = Color.Gray;

            private string labelX = "X";

            private string labelY = "Y";

            private string labelZ = "Z";

            /// <summary>
            /// </summary>
            public CoordinateSystemNode()
            {
                IsHitTestVisible = false;
                CameraType = ScreenSpacedCameraType.Perspective;
                arrowMeshModel.Material = new ColorMaterialCore();
                arrowMeshModel.CullMode = CullMode.Back;
                axisBillboard.EnableViewFrustumCheck = false;
                var axisLabel = new BillboardText3D();
                axisLabel.TextInfo.Add(new TextInfo());
                axisLabel.TextInfo.Add(new TextInfo());
                axisLabel.TextInfo.Add(new TextInfo());
                axisBillboard.Geometry = axisLabel;
                AddChildNode(arrowMeshModel);
                AddChildNode(axisBillboard);
                UpdateModel();
            }

            /// <summary>
            ///     Gets or sets the color of the axis x.
            /// </summary>
            /// <value>
            ///     The color of the axis x.
            /// </value>
            public Color4 AxisXColor
            {
                get => axisXColor;
                set
                {
                    if (Set(ref axisXColor, value)) UpdateAxisColor(0, value);
                }
            }

            /// <summary>
            ///     Gets or sets the color of the axis y.
            /// </summary>
            /// <value>
            ///     The color of the axis y.
            /// </value>
            public Color4 AxisYColor
            {
                get => axisYColor;
                set
                {
                    if (Set(ref axisYColor, value)) UpdateAxisColor(1, value);
                }
            }

            /// <summary>
            ///     Gets or sets the color of the axis z.
            /// </summary>
            /// <value>
            ///     The color of the axis z.
            /// </value>
            public Color4 AxisZColor
            {
                get => axisZColor;
                set
                {
                    if (Set(ref axisZColor, value)) UpdateAxisColor(2, value);
                }
            }

            /// <summary>
            ///     Gets or sets the color of the label.
            /// </summary>
            /// <value>
            ///     The color of the label.
            /// </value>
            public Color4 LabelColor
            {
                get => labelColor;
                set
                {
                    if (Set(ref labelColor, value)) UpdateLabelColor(value);
                }
            }

            /// <summary>
            ///     Gets or sets the label x.
            /// </summary>
            /// <value>
            ///     The label x.
            /// </value>
            public string LabelX
            {
                get => labelX;
                set
                {
                    if (Set(ref labelX, value)) UpdateAxisLabel(0, value);
                }
            }

            /// <summary>
            ///     Gets or sets the label y.
            /// </summary>
            /// <value>
            ///     The label y.
            /// </value>
            public string LabelY
            {
                get => labelY;
                set
                {
                    if (Set(ref labelY, value)) UpdateAxisLabel(1, value);
                }
            }

            /// <summary>
            ///     Gets or sets the label z.
            /// </summary>
            /// <value>
            ///     The label z.
            /// </value>
            public string LabelZ
            {
                get => labelZ;
                set
                {
                    if (Set(ref labelZ, value)) UpdateAxisLabel(2, value);
                }
            }

            private void UpdateModel()
            {
                var builder = new MeshBuilder(true, false);

                builder.AddArrow(Vector3.Zero, new Vector3(arrowSize, 0, 0), arrowWidth, arrowHead, 8);
                builder.AddArrow(Vector3.Zero, new Vector3(0, arrowSize, 0), arrowWidth, arrowHead, 8);
                builder.AddArrow(Vector3.Zero, new Vector3(0, 0, arrowSize), arrowWidth, arrowHead, 8);

                var mesh = builder.ToMesh();
                arrowMeshModel.Geometry = mesh;
                UpdateAxisColor(arrowMeshModel.Geometry, 0, AxisXColor, LabelX, LabelColor);
                UpdateAxisColor(arrowMeshModel.Geometry, 1, AxisYColor, LabelY, LabelColor);
                UpdateAxisColor(arrowMeshModel.Geometry, 2, AxisZColor, LabelZ, LabelColor);
            }

            private void UpdateAxisColor(int which, Color4 color)
            {
                var label = string.Empty;
                switch (which)
                {
                    case 0:
                        label = LabelX;
                        break;

                    case 1:
                        label = LabelY;
                        break;

                    case 2:
                        label = LabelZ;
                        break;
                }

                UpdateAxisColor(arrowMeshModel.Geometry, which, color, label, LabelColor);
            }

            private void UpdateAxisLabel(int which, string label)
            {
                Color4 color = Color.Red;
                switch (which)
                {
                    case 0:
                        color = AxisXColor;
                        break;

                    case 1:
                        color = AxisYColor;
                        break;

                    case 2:
                        color = AxisZColor;
                        break;
                }

                UpdateAxisColor(arrowMeshModel.Geometry, which, color, label, LabelColor);
            }

            private void UpdateLabelColor(Color4 color)
            {
                UpdateAxisColor(arrowMeshModel.Geometry, 0, AxisXColor, LabelX, LabelColor);
                UpdateAxisColor(arrowMeshModel.Geometry, 1, AxisYColor, LabelY, LabelColor);
                UpdateAxisColor(arrowMeshModel.Geometry, 2, AxisZColor, LabelZ, LabelColor);
            }

            /// <summary>
            /// </summary>
            /// <param name="mesh"></param>
            /// <param name="which"></param>
            /// <param name="color"></param>
            /// <param name="label"></param>
            /// <param name="labelColor"></param>
            protected void UpdateAxisColor(Geometry3D mesh, int which, Color4 color, string label, Color4 labelColor)
            {
                var labelText = axisBillboard.Geometry as BillboardText3D;
                switch (which)
                {
                    case 0:
                        labelText.TextInfo[which] = new TextInfo(label, new Vector3(arrowSize + 1.5f, 0, 0))
                            {Foreground = labelColor, Scale = 0.5f};
                        break;

                    case 1:
                        labelText.TextInfo[which] = new TextInfo(label, new Vector3(0, arrowSize + 1.5f, 0))
                            {Foreground = labelColor, Scale = 0.5f};
                        break;

                    case 2:
                        labelText.TextInfo[which] = new TextInfo(label, new Vector3(0, 0, arrowSize + 1.5f))
                            {Foreground = labelColor, Scale = 0.5f};
                        break;
                }

                var segment = mesh.Positions.Count / 3;
                var colors = new Color4Collection(mesh.Colors == null
                    ? Enumerable.Repeat<Color4>(Color.Black, mesh.Positions.Count)
                    : mesh.Colors);
                for (var i = segment * which; i < segment * (which + 1); ++i) colors[i] = color;
                mesh.Colors = colors;
            }

            protected override bool CanHitTest(HitTestContext context)
            {
                return false;
            }
        }
    }
}