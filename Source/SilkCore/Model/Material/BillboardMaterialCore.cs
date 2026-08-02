/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        public sealed class BillboardMaterialCore : MaterialCore, IBillboardRenderParams {
            private bool fixedSize = true;

            private SamplerStateDescription samplerDescription = DefaultSamplers.LinearSamplerClampAni1;

            private BillboardType type = BillboardType.SingleText;

            /// <summary>
            ///     Gets or sets a value indicating whether [fixed size].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [fixed size]; otherwise, <c>false</c>.
            /// </value>
            public bool FixedSize {
                get => fixedSize;
                set => Set(ref fixedSize, value);
            }

            public BillboardType Type {
                get => type;
                set => Set(ref type, value);
            }

            /// <summary>
            ///     Billboard texture sampler description
            /// </summary>
            public SamplerStateDescription SamplerDescription {
                get => samplerDescription;
                set => Set(ref samplerDescription, value);
            }

            public override MaterialVariable CreateMaterialVariables(
                IEffectsManager manager,
                IRenderTechnique technique
            ) {
                return new BillboardMaterialVariable(manager, technique, this);
            }
        }
    }
}
