/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        public class DynamicReflectionNode : GroupNode, IDynamicReflector {
            /// <summary>
            ///     Initializes a new instance of the <see cref="DynamicReflectionNode" /> class.
            /// </summary>
            public DynamicReflectionNode() {
                ChildNodeAdded += DynamicReflectionNode_OnAddChildNode;
                ChildNodeRemoved += DynamicReflectionNode_OnRemoveChildNode;
                Cleared += DynamicReflectionNode_OnClear;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [enable reflector].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [enable reflector]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableReflector {
                get => (RenderCore as IDynamicReflector).EnableReflector;
                set => (RenderCore as IDynamicReflector).EnableReflector = value;
            }

            /// <summary>
            ///     Gets or sets the center.
            /// </summary>
            /// <value>
            ///     The center.
            /// </value>
            public Vector3 Center {
                get => (RenderCore as IDynamicReflector).Center;
                set => (RenderCore as IDynamicReflector).Center = value;
            }

            /// <summary>
            ///     Gets or sets the size of the face.
            /// </summary>
            /// <value>
            ///     The size of the face.
            /// </value>
            public int FaceSize {
                get => (RenderCore as IDynamicReflector).FaceSize;
                set => (RenderCore as IDynamicReflector).FaceSize = value;
            }

            /// <summary>
            ///     Gets or sets the near field.
            /// </summary>
            /// <value>
            ///     The near field.
            /// </value>
            public float NearField {
                get => (RenderCore as IDynamicReflector).NearField;
                set => (RenderCore as IDynamicReflector).NearField = value;
            }

            /// <summary>
            ///     Gets or sets the far field.
            /// </summary>
            /// <value>
            ///     The far field.
            /// </value>
            public float FarField {
                get => (RenderCore as IDynamicReflector).FarField;
                set => (RenderCore as IDynamicReflector).FarField = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether this coordinate system is left handed.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this coordinate system is left handed; otherwise, <c>false</c>.
            /// </value>
            public bool IsLeftHanded {
                get => (RenderCore as IDynamicReflector).IsLeftHanded;
                set => (RenderCore as IDynamicReflector).IsLeftHanded = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether this scene is dynamic scene.
            ///     If true, reflection map will be updated in each frame. Otherwise it will only be updated if scene graph or
            ///     visibility changed.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance is dynamic scene; otherwise, <c>false</c>.
            /// </value>
            public bool IsDynamicScene {
                get => (RenderCore as IDynamicReflector).IsDynamicScene;
                set => (RenderCore as IDynamicReflector).IsDynamicScene = value;
            }

            /// <summary>
            ///     Binds the cube map.
            /// </summary>
            /// <param name="deviceContext">The device context.</param>
            public void BindCubeMap(DeviceContextProxy deviceContext) {
                (RenderCore as IDynamicReflector).BindCubeMap(deviceContext);
            }

            /// <summary>
            ///     Uns the bind cube map.
            /// </summary>
            /// <param name="deviceContext">The device context.</param>
            public void UnBindCubeMap(DeviceContextProxy deviceContext) {
                (RenderCore as IDynamicReflector).UnBindCubeMap(deviceContext);
            }

            private void DynamicReflectionNode_OnClear(object sender, OnChildNodeChangedArgs e) {
                (RenderCore as DynamicCubeMapCore).IgnoredGuid.Clear();
            }

            private void DynamicReflectionNode_OnRemoveChildNode(object sender, OnChildNodeChangedArgs e) {
                (RenderCore as DynamicCubeMapCore).IgnoredGuid.Remove(e.Node.RenderCore.GUID);
                if (e.Node is IDynamicReflectable dyn) dyn.DynamicReflector = null;
            }

            private void DynamicReflectionNode_OnAddChildNode(object sender, OnChildNodeChangedArgs e) {
                (RenderCore as DynamicCubeMapCore).IgnoredGuid.Add(e.Node.RenderCore.GUID);
                if (e.Node is IDynamicReflectable dyn) dyn.DynamicReflector = this;
            }

            protected override RenderCore OnCreateRenderCore() {
                return new DynamicCubeMapCore();
            }

            protected override bool OnAttach(IEffectsManager effectsManager) {
                if (base.OnAttach(effectsManager)) {
                    RenderCore.Attach(EffectTechnique);
                    return true;
                }

                return false;
            }

            public override void UpdateNotRender(RenderContext context) {
                base.UpdateNotRender(context);
                if (Octree != null) {
                    Center = Octree.Bound.Center();
                } else {
                    var box = new BoundingBox();
                    var i = 0;
                    for (; i < ItemsInternal.Count; ++i)
                        if (ItemsInternal[i] is IDynamicReflectable) {
                            box = ItemsInternal[i].BoundsWithTransform;
                            break;
                        }

                    for (; i < ItemsInternal.Count; ++i)
                        if (ItemsInternal[i] is IDynamicReflectable)
                            box = BoundingBox.Merge(box, ItemsInternal[i].BoundsWithTransform);

                    Center = box.Center();
                }
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
                return effectsManager[DefaultRenderTechniqueNames.Skybox];
            }

            protected override bool CanRender(RenderContext context) {
                return base.CanRender(context);
            }
        }
    }
}
