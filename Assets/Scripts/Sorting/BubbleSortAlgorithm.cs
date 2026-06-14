using System.Collections.Generic;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 冒泡排序
    /// </summary>
    public sealed class BubbleSortAlgorithm : ISortAlgorithm
    {
        public SortAlgorithmType Type => SortAlgorithmType.Bubble;
        public string DisplayName => "冒泡排序";

        public IEnumerable<SortOperation> CreateOperations(IReadOnlyList<float> values)
        {
            float[] workingValues = CopyValues(values);

            for (int end = workingValues.Length - 1; end > 0; end--)
            {
                for (int index = 0; index < end; index++)
                {
                    if (workingValues[index] <= workingValues[index + 1])
                    {
                        yield return SortOperation.Compare(index, index + 1); // 比较
                        continue;
                    }

                    Swap(workingValues, index, index + 1);

                    yield return SortOperation.Swap(index, index + 1); // 比较后需要交换时，交换本身就是这一单位操作
                }

                // 每一轮冒泡结束后，end 位置已经是当前未排序区间中的最大值，后续不会再交换。
                yield return SortOperation.MarkSorted(end);
            }

            if (workingValues.Length > 0)
            {
                yield return SortOperation.MarkSorted(0);
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
            for (int i = 0; i < values.Count; i++) copy[i] = values[i];

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
