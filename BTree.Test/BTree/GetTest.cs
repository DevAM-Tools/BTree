using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BTree.Test;

public class GetTest
{
    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task Get(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BTree<Ref<int>> tree = new(degree);

        Ref<int> currentMinItem = null;
        Ref<int> currentMaxItem = null;

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);

            currentMinItem = currentMinItem == null || item < currentMinItem ? item : currentMinItem;
            bool getMinResult = tree.GetMin(out Ref<int> retrievedMinItem);

            await Assert.That(getMinResult).IsTrue();
            await Assert.That(retrievedMinItem).IsEqualTo(currentMinItem);

            currentMaxItem = currentMaxItem == null || item > currentMaxItem ? item : currentMaxItem;
            bool getMaxResult = tree.GetMax(out Ref<int> retrievedMaxItem);

            await Assert.That(getMaxResult).IsTrue();
            await Assert.That(retrievedMaxItem).IsEqualTo(currentMaxItem);
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
        BTree<Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);
        }

        Option<Ref<int>> lowerLimit = minItem.HasValue ? new(true, new Ref<int>(minItem.Value)) : default;
        Option<Ref<int>> upperLimit = maxItem.HasValue ? new(true, new Ref<int>(maxItem.Value)) : default;

        Ref<int>[] expectedSequence = orderedItems;
        Ref<int>[] actualSequence = tree.GetAll().ToArray();
        await Assert.That(actualSequence).IsEquivalentTo(expectedSequence);

        List<Ref<int>> actualList = [];
        bool canceled = tree.DoForEach<Ref<int>>(item =>
        {
            actualList.Add(item);
            return false;
        });
        await Assert.That(actualList).IsEquivalentTo(expectedSequence);
        await Assert.That(canceled).IsFalse();

        // inclusive max
        Ref<int>[] expectedRangeSequence = orderedItems
            .Where(item => (!minItem.HasValue || item >= minItem.Value) && (!maxItem.HasValue || item <= maxItem.Value))
            .ToArray();
        Ref<int>[] actualRangeSequence = tree.GetRange(lowerLimit, upperLimit, true).ToArray();
        await Assert.That(actualRangeSequence).IsEquivalentTo(expectedRangeSequence);

        List<Ref<int>> actualRangeList = [];
        canceled = tree.DoForEach(item =>
        {
            actualRangeList.Add(item);
            return false;
        }, lowerLimit, upperLimit, true);
        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsFalse();

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        actualRangeList = [];
        canceled = tree.DoForEach(item =>
        {
            actualRangeList.Add(item);
            return true;
        }, lowerLimit, upperLimit, true);

        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsEqualTo(expectedRangeSequence.Length > 0);

        // exclusive max
        expectedRangeSequence = orderedItems
            .Where(item => (!minItem.HasValue || item >= minItem.Value) && (!maxItem.HasValue || item < maxItem.Value))
            .ToArray();
        actualRangeSequence = tree.GetRange(lowerLimit, upperLimit, false).ToArray();
        await Assert.That(actualRangeSequence).IsEquivalentTo(expectedRangeSequence);

        actualRangeList = [];
        canceled = tree.DoForEach(item =>
        {
            actualRangeList.Add(item);
            return false;
        }, lowerLimit, upperLimit, false);
        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsFalse();

        expectedRangeSequence = expectedRangeSequence.Take(1).ToArray();
        actualRangeList = [];
        canceled = tree.DoForEach(item =>
        {
            actualRangeList.Add(item);
            return true;
        }, lowerLimit, upperLimit, false);

        await Assert.That(actualRangeList).IsEquivalentTo(expectedRangeSequence);
        await Assert.That(canceled).IsEqualTo(expectedRangeSequence.Length > 0);
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task GetNearest(ushort degree, int count, int? minItem, int? maxItem)
    {
        Ref<int>[] orderedItems = Enumerable.Range(1, count).Select(x => new Ref<int>(x * 10)).ToArray();
        Ref<int>[] items = orderedItems.ToArray();
        items.Shuffle();
        BTree<Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);
        }

        foreach (Ref<int> item in items)
        {
            BTree<Ref<int>>.NearestItems nearestItems = tree.GetNearest(item);
            await Assert.That(nearestItems.LowerHasValue).IsFalse();
            await Assert.That(nearestItems.UpperHasValue).IsFalse();
            await Assert.That(nearestItems.MatchHasValue).IsTrue();
            await Assert.That(nearestItems.MatchItem).IsEqualTo(item);
        }

        Ref<int>[] keys = Enumerable.Range(0, 10 * (count + 1) + 1).Select(x => new Ref<int>(x)).ToArray();
        keys.Shuffle();

        foreach (Ref<int> key in keys)
        {
            BTree<Ref<int>>.NearestItems nearestItems = tree.GetNearest(key);

            if (key.CompareTo(10) < 0) // only upper
            {
                await Assert.That(nearestItems.MatchHasValue).IsFalse();
                await Assert.That(nearestItems.LowerHasValue).IsFalse();
                await Assert.That(nearestItems.UpperHasValue).IsTrue();
                await Assert.That(nearestItems.UpperItem).IsEqualTo(new Ref<int>(10));
            }
            else if (key.CompareTo(count * 10) > 0) // only lower
            {
                await Assert.That(nearestItems.MatchHasValue).IsFalse();
                await Assert.That(nearestItems.LowerHasValue).IsTrue();
                await Assert.That(nearestItems.LowerItem).IsEqualTo(new Ref<int>(count * 10));
                await Assert.That(nearestItems.UpperHasValue).IsFalse();
            }
            else if (key.Value % 10 == 0) // match
            {
                await Assert.That(nearestItems.LowerHasValue).IsFalse();
                await Assert.That(nearestItems.UpperHasValue).IsFalse();
                await Assert.That(nearestItems.MatchHasValue).IsTrue();
                await Assert.That(nearestItems.MatchItem).IsEqualTo(key);
            }
            else // between
            {
                Ref<int> expectedMinItem = (key / 10) * 10;
                Ref<int> expectedMaxItem = ((key / 10) + 1) * 10;

                await Assert.That(nearestItems.MatchHasValue).IsFalse();
                await Assert.That(nearestItems.LowerHasValue).IsTrue();
                await Assert.That(nearestItems.LowerItem).IsEqualTo(expectedMinItem);
                await Assert.That(nearestItems.UpperHasValue).IsTrue();
                await Assert.That(nearestItems.UpperItem).IsEqualTo(expectedMaxItem);
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