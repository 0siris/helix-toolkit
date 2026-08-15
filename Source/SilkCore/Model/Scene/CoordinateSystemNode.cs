/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
/// <summary>
/// </summary>
public class CoordinateSystemNode : ScreenSpacedNode {
    private static readonly float ArrowSize = 5.5f;
    private static readonly float ArrowWidth = 0.6f;
    private static readonly float ArrowHead = 1.7f;
    private readonly MeshNode arrowMeshModel = new() { EnableViewFrustumCheck = false };

    private readonly BillboardNode axisBillboard = new() { Material = new BillboardMaterialCore() };

    /// <summary>
    /// </summary>
    public CoordinateSystemNode() {
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
    public Color4 AxisXColor {
        get;
        set {
            if (Set(ref field, value)) UpdateAxisColor(0, value);
        }
    } = Color.Red;

    /// <summary>
    ///     Gets or sets the color of the axis y.
    /// </summary>
    /// <value>
    ///     The color of the axis y.
    /// </value>
    public Color4 AxisYColor {
        get;
        set {
            if (Set(ref field, value)) UpdateAxisColor(1, value);
        }
    } = Color.Green;

    /// <summary>
    ///     Gets or sets the color of the axis z.
    /// </summary>
    /// <value>
    ///     The color of the axis z.
    /// </value>
    public Color4 AxisZColor {
        get;
        set {
            if (Set(ref field, value)) UpdateAxisColor(2, value);
        }
    } = Color.Blue;

    /// <summary>
    ///     Gets or sets the color of the label.
    /// </summary>
    /// <value>
    ///     The color of the label.
    /// </value>
    public Color4 LabelColor {
        get;
        set {
            if (Set(ref field, value)) UpdateLabelColor();
        }
    } = Color.Gray;

    /// <summary>
    ///     Gets or sets the label x.
    /// </summary>
    /// <value>
    ///     The label x.
    /// </value>
    public string LabelX {
        get;
        set {
            if (Set(ref field, value)) UpdateAxisLabel(0, value);
        }
    } = "X";

    /// <summary>
    ///     Gets or sets the label y.
    /// </summary>
    /// <value>
    ///     The label y.
    /// </value>
    public string LabelY {
        get;
        set {
            if (Set(ref field, value)) UpdateAxisLabel(1, value);
        }
    } = "Y";

    /// <summary>
    ///     Gets or sets the label z.
    /// </summary>
    /// <value>
    ///     The label z.
    /// </value>
    public string LabelZ {
        get;
        set {
            if (Set(ref field, value)) UpdateAxisLabel(2, value);
        }
    } = "Z";

    private void UpdateModel() {
        var builder = new MeshBuilder(true, false);

        builder.AddArrow(Vector3.Zero, new Vector3(ArrowSize, 0, 0), ArrowWidth, ArrowHead, 8);
        builder.AddArrow(Vector3.Zero, new Vector3(0, ArrowSize, 0), ArrowWidth, ArrowHead, 8);
        builder.AddArrow(Vector3.Zero, new Vector3(0, 0, ArrowSize), ArrowWidth, ArrowHead, 8);

        var mesh = builder.ToMesh();
        arrowMeshModel.Geometry = mesh;
        UpdateAxisColor(mesh, 0, AxisXColor, LabelX, LabelColor);
        UpdateAxisColor(mesh, 1, AxisYColor, LabelY, LabelColor);
        UpdateAxisColor(mesh, 2, AxisZColor, LabelZ, LabelColor);
    }

    private void UpdateAxisColor(int which, Color4 color) {
        var label = string.Empty;
        switch (which) {
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

        if (arrowMeshModel.Geometry is { } mesh)
            UpdateAxisColor(mesh, which, color, label, LabelColor);
    }

    private void UpdateAxisLabel(int which, string label) {
        Color4 color = Color.Red;
        switch (which) {
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

        if (arrowMeshModel.Geometry is { } mesh)
            UpdateAxisColor(mesh, which, color, label, LabelColor);
    }

    private void UpdateLabelColor() {
        if (arrowMeshModel.Geometry is not { } mesh) return;
        UpdateAxisColor(mesh, 0, AxisXColor, LabelX, LabelColor);
        UpdateAxisColor(mesh, 1, AxisYColor, LabelY, LabelColor);
        UpdateAxisColor(mesh, 2, AxisZColor, LabelZ, LabelColor);
    }

    /// <summary>
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="which"></param>
    /// <param name="color"></param>
    /// <param name="label"></param>
    /// <param name="labelColor"></param>
    protected void UpdateAxisColor(Geometry3D mesh, int which, Color4 color, string label, Color4 labelColor) {
        if (axisBillboard.Geometry is not BillboardText3D labelText || mesh.Positions is not { } positions) return;
        switch (which) {
            case 0:
                labelText.TextInfo[which] = new TextInfo(label, new Vector3(ArrowSize + 1.5f, 0, 0)) { Foreground = labelColor, Scale = 0.5f };
                break;

            case 1:
                labelText.TextInfo[which] = new TextInfo(label, new Vector3(0, ArrowSize + 1.5f, 0)) { Foreground = labelColor, Scale = 0.5f };
                break;

            case 2:
                labelText.TextInfo[which] = new TextInfo(label, new Vector3(0, 0, ArrowSize + 1.5f)) { Foreground = labelColor, Scale = 0.5f };
                break;
        }

        var segment = positions.Count / 3;
        var colors = new Color4Collection(mesh.Colors is { } meshColors
                                              ? meshColors
                                              : Enumerable.Repeat<Color4>(Color.Black, positions.Count));
        for (var i = segment * which; i < segment * (which + 1); ++i) colors[i] = color;
        mesh.Colors = colors;
    }

    protected override bool CanHitTest(HitTestContext? context) => false;
}
