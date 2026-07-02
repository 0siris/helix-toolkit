using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;

namespace HelixToolkit.SharpDX.Core
{
    namespace Render
    {
        using Native;
        using Shaders;
        using Utilities;

        /// <summary>
        ///
        /// </summary>
        public sealed partial class DeviceContextProxy : DisposeObject
        {
            public static bool AutoSkipRedundantStateSetting = false;
            private SilkD3DDeviceContext nativeDeviceContext;
            private readonly SilkD3DDevice nativeDevice;
            private RasterizerStateProxy currRasterState = null;
            private DepthStencilStateProxy currDepthStencilState = null;
            private int currStencilRef;
            private BlendStateProxy currBlendState = null;
            private Color4? currBlendFactor = null;
            private uint currSampleMask = uint.MaxValue;
            public readonly bool IsDeferred = false;

            #region Properties

            /// <summary>
            /// Gets or sets the last shader pass.
            /// </summary>
            /// <value>
            /// The last shader pass.
            /// </value>
            public ShaderPass CurrShaderPass
            {
                private set; get;
            }

            /// <summary>
            /// Gets the number of draw calls.
            /// </summary>
            /// <value>
            /// The number of draw calls.
            /// </value>
            public int NumberOfDrawCalls { private set; get; } = 0;
            #endregion Properties

            #region Constructor
            /// <summary>
            /// Initializes a proxy for a native Silk.NET D3D11 context.
            /// </summary>
            /// <param name="context">The native context.</param>
            /// <param name="device">The native device.</param>
            internal DeviceContextProxy(SilkD3DDeviceContext context, SilkD3DDevice device)
            {
                nativeDeviceContext = context;
                nativeDevice = device;
                IsDeferred = context.IsDeferred;
            }
            #endregion Constructor

            internal SilkD3DDeviceContext NativeContext => nativeDeviceContext;

            internal SilkD3DDevice NativeDevice => nativeDevice;

            /// <summary>
            /// Resets this instance.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Reset()
            {
                currRasterState = null;
                currBlendState = null;
                currDepthStencilState = null;
                currBlendFactor = null;
                currSampleMask = uint.MaxValue;
                currStencilRef = 0;
                currInputLayout = null;
                PrimitiveTopology = PrimitiveTopology.Undefined;
                CurrShaderPass = null;
                for (var i = 0; i < ConstantBufferCheck.Length; ++i)
                {
                    ConstantBufferCheck[i] = null;
                }
                for (var i = 0; i < SamplerStateCheck.Length; ++i)
                {
                    SamplerStateCheck[i] = null;
                }
            }

            /// <summary>
            /// Restore all default settings.
            /// </summary>
            /// <remarks>
            ///     This method resets any device context to the default settings.
            /// </remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ClearState()
            {
                nativeDeviceContext.ClearState();
                Reset();
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="disposeManagedResources"></param>
            protected override void OnDispose(bool disposeManagedResources)
            {
                if (nativeDeviceContext != null && !nativeDeviceContext.IsDisposed)
                {
                    nativeDeviceContext.ClearState();
                }
                if (IsDeferred)
                {
                    RemoveAndDispose(ref nativeDeviceContext);
                }
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
