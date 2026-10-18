using HelixToolkit.SharpDX.Core;
using System.Collections;

namespace HelixToolkit.UWP;

/// Verifies the source dictionary contract and the retained target collection API.
public sealed class DoubleKeyDictionaryTests {
    /// Replaces the value for one complete key pair without changing its neighbours.
    [Fact]
    public void AddOverwriteTest() {
        var dictionary = new DoubleKeyDictionary<string, int, string>();
        dictionary.Add("left", 1, "old");
        dictionary.Add("left", 2, "sibling");
        dictionary.Add("right", 1, "other");

        dictionary.Add("left", 1, "replacement");
        var entries = dictionary.ToArray();
        var found = dictionary.TryGetValue("left", 1, out var value);

        Assert.Equal(3, entries.Length);
        Assert.True(found);
        Assert.Equal("replacement", value);
        Assert.Contains(new KeyValuePair<(string, int), string>(("left", 2), "sibling"), entries);
        Assert.Contains(new KeyValuePair<(string, int), string>(("right", 1), "other"), entries);
    }

    /// Compares mappings independently of their insertion order through the normal object contract.
    [Fact]
    public void EqualsSameMappingsTest() {
        var left = new DoubleKeyDictionary<string, int, string>();
        left.Add("a", 1, "one");
        left.Add("b", 2, "two");
        var right = new DoubleKeyDictionary<string, int, string>();
        right.Add("b", 2, "two");
        right.Add("a", 1, "one");

        var equal = left.Equals((object)right);

        Assert.True(equal);
    }

    /// Rejects equal value sets whose values belong to different second keys.
    [Fact]
    public void EqualsSwappedValuesTest() {
        var left = new DoubleKeyDictionary<string, int, string>();
        left.Add("group", 1, "one");
        left.Add("group", 2, "two");
        var right = new DoubleKeyDictionary<string, int, string>();
        right.Add("group", 1, "two");
        right.Add("group", 2, "one");

        var forward = left.Equals(right);
        var backward = right.Equals(left);

        Assert.False(forward);
        Assert.False(backward);
    }

    /// Requires the new indexer to report absent outer and inner keys as dictionary lookup errors.
    [Fact]
    public void IndexerMissingKeysTest() {
        dynamic dictionary = new DoubleKeyDictionary<string, int, string>();
        dictionary.Add("present", 1, "value");

        Assert.Throws<KeyNotFoundException>(() => { string value = dictionary["absent", 1]; });
        Assert.Throws<KeyNotFoundException>(() => { string value = dictionary["present", 2]; });
    }

    /// Exercises actual source carrier instances while remaining compilable against the pinned incomplete API.
    [Fact]
    public void PublicCarrierOutputTest() {
        var dictionary = new DoubleKeyDictionary<string, int, string>();
        dictionary.Add("group", 3, "original");
        dictionary.Add("other", 7, "neighbour");
        dynamic sourceView = dictionary;
        IEnumerable pairs = sourceView.GetPairs();
        var entries = pairs.Cast<dynamic>().ToArray();
        var carrierType = dictionary.GetType().Assembly.GetType("HelixToolkit.SharpDX.Core.DoubleKeyPairValue`3");
        Assert.NotNull(carrierType);
        var expectedType = carrierType.MakeGenericType(typeof(string), typeof(int), typeof(string));

        Assert.Equal(2, entries.Length);
        Assert.Equal(expectedType, (Type)entries[0].GetType());
        Assert.Equal("group", (string)entries[0].Key1);
        Assert.Equal(3, (int)entries[0].Key2);
        Assert.Equal("original", (string)entries[0].Value);
        Assert.Equal("group - 3 - original", (string)entries[0].ToString());
        entries[0].Key1 = "changed";
        entries[0].Key2 = 99;
        entries[0].Value = "detached";
        var found = dictionary.TryGetValue("group", 3, out var stored);
        IEnumerable secondPass = sourceView.GetPairs();
        var fresh = secondPass.Cast<dynamic>().ToArray();

        Assert.True(found);
        Assert.Equal("original", stored);
        Assert.NotSame((object)entries[0], (object)fresh[0]);
        Assert.Equal("group", (string)fresh[0].Key1);
        Assert.Equal("other", (string)fresh[1].Key1);
        Assert.Equal(7, (int)fresh[1].Key2);
        Assert.Equal("neighbour", (string)fresh[1].Value);
    }

