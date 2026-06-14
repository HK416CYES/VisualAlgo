using System.Collections.Generic;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 希尔排序。由于间隔插入过程中元素仍可能被后续 gap 调整移动，因此不在过程中逐个标绿。
    /// </summary>
    public sealed class ShellSortAlgorithm : ISortAlgorithm
    {
        public SortAlgorithmType Type => SortAlgorithmType.Shell;
        public string DisplayName => "希尔排序";

        public IEnumerable<SortOperation> CreateOperations(IReadOnlyList<float> values)
        {
            float[] workingValues = CopyValues(values);

            for (int gap = workingValues.Length / 2; gap > 0; gap /= 2)
            {
                for (int i = gap; i < workingValues.Length; i++)
                {
                    for (int j = i; j >= gap; j -= gap)
                    {
                        if (workingValues[j - gap] <= workingValues[j])
                        {
                            yield return SortOperation.Compare(j - gap, j);
                            break;
                        }

                        Swap(workingValues, j - gap, j);
                        yield return SortOperation.Swap(j - gap, j);
                    }
                }
            }
        }

        private static float[] CopyValues(IReadOnlyList<float> values)
        {
            float[] copy = new float[values.Count];
            for (int i = 0; i < values.Count; i++) copy[i] = values[i];
            return copy;
        }

        private static void Swap(float[] values, int firstIndex, int secondIndex)
        {
            (values[firstIndex], values[secondIndex]) = (values[secondIndex], values[firstIndex]);
        }
    }
}
