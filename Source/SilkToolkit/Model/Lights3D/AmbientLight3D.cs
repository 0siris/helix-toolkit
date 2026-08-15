// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AmbientLight3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene.Lights;

namespace HelixToolkit.Wpf.SharpDX.Model.Lights3D;

public sealed class AmbientLight3D : Light3D {
    protected override SceneNode OnCreateSceneNode() => new AmbientLightNode();
}