    /// Requires the missing source API without making the baseline tests fail to compile.
    [Fact]
    public void SourcePublicSurfaceTest() {
        var type = typeof(DoubleKeyDictionary<string, int, string>);
        var equality = typeof(IEquatable<DoubleKeyDictionary<string, int, string>>).IsAssignableFrom(type);
        var indexer = type.GetProperty("Item", [typeof(string), typeof(int)]);
        var containsKey = type.GetMethod("ContainsKey", [typeof(string), typeof(int)]);
        var pairs = type.GetMethod("GetPairs", Type.EmptyTypes);
        var carrier = type.Assembly.GetType("HelixToolkit.SharpDX.Core.DoubleKeyPairValue`3");

        Assert.True(equality);
        Assert.NotNull(indexer);
        Assert.NotNull(containsKey);
        Assert.NotNull(pairs);
        Assert.NotNull(carrier);
    }

    /// Uses the source two-key operations through their statically typed public signatures.
    [Fact]
    public void IndexerInsertReplaceAndContainsKeyTest() {
        var dictionary = new DoubleKeyDictionary<string, string, int>();
        dictionary["a", "b"] = 10;
        dictionary["b", "a"] = 20;
        dictionary["a", "b"] = 30;

        var first = dictionary["a", "b"];
        var reversed = dictionary["b", "a"];
        var present = dictionary.ContainsKey("a", "b");
        var absentFirst = dictionary.ContainsKey("absent", "b");
        var absentSecond = dictionary.ContainsKey("a", "absent");
        var count = dictionary.Count();

        Assert.Equal(30, first);
        Assert.Equal(20, reversed);
        Assert.True(present);
        Assert.False(absentFirst);
        Assert.False(absentSecond);
        Assert.Equal(2, count);
    }

    /// Preserves missing-value defaults and removal of one sibling without disturbing the other.
    [Fact]
    public void TryGetValueAndRemoveSiblingsTest() {
        var dictionary = new DoubleKeyDictionary<string, int, int>();
        dictionary.Add("group", 1, 10);
        dictionary.Add("group", 2, 20);
        var missingFirst = dictionary.TryGetValue("absent", 1, out var firstDefault);
        var missingSecond = dictionary.TryGetValue("group", 3, out var secondDefault);

        var removed = dictionary.Remove("group", 1);
        var sibling = dictionary.TryGetValue("group", 2, out var siblingValue);
        var absent = dictionary.ContainsKey("group", 1);
        var removedLast = dictionary.Remove("group", 2);
        var entries = dictionary.ToArray();

        Assert.False(missingFirst);
        Assert.Equal(0, firstDefault);
        Assert.False(missingSecond);
        Assert.Equal(0, secondDefault);
        Assert.True(removed);
        Assert.True(sibling);
        Assert.Equal(20, siblingValue);
        Assert.False(absent);
        Assert.True(removedLast);
        Assert.Empty(entries);
    }

    /// Compares through IEquatable and supplies insertion-order-independent hashes for hashed collection lookup.
    [Fact]
    public void EqualityAndHashContractTest() {
        var left = new DoubleKeyDictionary<string, int, string?>();
        left.Add("a", 1, null);
        left.Add("b", 2, new string('x', 3));
        var right = new DoubleKeyDictionary<string, int, string?>();
        right.Add("b", 2, new string('x', 3));
        right.Add("a", 1, null);
        IEquatable<DoubleKeyDictionary<string, int, string?>> equatable = left;

        var equal = equatable.Equals(right);
        var reflexive = equatable.Equals(left);
        var nullEqual = equatable.Equals(null);
        var unrelated = left.Equals((object)"unrelated");
        var leftHash = left.GetHashCode();
        var rightHash = right.GetHashCode();
        var set = new HashSet<DoubleKeyDictionary<string, int, string?>> { left };
        var found = set.Contains(right);

        Assert.True(equal);
        Assert.True(reflexive);
        Assert.False(nullEqual);
        Assert.False(unrelated);
        Assert.Equal(leftHash, rightHash);
        Assert.True(found);
    }

    /// Rejects an extra inner entry in either comparison direction, including a duplicate value.
    [Fact]
    public void EqualsExtraEntryTest() {
        var small = new DoubleKeyDictionary<string, int, string>();
        small.Add("group", 1, "same");
        var large = new DoubleKeyDictionary<string, int, string>();
        large.Add("group", 1, "same");
        large.Add("group", 2, "same");

        var forward = small.Equals(large);
        var backward = large.Equals(small);

        Assert.False(forward);
        Assert.False(backward);
    }

