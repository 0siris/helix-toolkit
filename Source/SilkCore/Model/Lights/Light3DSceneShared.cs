/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;
using System.Runtime.CompilerServices;
namespace HelixToolkit.SharpDX.Core
{
    namespace Model
    {
        using Render;
        using ShaderManager;
        using Shaders;
        using Utilities;

        /// <summary>
        /// Used to hold shared variables for Lights per scene
        /// </summary>
        public sealed class Light3DSceneShared : DisposeObject
        {
            public readonly LightsBufferModel LightModels = new LightsBufferModel();

            private IBufferProxy buffer;
            /// <summary>
            /// 
            /// </summary>
            public Light3DSceneShared(IConstantBufferPool pool)
            {
                buffer = pool.Register(DefaultBufferNames.LightCB, LightsBufferModel.SizeInBytes);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UploadToBuffer(DeviceContextProxy context)
            {
                LightModels.UploadToBuffer(buffer, context);
            }

            protected override void OnDispose(bool disposeManagedResources)
            {
                RemoveAndDispose(ref buffer);
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
