/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core
{
    namespace Core
    {
        /// <summary>
        /// </summary>
        public class MeshOutlineRenderCore : MeshRenderCore, IMeshOutlineParams
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="MeshOutlineRenderCore" /> class.
            /// </summary>
            public MeshOutlineRenderCore()
            {
                OutlineFadingFactor = 1.5f;
            }

            #region Variables

            /// <summary>
            /// </summary>
            protected ShaderPass OutlineShaderPass { get; private set; }

            #endregion

            /// <summary>
            ///     Called when [attach].
            /// </summary>
            /// <param name="technique">The technique.</param>
            /// <returns></returns>
            protected override bool OnAttach(IRenderTechnique technique)
            {
                OutlineShaderPass = technique[OutlinePassName];
                return base.OnAttach(technique);
            }

            /// <summary>
            ///     Called when [update per model structure].
            /// </summary>
            /// <param name="context">The context.</param>
            protected override void OnUpdatePerModelStruct(RenderContext context)
            {
                base.OnUpdatePerModelStruct(context);
                modelStruct.Params.Y = OutlineFadingFactor;
            }

            /// <summary>
            ///     Called when [render].
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="deviceContext">The device context.</param>
            protected override void OnRender(RenderContext context, DeviceContextProxy deviceContext)
            {
                if (DrawOutlineBeforeMesh)
                {
                    OutlineShaderPass.BindShader(deviceContext);
                    OutlineShaderPass.BindStates(deviceContext, DefaultStateBinding);
                    DrawIndexed(deviceContext, GeometryBuffer.IndexBuffer, InstanceBuffer);
                }

                if (DrawMesh) base.OnRender(context, deviceContext);
                if (!DrawOutlineBeforeMesh)
                {
                    OutlineShaderPass.BindShader(deviceContext);
                    OutlineShaderPass.BindStates(deviceContext, DefaultStateBinding);
                    DrawIndexed(deviceContext, GeometryBuffer.IndexBuffer, InstanceBuffer);
                }
            }

            #region Properties

            /// <summary>
            ///     Outline color
            /// </summary>
            public Color4 Color
            {
                get => modelStruct.Color.ToColor4();
                set => SetAffectsRender(ref modelStruct.Color, value);
            }

            private bool outlineEnabled;

            /// <summary>
            ///     Enable outline
            /// </summary>
            public bool OutlineEnabled
            {
                get => outlineEnabled;
                set => SetAffectsRender(ref outlineEnabled, value);
            }

            private bool drawMesh = true;

            /// <summary>
            ///     Draw original mesh
            /// </summary>
            public bool DrawMesh
            {
                get => drawMesh;
                set => SetAffectsRender(ref drawMesh, value);
            }

            private bool drawOutlineBeforeMesh;

            /// <summary>
            ///     Draw outline order
            /// </summary>
            public bool DrawOutlineBeforeMesh
            {
                get => drawOutlineBeforeMesh;
                set => SetAffectsRender(ref drawOutlineBeforeMesh, value);
            }

            /// <summary>
            ///     Outline fading
            /// </summary>
            public float OutlineFadingFactor
            {
                get => modelStruct.Params.Y;
                set => SetAffectsRender(ref modelStruct.Params.Y, value);
            }

            private string outlinePassName = DefaultPassNames.MeshOutline;

            /// <summary>
            ///     Gets or sets the name of the outline pass.
            /// </summary>
            /// <value>
            ///     The name of the outline pass.
            /// </value>
            public string OutlinePassName
            {
                get => outlinePassName;
                set
                {
                    if (SetAffectsRender(ref outlinePassName, value) && IsAttached)
                        OutlineShaderPass = EffectTechnique[value];
                }
            }

            #endregion
        }
    }
}