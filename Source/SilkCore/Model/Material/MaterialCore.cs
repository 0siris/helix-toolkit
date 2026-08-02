/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        /// <summary>
        /// </summary>
        public abstract class MaterialCore : ObservableObject, IMaterial {
            private string name = "Material";

            public string Name {
                get => name;
                set => Set(ref name, value);
            }

            public Guid Guid { get; } = Guid.NewGuid();

            /// <summary>
            ///     Creates the material variables.
            /// </summary>
            /// <param name="manager">The manager.</param>
            /// <param name="technique">The technique.</param>
            /// <returns></returns>
            public abstract MaterialVariable CreateMaterialVariables(
                IEffectsManager manager,
                IRenderTechnique technique
            );
        }
    }
}
