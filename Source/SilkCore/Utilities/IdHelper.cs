/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Collections.Concurrent;

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        public sealed class IdHelper {
            private readonly ConcurrentStack<int> freedIds_ = new();
            private int maxId_;

            public int MaxId => Interlocked.CompareExchange(ref maxId_, 0, 0);

            public int Count => MaxId - freedIds_.Count;

            public int GetNextId() {
                return freedIds_.TryPop(out var id) ? id : Interlocked.Increment(ref maxId_);
            }

            public void ReleaseId(int id) {
                freedIds_.Push(id);
            }
        }
    }
}
