/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Animations;
using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        public sealed class BoneGroupNode : GroupNodeBase, IBoneMatricesNode {
            private readonly BoneUploaderCore core = new();

            public BoneGroupNode() {
                ChildNodeAdded += NodeGroup_OnAddChildNode;
                ChildNodeRemoved += NodeGroup_OnRemoveChildNode;
            }

            public Matrix[] BoneMatrices {
                get => core.BoneMatrices;
                set => core.BoneMatrices = value;
            }

            /// <summary>
            ///     Gets or sets the bones.
            /// </summary>
            /// <value>
            ///     The bones.
            /// </value>
            public Bone[] Bones { get; set; }

            public float[] MorphTargetWeights { get; set; }

            /// <summary>
            ///     Always return false for bone groups
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance has bone group; otherwise, <c>false</c>.
            /// </value>
            public bool HasBoneGroup { get; } = false;

            protected override RenderCore OnCreateRenderCore() {
                return core;
            }

            private void NodeGroup_OnRemoveChildNode(object sender, OnChildNodeChangedArgs e) {
                if (e.Node is BoneSkinMeshNode b) {
                    b.HasBoneGroup = false;
                    (b.RenderCore as BoneSkinRenderCore).SharedBoneBuffer = null;
                }
            }

            private void NodeGroup_OnAddChildNode(object sender, OnChildNodeChangedArgs e) {
                if (e.Node is BoneSkinMeshNode b) {
                    b.HasBoneGroup = true;
                    (b.RenderCore as BoneSkinRenderCore).SharedBoneBuffer = core;
                }
            }
        }
    }
}
