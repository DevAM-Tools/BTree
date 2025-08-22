using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BTree.Test;

public class RemoveTest
{
    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task Remove(ushort degree, int count, bool reverseOrder, bool randomOrder)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        Ref<int>[] removeItems = items.ToArray();
        removeItems.Shuffle();

        BTree<Ref<int>> tree = new(degree);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        int currentExpectedCount = count;

        foreach (Ref<int> item in removeItems)
        {
            bool removeResult = tree.Remove(item, out Ref<int> existingItem);

            await Assert.That(removeResult).IsTrue();

            await Assert.That(existingItem).IsEqualTo(item);

            currentExpectedCount--;
            await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);

            tree.InsertOrUpdate(item);

            bool getResult = tree.Get(item, out Ref<int> existingValue);

            await Assert.That(getResult).IsTrue();

            await Assert.That(existingValue).IsEqualTo(item);

            removeResult = tree.Remove(item, out existingItem);

            await Assert.That(removeResult).IsTrue();

            await Assert.That(existingItem).IsEqualTo(item);

            await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);

            getResult = tree.Get(item, out _);

            await Assert.That(getResult).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task RemoveMax(ushort degree, int count, bool reverseOrder, bool randomOrder)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        Ref<int>[] removeItems = items.ToArray();
        removeItems.Shuffle();

        BTree<Ref<int>> tree = new(degree);

        int currentExpectedCount = 0;
        await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);

            currentExpectedCount++;

            await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        currentExpectedCount = count;

        while (tree.Count > 0)
        {
            currentExpectedCount--;

            bool removeResult = tree.RemoveMax(out Ref<int> maxItem);
            await Assert.That(removeResult).IsTrue();
            await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);
            await Assert.That(maxItem.Value).IsEqualTo(currentExpectedCount);

            bool getResult = tree.Get(maxItem, out Ref<int> _);

            await Assert.That(getResult).IsFalse();
        }
    }

    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task RemoveMin(ushort degree, int count, bool reverseOrder, bool randomOrder)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        Ref<int>[] removeItems = items.ToArray();
        removeItems.Shuffle();

        BTree<Ref<int>> tree = new(degree);

        int currentExpectedCount = 0;
        await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);

        foreach (Ref<int> item in items)
        {
            tree.InsertOrUpdate(item);

            currentExpectedCount++;

            await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        int currentMin = 0;
        currentExpectedCount = count;

        while (tree.Count > 0)
        {
            currentExpectedCount--;

            bool removeResult = tree.RemoveMin(out Ref<int> minItem);
            await Assert.That(removeResult).IsTrue();
            await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);
            await Assert.That(minItem.Value).IsEqualTo(currentMin);

            bool getResult = tree.Get(minItem, out Ref<int> _);

            await Assert.That(getResult).IsFalse();

            currentMin++;
        }
    }

    public static IEnumerable<object[]> GetTestCases()
    {
        ushort[] degrees = [3, 4, 5, 6];
        int[] counts = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 90, 900];
        bool[] reverseOrders = [false, true];
        bool[] randomOrders = [false, true];

        foreach (ushort degree in degrees)
        {
            foreach (int count in counts)
            {
                foreach (bool reverseOrder in reverseOrders)
                {
                    foreach (bool randomOrder in randomOrders)
                    {
                        yield return new object[] { degree, count, reverseOrder, randomOrder };
                    }
                }
            }
        }
    }
}