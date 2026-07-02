using System;
using System.Collections.Generic;
using System.Text;

namespace HelixToolkit.SharpDX.Core
{
    namespace Core2D
    {
        /// <summary>
        /// 
        /// </summary>
        public sealed class EmptyRenderCore2D : RenderCore2DBase
        {
            /// <summary>
            /// Called when [render].
            /// </summary>
            /// <param name="matrices">The matrices.</param>
            protected override void OnRender(RenderContext2D matrices)
            {

            }
        }
    }
}
