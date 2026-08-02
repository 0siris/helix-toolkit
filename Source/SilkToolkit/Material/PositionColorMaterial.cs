using System.Windows;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Render color by mesh vertex position
/// </summary>
public sealed class PositionColorMaterial : Material {
    public PositionColorMaterial() { }

    public PositionColorMaterial(PositionMaterialCore core) : base(core) { }

    protected override MaterialCore OnCreateCore() {
        return PositionMaterialCore.Core;
    }
    protected override Freezable CreateInstanceCore() {
        return new PositionColorMaterial {
            Name = Name
        };
    }
}
