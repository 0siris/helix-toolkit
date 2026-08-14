using System.Collections.Concurrent;

namespace HelixToolkit.SharpDX.Core.Utilities;
public sealed class ObjectPool<T> {
    private readonly Func<T> objectGenerator;
    private readonly ConcurrentBag<T> objects;
    private readonly int maxCapacity;

    public ObjectPool(Func<T> objectGenerator, int maxCapacity = int.MaxValue / 2) {
        if (objectGenerator == null)
            ArgumentNullException.ThrowIfNull(objectGenerator);

        objects = [];
        this.objectGenerator = objectGenerator;
        this.maxCapacity = maxCapacity;
    }

    public int Count => objects.Count;

    public T GetObject() {
        T item;
        if (objects.TryTake(out item))
            return item;
        return objectGenerator();
    }

    public void PutObject(T item) {
        if (Count < maxCapacity)
            objects.Add(item);
    }
}
