/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Model.Lights;
/// <summary>
///     Default Light Model
/// </summary>
public sealed class LightsBufferModel : ILightsBufferProxy<LightStruct> {
    public const int SizeInBytes = LightStruct.SizeInBytes * Constants.MaxLights + 4 * 4 * 2;

    /// <summary>
    ///     Gets or sets the environment map mip levels.
    /// </summary>
    /// <value>
    ///     The environment map mip levels.
    /// </value>
    internal int EnvironmentMapMipLevels = 0;

    /// <summary>
    ///     Gets or sets a value indicating whether the scene has environment map.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the scene has environment map; otherwise, <c>false</c>.
    /// </value>
    internal bool HasEnvironmentMap = false;

    public Color4 AmbientLight { get; set; } = new(0, 0, 0, 1);
    public int LightCount { get; private set; }

    public int BufferSize => SizeInBytes;

    public LightStruct[] Lights { get; } = new LightStruct[Constants.MaxLights];

    public void IncrementLightCount() {
        ++LightCount;
    }

    public void ResetLightCount() {
        LightCount = 0;
        AmbientLight = new Color4(0, 0, 0, 1);
    }

    private void Upload(DataBox dataBox) {
        var ptr = UnsafeHelper.Write(dataBox.DataPointer, Lights, 0, Lights.Length);
        ptr = UnsafeHelper.Write(ptr, AmbientLight);
        ptr = UnsafeHelper.Write(ptr, LightCount);
        ptr = UnsafeHelper.Write(ptr, HasEnvironmentMap ? 1 : 0);
        UnsafeHelper.Write(ptr, EnvironmentMapMipLevels);
    }
}
