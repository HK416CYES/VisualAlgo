using System.Collections.Generic;
using UnityEngine;
using VisualAlgo.Managers;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 管理场景中全部二叉树实例与全局操作调度。
    /// </summary>
    public sealed class BinaryTreeComparisonManager : MonoBehaviour
    {
        /// <summary>
        /// 树实例工厂。
        /// </summary>
        [Header("核心依赖")][SerializeField] private BinaryTreeFactory treeFactory;

        /// <summary>
        /// 树预制体。
        /// </summary>
        [SerializeField] private BinaryTreeController treePrefab;

        /// <summary>
        /// 节点预制体。
        /// </summary>
        [SerializeField] private BinaryTreeNodeView nodePrefab;

        /// <summary>
        /// 所有树的父节点。
        /// </summary>
        [SerializeField] private Transform treesRoot;

        /// <summary>
        /// 树之间的垂直间距。
        /// </summary>
        [Header("布局配置")][SerializeField, Min(2f)] private float treeSpacing = 8f;

        /// <summary>
        /// 默认操作间隔。
        /// </summary>
        [Header("默认配置")][SerializeField, Min(0.05f)] private float stepInterval = 0.45f;

        /// <summary>
        /// 新建树时使用的默认模式。
        /// </summary>
        [SerializeField] private BinaryTreeImplementationType newTreeMode = BinaryTreeImplementationType.NaiveBinarySearchTree;

        /// <summary>
        /// 当前场景中的全部树实例。
        /// </summary>
        private readonly List<BinaryTreeController> trees = new();

        /// <summary>
        /// 当前选中的树。
        /// </summary>
        private BinaryTreeController selectedTree;

        /// <summary>
        /// 当前是否处于全局暂停状态。
        /// </summary>
        private bool paused;

        /// <summary>
        /// 当前运行中的全局同步操作协程。
        /// </summary>
        private Coroutine synchronizedOperationRoutine;

        /// <summary>
        /// 当选中树变化时触发。
        /// </summary>
        public event System.Action<BinaryTreeController> OnSelectedTreeChanged;

        /// <summary>
        /// 获取全部树实例。
        /// </summary>
        public IReadOnlyList<BinaryTreeController> Trees => trees;

        /// <summary>
        /// 获取当前选中的树。
        /// </summary>
        public BinaryTreeController SelectedTree => selectedTree;

        /// <summary>
        /// 获取当前全局操作间隔。
        /// </summary>
        public float StepInterval => stepInterval;

        /// <summary>
        /// 获取新建树默认模式。
        /// </summary>
        public BinaryTreeImplementationType NewTreeMode => newTreeMode;

        /// <summary>
        /// 获取是否存在任意忙碌树。
        /// </summary>
        public bool AnyTreeBusy
        {
            get
            {
                for (int i = 0; i < trees.Count; i++)
                {
                    if (trees[i] != null && trees[i].IsBusy) return true;
                }

                return false;
            }
        }

        /// <summary>
        /// 获取当前是否处于暂停状态。
        /// </summary>
        public bool IsPaused => paused;

        /// <summary>
        /// 在脚本启动时补全依赖并保证至少存在一棵树。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            treeFactory.Configure(treePrefab, treesRoot);
            CollectExistingTrees();
            EnsureAtLeastOneTree();
        }

        /// <summary>
        /// 设置全局操作间隔。
        /// </summary>
        /// <param name="interval">新的间隔。</param>
        public void SetStepInterval(float interval)
        {
            stepInterval = Mathf.Max(0.05f, interval);
        }

        /// <summary>
        /// 设置新建树默认模式。
        /// </summary>
        /// <param name="mode">新的模式。</param>
        public void SetNewTreeMode(BinaryTreeImplementationType mode)
        {
            newTreeMode = mode;
        }

        /// <summary>
        /// 新建一棵树并聚焦到该树。
        /// </summary>
        /// <returns>新建树控制器。</returns>
        public BinaryTreeController AddTree()
        {
            ResolveReferences();
            BinaryTreeController tree = treeFactory.CreateTree($"Binary Tree {trees.Count + 1}");
            if (tree == null) return null;

            tree.Configure(nodePrefab, trees.Count + 1, newTreeMode);
            trees.Add(tree);
            PositionNewTree(tree);
            SelectTree(tree);
            FocusCameraOnTree(tree);
            return tree;
        }

        /// <summary>
        /// 选中指定树。
        /// </summary>
        /// <param name="tree">目标树。</param>
        public void SelectTree(BinaryTreeController tree)
        {
            selectedTree = tree;
            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i] != null) trees[i].SetSelected(trees[i] == tree);
            }

            OnSelectedTreeChanged?.Invoke(selectedTree);
        }

        /// <summary>
        /// 取消当前树选中状态。
        /// </summary>
        public void DeselectTree()
        {
            SelectTree(null);
        }

        /// <summary>
        /// 对所有树执行相同操作。
        /// </summary>
        /// <param name="request">操作请求。</param>
        public void ExecuteOnAllTrees(BinaryTreeOperationRequest request)
        {
            StopSynchronizedRoutineIfRunning();
            synchronizedOperationRoutine = StartCoroutine(SynchronizedOperationRoutine(request));
        }

        /// <summary>
        /// 按给定列表顺序向所有树插入节点。
        /// </summary>
        /// <param name="insertStackInput">插入栈控件。</param>
        public void InsertListOnAllTrees(BinaryTreeInsertListInput insertStackInput)
        {
            if (insertStackInput == null || !insertStackInput.HasAnyInput()) return;
            StopSynchronizedRoutineIfRunning();
            paused = false;
            synchronizedOperationRoutine = StartCoroutine(SynchronizedInsertStackRoutine(insertStackInput));
        }

        /// <summary>
        /// 对当前选中树执行单独操作。
        /// </summary>
        /// <param name="request">操作请求。</param>
        public void ExecuteOnSelectedTree(BinaryTreeOperationRequest request)
        {
            if (selectedTree == null) return;
            selectedTree.ExecuteOperation(request, stepInterval);
        }

        /// <summary>
        /// 按给定列表顺序向当前选中树插入节点。
        /// </summary>
        /// <param name="insertStackInput">插入栈控件。</param>
        public void InsertListOnSelectedTree(BinaryTreeInsertListInput insertStackInput)
        {
            if (selectedTree == null || insertStackInput == null || !insertStackInput.HasAnyInput()) return;
            paused = false;
            StartCoroutine(InsertStackOnSingleTreeRoutine(selectedTree, insertStackInput));
        }

        /// <summary>
        /// 暂停全部树操作。
        /// </summary>
        public void PauseAllOperations()
        {
            if (!AnyTreeBusy) return;
            paused = true;
            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i] != null) trees[i].PauseOperation();
            }
        }

        /// <summary>
        /// 恢复全部树操作。
        /// </summary>
        public void ResumeAllOperations()
        {
            paused = false;
            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i] != null) trees[i].ResumeOperation();
            }
        }

        /// <summary>
        /// 停止全部树操作。
        /// </summary>
        public void StopAllOperations()
        {
            paused = false;
            StopSynchronizedRoutineIfRunning();
            StopAllCoroutines();
            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i] != null) trees[i].StopOperation();
            }
        }

        /// <summary>
        /// 删除当前选中的树。
        /// </summary>
        public void ClearSelectedTree()
        {
            if (selectedTree == null) return;
            RemoveTree(selectedTree);
        }

        /// <summary>
        /// 设置当前选中树的实现模式。
        /// </summary>
        /// <param name="mode">新的实现模式。</param>
        public void SetSelectedTreeMode(BinaryTreeImplementationType mode)
        {
            if (selectedTree == null) return;
            selectedTree.SetImplementationTypeAnimated(mode, stepInterval);
        }

        /// <summary>
        /// 收集场景中已有的树实例。
        /// </summary>
        private void CollectExistingTrees()
        {
            trees.Clear();
            if (treesRoot == null) return;

            BinaryTreeController[] existingTrees = treesRoot.GetComponentsInChildren<BinaryTreeController>(true);
            for (int i = 0; i < existingTrees.Length; i++)
            {
                BinaryTreeController tree = existingTrees[i];
                if (tree == null) continue;
                tree.Configure(nodePrefab, i + 1, tree.ImplementationType);
                trees.Add(tree);
            }
        }

        /// <summary>
        /// 保证场景中至少存在一棵树。
        /// </summary>
        private void EnsureAtLeastOneTree()
        {
            if (trees.Count == 0) AddTree();
            else SelectTree(trees[0]);
        }

        /// <summary>
        /// 为新建树计算默认位置。
        /// </summary>
        /// <param name="tree">新建树。</param>
        private void PositionNewTree(BinaryTreeController tree)
        {
            if (tree == null) return;
            if (trees.Count <= 1)
            {
                tree.transform.localPosition = Vector3.zero;
                return;
            }

            BinaryTreeController previousTree = trees[trees.Count - 2];
            float y = previousTree == null ? 0f : previousTree.transform.localPosition.y - treeSpacing;
            tree.transform.localPosition = new Vector3(0f, y, 0f);
        }

        /// <summary>
        /// 将主相机聚焦到目标树。
        /// </summary>
        /// <param name="tree">目标树。</param>
        private void FocusCameraOnTree(BinaryTreeController tree)
        {
            if (tree == null || Camera.main == null) return;
            CameraManager cameraManager = Camera.main.GetComponent<CameraManager>();
            if (cameraManager != null) cameraManager.FocusOn(tree.FocusPosition);
        }

        /// <summary>
        /// 补全管理器依赖引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (treeFactory == null) treeFactory = GetComponentInChildren<BinaryTreeFactory>();
            if (treesRoot == null) treesRoot = transform.Find("Trees Root");
        }

        /// <summary>
        /// 同步执行一次全局单步操作，并等待全部树完成。
        /// </summary>
        /// <param name="request">操作请求。</param>
        /// <returns>协程迭代器。</returns>
        private System.Collections.IEnumerator SynchronizedOperationRoutine(BinaryTreeOperationRequest request)
        {
            DispatchOperationToAllTrees(request);
            yield return WaitUntilAllTreesIdle();
            synchronizedOperationRoutine = null;
        }

        /// <summary>
        /// 按给定列表顺序同步向所有树插入节点。
        /// </summary>
        /// <param name="insertStackInput">插入栈控件。</param>
        /// <returns>协程迭代器。</returns>
        private System.Collections.IEnumerator SynchronizedInsertStackRoutine(BinaryTreeInsertListInput insertStackInput)
        {
            while (insertStackInput != null)
            {
                BinaryTreeInsertStackReadState readState = insertStackInput.PeekTopValue(out int value);
                if (readState == BinaryTreeInsertStackReadState.Empty) break;
                if (readState == BinaryTreeInsertStackReadState.Invalid)
                {
                    insertStackInput.RemoveTopInputField();
                    yield return null;
                    continue;
                }

                DispatchOperationToAllTrees(new BinaryTreeOperationRequest(BinaryTreeOperationType.Insert, value));
                yield return WaitUntilAllTreesIdle();
                if (insertStackInput != null) insertStackInput.RemoveTopInputField();
            }

            synchronizedOperationRoutine = null;
        }

        /// <summary>
        /// 按给定列表顺序向单棵树插入节点。
        /// </summary>
        /// <param name="tree">目标树。</param>
        /// <param name="insertStackInput">插入栈控件。</param>
        /// <returns>协程迭代器。</returns>
        private System.Collections.IEnumerator InsertStackOnSingleTreeRoutine(BinaryTreeController tree, BinaryTreeInsertListInput insertStackInput)
        {
            while (tree != null && insertStackInput != null)
            {
                BinaryTreeInsertStackReadState readState = insertStackInput.PeekTopValue(out int value);
                if (readState == BinaryTreeInsertStackReadState.Empty) yield break;
                if (readState == BinaryTreeInsertStackReadState.Invalid)
                {
                    insertStackInput.RemoveTopInputField();
                    yield return null;
                    continue;
                }

                tree.ExecuteOperation(new BinaryTreeOperationRequest(BinaryTreeOperationType.Insert, value), stepInterval);
                while (tree != null && tree.IsBusy) yield return null;
                if (tree != null && insertStackInput != null) insertStackInput.RemoveTopInputField();
            }
        }

        /// <summary>
        /// 对全部树同时派发一次操作。
        /// </summary>
        /// <param name="request">操作请求。</param>
        private void DispatchOperationToAllTrees(BinaryTreeOperationRequest request)
        {
            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i] != null) trees[i].ExecuteOperation(request, stepInterval);
            }
        }

        /// <summary>
        /// 等待全部树完成当前操作。
        /// </summary>
        /// <returns>协程迭代器。</returns>
        private System.Collections.IEnumerator WaitUntilAllTreesIdle()
        {
            while (AnyTreeBusy) yield return null;
        }

        /// <summary>
        /// 若存在旧的全局同步协程则先停止。
        /// </summary>
        private void StopSynchronizedRoutineIfRunning()
        {
            if (synchronizedOperationRoutine == null) return;
            StopCoroutine(synchronizedOperationRoutine);
            synchronizedOperationRoutine = null;
        }

        /// <summary>
        /// 从场景中移除指定树，并刷新剩余树编号与选中状态。
        /// </summary>
        /// <param name="tree">待移除树。</param>
        private void RemoveTree(BinaryTreeController tree)
        {
            if (tree == null) return;

            StopSynchronizedRoutineIfRunning();
            tree.StopOperation();

            int removedIndex = trees.IndexOf(tree);
            if (removedIndex < 0) return;

            trees.RemoveAt(removedIndex);
            if (selectedTree == tree) selectedTree = null;

            if (Application.isPlaying) Destroy(tree.gameObject);
            else DestroyImmediate(tree.gameObject);

            for (int i = 0; i < trees.Count; i++)
            {
                if (trees[i] != null) trees[i].SetTreeIndex(i + 1);
            }

            BinaryTreeController nextSelection = null;
            if (trees.Count > 0)
            {
                int nextIndex = Mathf.Clamp(removedIndex, 0, trees.Count - 1);
                nextSelection = trees[nextIndex];
            }

            SelectTree(nextSelection);
        }
    }
}
