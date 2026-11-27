/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpAssimp;

namespace HelixToolkit.SharpDX.Core.Assimp;

public partial class Exporter {
    /// <summary>The process-wide animation export rate in ticks per second, defaulting to thirty.</summary>
    public static double DefaultTicksPerSecond = 30;

    private ErrorCode AddAnimationsToScene(Scene scene) {
        if (animations == null || animations.Count == 0) 
            return ErrorCode.Succeed;
        

        for (var i = 0; i < animations.Count; ++i) {
            var animation = animations[i];
            var name = animation.Name;
            var ani = new Animation {
                Name = string.IsNullOrEmpty(name) ? $"Animation_{i}" : name,
                TicksPerSecond = DefaultTicksPerSecond,
                DurationInTicks = (animation.EndTime - animation.StartTime) * DefaultTicksPerSecond
            };
            foreach (var f in animation.NodeAnimationCollection) {
                if (f.Node == null || string.IsNullOrEmpty(f.Node.Name)) {
                    Logger.Warn("Node Animation NodeName is empty. AnimationName:{Value0}", ani.Name);
                    continue;
                }

                var ch = new NodeAnimationChannel {
                    NodeName = f.Node.Name
                };
                foreach (var kf in f.KeyFrames) {
                    var t = kf.Time * DefaultTicksPerSecond;
                    ch.PositionKeys.Add(new VectorKey(t, kf.Translation.ToAssimpVector3D(), AnimationInterpolation.Linear));
                    ch.ScalingKeys.Add(new VectorKey(t, kf.Scale.ToAssimpVector3D(), AnimationInterpolation.Linear));
                    ch.RotationKeys.Add(new QuaternionKey(t, kf.Rotation.ToAssimpQuaternion(), AnimationInterpolation.Linear));
                }

                ani.NodeAnimationChannels.Add(ch);
            }

            scene.Animations.Add(ani);
        }

        return ErrorCode.Succeed;
    }
}
