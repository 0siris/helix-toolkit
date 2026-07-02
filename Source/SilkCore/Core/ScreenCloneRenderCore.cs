/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;

#if !WINDOWS_UWP
namespace HelixToolkit.SharpDX.Core
{
    using Render;

    namespace Core
    {
        /// <summary>
        /// Screen duplication render-core contract.
        /// </summary>
        public interface IScreenClone
        {
            /// <summary>
            /// Gets or sets the output.
            /// </summary>
            int Output
            {
                set; get;
            }

            /// <summary>
            /// Gets or sets the clone rectangle.
            /// </summary>
            Rectangle CloneRectangle
            {
                set; get;
            }

            /// <summary>
            /// Gets or sets a value indicating whether cloned rectangle is stretched during rendering, default is false.
            /// </summary>
            bool StretchToFill
            {
                set; get;
            }

            /// <summary>
            /// Gets or sets a value indicating whether [show mouse cursor].
            /// </summary>
            bool ShowMouseCursor
            {
                set; get;
            }
        }

        /// <summary>
        /// Native migration placeholder for the Desktop Duplication render core.
        /// </summary>
        /// <remarks>
        /// The previous implementation depended on SharpDX DXGI output duplication types. The public render-core contract
        /// is kept so scene nodes continue to compile; real Silk.NET DXGI duplication is a separate interop edge.
        /// </remarks>
        public class ScreenCloneRenderCore : RenderCore, IScreenClone
        {
            private int output;
            private Rectangle cloneRectangle = new Rectangle();
            private bool stretchToFill;

            public ScreenCloneRenderCore()
                : base(RenderType.Opaque)
            {
            }

            /// <summary>
            /// Gets or sets the output.
            /// </summary>
            public int Output
            {
                set
                {
                    Set(ref output, value);
                }
                get
                {
                    return output;
                }
            }

            /// <summary>
            /// Gets or sets the clone rectangle.
            /// </summary>
            public Rectangle CloneRectangle
            {
                set
                {
                    Set(ref cloneRectangle, value);
                }
                get
                {
                    return cloneRectangle;
                }
            }

            /// <summary>
            /// Gets or sets a value indicating cloned rectangle is stretched during rendering, default is false.
            /// </summary>
            public bool StretchToFill
            {
                set
                {
                    Set(ref stretchToFill, value);
                }
                get
                {
                    return stretchToFill;
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether [show mouse cursor].
            /// </summary>
            public bool ShowMouseCursor
            {
                set; get;
            } = true;

            protected override bool OnUpdateCanRenderFlag()
            {
                return false;
            }

            protected override bool OnAttach(IRenderTechnique technique)
            {
                return true;
            }

            protected override void OnDetach()
            {
            }

            public override void Render(RenderContext context, DeviceContextProxy deviceContext)
            {
            }
        }
    }
}
#endif
