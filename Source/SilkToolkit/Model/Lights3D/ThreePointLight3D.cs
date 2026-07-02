// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThreePointLight3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

using HelixToolkit.SharpDX.Core;
namespace HelixToolkit.Wpf.SharpDX
{

    public class ThreePointLight3D : GroupElement3D, ILight3D
    {
        public ThreePointLight3D()
        {
        }

        public LightType LightType
        {
            get
            {
                return LightType.ThreePoint;
            }
        }
    }
}