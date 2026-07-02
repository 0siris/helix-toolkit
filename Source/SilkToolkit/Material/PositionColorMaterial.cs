
using System.ComponentModel;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model;
namespace HelixToolkit.Wpf.SharpDX

{
    using Model;
    /// <summary>
    /// Render color by mesh vertex position
    /// </summary>
    public sealed class PositionColorMaterial : Material
    {
        protected override MaterialCore OnCreateCore()
        {
            return PositionMaterialCore.Core;
        }

        public PositionColorMaterial()
        {
        }

        public PositionColorMaterial(PositionMaterialCore core) : base(core) { }
#if !NETFX_CORE && !WINUI
        protected override Freezable CreateInstanceCore()
        {
            return new PositionColorMaterial()
            {
                Name = Name
            };
        }
#endif
    }
}