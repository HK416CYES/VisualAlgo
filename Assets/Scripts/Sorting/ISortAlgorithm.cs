using System.Collections.Generic;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 排序算法的接口定义。它在生成操作步骤时，无需了解任何与Unity场景对象相关的信息。
    /// </summary>
    public interface ISortAlgorithm
    {
        /// <summary>
        /// 当前算法的类型。
        /// </summary>
        SortAlgorithmType Type { get; }

        /// <summary>
        /// 算法在UI上显示的名称。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 基于输入的数据生成排序操作步骤的序列。
        /// </summary>
        /// <param name="values">需要排序的初始数值列表</param>
        /// <returns>排序操作的枚举序列</returns>
        IEnumerable<SortOperation> CreateOperations(IReadOnlyList<float> values);
    }
}
