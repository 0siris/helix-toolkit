using Media = System.Windows.Media;

namespace HelixToolkit.Wpf.SharpDX
{
    namespace Elements2D
    {
        public interface ITransformable2D
        {
            Media.Transform Transform { get; set; }
        }

        public interface IBackground
        {
            Media.Brush Background { get; set; }
        }

        public interface ITextBlock : IBackground
        {
            Media.Brush Foreground { get; set; }
        }
    }
}