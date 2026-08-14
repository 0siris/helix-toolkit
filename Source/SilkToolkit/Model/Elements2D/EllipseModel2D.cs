using HelixToolkit.SharpDX.Core.Model.Scene2D;

namespace HelixToolkit.Wpf.SharpDX.Elements2D;
public class EllipseModel2D : ShapeModel2D {
    protected override SceneNode2D OnCreateSceneNode() => new EllipseNode2D();
}
