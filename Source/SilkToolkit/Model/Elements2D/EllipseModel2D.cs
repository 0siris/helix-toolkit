using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements2D.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements2D;
public class EllipseModel2D : ShapeModel2D {
    protected override SceneNode2D OnCreateSceneNode() => new EllipseNode2D();
}
