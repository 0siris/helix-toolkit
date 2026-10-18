/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using SharpAssimp;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Logger;
using HelixToolkit.SharpDX.Core.Model.Animations;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Utilities;
using Animation = SharpAssimp.Animation;

namespace HelixToolkit.SharpDX.Core.Assimp;

/// <summary>Converts Assimp scenes, including node and morph animation channels, to Silk scene data.</summary>
public partial class Importer {
    /// <summary>
    ///     Samples independent TRS channels at every distinct source key time without changing the source channel.
    /// </summary>
    /// <param name="channel">The channel with chronologically ordered component keys.</param>
    /// <param name="ticksPerSecond">The source or configured fallback tick rate used to convert times to seconds.</param>
    /// <param name="list">The resulting tightly sized keyframe list consumed by the node animation updater.</param>
    /// <returns><see cref="ErrorCode.Succeed"/> after converting the channel.</returns>
    /// <remarks>
    ///     Missing components use zero translation, identity rotation, and unit scale. Existing components hold their
    ///     endpoint values outside their own time range and interpolate inside it. The last key at a duplicate time wins.
    ///     No artificial time-zero key is added, preserving the consumer's behavior before the first source key.
    /// </remarks>
    protected virtual ErrorCode ProcessNodeAnimation(
        NodeAnimationChannel channel,
        double ticksPerSecond,
        out FastList<Keyframe> list
    ) {
        var times = new SortedSet<double>(channel.PositionKeys.Select(key => key.Time));
        times.UnionWith(channel.RotationKeys.Select(key => key.Time));
        times.UnionWith(channel.ScalingKeys.Select(key => key.Time));
        var ret = new FastList<Keyframe>(times.Count);
        int positionIndex = 0, rotationIndex = 0, scaleIndex = 0;
        foreach (var time in times)
            ret.Add(new Keyframe {
                Time = (float)(time / ticksPerSecond),
                Translation = SampleVectorKeys(channel.PositionKeys, time, ref positionIndex, Vector3.Zero),
                Rotation = SampleRotationKeys(channel.RotationKeys, time, ref rotationIndex),
                Scale = SampleVectorKeys(channel.ScalingKeys, time, ref scaleIndex, Vector3.One)
            });

        list = ret;
        return ErrorCode.Succeed;
    }

    /// <summary>
    ///     Samples ordered vector keys using the existing Silk vector interpolation boundary.
    /// </summary>
    /// <param name="keys">The position or scaling keys.</param>
    /// <param name="time">The source time in ticks, visited in ascending order.</param>
    /// <param name="index">The current key index, advanced past shared or duplicate times.</param>
    /// <param name="defaultValue">The value for an absent component.</param>
    /// <returns>The interpolated vector or the nearest endpoint value.</returns>
    private static Vector3 SampleVectorKeys(List<VectorKey> keys, double time, ref int index, Vector3 defaultValue) {
        if (keys.Count == 0) return defaultValue;
        while (index + 1 < keys.Count &&
               (keys[index + 1].Time <= time || keys[index + 1].Time == keys[index].Time)) ++index;
        var current = keys[index];
        if (time <= current.Time || index == keys.Count - 1) return current.Value.ToSharpDXVector3();
        var nextIndex = index + 1;
        while (nextIndex + 1 < keys.Count && keys[nextIndex + 1].Time == keys[nextIndex].Time) ++nextIndex;
        var next = keys[nextIndex];
        var amount = (float)((time - current.Time) / (next.Time - current.Time));
        return Silk.NET.Maths.Vector3D.Lerp(current.Value.ToSharpDXVector3(), next.Value.ToSharpDXVector3(), amount);
    }

    /// <summary>
    ///     Samples ordered quaternion keys using the same spherical interpolation as the node updater.
    /// </summary>
    /// <param name="keys">The rotation keys.</param>
    /// <param name="time">The source time in ticks, visited in ascending order.</param>
    /// <param name="index">The current key index, advanced past shared or duplicate times.</param>
    /// <returns>The interpolated rotation, nearest endpoint, or identity for an absent component.</returns>
    private static Quaternion SampleRotationKeys(List<QuaternionKey> keys, double time, ref int index) {
        if (keys.Count == 0) return Quaternion.Identity;
        while (index + 1 < keys.Count &&
               (keys[index + 1].Time <= time || keys[index + 1].Time == keys[index].Time)) ++index;
        var current = keys[index];
        if (time <= current.Time || index == keys.Count - 1) return current.Value.ToSharpDXQuaternion();
        var nextIndex = index + 1;
        while (nextIndex + 1 < keys.Count && keys[nextIndex + 1].Time == keys[nextIndex].Time) ++nextIndex;
        var next = keys[nextIndex];
        var amount = (float)((time - current.Time) / (next.Time - current.Time));
        return Quaternion.Slerp(current.Value.ToSharpDXQuaternion(), next.Value.ToSharpDXQuaternion(), amount);
    }

