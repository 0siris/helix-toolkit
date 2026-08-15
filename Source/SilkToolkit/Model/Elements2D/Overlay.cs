using HelixToolkit.SharpDX.Core.Model.Scene2D;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements2D;
internal sealed class Overlay : Panel2D {
    protected override SceneNode2D OnCreateSceneNode() => new OverlayNode2D();
}
