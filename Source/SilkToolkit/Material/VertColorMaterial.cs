
using System.ComponentModel;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.Wpf.SharpDX {
    using Model;
    /// <summary>
    /// Render color by mesh vertex color
    /// </summary>
    public sealed class VertColorMaterial : Material
    {
        protected override MaterialCore OnCreateCore()
        {
            return ColorMaterialCore.Core;
        }

        public VertColorMaterial()
        {
        }

        public VertColorMaterial(ColorMaterialCore core) : base(core) { }

        protected override Freezable CreateInstanceCore()
        {
            return new VertColorMaterial()
            {
                Name = Name
            };
        }

    }
}
