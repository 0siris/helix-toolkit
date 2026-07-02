// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AmbientLight3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;

public sealed class AmbientLight3D : Light3D
{
    protected override SceneNode OnCreateSceneNode()
    {
        return new AmbientLightNode();
    }
}