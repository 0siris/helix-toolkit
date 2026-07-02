/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;

#if !WINDOWS_UWP
namespace HelixToolkit.SharpDX.Core
{
    namespace Model.Scene
    {
        /// <summary>
        /// </summary>
        public class ScreenDuplicationNode : SceneNode
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="ScreenDuplicationNode" /> class.
            /// </summary>
            public ScreenDuplicationNode()
            {
                IsHitTestVisible = false;
            }

            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore()
            {
                return new ScreenCloneRenderCore();
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager)
            {
                return effectsManager[DefaultRenderTechniqueNames.ScreenDuplication];
            }

            protected override bool OnHitTest(HitTestContext context, Matrix totalModelMatrix,
                ref List<HitTestResult> hits)
            {
                return false;
            }

            #region Properties

            /// <summary>
            ///     Gets or sets the capture rectangle.
            /// </summary>
            /// <value>
            ///     The capture rectangle.
            /// </value>
            public Rectangle CaptureRectangle
            {
                get => (RenderCore as IScreenClone).CloneRectangle;
                set => (RenderCore as IScreenClone).CloneRectangle = value;
            }

            /// <summary>
            ///     Gets or sets the display index.
            /// </summary>
            /// <value>
            ///     The display index.
            /// </value>
            public int DisplayIndex
            {
                get => (RenderCore as IScreenClone).Output;
                set => (RenderCore as IScreenClone).Output = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [stretch to fill].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [stretch to fill]; otherwise, <c>false</c>.
            /// </value>
            public bool StretchToFill
            {
                get => (RenderCore as IScreenClone).StretchToFill;
                set => (RenderCore as IScreenClone).StretchToFill = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [show mouse cursor].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [show mouse cursor]; otherwise, <c>false</c>.
            /// </value>
            public bool ShowMouseCursor
            {
                get => (RenderCore as IScreenClone).ShowMouseCursor;
                set => (RenderCore as IScreenClone).ShowMouseCursor = value;
            }

            #endregion
        }
    }
}
#endif