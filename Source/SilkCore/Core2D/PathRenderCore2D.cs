/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGBOUNDS

using System.Diagnostics.CodeAnalysis;

namespace HelixToolkit.SharpDX.Core
{
    namespace Core2D
    {
        /// <summary>
        /// </summary>
        public class PathRenderCore2D : ShapeRenderCore2DBase
        {
            private List<Figure> figures = new();

            private D2DFillMode fillMode = D2DFillMode.Alternate;

            /// <summary>
            ///     The geometry
            /// </summary>
            protected PathGeometry geometry;

            /// <summary>
            ///     The is geometry changed
            /// </summary>
            protected bool isGeometryChanged = true;

            /// <summary>
            ///     Gets or sets the figures.
            /// </summary>
            /// <value>
            ///     The figures.
            /// </value>
            public List<Figure> Figures
            {
                get => figures;
                set
                {
                    if (SetAffectsRender(ref figures, value)) isGeometryChanged = true;
                }
            }

            /// <summary>
            ///     Gets or sets the fill mode.
            /// </summary>
            /// <value>
            ///     The fill mode.
            /// </value>
            public D2DFillMode FillMode
            {
                get => fillMode;
                set
                {
                    if (SetAffectsRender(ref fillMode, value)) isGeometryChanged = true;
                }
            }

            /// <summary>
            ///     Called when [attach].
            /// </summary>
            /// <param name="host">The host.</param>
            /// <returns></returns>
            protected override bool OnAttach(IRenderHost host)
            {
                isGeometryChanged = true;
                return base.OnAttach(host);
            }

            /// <summary>
            ///     Called when [render].
            /// </summary>
            /// <param name="context">The context.</param>
            [SuppressMessage("Microsoft.Usage", "CA2202: Do not dispose objects multiple times",
                Justification = "False positive.")]
            protected override void OnRender(RenderContext2D context)
            {
                if (isGeometryChanged)
                {
                    RemoveAndDispose(ref geometry);
                    if (Figures == null || Figures.Count == 0) return;
                    geometry = new PathGeometry(context.DeviceResources.Factory2D);
                    using (var sink = geometry.Open())
                    {
                        sink.SetFillMode(FillMode);
                        foreach (var figure in Figures) figure.Create(sink);
                        sink.Close();
                    }

                    isGeometryChanged = false;
                }

                if (StrokeBrush != null && StrokeWidth > 0 && StrokeStyle != null)
                    context.DeviceContext.DrawGeometry(geometry, StrokeBrush, StrokeWidth, StrokeStyle);
                if (FillBrush != null) context.DeviceContext.FillGeometry(geometry, FillBrush);
            }

            protected override void OnDetach()
            {
                RemoveAndDispose(ref geometry);
                base.OnDetach();
            }
        }
    }
}