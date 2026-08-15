/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Model.Lights;
/// <summary>
///     Used to hold shared variables for Lights per scene
/// </summary>
public sealed class Light3DSceneShared : DisposeObject {
    public readonly LightsBufferModel LightModels = new();

    [System.Diagnostics.CodeAnalysis.AllowNull]
    private IBufferProxy buffer;

    /// <summary>
    /// </summary>
    public Light3DSceneShared(IConstantBufferPool pool) {
        buffer = pool.Register(DefaultBufferNames.LightCb, LightsBufferModel.SizeInBytes);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UploadToBuffer(DeviceContextProxy context) {
        LightModels.UploadToBuffer(buffer, context);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref buffer);
        base.OnDispose(disposeManagedResources);
    }
}
