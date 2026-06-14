using System.Collections;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 二叉树策略工厂。
    /// </summary>
    internal static class BinaryTreeStrategyFactory
    {
        /// <summary>
        /// 创建指定模式对应的策略。
        /// </summary>
        /// <param name="mode">树模式。</param>
        /// <returns>策略实例。</returns>
        public static IBinaryTreeStrategy Create(BinaryTreeImplementationType mode)
        {
            return mode switch
            {
                BinaryTreeImplementationType.BalancedBinarySearchTree => new AvlBinaryTreeStrategy(),
                BinaryTreeImplementationType.RedBlackTree => new RedBlackBinaryTreeStrategy(),
                _ => new NaiveBinaryTreeStrategy()
            };
        }
    }

    /// <summary>
    /// 朴素 BST 策略。
    /// </summary>
    internal sealed class NaiveBinaryTreeStrategy : IBinaryTreeStrategy
    {
        public IEnumerator RunInsert(BinaryTreeController controller, int value, float stepInterval)
        {
            controller.InsertValueAsBst(value, false);
            yield return controller.AnimateLayout(stepInterval);
        }

        public IEnumerator RunDelete(BinaryTreeController controller, BinaryTreeNode targetNode, float stepInterval)
        {
            BinaryTreeNode fixNode;
            BinaryTreeNode fixParent;
            bool removedWasRed;
            controller.DeleteBstNode(targetNode, out fixNode, out fixParent, out removedWasRed);
            yield return controller.AnimateLayout(stepInterval);
        }
    }

    /// <summary>
    /// AVL 策略。
    /// </summary>
    internal sealed class AvlBinaryTreeStrategy : IBinaryTreeStrategy
    {
        public IEnumerator RunInsert(BinaryTreeController controller, int value, float stepInterval)
        {
            BinaryTreeNode insertedNode = controller.InsertValueAsBst(value, false);
            yield return controller.AnimateLayout(stepInterval);
            if (controller.IsBusy) yield return controller.AnimateAvlRebalance(insertedNode?.Parent, stepInterval);
        }

        public IEnumerator RunDelete(BinaryTreeController controller, BinaryTreeNode targetNode, float stepInterval)
        {
            BinaryTreeNode fixNode;
            BinaryTreeNode fixParent;
            bool removedWasRed;
            BinaryTreeNode rebalanceStart = controller.DeleteBstNode(targetNode, out fixNode, out fixParent, out removedWasRed);
            yield return controller.AnimateLayout(stepInterval);
            yield return controller.AnimateAvlRebalance(rebalanceStart, stepInterval);
        }
    }

    /// <summary>
    /// 红黑树策略。
    /// </summary>
    internal sealed class RedBlackBinaryTreeStrategy : IBinaryTreeStrategy
    {
        public IEnumerator RunInsert(BinaryTreeController controller, int value, float stepInterval)
        {
            BinaryTreeNode insertedNode = controller.InsertValueAsBst(value, true);
            yield return controller.AnimateLayout(stepInterval);
            yield return controller.AnimateRedBlackInsertFixup(insertedNode, stepInterval);
        }

        public IEnumerator RunDelete(BinaryTreeController controller, BinaryTreeNode targetNode, float stepInterval)
        {
            yield return controller.AnimateRedBlackDelete(targetNode, stepInterval);
        }
    }
}
