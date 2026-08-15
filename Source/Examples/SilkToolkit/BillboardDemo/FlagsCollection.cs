using HelixToolkit.SharpDX.Core.Model.Geometry;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace BillboardDemo;

public static class FlagsCollection {
    public static Flag[] Flags;
    private const float OffSetX = 1 / 8f;
    private const float OffSetY = 1 / 8f;
    private const float BorderThickness = 0.02f;

    static FlagsCollection() {
        Flags = [
            new Flag("China", new Vector3(1.26659f, -3.595175f, 2.323998f), GetCoordRowColumn(4, 7)),
            new Flag("USA", new Vector3(0.6496152f, 3.337777f, 2.847751f), GetCoordRowColumn(5, 2)),
            new Flag("Japan", new Vector3(2.708371f, -2.284147f, 2.697974f), GetCoordRowColumn(2, 0)),
            new Flag("Canada", new Vector3(0.6104228f, 2.78573f, 3.415769f), GetCoordRowColumn(0, 4)),
            new Flag("Brazil", new Vector3(), GetCoordRowColumn(0, 5)),
            new Flag("South Korea", new Vector3(), GetCoordRowColumn(2, 1)),
            new Flag("Australia", new Vector3(), GetCoordRowColumn(3, 0)),
            new Flag("United Kingdom", new Vector3(), GetCoordRowColumn(7, 0)),
            new Flag("Russia", new Vector3(), GetCoordRowColumn(4, 6)),
            new Flag("Turkey", new Vector3(), GetCoordRowColumn(4, 3)),
        ];
    }

    public static Vector4 GetCoordRowColumn(int row, int column) => new(column * OffSetX + BorderThickness,
        row * OffSetY + BorderThickness,
        (column + 1) * OffSetX - BorderThickness,
        (row + 1) * OffSetY - BorderThickness);
}

public class Flag : ImageInfo {
    public string Name { get; }

    public Flag(string name, Vector3 pos, Vector4 coord) {
        Name = name;
        Position = pos;
        UvTopLeft = new Vector2(coord.X, coord.Y);
        UvBottomRight = new Vector2(coord.Z, coord.W);
        Width = 0.5f;
        Height = 0.4f;
    }
}
