using HelixToolkit.SharpDX.Core;
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThreePointLight3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.Wpf.SharpDX;

public class ThreePointLight3D : GroupElement3D, ILight3D {
    public LightType LightType => LightType.ThreePoint;
}
