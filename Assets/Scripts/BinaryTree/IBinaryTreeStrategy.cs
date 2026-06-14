using System.Collections;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 二叉树行为策略接口。
    /// </summary>
    internal interface IBinaryTreeStrategy
    {
        /// <summary>
        /// 执行一次插入流程。
        /// </summary>
        /// <param name="controller">树控制器。</param>
        /// <param name="value">待插入值。</param>
        /// <param name="stepInterval">操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        IEnumerator RunInsert(BinaryTreeController controller, int value, float stepInterval);

        /// <summary>
        /// 执行一次删除流程。
        /// </summary>
        /// <param name="controller">树控制器。</param>
        /// <param name="targetNode">待删除节点。</param>
        /// <param name="stepInterval">操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        IEnumerator RunDelete(BinaryTreeController controller, BinaryTreeNode targetNode, float stepInterval);
    }
}
