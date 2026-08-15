// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThreePointLight3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace HelixToolkit.Wpf.SharpDX.Model.Lights3D;

public class ThreePointLight3D : GroupElement3D, ILight3D {
    public LightType LightType => LightType.ThreePoint;
}
