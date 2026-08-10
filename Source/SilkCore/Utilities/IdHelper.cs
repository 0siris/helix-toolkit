/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Collections.Concurrent;

namespace HelixToolkit.SharpDX.Core.Utilities;

public sealed class IdHelper {
    private readonly ConcurrentStack<int> freedIds = new();
    private int maxId;

    public int MaxId => Interlocked.CompareExchange(ref maxId, 0, 0);

    public int Count => MaxId - freedIds.Count;

    public int GetNextId() => freedIds.TryPop(out var id)
                                  ? id
                                  : Interlocked.Increment(ref maxId);

    public void ReleaseId(int id) => freedIds.Push(id);
}
