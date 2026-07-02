using System.Collections.Concurrent;

namespace HelixToolkit.SharpDX.Core
{
    namespace Utilities
    {
        public sealed class ObjectPool<T>
        {
            private readonly Func<T> _objectGenerator;
            private readonly ConcurrentBag<T> _objects;
            private readonly int MaxCapacity;

            public ObjectPool(Func<T> objectGenerator, int maxCapacity = int.MaxValue / 2)
            {
                if (objectGenerator == null)
                    throw new ArgumentNullException("objectGenerator");
                _objects = new ConcurrentBag<T>();
                _objectGenerator = objectGenerator;
                MaxCapacity = maxCapacity;
            }

            public int Count => _objects.Count;

            public T GetObject()
            {
                T item;
                if (_objects.TryTake(out item))
                    return item;
                return _objectGenerator();
            }

            public void PutObject(T item)
            {
                if (Count < MaxCapacity)
                    _objects.Add(item);
            }
        }
    }
}