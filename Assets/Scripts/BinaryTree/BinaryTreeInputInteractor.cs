using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 处理二叉树场景中的鼠标选择交互。
    /// </summary>
    public sealed class BinaryTreeInputInteractor : MonoBehaviour
    {
        /// <summary>
        /// 树总管理器。
        /// </summary>
        [SerializeField] private BinaryTreeComparisonManager comparisonManager;

        /// <summary>
        /// 当前正在拖动的树。
        /// </summary>
        private BinaryTreeController draggingTree;

        /// <summary>
        /// 拖动时鼠标与树中心的偏移量。
        /// </summary>
        private Vector3 dragOffset;

        /// <summary>
        /// 每帧轮询鼠标左键，处理树选择。
        /// </summary>
        private void Update()
        {
            Mouse mouse = Mouse.current;
            Camera mainCamera = Camera.main;
            if (mouse == null || mainCamera == null || comparisonManager == null) return;

            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(mouse.position.ReadValue());
            worldPoint.z = 0f;
            if (mouse.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
                HandlePointerPressed(worldPoint);
            }

            if (mouse.leftButton.isPressed) HandleDragging(worldPoint);
            if (mouse.leftButton.wasReleasedThisFrame) draggingTree = null;
        }

        /// <summary>
        /// 根据点击位置更新当前选中树，并准备拖动。
        /// </summary>
        /// <param name="worldPoint">鼠标世界坐标。</param>
        private void HandlePointerPressed(Vector3 worldPoint)
        {
            for (int i = comparisonManager.Trees.Count - 1; i >= 0; i--)
            {
                BinaryTreeController tree = comparisonManager.Trees[i];
                if (tree == null) continue;

                bool hitNode = tree.ContainsNodePoint(worldPoint);
                if (hitNode)
                {
                    comparisonManager.SelectTree(tree);
                    draggingTree = tree;
                    dragOffset = tree.transform.position - worldPoint;
                    return;
                }
            }

            draggingTree = null;
            comparisonManager.DeselectTree();
        }

        /// <summary>
        /// 在左键按住时拖动当前树。
        /// </summary>
        /// <param name="worldPoint">鼠标世界坐标。</param>
        private void HandleDragging(Vector3 worldPoint)
        {
            if (draggingTree == null) return;
            draggingTree.transform.position = new Vector3(worldPoint.x + dragOffset.x, worldPoint.y + dragOffset.y, draggingTree.transform.position.z);
            draggingTree.RefreshAfterExternalMove();
        }
    }
}
