/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using SharpAssimp;

namespace HelixToolkit.SharpDX.Core.Assimp;

public partial class Exporter {
    public const double DefaultTicksPerSecond = 30;

    private ErrorCode AddAnimationsToScene(Scene scene) {
        if (animations == null || animations.Count == 0) 
            return ErrorCode.Succeed;
        

        for (var i = 0; i < animations.Count; ++i) {
            var ani = new Animation {
                Name = string.IsNullOrEmpty(animations[i].Name) ? $"Animation_{i}" : animations[i].Name,
                TicksPerSecond = DefaultTicksPerSecond,
                DurationInTicks = (animations[i].EndTime - animations[i].StartTime) * DefaultTicksPerSecond
            };
            foreach (var f in animations[i].NodeAnimationCollection) {
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