    /// Rejects mismatches in either key even when the value and entry count agree.
    [Fact]
    public void EqualsDifferentKeysTest() {
        var original = new DoubleKeyDictionary<string, int, string>();
        original.Add("group", 1, "same");
        var first = new DoubleKeyDictionary<string, int, string>();
        first.Add("other", 1, "same");
        var second = new DoubleKeyDictionary<string, int, string>();
        second.Add("group", 2, "same");

        var firstEqual = original.Equals(first);
        var secondEqual = original.Equals(second);

        Assert.False(firstEqual);
        Assert.False(secondEqual);
    }

    /// Makes pair enumeration lazy and typed, preserving borrowed reference values without cloning them.
    [Fact]
    public void GetPairsTypedLazyViewTest() {
        var dictionary = new DoubleKeyDictionary<string, int, List<int>>();
        IEnumerable<DoubleKeyPairValue<string, int, List<int>>> pairs = dictionary.GetPairs();
        var value = new List<int> { 5 };
        dictionary.Add("group", 2, value);

        var entries = pairs.ToArray();

        Assert.Single(entries);
        Assert.Equal("group", entries[0].Key1);
        Assert.Equal(2, entries[0].Key2);
        Assert.Same(value, entries[0].Value);
    }

    /// Keeps the public carrier constructor, mutable properties and source string representation usable directly.
    [Fact]
    public void PublicCarrierConstructionTest() {
        var pair = new DoubleKeyPairValue<string?, int, string?>(null, 4, null);
        var initialText = pair.ToString();
        pair.Key1 = "updated";
        pair.Key2 = 8;
        pair.Value = "value";
        var updatedText = pair.ToString();

        Assert.Equal(" - 4 - ", initialText);
        Assert.Equal("updated", pair.Key1);
        Assert.Equal(8, pair.Key2);
        Assert.Equal("value", pair.Value);
        Assert.Equal("updated - 8 - value", updatedText);
    }

    /// Clears retained live views, permits reuse and equates empty dictionaries.
    [Fact]
    public void ClearAndReuseTest() {
        var dictionary = new DoubleKeyDictionary<string, int, string>();
        dictionary.Add("group", 1, "one");
        var values = dictionary.Values;
        var pairs = dictionary.GetPairs();
        dictionary.Clear();
        dictionary.Clear();
        var clearedValues = values.ToArray();
        var clearedPairs = pairs.ToArray();
        var empty = new DoubleKeyDictionary<string, int, string>();
        var emptyEqual = dictionary.Equals(empty);
        dictionary.Add("new", 2, "two");
        var reused = values.ToArray();

        Assert.Empty(clearedValues);
        Assert.Empty(clearedPairs);
        Assert.True(emptyEqual);
        Assert.Equal(new[] { "two" }, reused);
    }

    /// Preserves the established tuple storage's acceptance of null component keys and values.
    [Fact]
    public void RetainedNullKeysTest() {
        var dictionary = new DoubleKeyDictionary<string?, string?, string?>();
        dictionary.Add(null, "second", null);
        dictionary["first", null] = "value";

        var found = dictionary.ContainsKey(null, "second");
        var nullValue = dictionary[null, "second"];
        var otherValue = dictionary["first", null];
        var removed = dictionary.Remove(null, "second");

        Assert.True(found);
        Assert.Null(nullValue);
        Assert.Equal("value", otherValue);
        Assert.True(removed);
    }

    /// Keeps generic and nongeneric enumeration, LINQ inference and Boolean removal source-compatible.
    [Fact]
    public void RetainedEnumeratorAndRemoveTest() {
        var dictionary = new DoubleKeyDictionary<string, int, string>();
        dictionary.Add("a", 1, "one");
        IEnumerator<KeyValuePair<(string, int), string>> enumerator = dictionary.GetEnumerator();
        using (enumerator) {
            var moved = enumerator.MoveNext();
            Assert.True(moved);
            Assert.Equal(("a", 1), enumerator.Current.Key);
            Assert.Equal("one", enumerator.Current.Value);
        }

        var projected = dictionary.Select(entry => entry.Key).ToArray();
        var count = dictionary.Count();
        var nongeneric = ((IEnumerable)dictionary).Cast<object>().ToArray();
        bool removed = dictionary.Remove("a", 1);
        bool absent = dictionary.Remove("a", 1);

        Assert.Equal(new[] { ("a", 1) }, projected);
        Assert.Equal(1, count);
        Assert.IsType<KeyValuePair<(string, int), string>>(nongeneric[0]);
        Assert.True(removed);
        Assert.False(absent);
    }
}
