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
}
