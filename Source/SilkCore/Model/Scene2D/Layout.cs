/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public enum HorizontalAlignment {
    Left,
    Right,
    Center,
    Stretch
}

public enum VerticalAlignment {
    Top,
    Bottom,
    Center,
    Stretch
}

public enum Visibility {
    Visible,
    Collapsed,
    Hidden
}

public enum Orientation {
    Horizontal,
    Vertical
}

public struct Thickness : IEquatable<Thickness> {
    public float Left;
    public float Right;
    public float Top;
    public float Bottom;

    public Thickness(float size) {
        Left = size;
        Right = size;
        Top = size;
        Bottom = size;
    }

    public Thickness(float left, float right, float top, float bottom) {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    public bool Equals(Thickness other) => Left == other.Left && Right == other.Right && Top == other.Top && Bottom == other.Bottom;

    public static implicit operator Vector4(Thickness t) => new(t.Left, t.Top, t.Right, t.Bottom);

    public override bool Equals(object? obj) => obj is Thickness && Equals((Thickness)obj);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Left, Right, Top, Bottom);

    /// <summary>Compares thicknesses for equality.</summary>
    /// <param name="left">The first thickness.</param>
    /// <param name="right">The second thickness.</param>
    /// <returns>Whether all four edges are equal.</returns>
    public static bool operator ==(Thickness left, Thickness right) => left.Equals(right);

    /// <summary>Compares thicknesses for inequality.</summary>
    /// <param name="left">The first thickness.</param>
    /// <param name="right">The second thickness.</param>
    /// <returns>Whether any edge differs.</returns>
    public static bool operator !=(Thickness left, Thickness right) => !left.Equals(right);
}
