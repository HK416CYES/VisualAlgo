namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 单个运行时二叉树节点数据。
    /// </summary>
    internal sealed class BinaryTreeNode
    {
        /// <summary>
        /// 节点值。
        /// </summary>
        public int Value;

        /// <summary>
        /// 父节点。
        /// </summary>
        public BinaryTreeNode Parent;

        /// <summary>
        /// 左子节点。
        /// </summary>
        public BinaryTreeNode Left;

        /// <summary>
        /// 右子节点。
        /// </summary>
        public BinaryTreeNode Right;

        /// <summary>
        /// 绑定的视图。
        /// </summary>
        public BinaryTreeNodeView View;

        /// <summary>
        /// 当前节点是否为空占位根节点。
        /// </summary>
        public bool IsPlaceholder;

        /// <summary>
        /// 当前节点是否为红色。
        /// </summary>
        public bool IsRed;

        /// <summary>
        /// 当前节点高度。
        /// </summary>
        public int Height;

        /// <summary>
        /// 构造一个新运行时节点。
        /// </summary>
        /// <param name="value">节点值。</param>
        /// <param name="view">节点视图。</param>
        /// <param name="isPlaceholder">是否为空占位节点。</param>
        public BinaryTreeNode(int value, BinaryTreeNodeView view, bool isPlaceholder)
        {
            Value = value;
            View = view;
            IsPlaceholder = isPlaceholder;
            IsRed = false;
            Height = 1;
        }
    }
}
