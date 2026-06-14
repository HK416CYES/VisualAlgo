using System.Collections.Generic;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 快速排序。每次分区完成后，枢轴所在位置已经最终确定，可以立即标记为有序。
    /// </summary>
    public sealed class QuickSortAlgorithm : ISortAlgorithm
    {
        public SortAlgorithmType Type => SortAlgorithmType.Quick;
        public string DisplayName => "快速排序";

        public IEnumerable<SortOperation> CreateOperations(IReadOnlyList<float> values)
        {
            float[] workingValues = CopyValues(values);
            foreach (SortOperation operation in QuickSort(workingValues, 0, workingValues.Length - 1))
                yield return operation;
        }

        private static IEnumerable<SortOperation> QuickSort(float[] values, int left, int right)
        {
            if (left > right) yield break;

            if (left == right)
            {
                yield return SortOperation.MarkSorted(left);
                yield break;
            }

            int pivotIndex = right;
            float pivotValue = values[pivotIndex];
            int storeIndex = left;

            yield return SortOperation.Pivot(pivotIndex);

            for (int scan = left; scan < right; scan++)
            {
                if (values[scan] >= pivotValue)
                {
                    yield return SortOperation.Compare(scan, pivotIndex);
                    continue;
                }

                if (scan != storeIndex)
                {
                    Swap(values, scan, storeIndex);
                    yield return SortOperation.Swap(scan, storeIndex);
                }
                else
                {
                    yield return SortOperation.Compare(scan, pivotIndex);
                }

                storeIndex++;
            }

            if (storeIndex != pivotIndex)
            {
                Swap(values, storeIndex, pivotIndex);
                yield return SortOperation.Swap(storeIndex, pivotIndex);
            }

            yield return SortOperation.MarkSorted(storeIndex);

            foreach (SortOperation operation in QuickSort(values, left, storeIndex - 1))
                yield return operation;

            foreach (SortOperation operation in QuickSort(values, storeIndex + 1, right))
                yield return operation;
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
