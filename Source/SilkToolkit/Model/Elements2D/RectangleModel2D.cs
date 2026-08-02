using HelixToolkit.SharpDX.Core.Model.Scene2D;

namespace HelixToolkit.Wpf.SharpDX {
    namespace Elements2D {
        public class RectangleModel2D : ShapeModel2D {
            protected override SceneNode2D OnCreateSceneNode() {
                return new RectangleNode2D();
            }
        }
    }
}
