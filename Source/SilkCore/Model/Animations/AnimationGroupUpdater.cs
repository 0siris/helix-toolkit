/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Animations;
public class AnimationGroupUpdater : IAnimationUpdater {
    private readonly List<IAnimationUpdater> children = [];

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
        get;
        set {
            field = value;
            foreach (var updater in Children) updater.RepeatMode = value;
        }
    } = AnimationRepeatMode.PlayOnce;

    public float StartTime { get; }

    public float EndTime { get; }

    public void Reset() {
        foreach (var updater in Children) updater.Reset();
    }

    public void Update(float timeStamp, long frequency) {
        foreach (var updater in Children) updater.Update(timeStamp, frequency);
    }
}
