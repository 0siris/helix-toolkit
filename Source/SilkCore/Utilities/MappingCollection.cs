/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core.Utilities;
/// <summary>
/// </summary>
/// <typeparam name="Indextype"></typeparam>
/// <typeparam name="Nametype"></typeparam>
/// <typeparam name="Datatype"></typeparam>
public sealed class MappingCollection<Indextype, Nametype, Datatype>
    where Indextype : notnull
    where Nametype : notnull {
    private readonly Dictionary<Indextype, Datatype> indexDataMapping = [];
    private readonly Dictionary<Indextype, Nametype> indexNameMapping = [];
    private readonly Dictionary<Nametype, Indextype> nameIndexMapping = [];

    /// <summary>
    /// </summary>
    public KeyValuePair<Indextype, Datatype>[] MappingArray { get; private set; } =
        [];

    /// <summary>
    /// </summary>
    public IEnumerable<Datatype> Datas => indexDataMapping.Values;

    /// <summary>
    /// </summary>
    public int Count => indexNameMapping.Count;

    /// <summary>
    /// </summary>
    public IEnumerable<Indextype> Keys => indexNameMapping.Keys;

    /// <summary>
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Datatype this[Indextype key] => indexDataMapping[key];

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public Indextype this[Nametype name] => nameIndexMapping[name];

    /// <summary>
    /// </summary>
    /// <param name="index"></param>
    /// <param name="name"></param>
    /// <param name="item"></param>
    public void Add(Indextype index, Nametype name, Datatype item) {
        if (nameIndexMapping.ContainsKey(name)) throw new ArgumentException("Cannot add duplicate name.");
        if (indexNameMapping.ContainsKey(index)) throw new ArgumentException("Cannot add duplicate index");
        indexNameMapping.Add(index, name);
        nameIndexMapping.Add(name, index);
        indexDataMapping.Add(index, item);
        MappingArray = [.. indexDataMapping];
    }

    /// <summary>
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool Remove(Indextype index) {
        if (indexNameMapping.ContainsKey(index)) {
            nameIndexMapping.Remove(indexNameMapping[index]);
            indexNameMapping.Remove(index);
            indexDataMapping.Remove(index);
            MappingArray = [.. indexDataMapping];
            return true;
        }

        return false;
    }

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool Remove(Nametype name) {
        if (nameIndexMapping.ContainsKey(name)) {
            indexNameMapping.Remove(nameIndexMapping[name]);
            indexDataMapping.Remove(nameIndexMapping[name]);
            nameIndexMapping.Remove(name);
            MappingArray = [.. indexDataMapping];
            return true;
        }

        return false;
    }

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool HasItem(Indextype id) => indexNameMapping.ContainsKey(id);

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool TryGetItem(Indextype id, out Datatype data) => indexDataMapping.TryGetValue(id, out data);

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public bool TryGetSlot(Nametype name, out Indextype index) => nameIndexMapping.TryGetValue(name, out index);

    /// <summary>
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool TryGetName(Indextype id, out Nametype name) => indexNameMapping.TryGetValue(id, out name);

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool HasItem(Nametype name) => nameIndexMapping.ContainsKey(name);

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public bool TryGetItem(Nametype name, out Datatype data) {
        Indextype idx;
        if (nameIndexMapping.TryGetValue(name, out idx) && indexDataMapping.TryGetValue(idx, out data))
            return true;

        data = default;
        return false;
    }

    /// <summary>
    /// </summary>
    public void Clear() {
        nameIndexMapping.Clear();
        indexNameMapping.Clear();
        indexDataMapping.Clear();
        MappingArray = [];
    }
}
