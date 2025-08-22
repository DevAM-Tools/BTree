using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTree.Test;

namespace BTree.BPlusTree.Test;

public class InsertTest
{
    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task Insert(ushort degree, int count, bool reverseOrder, bool randomOrder, bool withUpdate)
    {
        Ref<int>[] items = Enumerable.Range(0, count).Select(x => new Ref<int>(x)).ToArray();
        items = reverseOrder ? items.Reverse().ToArray() : items;
        if (randomOrder)
        {
            items.Shuffle();
        }
        BPlusTree<Ref<int>, Ref<int>> tree = new(degree);

        int currentExpectedCount = 0;
        await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);

        foreach (Ref<int> item in items)
        {
            bool updated = tree.InsertOrUpdate(item, item);

            await Assert.That(updated).IsFalse();

            currentExpectedCount++;

            await Assert.That(tree.Count).IsEqualTo(currentExpectedCount);
        }

        await Assert.That(tree.Count).IsEqualTo(count);

        items.Shuffle();

        foreach (Ref<int> item in items)
        {
            bool getResult = tree.Contains(item);

            await Assert.That(getResult).IsTrue();
        }

        if (withUpdate)
        {
            items.Shuffle();

            foreach (Ref<int> item in items)
            {
                bool updated = tree.InsertOrUpdate(item, item);

                await Assert.That(updated).IsTrue();

                await Assert.That(tree.Count).IsEqualTo(count);
            }

            await Assert.That(tree.Count).IsEqualTo(count);

            items.Shuffle();

            foreach (Ref<int> item in items)
            {
                bool getResult = tree.Contains(item);

                await Assert.That(getResult).IsTrue();
            }
        }
    }

    public static IEnumerable<object[]> GetTestCases()
    {
        ushort[] degrees = [3, 4, 5, 6];
        int[] counts = [3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 90, 900];
        bool[] reverseOrders = [false, true];
        bool[] randomOrders = [false, true];
        bool[] withUpdates = [false, true];

        foreach (ushort degree in degrees)
        {
            foreach (int count in counts)
            {
                foreach (bool reverseOrder in reverseOrders)
                {
                    foreach (bool randomOrder in randomOrders)
                    {
                        foreach (bool withUpdate in withUpdates)
                        {
                            yield return new object[] { degree, count, reverseOrder, randomOrder, withUpdate };
                        }
                    }
                }
            }
        }
    }
}