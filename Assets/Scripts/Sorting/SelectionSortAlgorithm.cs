using System.Collections.Generic;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 选择排序
    /// </summary>
    public sealed class SelectionSortAlgorithm : ISortAlgorithm
    {
        public SortAlgorithmType Type => SortAlgorithmType.Selection;
        public string DisplayName => "选择排序";

        public IEnumerable<SortOperation> CreateOperations(IReadOnlyList<float> values)
        {
            float[] workingValues = CopyValues(values);

            for (int start = 0; start < workingValues.Length - 1; start++)
            {
                int minIndex = start;
                for (int candidate = start + 1; candidate < workingValues.Length; candidate++)
                {
                    yield return SortOperation.Compare(minIndex, candidate); // 比较

                    if (workingValues[candidate] < workingValues[minIndex]) minIndex = candidate;
                }

                if (minIndex != start)
                {
                    Swap(workingValues, start, minIndex);
                    yield return SortOperation.Swap(start, minIndex); // 交换
                }

                // 选择排序每轮确定 start 位置的最小值，后续不会再移动该位置。
                yield return SortOperation.MarkSorted(start);
            }

            if (workingValues.Length > 0)
            {
                yield return SortOperation.MarkSorted(workingValues.Length - 1);
            }
        }

        /// <summary>
        /// 复制给定的值列表到一个新的数组中。
        /// </summary>
        /// <param name="values">要复制的原始值列表。</param>
        /// <returns>复制出的新数组。</returns>
        private static float[] CopyValues(IReadOnlyList<float> values)
        {
            float[] copy = new float[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                copy[i] = values[i];
            }

            return copy;
        }

        /// <summary>
        /// 交换数组中指定索引处的两个元素。
        /// </summary>
        /// <param name="values">包含要交换元素的数组。</param>
        /// <param name="firstIndex">第一个元素的索引。</param>
        /// <param name="secondIndex">第二个元素的索引。</param>
        private static void Swap(float[] values, int firstIndex, int secondIndex)
        {
            (values[firstIndex], values[secondIndex]) = (values[secondIndex], values[firstIndex]);
        }
    }
}
