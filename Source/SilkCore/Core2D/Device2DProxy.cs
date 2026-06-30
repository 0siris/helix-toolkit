/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Core2D
    {
        using Native;
        using Utilities;

        /// <summary>
        /// 
        /// </summary>
        public sealed class D2DTargetProxy : DisposeObject
        {
            private BitmapProxy d2DTarget;
            /// <summary>
            /// Gets the d2d target. Which is bind to the 3D back buffer/texture
            /// </summary>
            /// <value>
            /// The d2d target.
            /// </value>
            public BitmapProxy D2DTarget
            {
                get
                {
                    return d2DTarget;
                }
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="swapChain"></param>
            /// <param name="deviceContext"></param>
            public void Initialize(object swapChain, D2DDeviceContext deviceContext)
            {
                RemoveAndDispose(ref d2DTarget);
                d2DTarget = BitmapProxy.Create("SwapChainTarget", deviceContext, swapChain);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="texture"></param>
            /// <param name="deviceContext"></param>
            public void Initialize(Texture2D texture, D2DDeviceContext deviceContext)
            {
                RemoveAndDispose(ref d2DTarget);
                d2DTarget = BitmapProxy.Create("TextureTarget", deviceContext, texture);
            }

            protected override void OnDispose(bool disposeManagedResources)
            {
                RemoveAndDispose(ref d2DTarget);
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
