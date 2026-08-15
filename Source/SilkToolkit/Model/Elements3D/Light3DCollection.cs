// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Light3DCollection.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace HelixToolkit.Wpf.SharpDX.Model.Elements3D;

public class Light3DCollection : GroupElement3D, ILight3D {
    public LightType LightType => LightType.None;

    public override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) => false;
}
