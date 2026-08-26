/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public interface IDeviceContextPool : IDisposable {
    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <returns></returns>
    DeviceContextProxy.DeviceContextProxy Get();

    /// <summary>
    ///     Puts the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    void Put(DeviceContextProxy.DeviceContextProxy context);

    /// <summary>
    ///     Resets the draw calls.
    /// </summary>
    int ResetDrawCalls();
}
