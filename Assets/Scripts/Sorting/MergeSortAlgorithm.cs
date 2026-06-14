using System.Collections.Generic;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 归并排序。合并阶段通过插入操作表现“选择块插入目标位置，后续块右移”的过程。
    /// </summary>
    public sealed class MergeSortAlgorithm : ISortAlgorithm
    {
        public SortAlgorithmType Type => SortAlgorithmType.Merge;
        public string DisplayName => "归并排序";

        public IEnumerable<SortOperation> CreateOperations(IReadOnlyList<float> values)
        {
            float[] workingValues = CopyValues(values);

            foreach (SortOperation operation in MergeSort(workingValues, 0, workingValues.Length - 1))
                yield return operation;
        }

        private static IEnumerable<SortOperation> MergeSort(float[] values, int left, int right)
        {
            if (left >= right) yield break;

            int middle = left + (right - left) / 2;
            foreach (SortOperation operation in MergeSort(values, left, middle))
                yield return operation;

            foreach (SortOperation operation in MergeSort(values, middle + 1, right))
                yield return operation;

            foreach (SortOperation operation in Merge(values, left, middle, right))
                yield return operation;
        }

        private static IEnumerable<SortOperation> Merge(float[] values, int left, int middle, int right)
        {
            int leftCursor = left;
            int rightCursor = middle + 1;

            while (leftCursor <= middle && rightCursor <= right)
            {
                if (values[leftCursor] <= values[rightCursor])
                {
                    yield return SortOperation.Compare(leftCursor, rightCursor);
                    leftCursor++;
                    continue;
                }

                Insert(values, rightCursor, leftCursor);
                yield return SortOperation.Insert(rightCursor, leftCursor);

                leftCursor++;
                middle++;
                rightCursor++;
            }
        }

        private static void Insert(float[] values, int fromIndex, int toIndex)
        {
            float insertedValue = values[fromIndex];
            for (int index = fromIndex; index > toIndex; index--)
                values[index] = values[index - 1];

            values[toIndex] = insertedValue;
        }

        private static float[] CopyValues(IReadOnlyList<float> values)
        {
            float[] copy = new float[values.Count];
            for (int i = 0; i < values.Count; i++) copy[i] = values[i];
            return copy;
        }
    }
}
