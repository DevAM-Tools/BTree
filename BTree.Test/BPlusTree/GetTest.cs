using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTree.Test;

namespace BTree.BPlusTree.Test;

public class GetTest
{
    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task Get(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        Ref<int> currentMinItem = null;
        Ref<int> currentMaxItem = null;

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);

            currentMinItem = currentMinItem == null || item < currentMinItem ? item : currentMinItem;
            bool getMinResult = tree.GetMin(out KeyValuePair<Ref<int>, Ref<int>> retrievedMinItem);

            await Assert.That(getMinResult).IsTrue();
            await Assert.That(retrievedMinItem.Key).IsEqualTo(currentMinItem);
            await Assert.That(retrievedMinItem.Value).IsEqualTo(currentMinItem);

            currentMaxItem = currentMaxItem == null || item > currentMaxItem ? item : currentMaxItem;
            bool getMaxResult = tree.GetMax(out KeyValuePair<Ref<int>, Ref<int>> retrievedMaxItem);

            await Assert.That(getMaxResult).IsTrue();
            await Assert.That(retrievedMaxItem.Key).IsEqualTo(currentMaxItem);
            await Assert.That(retrievedMaxItem.Value).IsEqualTo(currentMaxItem);
        }

        foreach (Ref<int> item in items)
        {
            bool getResult = tree.Get(item, out Ref<int> existingItem);

            await Assert.That(getResult).IsTrue();
            await Assert.That(existingItem).IsEqualTo(item);
        }

        await Assert.That(tree.Count).IsEqualTo(count);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task GetRange(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);
        }

        Option<Ref<int>> lowerLimit = minItem.HasValue ? new(true, new Ref<int>(minItem.Value)) : default;
        Option<Ref<int>> upperLimit = maxItem.HasValue ? new(true, new Ref<int>(maxItem.Value)) : default;

        KeyValuePair<Ref<int>, Ref<int>>[] expectedSequence = orderedItems.Select(x => new KeyValuePair<Ref<int>, Ref<int>>(x, x)).ToArray();
        KeyValuePair<Ref<int>, Ref<int>>[] actualSequence = tree.GetAll().ToArray();
        await Assert.That(actualSequence).IsEquivalentTo(expectedSequence);

        List<KeyValuePair<Ref<int>, Ref<int>>> actualList = [];
        bool canceled = tree.DoForEach((key, item) =>
        {
            actualList.Add(new KeyValuePair<Ref<int>, Ref<int>>(key, item));
            return false;
        });
        await Assert.That(actualList).IsEquivalentTo(expectedSequence);
        await Assert.That(canceled).IsFalse();

        // inclusive max
        KeyValuePair<Ref<int>, Ref<int>>[] expectedRangeSequence = orderedItems
            .Where(item => (!minItem.HasValue || item >= minItem.Value) && (!maxItem.HasValue || item <= maxItem.Value))
            .Select(x => new KeyValuePair<Ref<int>, Ref<int>>(x, x))
            .ToArray();
        KeyValuePair<Ref<int>, Ref<int>>[] actualRangeSequence = tree.GetRange(lowerLimit, upperLimit, true).ToArray();
        await Assert.That(actualRangeSequence).IsEquivalentTo(expectedRangeSequence);

        List<KeyValuePair<Ref<int>, Ref<int>>> actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new(key, item));
            return false;
        }, lowerLimit, upperLimit, true);
        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsFalse();

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new KeyValuePair<Ref<int>, Ref<int>>(key, item));
            return true;
        }, lowerLimit, upperLimit, true);

        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsEqualTo(expectedRangeSequence.Length > 0 ? true : false);

        // exclusive max
        expectedRangeSequence = orderedItems
            .Where(item => (!minItem.HasValue || item >= minItem.Value) && (!maxItem.HasValue || item < maxItem.Value))
            .Select(x => new KeyValuePair<Ref<int>, Ref<int>>(x, x))
            .ToArray();
        actualRangeSequence = tree.GetRange(lowerLimit, upperLimit, false).ToArray();
        await Assert.That(actualRangeSequence).IsEquivalentTo(expectedRangeSequence);

        actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new(key, item));
            return false;
        }, lowerLimit, upperLimit, false);
        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsFalse();

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        actualRangeList = [];
        canceled = tree.DoForEach((key, item) =>
        {
            actualRangeList.Add(new(key, item));
            return true;
        }, lowerLimit, upperLimit, false);

        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsEqualTo(expectedRangeSequence.Length > 0 ? true : false);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task GetNearest(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(1, count).Select(x => new Ref<int>(x * 10)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item, item);
        }

        foreach (Ref<int> item in items)
        {
            BPlusTree<Ref<int>, Ref<int>>.NearestItems nearestItems = tree.GetNearest(item);
            await Assert.That(nearestItems.Lower.HasValue).IsFalse();
            await Assert.That(nearestItems.Upper.HasValue).IsFalse();
            await Assert.That(nearestItems.Match.HasValue).IsTrue();
            await Assert.That(nearestItems.Match.Value.Key).IsEqualTo(item);
            await Assert.That(nearestItems.Match.Value.Value).IsEqualTo(item);
        }

        Ref<int>[] keys = Enumerable.Range(0, 10 * (count + 1) + 1).Select(x => new Ref<int>(x)).ToArray();
        keys.Shuffle();

        foreach (Ref<int> key in keys)
        {
            BPlusTree<Ref<int>, Ref<int>>.NearestItems nearestItems = tree.GetNearest(key);

            if (key.CompareTo(10) < 0) // only upper
            {
                await Assert.That(nearestItems.Match.HasValue).IsFalse();
                await Assert.That(nearestItems.Lower.HasValue).IsFalse();
                await Assert.That(nearestItems.Upper.HasValue).IsTrue();
                await Assert.That(nearestItems.Upper.Value.Key).IsEqualTo(new Ref<int>(10));
                await Assert.That(nearestItems.Upper.Value.Value).IsEqualTo(new Ref<int>(10));
            }
            else if (key.CompareTo(count * 10) > 0) // only lower
            {
                await Assert.That(nearestItems.Match.HasValue).IsFalse();
                await Assert.That(nearestItems.Lower.HasValue).IsTrue();
                await Assert.That(nearestItems.Lower.Value.Key).IsEqualTo(new Ref<int>(count * 10));
                await Assert.That(nearestItems.Lower.Value.Value).IsEqualTo(new Ref<int>(count * 10));
                await Assert.That(nearestItems.Upper.HasValue).IsFalse();
            }
            else if (key.Value % 10 == 0) // match
            {
                await Assert.That(nearestItems.Lower.HasValue).IsFalse();
                await Assert.That(nearestItems.Upper.HasValue).IsFalse();
                await Assert.That(nearestItems.Match.HasValue).IsTrue();
                await Assert.That(nearestItems.Match.Value.Key).IsEqualTo(key);
                await Assert.That(nearestItems.Match.Value.Value).IsEqualTo(key);
            }
            else // between
            {
                Ref<int> expectedMinItem = (key / 10) * 10;
                Ref<int> expectedMaxItem = ((key / 10) + 1) * 10;

                await Assert.That(nearestItems.Match.HasValue).IsFalse();
                await Assert.That(nearestItems.Lower.HasValue).IsTrue();
                await Assert.That(nearestItems.Lower.Value.Key).IsEqualTo(expectedMinItem);
                await Assert.That(nearestItems.Lower.Value.Value).IsEqualTo(expectedMinItem);
                await Assert.That(nearestItems.Upper.HasValue).IsTrue();
                await Assert.That(nearestItems.Upper.Value.Key).IsEqualTo(expectedMaxItem);
                await Assert.That(nearestItems.Upper.Value.Value).IsEqualTo(expectedMaxItem);
            }

        }
    }

    public static IEnumerable<object[]> GetTestCases()
    {
        ushort[] degrees = [3, 4, 5, 6];
        int[] counts = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 90, 900];

        foreach (ushort degree in degrees)
        {
            foreach (int count in counts)
            {
                int?[] minKeys = [null, 0, count, count / 2, count / 4];
                int?[] maxKeys = [null, 0, count, count / 2, count / 4];

                foreach (int? minKey in minKeys)
                {
                    foreach (int? maxKey in maxKeys)
                    {
                        yield return new object[] { degree, count, minKey, maxKey };
                    }
                }
            }
        }
    }
}