/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Extensions;

public static class DataStreamExtension {
    public static int ReadInt(this DataStream ds) => ds.Read<int>();

    public static float ReadFloat(this DataStream ds) => ds.Read<float>();

    public static Vector4 ReadVector4(this DataStream ds) => ds.Read<Vector4>();

    public static Matrix ReadMatrix(this DataStream ds) => ds.Read<Matrix>();
}
