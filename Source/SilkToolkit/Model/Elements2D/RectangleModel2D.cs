using HelixToolkit.SharpDX.Core.Model.Scene2D;

namespace HelixToolkit.Wpf.SharpDX.Elements2D;
public class RectangleModel2D : ShapeModel2D {
    protected override SceneNode2D OnCreateSceneNode() => new RectangleNode2D();
}
