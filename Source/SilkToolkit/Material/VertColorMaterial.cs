using System.Windows;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Render color by mesh vertex color
/// </summary>
public sealed class VertColorMaterial : Material {
    public VertColorMaterial() { }

    public VertColorMaterial(ColorMaterialCore core) : base(core) { }

    protected override MaterialCore OnCreateCore() {
        return ColorMaterialCore.Core;
    }

    protected override Freezable CreateInstanceCore() {
        return new VertColorMaterial {
            Name = Name
        };
    }
}
