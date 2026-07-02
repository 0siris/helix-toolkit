/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
namespace HelixToolkit.SharpDX.Core
{
    namespace Model
    {
        public class LineArrowHeadMaterialCore : LineMaterialCore
        {
            private float arrowSize = 0.1f;
            /// <summary>
            /// Gets or sets the size of the arrow.
            /// </summary>
            /// <value>
            /// The size of the arrow.
            /// </value>
            public float ArrowSize
            {
                set
                {
                    Set(ref arrowSize, value);
                }
                get
                {
                    return arrowSize;
                }
            }

            public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique)
            {
                return new LineArrowMaterialVariable(manager, manager.GetTechnique(DefaultRenderTechniqueNames.LinesArrowHead), this);
            }
        }

        public class LineArrowHeadTailMaterialCore : LineArrowHeadMaterialCore
        {
            public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique)
            {
                return new LineArrowMaterialVariable(manager, manager.GetTechnique(DefaultRenderTechniqueNames.LinesArrowHeadTail), this);
            }
        }
    }
}
