/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        public class LineArrowMaterialVariable : LineMaterialVariable {
            private readonly LineArrowHeadMaterialCore material;

            /// <summary>
            ///     Initializes a new instance of the <see cref="LineMaterialVariable" /> class.
            /// </summary>
            /// <param name="manager">The manager.</param>
            /// <param name="technique">The technique.</param>
            /// <param name="materialCore">The material core.</param>
            /// <param name="defaultPassName">Default pass name</param>
            public LineArrowMaterialVariable(
                IEffectsManager manager,
                IRenderTechnique technique,
                LineArrowHeadMaterialCore materialCore,
                string defaultPassName = DefaultPassNames.Default
            )
                : base(manager, technique, materialCore, defaultPassName) {
                material = materialCore;
            }

            protected override void OnInitialPropertyBindings() {
                base.OnInitialPropertyBindings();
                AddPropertyBinding(nameof(LineArrowHeadMaterialCore.ArrowSize),
                                   () => {
                                       WriteValue(PointLineMaterialStruct.ParamsStr,
                                                  new Vector3(material.Thickness,
                                                              material.Smoothness,
                                                              material.ArrowSize));
                                   });
            }
        }
    }
}
