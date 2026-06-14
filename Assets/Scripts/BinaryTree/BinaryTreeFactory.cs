using UnityEngine;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 负责实例化二叉树对象的工厂。
    /// </summary>
    public sealed class BinaryTreeFactory : MonoBehaviour
    {
        /// <summary>
        /// 二叉树预制体。
        /// </summary>
        [SerializeField] private BinaryTreeController treePrefab;

        /// <summary>
        /// 所有树实例的父节点。
        /// </summary>
        [SerializeField] private Transform treesRoot;

        /// <summary>
        /// 配置工厂依赖。
        /// </summary>
        /// <param name="prefab">树预制体。</param>
        /// <param name="root">树父节点。</param>
        public void Configure(BinaryTreeController prefab, Transform root)
        {
            treePrefab = prefab;
            treesRoot = root;
        }

        /// <summary>
        /// 创建一棵新的二叉树实例。
        /// </summary>
        /// <param name="treeName">树对象名称。</param>
        /// <returns>新建树控制器。</returns>
        public BinaryTreeController CreateTree(string treeName)
        {
            if (treePrefab == null || treesRoot == null) return null;
            BinaryTreeController instance = Instantiate(treePrefab, treesRoot);
            instance.name = treeName;
            return instance;
        }
    }
}
