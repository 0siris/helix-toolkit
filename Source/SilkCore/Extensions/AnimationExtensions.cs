/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Animations;

namespace HelixToolkit.SharpDX.Core.Extensions;

public static class AnimationExtensions {
    public static Dictionary<string, IAnimationUpdater>
        CreateAnimationUpdaters(this IEnumerable<Animation> animations) {
        var dict = new Dictionary<string, IAnimationUpdater>();
        foreach (var ani in animations)
            switch (ani.AnimationType) {
                case AnimationType.Keyframe:
                    if (ani.RootNode is IBoneMatricesNode bNode && bNode.Bones is { } bones)
                        AddUpdaterToDict(dict, new KeyFrameUpdater(ani, bones));
                    else
                        foreach (var b in ani.BoneSkinMeshes)
                            if (b.Bones is { } meshBones)
                                AddUpdaterToDict(dict, new KeyFrameUpdater(ani, meshBones));

                    break;
                case AnimationType.Node:
                    AddUpdaterToDict(dict, new NodeAnimationUpdater(ani));
                    break;
                case AnimationType.MorphTarget:
                    if (ani.RootNode is IBoneMatricesNode mNode)
                        AddUpdaterToDict(dict, new MorphTargetKeyFrameUpdater(ani, mNode.MorphTargetWeights));
                    else
                        foreach (var b in ani.BoneSkinMeshes)
                            AddUpdaterToDict(dict, new MorphTargetKeyFrameUpdater(ani, b.MorphTargetWeights));

                    break;
            }

        return dict;
    }

    private static void AddUpdaterToDict(Dictionary<string, IAnimationUpdater> dict, IAnimationUpdater updater) {
        if (dict.TryGetValue(updater.Name, out var existingUpdater)) {
            if (existingUpdater is AnimationGroupUpdater group) {
                group.Children.Add(updater);
            } else {
                dict.Remove(updater.Name);
                var newGroup = new AnimationGroupUpdater(updater.Name);
                newGroup.Children.Add(existingUpdater);
                newGroup.Children.Add(updater);
                dict.Add(newGroup.Name, newGroup);
            }
        } else {
            dict.Add(updater.Name, updater);
        }
    }
}
