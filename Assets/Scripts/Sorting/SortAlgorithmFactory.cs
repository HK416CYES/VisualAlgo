using System.Collections.Generic;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 提供统一给UI调用的各种排序算法创建中心。
    /// </summary>
    public static class SortAlgorithmFactory
    {
        /// <summary>
        /// 实例化所有支持的排序算法以便在界面中列出和选用。
        /// </summary>
        /// <returns>包含所有排序算法实例的只读列表。</returns>
        public static IReadOnlyList<ISortAlgorithm> CreateAll()
        {
            return new ISortAlgorithm[]
            {
                new BubbleSortAlgorithm(),
                new SelectionSortAlgorithm(),
                new ShellSortAlgorithm(),
                new QuickSortAlgorithm(),
                new MergeSortAlgorithm()
            };
        }
    }
}
