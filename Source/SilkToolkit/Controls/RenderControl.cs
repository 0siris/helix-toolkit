// Copyright (c) 2010-2014 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Media;
using Color = System.Drawing.Color;
using FontFamily = System.Drawing.FontFamily;

namespace HelixToolkit.Wpf.SharpDX {
    namespace Controls {
        /// <summary>
        ///     A Renderable UserControl.
        /// </summary>
        public class RenderControl : UserControl {
            private readonly Visual hostVisual;

            private Font fontForDesignMode;

            /// <summary>
            ///     Initializes a new instance of the <see cref="RenderControl" /> class.
            /// </summary>
            public RenderControl(Visual? hostVisual = null) {
                this.hostVisual = hostVisual;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.Opaque | ControlStyles.UserPaint, true);
                UpdateStyles();
            }

            /// <summary>
            ///     Paints the background of the control.
            /// </summary>
            /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data.</param>
            protected override void OnPaintBackground(PaintEventArgs e) {
                if (DesignMode)
                    base.OnPaintBackground(e);
            }

            /// <summary>
            ///     Raises the <see cref="E:System.Windows.Forms.Control.Paint" /> event.
            /// </summary>
            /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the event data.</param>
            protected override void OnPaint(PaintEventArgs e) {
                base.OnPaint(e);
                if (DesignMode) {
                    fontForDesignMode ??= new Font(new FontFamily("Calibri"),
                                                     24,
                                                     System.Drawing.FontStyle.Regular);

                    e.Graphics.Clear(Color.WhiteSmoke);
                    var text = "SharpDX RenderControl";
                    var sizeText = e.Graphics.MeasureString(text, fontForDesignMode);

                    e.Graphics.DrawString(text,
                                          fontForDesignMode,
                                          new SolidBrush(Color.Black),
                                          (Width - sizeText.Width) / 2,
                                          (Height - sizeText.Height) / 2);
                }
            }

            protected override void OnHandleCreated(EventArgs e) {
                base.OnHandleCreated(e);
                if (hostVisual != null) VirtualTouchDevice.RegisterTouchWindow(Handle, 0);
            }

            protected override void WndProc(ref Message m) {
                if (VirtualTouchDevice.WndProc(hostVisual, ref m))
                    DefWndProc(ref m);
                else
                    base.WndProc(ref m);
            }
        }
    }
}
