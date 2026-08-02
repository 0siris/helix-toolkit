/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core {
    namespace Animations {
        public class AnimationGroupUpdater : IAnimationUpdater {
            private readonly List<IAnimationUpdater> children = [];

            private AnimationRepeatMode repeatMode = AnimationRepeatMode.PlayOnce;

            public AnimationGroupUpdater(string name = StringHelper.EmptyStr) {
                Name = name;
            }

            public AnimationGroupUpdater(IEnumerable<IAnimationUpdater> updaters, string name = StringHelper.EmptyStr)
                : this(name) {
                children.AddRange(updaters);
                foreach (var updater in Children) {
                    StartTime = Math.Min(StartTime, updater.StartTime);
                    EndTime = Math.Max(EndTime, updater.EndTime);
                }
            }

            public IList<IAnimationUpdater> Children => children;

            public string Name { get; set; } = string.Empty;

            public AnimationRepeatMode RepeatMode {
                get => repeatMode;
                set {
                    repeatMode = value;
                    foreach (var updater in Children) updater.RepeatMode = value;
                }
            }

            public float StartTime { get; }

            public float EndTime { get; }

            public void Reset() {
                foreach (var updater in Children) updater.Reset();
            }

            public void Update(float timeStamp, long frequency) {
                foreach (var updater in Children) updater.Update(timeStamp, frequency);
            }
        }
    }
}