    private ErrorCode LoadAnimations(HelixInternalScene scene) {
        var dict = new Dictionary<string, SceneNode>(SceneNodes.Count);
        foreach (var node in SceneNodes.OfType<GroupNode>())
            dict.TryAdd(node.Name, node);

        var nodeIdxDict = new Dictionary<string, int>();
        foreach (var node in SceneNodes
                     .OfType<IBoneMatricesNode>())
            if (node.Bones != null) {
                nodeIdxDict.Clear();
                for (var i = 0; i < node.Bones.Length; ++i) nodeIdxDict.Add(node.Bones[i].Name, i);
                for (var i = 0; i < node.Bones.Length; ++i)
                    if (dict.TryGetValue(node.Bones[i].Name, out var s)) {
                        ref var b = ref node.Bones[i];
                        b.ParentNode = s.Parent;
                        b.Node = s;
                        b.BoneLocalTransform = s.ModelMatrix;
                        if (s.Parent != null && nodeIdxDict.TryGetValue(s.Parent.Name, out var idx))
                            b.ParentIndex = idx;
                        s.IsAnimationNode =
                            true; // Make sure to set this to true                                   
                    }

                if (Configuration.CreateSkeletonForBoneSkinningMesh
                    && node is BoneSkinMeshNode {Parent: GroupNodeBase group} sk) {
                    var skeleton = sk.CreateSkeletonNode(Configuration.SkeletonMaterial,
                                                         Configuration.SkeletonEffects,
                                                         Configuration.SkeletonSizeScale);
                    skeleton.Name = "HxSK_" + sk.Name;
                    group.AddChildNode(skeleton);
                }

                //Setup bone matrices initially if it's morphable (unable to render w/o bones)
                if (node is BoneSkinMeshNode {MorphTargetWeights.Length: > 0, BoneMatrices.Length: 0} sn)
                    sn.UpdateBoneMatrices();
            }

        if (scene.AssimpScene.HasAnimations) {
            var hasBoneSkinnedMesh = scene.Meshes.Any(x => x.Mesh is BoneSkinnedMeshGeometry3D);
            
            var animationList = new List<Model.Animations.Animation>(scene.AssimpScene.AnimationCount);
            if (Configuration.EnableParallelProcessing)
                Parallel.ForEach(scene.AssimpScene.Animations,
                                 ani => {
                                     if (LoadAnimation(ani, dict, hasBoneSkinnedMesh, out var hxAni) ==
                                         ErrorCode.Succeed)
                                         lock (animationList) {
                                             animationList.Add(hxAni);
                                         }

                                     if (LoadMorphAnimation(ani, dict, hasBoneSkinnedMesh, out var hxMAni) ==
                                         ErrorCode.Succeed)
                                         lock (animationList) {
                                             animationList.AddRange(hxMAni);
                                         }
                                 });
            else
                foreach (var ani in scene.AssimpScene.Animations) {
                    if (LoadAnimation(ani, dict, hasBoneSkinnedMesh, out var hxAni) == ErrorCode.Succeed)
                        animationList.Add(hxAni);
                    if (LoadMorphAnimation(ani, dict, hasBoneSkinnedMesh, out var hxMAni) == ErrorCode.Succeed)
                        animationList.AddRange(hxMAni);
                }

            scene.Animations = animationList;
            Animations.AddRange(animationList);
        }

        return ErrorCode.Succeed;
    }

