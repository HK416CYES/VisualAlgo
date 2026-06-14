using System;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 二叉树实现模式。
    /// </summary>
    public enum BinaryTreeImplementationType
    {
        /// <summary>
        /// 朴素二叉搜索树。
        /// </summary>
        NaiveBinarySearchTree,

        /// <summary>
        /// 平衡二叉树。
        /// </summary>
        BalancedBinarySearchTree,

        /// <summary>
        /// 红黑树。
        /// </summary>
        RedBlackTree
    }

    /// <summary>
    /// 二叉树操作类型。
    /// </summary>
    public enum BinaryTreeOperationType
    {
        /// <summary>
        /// 查找节点。
        /// </summary>
        Search,

        /// <summary>
        /// 插入节点。
        /// </summary>
        Insert,

        /// <summary>
        /// 修改节点。
        /// </summary>
        Update,

        /// <summary>
        /// 删除节点。
        /// </summary>
        Delete
    }

    /// <summary>
    /// 单次树操作请求。
    /// </summary>
    [Serializable]
    public readonly struct BinaryTreeOperationRequest
    {
        /// <summary>
        /// 操作类型。
        /// </summary>
        public BinaryTreeOperationType OperationType { get; }

        /// <summary>
        /// 目标值。
        /// </summary>
        public int TargetValue { get; }

        /// <summary>
        /// 新值。
        /// </summary>
        public int NewValue { get; }

        /// <summary>
        /// 构造一次新的树操作请求。
        /// </summary>
        /// <param name="operationType">操作类型。</param>
        /// <param name="targetValue">目标值。</param>
        /// <param name="newValue">新值。</param>
        public BinaryTreeOperationRequest(BinaryTreeOperationType operationType, int targetValue, int newValue = 0)
        {
            OperationType = operationType;
            TargetValue = targetValue;
            NewValue = newValue;
        }
    }

    /// <summary>
    /// 二叉树模式辅助工具。
    /// </summary>
    public static class BinaryTreeModeUtility
    {
        /// <summary>
        /// 获取模式显示名称。
        /// </summary>
        /// <param name="mode">目标模式。</param>
        /// <returns>显示名称。</returns>
        public static string GetDisplayName(BinaryTreeImplementationType mode)
        {
            return mode switch
            {
                BinaryTreeImplementationType.BalancedBinarySearchTree => "平衡二叉树",
                BinaryTreeImplementationType.RedBlackTree => "红黑树",
                _ => "二叉搜索树"
            };
        }
    }
}
