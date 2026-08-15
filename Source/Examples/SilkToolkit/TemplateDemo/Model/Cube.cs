// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Cube.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;

namespace TemplateDemo;

using Vector3 = Silk.NET.Maths.Vector3D<float>;

public class Cube : Shape {
    private static Geometry3D _geometry;

    static Cube() {
        var b1 = new MeshBuilder();
        b1.AddBox(new Vector3(0, 0, 0), 1, 1, 1);
        _geometry = b1.ToMeshGeometry3D();
    }

    protected override Geometry3D GetGeometry() => _geometry;
}
