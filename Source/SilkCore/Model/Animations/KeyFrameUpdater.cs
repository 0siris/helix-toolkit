/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Model.Animations;
/// <summary>
/// </summary>
public class KeyFrameUpdater : IAnimationUpdater {
    private readonly int boneCount;

    private readonly Matrix[] currentBones;

    private readonly List<Keyframe>[] keyframes;

    private readonly Matrix[] tempBones;

    private readonly Keyframe?[] tempKeyframes;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KeyFrameUpdater" /> class.
    /// </summary>
    /// <param name="animation">The animation.</param>
    /// <param name="bones">The bones.</param>
    public KeyFrameUpdater(Animation animation, IList<Bone> bones) {
        Animation = animation;
        Name = animation.Name ?? string.Empty;
        boneCount = bones.Count;
        tempKeyframes = new Keyframe?[boneCount];
        tempBones = new Matrix[boneCount];
        currentBones = new Matrix[boneCount];
        Bones = bones;
        keyframes = new List<Keyframe>[boneCount];
        for (var i = 0; i < boneCount; ++i)
            keyframes[i] = new List<Keyframe>(animation.Keyframes.Count / boneCount);
        foreach (var frame in animation.Keyframes.OrderBy(x => x.Time)) keyframes[frame.BoneIndex].Add(frame);
    }

    public Animation Animation { get; }

    public IList<Bone> Bones { get; }

    public string Name { get; set; } = string.Empty;

    public AnimationRepeatMode RepeatMode { get; set; } = AnimationRepeatMode.PlayOnce;

    public float StartTime => Animation.StartTime;

    public float EndTime => Animation.EndTime;

    /// <summary>
    ///     Updates the animation by specified time stamp (ticks) and frequency (ticks per second).
    /// </summary>
    /// <param name="timeStamp">The time stamp (ticks).</param>
    /// <param name="frequency">The frequency (ticks per second).</param>
    public void Update(float timeStamp, long frequency) {
        if (Animation.BoneSkinMeshes.Count == 0) return;
        var timeSec = timeStamp / frequency;
        if (timeSec < StartTime) return;
        if (StartTime == EndTime) return;
        var timeElapsed = timeSec - StartTime;
        var boneNode = Animation.BoneSkinMeshes[0];
        if (timeElapsed > Animation.EndTime)
            switch (RepeatMode) {
                case AnimationRepeatMode.PlayOnce:
                    return;
                case AnimationRepeatMode.PlayOnceHold:
                    OutputBones(boneNode);
                    return;
                case AnimationRepeatMode.Loop:
                    timeElapsed = timeElapsed % EndTime + StartTime;
                    return;
            }

        foreach (var frames in keyframes) {
            var idx = AnimationUtils.FindKeyFrame(timeElapsed, frames);
            ref var currFrame = ref frames.GetInternalArray()[idx];
            if (currFrame.Time > timeElapsed && idx == 0) continue;
            Debug.Assert(currFrame.Time <= timeElapsed);
            if (frames.Count == 1 || idx == frames.Count - 1) {
                tempBones[currFrame.BoneIndex] = currFrame.ToTransformMatrix();
                continue;
            }

            ref var nextFrame = ref frames.GetInternalArray()[idx + 1];
            Debug.Assert(nextFrame.Time >= timeElapsed);
            var diff = timeElapsed - currFrame.Time;
            var length = nextFrame.Time - currFrame.Time;
            var amount = diff / length;
            tempBones[currFrame.BoneIndex] =
                SilkMath.Scaling(SilkMath.Lerp(currFrame.Scale, nextFrame.Scale, amount)) *
                SilkMath.RotationQuaternion(Quaternion.Slerp(currFrame.Rotation, nextFrame.Rotation, amount)) *
                SilkMath.Translation(SilkMath.Lerp(currFrame.Translation, nextFrame.Translation, amount));
        }

        // Apply parent bone transforms
        // We assume here that the first bone has no parent
        // and that each parent bone appears before children
        for (var i = 1; i < boneCount; i++) {
            var bone = Bones[i];
            if (bone.ParentIndex > -1) {
                var parentTransform = tempBones[bone.ParentIndex];
                tempBones[i] = tempBones[i] * parentTransform;
            }
        }

        // Change the bone transform from rest pose space into bone space (using the inverse of the bind/rest pose)
        for (var i = 0; i < boneCount; i++) currentBones[i] = Bones[i].InvBindPose * tempBones[i];
        OutputBones(boneNode);
    }

    public void Reset() { }


    private void OutputBones(IBoneMatricesNode node) {
        if (node.BoneMatrices.Length != boneCount)
            node.BoneMatrices = [.. currentBones];
        else
            currentBones.CopyTo(node.BoneMatrices, 0);
    }
}