    private ErrorCode LoadAnimation(
        Animation ani,
        Dictionary<string, SceneNode> dict,
        bool searchBoneSkinMeshNode,
        out Model.Animations.Animation hxAni
    ) {
        if (ani.TicksPerSecond == 0) {
            Logger.Warn("Animation TicksPerSecond is 0. Set to {Value0}", configuration.TickesPerSecond);
            ani.TicksPerSecond = configuration.TickesPerSecond;
        }

        hxAni = new Model.Animations.Animation(AnimationType.Node) {
            StartTime = 0,
            EndTime = (float)(ani.DurationInTicks / ani.TicksPerSecond),
            Name = ani.Name,
            NodeAnimationCollection = new List<NodeAnimation>(ani.NodeAnimationChannelCount)
        };

        if (ani.HasNodeAnimations) {
            var code = ErrorCode.None;
            foreach (var key in ani.NodeAnimationChannels) {
                Debug.WriteLine(key.NodeName);
                if (dict.TryGetValue(key.NodeName, out var node)) {
                    var nAni = new NodeAnimation {
                        Node = node
                    };

                    node.IsAnimationNode = true; // Make sure to set this to true
                    code = ProcessNodeAnimation(key, ani.TicksPerSecond, out var keyframes);
                    if (code == ErrorCode.Succeed) {
                        nAni.KeyFrames = keyframes;
                        hxAni.NodeAnimationCollection.Add(nAni);
                    } else {
                        break;
                    }
                }
            }

            if (searchBoneSkinMeshNode) FindBoneSkinMeshes(hxAni);
            return code;
        }

        return ErrorCode.Failed;
    }

    private ErrorCode LoadMorphAnimation(
        Animation ani,
        Dictionary<string, SceneNode> dict,
        bool searchBoneSkinMeshNode,
        out List<Model.Animations.Animation> hxAnis
    ) {
        if (ani.TicksPerSecond == 0) {
            Logger.Warn("Animation TicksPerSecond is 0. Set to {Value0}", configuration.TickesPerSecond);
            ani.TicksPerSecond = configuration.TickesPerSecond;
        }

        hxAnis = [];
        if (ani.MeshMorphAnimationChannelCount > 0) {
            foreach (var aniChannel in ani.MeshMorphAnimationChannels) {
                var hxAni = new Model.Animations.Animation(AnimationType.MorphTarget) {
                    StartTime = 0,
                    EndTime = (float)(ani.DurationInTicks / ani.TicksPerSecond),
                    Name = ani.Name ?? string.Empty,
                    MorphTargetKeyframes = []
                };

                //Reference node (removes "*0", i don't know why but its there sometimes)
                var nodeName = aniChannel.Name.Replace("*0", "");
                if (dict.TryGetValue(nodeName, out var node))
                    hxAni.RootNode = node.Items
                                         .Where(i => i is BoneSkinMeshNode {
                                             MorphTargetWeights: { Length: > 0 }
                                         })
                                         .FirstOrDefault();
                else
                    continue;

                //Add keyframes
                foreach (var key in aniChannel.MeshMorphKeys)
                    for (var i = 0; i < key.Values.Count; i++)
                        hxAni.MorphTargetKeyframes.Add(new MorphTargetKeyframe {
                            Index = key.Values[i],
                            Weight = (float)key.Weights[i],
                            Time = (float)key.Time / (float)ani.TicksPerSecond
                        });

                if (searchBoneSkinMeshNode) FindBoneSkinMeshes(hxAni);
                hxAnis.Add(hxAni);
            }

            return ErrorCode.Succeed;
        }

        return ErrorCode.Failed;
    }

    private void FindBoneSkinMeshes(Model.Animations.Animation animation) {
        if (animation.NodeAnimationCollection is { Count: > 0 }) {
            // Search all the bone skinned meshes from the common animation node root
            var node = animation.NodeAnimationCollection[0].Node;
            while (node is {IsAnimationNodeRoot: false}) node = node.Parent;

            if (node == null) return;

            if (node.Parent != null)
                node = node.Parent;
            animation.BoneSkinMeshes = [];
            animation.RootNode = node;
            foreach (var n in SceneNodes[0].Items.PreorderDft(_ => true))
                if (n is IBoneMatricesNode boneNode)
                    animation.BoneSkinMeshes.Add(boneNode);
        } else if (animation.MorphTargetKeyframes is { Count: > 0 }) {
            animation.BoneSkinMeshes = [];
            if (animation.RootNode is IBoneMatricesNode bnode) animation.BoneSkinMeshes.Add(bnode);
        }
    }
}
