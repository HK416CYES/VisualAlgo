using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 单棵二叉树的显示、操作与动画控制器。
    /// </summary>
    public sealed class BinaryTreeController : MonoBehaviour
    {
        /// <summary>
        /// 节点预制体。
        /// </summary>
        [Header("资源引用")][SerializeField] private BinaryTreeNodeView nodePrefab;

        /// <summary>
        /// 节点父节点。
        /// </summary>
        [SerializeField] private Transform nodesRoot;

        /// <summary>
        /// 连线父节点。
        /// </summary>
        [SerializeField] private Transform edgesRoot;

        /// <summary>
        /// 外框父节点。
        /// </summary>
        [SerializeField] private Transform borderRoot;

        /// <summary>
        /// 标题文本。
        /// </summary>
        [SerializeField] private TextMeshPro titleText;

        /// <summary>
        /// 连线材质。
        /// </summary>
        [SerializeField] private Material lineMaterial;

        /// <summary>
        /// 普通节点颜色。
        /// </summary>
        [Header("颜色配置")][SerializeField] private Color normalNodeColor = new(0.92f, 0.95f, 0.98f, 1f);

        /// <summary>
        /// 空根节点颜色。
        /// </summary>
        [SerializeField] private Color placeholderNodeColor = new(0.9f, 0.92f, 0.95f, 1f);

        /// <summary>
        /// 红黑树红节点颜色。
        /// </summary>
        [SerializeField] private Color redBlackRedNodeColor = new(0.84f, 0.2f, 0.24f, 1f);

        /// <summary>
        /// 红黑树黑节点颜色。
        /// </summary>
        [SerializeField] private Color redBlackBlackNodeColor = new(0.12f, 0.14f, 0.18f, 1f);

        /// <summary>
        /// 当前访问节点颜色。
        /// </summary>
        [SerializeField] private Color activeNodeColor = new(1f, 0.82f, 0.22f, 1f);

        /// <summary>
        /// 已访问节点颜色。
        /// </summary>
        [SerializeField] private Color visitedNodeColor = new(0.77f, 0.82f, 0.88f, 1f);

        /// <summary>
        /// 命中节点颜色。
        /// </summary>
        [SerializeField] private Color foundNodeColor = new(0.27f, 0.79f, 0.4f, 1f);

        /// <summary>
        /// 节点选中时的颜色。
        /// </summary>
        [SerializeField] private Color selectionColor = new(0.18f, 0.52f, 0.96f, 0.2784314f);

        /// <summary>
        /// 当前实现模式。
        /// </summary>
        [Header("树配置")][SerializeField] private BinaryTreeImplementationType implementationType = BinaryTreeImplementationType.NaiveBinarySearchTree;

        /// <summary>
        /// 节点水平间距。
        /// </summary>
        [SerializeField, Min(0.8f)] private float horizontalSpacing = 1.8f;

        /// <summary>
        /// 节点命中半径。
        /// </summary>
        [SerializeField, Min(0.2f)] private float nodeHitRadius = 0.62f;

        /// <summary>
        /// 层级垂直间距。
        /// </summary>
        [SerializeField, Min(0.8f)] private float verticalSpacing = 1.6f;

        /// <summary>
        /// 布局动画时长。
        /// </summary>
        [SerializeField, Min(0.05f)] private float layoutAnimationDuration = 0.35f;

        /// <summary>
        /// 外框留白。
        /// </summary>
        [SerializeField, Min(0.2f)] private float borderPadding = 0.7f;

        /// <summary>
        /// 树标题与顶部的额外距离。
        /// </summary>
        [SerializeField, Min(0.2f)] private float titleOffset = 0.75f;

        /// <summary>
        /// 空根节点的显示文本。
        /// </summary>
        [SerializeField] private string placeholderRootLabel = "Root";

        /// <summary>
        /// 当前根节点。
        /// </summary>
        private BinaryTreeNode rootNode;

        /// <summary>
        /// 当前树中所有节点的缓存集合。
        /// </summary>
        private readonly List<BinaryTreeNode> allNodes = new();

        /// <summary>
        /// 当前树的连线渲染器缓存。
        /// </summary>
        private readonly List<LineRenderer> edgeRenderers = new();

        /// <summary>
        /// 当前树的外框渲染器缓存。
        /// </summary>
        private readonly List<LineRenderer> borderRenderers = new();

        /// <summary>
        /// 当前被高亮为活动态的节点。
        /// </summary>
        private BinaryTreeNode activeNode;

        /// <summary>
        /// 当前已访问节点集合。
        /// </summary>
        private readonly HashSet<BinaryTreeNode> visitedNodes = new();

        /// <summary>
        /// 当前是否处于选中状态。
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// 当前树编号。
        /// </summary>
        private int treeIndex = 1;

        /// <summary>
        /// 当前运行中的操作协程。
        /// </summary>
        private Coroutine operationRoutine;

        /// <summary>
        /// 当前是否请求暂停。
        /// </summary>
        private bool pauseRequested;

        /// <summary>
        /// 当前是否请求停止。
        /// </summary>
        private bool stopRequested;

        /// <summary>
        /// 当前是否忙碌。
        /// </summary>
        private bool isBusy;

        /// <summary>
        /// 当前树行为策略。
        /// </summary>
        private IBinaryTreeStrategy treeStrategy;

        /// <summary>
        /// 标题相对根节点的局部偏移。
        /// </summary>
        private Vector3 titleLocalOffset;

        /// <summary>
        /// 是否已缓存标题相对根节点的局部偏移。
        /// </summary>
        private bool hasCachedTitleOffset;

        /// <summary>
        /// 获取树编号。
        /// </summary>
        public int TreeIndex => treeIndex;

        /// <summary>
        /// 获取实现模式。
        /// </summary>
        public BinaryTreeImplementationType ImplementationType => implementationType;

        /// <summary>
        /// 获取当前是否忙碌。
        /// </summary>
        public bool IsBusy => isBusy;

        /// <summary>
        /// 获取当前是否已暂停。
        /// </summary>
        public bool IsPaused => isBusy && pauseRequested;

        /// <summary>
        /// 获取当前树的世界中心点。
        /// </summary>
        public Vector3 FocusPosition => transform.position;

        /// <summary>
        /// 在脚本启用时补全引用。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            RefreshTitle();
            RefreshTreeBounds();
        }

        /// <summary>
        /// 初始化该树控制器。
        /// </summary>
        /// <param name="prefab">节点预制体。</param>
        /// <param name="index">树编号。</param>
        /// <param name="mode">树模式。</param>
        public void Configure(BinaryTreeNodeView prefab, int index, BinaryTreeImplementationType mode)
        {
            nodePrefab = prefab;
            treeIndex = Mathf.Max(1, index);
            implementationType = mode;
            ResolveReferences();
            ResetRuntimeTreeVisuals();
            treeStrategy = BinaryTreeStrategyFactory.Create(implementationType);
            EnsureDefaultRootNode();
            RefreshTitle();
            RefreshTreeBounds();
        }

        /// <summary>
        /// 设置树编号。
        /// </summary>
        /// <param name="index">新的树编号。</param>
        public void SetTreeIndex(int index)
        {
            treeIndex = Mathf.Max(1, index);
            RefreshTitle();
        }

        /// <summary>
        /// 设置实现模式。
        /// </summary>
        /// <param name="mode">新的实现模式。</param>
        public void SetImplementationType(BinaryTreeImplementationType mode)
        {
            implementationType = mode;
            treeStrategy = BinaryTreeStrategyFactory.Create(implementationType);
            RecomputeHeights(rootNode);
            ApplyBasePaletteToAllNodes();
            RefreshTitle();
        }

        /// <summary>
        /// 以淡出旧树、重建新树、淡入新树的方式切换实现模式。
        /// </summary>
        /// <param name="mode">目标实现模式。</param>
        /// <param name="stepInterval">渐变时长基准。</param>
        public void SetImplementationTypeAnimated(BinaryTreeImplementationType mode, float stepInterval)
        {
            if (isBusy || implementationType == mode) return;
            if (operationRoutine != null) StopCoroutine(operationRoutine);
            stopRequested = false;
            pauseRequested = false;
            operationRoutine = StartCoroutine(AnimateImplementationTypeTransition(mode, Mathf.Max(0.05f, stepInterval)));
        }

        /// <summary>
        /// 设置树的选中状态。
        /// </summary>
        /// <param name="selected">是否选中。</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            RefreshSelectionVisuals();
            RefreshTreeBounds();
        }

        /// <summary>
        /// 判断某个世界坐标是否命中了当前树的可选区域。
        /// </summary>
        /// <param name="worldPosition">世界坐标。</param>
        /// <returns>若命中则返回真。</returns>
        public bool ContainsWorldPoint(Vector3 worldPosition)
        {
            Bounds bounds = CalculateTreeBounds();
            return bounds.size.x > 0.1f && bounds.Contains(worldPosition);
        }

        /// <summary>
        /// 在外部拖动树对象后刷新相关可视元素。
        /// </summary>
        public void RefreshAfterExternalMove()
        {
            RefreshEdges();
            RefreshTreeBounds();
            RefreshTitle();
        }

        /// <summary>
        /// 判断某个世界坐标是否命中了树中的某个节点。
        /// </summary>
        /// <param name="worldPosition">世界坐标。</param>
        /// <returns>若命中节点则返回真。</returns>
        public bool ContainsNodePoint(Vector3 worldPosition)
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                BinaryTreeNode node = allNodes[i];
                if (node?.View == null) continue;
                if (Vector2.Distance(node.View.transform.position, worldPosition) <= nodeHitRadius) return true;
            }

            return false;
        }

        /// <summary>
        /// 执行一次树操作。
        /// </summary>
        /// <param name="request">操作请求。</param>
        /// <param name="stepInterval">操作间隔。</param>
        public void ExecuteOperation(BinaryTreeOperationRequest request, float stepInterval)
        {
            if (isBusy) return;
            stopRequested = false;
            pauseRequested = false;
            if (operationRoutine != null) StopCoroutine(operationRoutine);
            operationRoutine = StartCoroutine(RunOperation(request, Mathf.Max(0.05f, stepInterval)));
        }

        /// <summary>
        /// 暂停当前树的操作。
        /// </summary>
        public void PauseOperation()
        {
            if (!isBusy) return;
            pauseRequested = true;
        }

        /// <summary>
        /// 恢复当前树的操作。
        /// </summary>
        public void ResumeOperation()
        {
            pauseRequested = false;
        }

        /// <summary>
        /// 停止当前树的操作并清空临时状态。
        /// </summary>
        public void StopOperation()
        {
            stopRequested = true;
            pauseRequested = false;
            if (operationRoutine != null)
            {
                StopCoroutine(operationRoutine);
                operationRoutine = null;
            }

            isBusy = false;
            ClearVisualStates();
        }

        /// <summary>
        /// 清空当前树的所有节点。
        /// </summary>
        public void ClearTree()
        {
            if (operationRoutine != null)
            {
                StopCoroutine(operationRoutine);
                operationRoutine = null;
            }

            ResetRuntimeTreeVisuals();
            isBusy = false;
            pauseRequested = false;
            stopRequested = false;
            EnsureDefaultRootNode();
            ClearVisualStates();
            RefreshEdges();
            RefreshTreeBounds();
            RefreshTitle();
        }

        /// <summary>
        /// 执行单次树操作协程。
        /// </summary>
        /// <param name="request">操作请求。</param>
        /// <param name="stepInterval">操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator RunOperation(BinaryTreeOperationRequest request, float stepInterval)
        {
            isBusy = true;
            ClearVisualStates();
            if (stopRequested) { FinishOperation(); yield break; }

            switch (request.OperationType)
            {
                case BinaryTreeOperationType.Search:
                    yield return RunSearch(request.TargetValue, stepInterval);
                    break;
                case BinaryTreeOperationType.Insert:
                    yield return RunInsert(request.TargetValue, stepInterval);
                    break;
                case BinaryTreeOperationType.Update:
                    yield return RunUpdate(request.TargetValue, request.NewValue, stepInterval);
                    break;
                case BinaryTreeOperationType.Delete:
                    yield return RunDelete(request.TargetValue, stepInterval);
                    break;
            }

            if (!stopRequested) yield return WaitControlled(stepInterval * 0.35f);
            FinishOperation();
        }

        /// <summary>
        /// 执行查找流程。
        /// </summary>
        /// <param name="value">待查值。</param>
        /// <param name="stepInterval">操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator RunSearch(int value, float stepInterval)
        {
            BinaryTreeNode foundNode = SearchNode(value, out List<BinaryTreeNode> path);
            yield return AnimatePath(path, stepInterval);
            if (stopRequested || foundNode == null) yield break;

            activeNode = foundNode;
            ApplyNodeColor(foundNode, foundNodeColor);
            yield return WaitControlled(stepInterval);
            ApplyBasePaletteToAllNodes();
        }

        /// <summary>
        /// 执行插入流程。
        /// </summary>
        /// <param name="value">待插入值。</param>
        /// <param name="stepInterval">操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator RunInsert(int value, float stepInterval)
        {
            BinaryTreeNode searchNode = SearchNode(value, out List<BinaryTreeNode> path);
            yield return AnimatePath(path, stepInterval);
            if (stopRequested) yield break;
            if (searchNode != null)
            {
                yield return WaitControlled(stepInterval);
                ApplyBasePaletteToAllNodes();
                yield break;
            }

            treeStrategy ??= BinaryTreeStrategyFactory.Create(implementationType);
            yield return treeStrategy.RunInsert(this, value, stepInterval);
        }

        /// <summary>
        /// 执行删除流程。
        /// </summary>
        /// <param name="value">待删除值。</param>
        /// <param name="stepInterval">操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator RunDelete(int value, float stepInterval)
        {
            BinaryTreeNode targetNode = SearchNode(value, out List<BinaryTreeNode> path);
            yield return AnimatePath(path, stepInterval);
            if (stopRequested || targetNode == null) yield break;

            activeNode = targetNode;
            ApplyNodeColor(targetNode, foundNodeColor);
            yield return WaitControlled(stepInterval * 0.5f);
            if (stopRequested) yield break;

            if (targetNode.Left != null && targetNode.Right != null)
            {
                List<BinaryTreeNode> successorPath = new();
                BinaryTreeNode successor = targetNode.Right;
                while (successor != null)
                {
                    successorPath.Add(successor);
                    if (successor.Left == null) break;
                    successor = successor.Left;
                }

                yield return AnimatePath(successorPath, stepInterval * 0.75f);
                if (stopRequested) yield break;
            }

            treeStrategy ??= BinaryTreeStrategyFactory.Create(implementationType);
            yield return treeStrategy.RunDelete(this, targetNode, stepInterval);
        }

        /// <summary>
        /// 执行修改流程。
        /// </summary>
        /// <param name="oldValue">旧值。</param>
        /// <param name="newValue">新值。</param>
        /// <param name="stepInterval">操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator RunUpdate(int oldValue, int newValue, float stepInterval)
        {
            if (oldValue == newValue)
            {
                yield return RunSearch(oldValue, stepInterval);
                yield break;
            }

            BinaryTreeNode existingNewValue = SearchNode(newValue, out _);
            if (existingNewValue != null) yield break;

            yield return RunDelete(oldValue, stepInterval);
            if (stopRequested) yield break;
            yield return RunInsert(newValue, stepInterval);
        }

        /// <summary>
        /// 在树中搜索指定值。
        /// </summary>
        /// <param name="value">待查值。</param>
        /// <param name="path">搜索路径。</param>
        /// <returns>命中的节点，若不存在则为空。</returns>
        private BinaryTreeNode SearchNode(int value, out List<BinaryTreeNode> path)
        {
            path = new List<BinaryTreeNode>();
            if (IsTreeEmpty()) return null;

            BinaryTreeNode current = rootNode;
            while (current != null)
            {
                path.Add(current);
                if (value == current.Value) return current;
                current = value < current.Value ? current.Left : current.Right;
            }

            return null;
        }

        /// <summary>
        /// 按普通 BST 方式插入一个节点。
        /// </summary>
        internal BinaryTreeNode InsertValueAsBst(int value, bool makeRed)
        {
            if (rootNode == null)
            {
                rootNode = CreateNode(value, implementationType == BinaryTreeImplementationType.RedBlackTree && makeRed);
                if (implementationType == BinaryTreeImplementationType.RedBlackTree) rootNode.IsRed = false;
                RecomputeHeights(rootNode);
                RefreshTitle();
                ApplyBasePaletteToAllNodes();
                return rootNode;
            }

            if (rootNode.IsPlaceholder)
            {
                SetNodeAsValue(rootNode, value, implementationType == BinaryTreeImplementationType.RedBlackTree && makeRed);
                if (implementationType == BinaryTreeImplementationType.RedBlackTree) rootNode.IsRed = false;
                RecomputeHeights(rootNode);
                RefreshTitle();
                ApplyBasePaletteToAllNodes();
                return rootNode;
            }

            BinaryTreeNode parent = null;
            BinaryTreeNode current = rootNode;
            while (current != null)
            {
                parent = current;
                current = value < current.Value ? current.Left : current.Right;
            }

            BinaryTreeNode insertedNode = CreateNode(value, implementationType == BinaryTreeImplementationType.RedBlackTree && makeRed);
            insertedNode.Parent = parent;
            if (value < parent.Value) parent.Left = insertedNode;
            else parent.Right = insertedNode;
            RecomputeHeights(rootNode);
            ApplyBasePaletteToAllNodes();
            return insertedNode;
        }

        /// <summary>
        /// 按普通 BST 方式删除一个节点，并返回需要向上检查的起点。
        /// </summary>
        internal BinaryTreeNode DeleteBstNode(BinaryTreeNode targetNode, out BinaryTreeNode fixNode, out BinaryTreeNode fixParent, out bool removedWasRed)
        {
            fixNode = null;
            fixParent = null;
            removedWasRed = false;
            if (targetNode == null || targetNode.IsPlaceholder) return null;

            if (targetNode == rootNode && targetNode.Left == null && targetNode.Right == null)
            {
                removedWasRed = targetNode.IsRed;
                SetNodeAsPlaceholder(targetNode);
                RecomputeHeights(rootNode);
                ApplyBasePaletteToAllNodes();
                return rootNode;
            }

            BinaryTreeNode rebalanceStart;
            BinaryTreeNode removedNode = targetNode;
            removedWasRed = removedNode.IsRed;

            if (targetNode.Left == null)
            {
                fixNode = targetNode.Right;
                fixParent = targetNode.Parent;
                rebalanceStart = targetNode.Parent;
                Transplant(targetNode, targetNode.Right);
                DestroyNode(targetNode);
            }
            else if (targetNode.Right == null)
            {
                fixNode = targetNode.Left;
                fixParent = targetNode.Parent;
                rebalanceStart = targetNode.Parent;
                Transplant(targetNode, targetNode.Left);
                DestroyNode(targetNode);
            }
            else
            {
                BinaryTreeNode successor = GetMinimum(targetNode.Right);
                removedNode = successor;
                removedWasRed = successor.IsRed;
                fixNode = successor.Right;
                if (successor.Parent == targetNode)
                {
                    fixParent = successor;
                    if (fixNode != null) fixNode.Parent = successor;
                }
                else
                {
                    fixParent = successor.Parent;
                    Transplant(successor, successor.Right);
                    successor.Right = targetNode.Right;
                    if (successor.Right != null) successor.Right.Parent = successor;
                }

                Transplant(targetNode, successor);
                successor.Left = targetNode.Left;
                if (successor.Left != null) successor.Left.Parent = successor;
                successor.IsRed = targetNode.IsRed;
                DestroyNode(targetNode);
                rebalanceStart = successor;
            }

            if (rootNode == null)
            {
                EnsureDefaultRootNode();
                RecomputeHeights(rootNode);
                ApplyBasePaletteToAllNodes();
                return rootNode;
            }

            RecomputeHeights(rootNode);
            ApplyBasePaletteToAllNodes();
            return rebalanceStart ?? fixParent ?? fixNode;
        }

        internal void Transplant(BinaryTreeNode oldNode, BinaryTreeNode newNode)
        {
            if (oldNode.Parent == null) rootNode = newNode;
            else if (oldNode == oldNode.Parent.Left) oldNode.Parent.Left = newNode;
            else oldNode.Parent.Right = newNode;
            if (newNode != null) newNode.Parent = oldNode.Parent;
        }

        internal BinaryTreeNode GetMinimum(BinaryTreeNode node)
        {
            BinaryTreeNode current = node;
            while (current != null && current.Left != null) current = current.Left;
            return current;
        }

        internal BinaryTreeNode RotateLeft(BinaryTreeNode pivot)
        {
            if (pivot?.Right == null) return pivot;
            BinaryTreeNode child = pivot.Right;
            pivot.Right = child.Left;
            if (child.Left != null) child.Left.Parent = pivot;
            child.Parent = pivot.Parent;
            if (pivot.Parent == null) rootNode = child;
            else if (pivot == pivot.Parent.Left) pivot.Parent.Left = child;
            else pivot.Parent.Right = child;
            child.Left = pivot;
            pivot.Parent = child;
            RecomputeHeights(rootNode);
            return child;
        }

        internal BinaryTreeNode RotateRight(BinaryTreeNode pivot)
        {
            if (pivot?.Left == null) return pivot;
            BinaryTreeNode child = pivot.Left;
            pivot.Left = child.Right;
            if (child.Right != null) child.Right.Parent = pivot;
            child.Parent = pivot.Parent;
            if (pivot.Parent == null) rootNode = child;
            else if (pivot == pivot.Parent.Left) pivot.Parent.Left = child;
            else pivot.Parent.Right = child;
            child.Right = pivot;
            pivot.Parent = child;
            RecomputeHeights(rootNode);
            return child;
        }

        internal IEnumerator AnimateAvlRebalance(BinaryTreeNode startNode, float stepInterval)
        {
            BinaryTreeNode current = startNode;
            while (current != null)
            {
                RecomputeHeights(rootNode);
                int balance = GetBalance(current);
                if (balance > 1)
                {
                    if (GetBalance(current.Left) < 0)
                    {
                        yield return HighlightRotation(current.Left, current.Left?.Right, stepInterval * 0.35f);
                        RotateLeft(current.Left);
                        RecomputeHeights(rootNode);
                        ApplyBasePaletteToAllNodes();
                        yield return AnimateLayout(stepInterval * 0.85f);
                        if (stopRequested) yield break;
                    }

                    yield return HighlightRotation(current, current.Left, stepInterval * 0.35f);
                    BinaryTreeNode rotatedRoot = RotateRight(current);
                    RecomputeHeights(rootNode);
                    ApplyBasePaletteToAllNodes();
                    yield return AnimateLayout(stepInterval);
                    if (stopRequested) yield break;
                    current = rotatedRoot?.Parent;
                    continue;
                }

                if (balance < -1)
                {
                    if (GetBalance(current.Right) > 0)
                    {
                        yield return HighlightRotation(current.Right, current.Right?.Left, stepInterval * 0.35f);
                        RotateRight(current.Right);
                        RecomputeHeights(rootNode);
                        ApplyBasePaletteToAllNodes();
                        yield return AnimateLayout(stepInterval * 0.85f);
                        if (stopRequested) yield break;
                    }

                    yield return HighlightRotation(current, current.Right, stepInterval * 0.35f);
                    BinaryTreeNode rotatedRoot = RotateLeft(current);
                    RecomputeHeights(rootNode);
                    ApplyBasePaletteToAllNodes();
                    yield return AnimateLayout(stepInterval);
                    if (stopRequested) yield break;
                    current = rotatedRoot?.Parent;
                    continue;
                }

                current = current.Parent;
            }
        }

        internal IEnumerator AnimateRedBlackInsertFixup(BinaryTreeNode node, float stepInterval)
        {
            if (node == null) yield break;
            if (node == rootNode)
            {
                node.IsRed = false;
                yield return AnimateNodesToBaseColors(stepInterval * 0.55f, node);
                yield break;
            }

            while (node != rootNode && IsNodeRed(node.Parent))
            {
                BinaryTreeNode parent = node.Parent;
                BinaryTreeNode grandParent = parent?.Parent;
                if (grandParent == null) break;
                bool parentOnLeft = parent == grandParent.Left;
                BinaryTreeNode uncle = parentOnLeft ? grandParent.Right : grandParent.Left;
                yield return HighlightFamily(node, parent, grandParent, uncle, stepInterval * 0.35f);
                if (stopRequested) yield break;

                if (IsNodeRed(uncle))
                {
                    parent.IsRed = false;
                    uncle.IsRed = false;
                    grandParent.IsRed = true;
                    yield return AnimateNodesToBaseColors(stepInterval * 0.55f, parent, uncle, grandParent);
                    node = grandParent;
                    continue;
                }

                if (parentOnLeft)
                {
                    if (node == parent.Right)
                    {
                        yield return HighlightRotation(parent, node, stepInterval * 0.35f);
                        RotateLeft(parent);
                        RecomputeHeights(rootNode);
                        ApplyBasePaletteToAllNodes();
                        yield return AnimateLayout(stepInterval * 0.85f);
                        if (stopRequested) yield break;
                        node = parent;
                        parent = node.Parent;
                        grandParent = parent?.Parent;
                    }

                    if (parent != null) parent.IsRed = false;
                    if (grandParent != null) grandParent.IsRed = true;
                    yield return HighlightRotation(grandParent, parent, stepInterval * 0.35f);
                    RotateRight(grandParent);
                }
                else
                {
                    if (node == parent.Left)
                    {
                        yield return HighlightRotation(parent, node, stepInterval * 0.35f);
                        RotateRight(parent);
                        RecomputeHeights(rootNode);
                        ApplyBasePaletteToAllNodes();
                        yield return AnimateLayout(stepInterval * 0.85f);
                        if (stopRequested) yield break;
                        node = parent;
                        parent = node.Parent;
                        grandParent = parent?.Parent;
                    }

                    if (parent != null) parent.IsRed = false;
                    if (grandParent != null) grandParent.IsRed = true;
                    yield return HighlightRotation(grandParent, parent, stepInterval * 0.35f);
                    RotateLeft(grandParent);
                }

                RecomputeHeights(rootNode);
                ApplyBasePaletteToAllNodes();
                yield return AnimateLayout(stepInterval);
                if (stopRequested) yield break;
            }

            if (rootNode != null && !rootNode.IsPlaceholder && rootNode.IsRed)
            {
                rootNode.IsRed = false;
                yield return AnimateNodesToBaseColors(stepInterval * 0.4f, rootNode);
            }

            ApplyBasePaletteToAllNodes();
        }

        internal IEnumerator AnimateRedBlackDelete(BinaryTreeNode targetNode, float stepInterval)
        {
            BinaryTreeNode fixNode;
            BinaryTreeNode fixParent;
            bool removedWasRed;
            DeleteBstNode(targetNode, out fixNode, out fixParent, out removedWasRed);
            yield return AnimateLayout(stepInterval);
            if (stopRequested) yield break;
            if (rootNode == null || rootNode.IsPlaceholder) yield break;
            if (!removedWasRed) yield return AnimateRedBlackDeleteFixup(fixNode, fixParent, stepInterval);
            if (rootNode != null && !rootNode.IsPlaceholder && rootNode.IsRed)
            {
                rootNode.IsRed = false;
                yield return AnimateNodesToBaseColors(stepInterval * 0.35f, rootNode);
            }

            ApplyBasePaletteToAllNodes();
        }

        internal IEnumerator AnimateRedBlackDeleteFixup(BinaryTreeNode node, BinaryTreeNode parent, float stepInterval)
        {
            while (node != rootNode && !IsNodeRed(node))
            {
                if (parent == null) break;
                bool nodeOnLeft = node == parent.Left;
                BinaryTreeNode sibling = nodeOnLeft ? parent.Right : parent.Left;
                yield return HighlightFamily(node, parent, sibling, sibling?.Left, stepInterval * 0.3f);
                if (stopRequested) yield break;

                if (IsNodeRed(sibling))
                {
                    sibling.IsRed = false;
                    parent.IsRed = true;
                    yield return AnimateNodesToBaseColors(stepInterval * 0.35f, sibling, parent);
                    if (nodeOnLeft) RotateLeft(parent);
                    else RotateRight(parent);
                    RecomputeHeights(rootNode);
                    ApplyBasePaletteToAllNodes();
                    yield return AnimateLayout(stepInterval * 0.85f);
                    if (stopRequested) yield break;
                    sibling = nodeOnLeft ? parent.Right : parent.Left;
                }

                bool siblingLeftRed = IsNodeRed(sibling?.Left);
                bool siblingRightRed = IsNodeRed(sibling?.Right);
                if (!siblingLeftRed && !siblingRightRed)
                {
                    if (sibling != null) sibling.IsRed = true;
                    yield return AnimateNodesToBaseColors(stepInterval * 0.35f, sibling);
                    node = parent;
                    parent = node?.Parent;
                    continue;
                }

                if (nodeOnLeft)
                {
                    if (!siblingRightRed)
                    {
                        if (sibling?.Left != null) sibling.Left.IsRed = false;
                        if (sibling != null) sibling.IsRed = true;
                        yield return AnimateNodesToBaseColors(stepInterval * 0.35f, sibling?.Left, sibling);
                        RotateRight(sibling);
                        RecomputeHeights(rootNode);
                        ApplyBasePaletteToAllNodes();
                        yield return AnimateLayout(stepInterval * 0.85f);
                        if (stopRequested) yield break;
                        sibling = parent.Right;
                    }

                    if (sibling != null) sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    if (sibling?.Right != null) sibling.Right.IsRed = false;
                    yield return AnimateNodesToBaseColors(stepInterval * 0.35f, sibling, parent, sibling?.Right);
                    RotateLeft(parent);
                }
                else
                {
                    if (!siblingLeftRed)
                    {
                        if (sibling?.Right != null) sibling.Right.IsRed = false;
                        if (sibling != null) sibling.IsRed = true;
                        yield return AnimateNodesToBaseColors(stepInterval * 0.35f, sibling?.Right, sibling);
                        RotateLeft(sibling);
                        RecomputeHeights(rootNode);
                        ApplyBasePaletteToAllNodes();
                        yield return AnimateLayout(stepInterval * 0.85f);
                        if (stopRequested) yield break;
                        sibling = parent.Left;
                    }

                    if (sibling != null) sibling.IsRed = parent.IsRed;
                    parent.IsRed = false;
                    if (sibling?.Left != null) sibling.Left.IsRed = false;
                    yield return AnimateNodesToBaseColors(stepInterval * 0.35f, sibling, parent, sibling?.Left);
                    RotateRight(parent);
                }

                RecomputeHeights(rootNode);
                ApplyBasePaletteToAllNodes();
                yield return AnimateLayout(stepInterval);
                if (stopRequested) yield break;
                node = rootNode;
                parent = null;
            }

            if (node != null && node.IsRed)
            {
                node.IsRed = false;
                yield return AnimateNodesToBaseColors(stepInterval * 0.35f, node);
            }

            ApplyBasePaletteToAllNodes();
        }

        internal IEnumerator HighlightRotation(BinaryTreeNode pivot, BinaryTreeNode child, float duration)
        {
            yield return WaitControlled(duration);
        }

        internal IEnumerator HighlightFamily(BinaryTreeNode first, BinaryTreeNode second, BinaryTreeNode third, BinaryTreeNode fourth, float duration)
        {
            yield return WaitControlled(duration);
        }

        internal int RecomputeHeights(BinaryTreeNode node)
        {
            if (node == null) return 0;
            int leftHeight = RecomputeHeights(node.Left);
            int rightHeight = RecomputeHeights(node.Right);
            node.Height = Mathf.Max(leftHeight, rightHeight) + 1;
            return node.Height;
        }

        internal int GetBalance(BinaryTreeNode node)
        {
            if (node == null) return 0;
            return GetHeight(node.Left) - GetHeight(node.Right);
        }

        internal int GetHeight(BinaryTreeNode node) => node?.Height ?? 0;

        internal bool IsNodeRed(BinaryTreeNode node) => node != null && !node.IsPlaceholder && node.IsRed;

        /// <summary>
        /// 保证空树状态下存在一个默认的占位根节点。
        /// </summary>
        private void EnsureDefaultRootNode()
        {
            if (rootNode != null || allNodes.Count > 0) return;
            rootNode = CreatePlaceholderNode();
            Dictionary<BinaryTreeNode, Vector3> layoutMap = BuildLayoutMap();
            foreach (KeyValuePair<BinaryTreeNode, Vector3> pair in layoutMap) pair.Key.View.transform.localPosition = pair.Value;
            ApplyBasePaletteToAllNodes();
            RefreshEdges();
        }

        /// <summary>
        /// 重置运行时树结构与全部可视对象缓存。
        /// </summary>
        private void ResetRuntimeTreeVisuals()
        {
            DetachTitleFromNodeRoot();
            rootNode = null;
            allNodes.Clear();
            activeNode = null;
            visitedNodes.Clear();
            if (nodesRoot != null)
            {
                for (int i = nodesRoot.childCount - 1; i >= 0; i--) DestroyImmediateOrRuntime(nodesRoot.GetChild(i).gameObject);
            }

            if (edgesRoot != null)
            {
                for (int i = edgesRoot.childCount - 1; i >= 0; i--) DestroyImmediateOrRuntime(edgesRoot.GetChild(i).gameObject);
            }

            edgeRenderers.Clear();
        }

        /// <summary>
        /// 创建一个真实节点并加入当前树缓存。
        /// </summary>
        /// <param name="value">节点值。</param>
        /// <param name="isRed">是否为红节点。</param>
        /// <returns>创建出的节点对象。</returns>
        private BinaryTreeNode CreateNode(int value, bool isRed)
        {
            BinaryTreeNodeView view = Instantiate(nodePrefab, nodesRoot);
            view.name = $"Node_{value}";
            view.SetValue(value);
            view.SetSortingOrder(12);
            BinaryTreeNode node = new(value, view, false) { IsRed = isRed, Height = 1 };
            allNodes.Add(node);
            ApplyBaseNodeVisual(node);
            return node;
        }

        /// <summary>
        /// 创建一个占位根节点。
        /// </summary>
        /// <returns>创建出的占位节点。</returns>
        private BinaryTreeNode CreatePlaceholderNode()
        {
            BinaryTreeNodeView view = Instantiate(nodePrefab, nodesRoot);
            view.name = "Node_Root";
            view.SetLabel(placeholderRootLabel);
            view.SetSortingOrder(12);
            BinaryTreeNode node = new(0, view, true) { IsRed = false, Height = 1 };
            allNodes.Add(node);
            ApplyBaseNodeVisual(node);
            return node;
        }

        /// <summary>
        /// 将占位节点切换为真实值节点。
        /// </summary>
        /// <param name="node">待修改节点。</param>
        /// <param name="value">新的节点值。</param>
        /// <param name="isRed">是否为红节点。</param>
        private void SetNodeAsValue(BinaryTreeNode node, int value, bool isRed)
        {
            if (node?.View == null) return;
            node.Value = value;
            node.IsPlaceholder = false;
            node.IsRed = isRed;
            node.View.name = $"Node_{value}";
            node.View.SetValue(value);
            ApplyBaseNodeVisual(node);
        }

        /// <summary>
        /// 将节点重置为占位根节点状态。
        /// </summary>
        /// <param name="node">待重置节点。</param>
        private void SetNodeAsPlaceholder(BinaryTreeNode node)
        {
            if (node?.View == null) return;
            node.Value = 0;
            node.IsPlaceholder = true;
            node.IsRed = false;
            node.Parent = null;
            node.Left = null;
            node.Right = null;
            node.Height = 1;
            node.View.name = "Node_Root";
            node.View.SetLabel(placeholderRootLabel);
            ApplyBaseNodeVisual(node);
        }

        /// <summary>
        /// 判断当前树是否为空树或仅剩占位根。
        /// </summary>
        /// <returns>若为空则返回真。</returns>
        private bool IsTreeEmpty()
        {
            return rootNode == null || rootNode.IsPlaceholder;
        }

        /// <summary>
        /// 销毁单个节点及其视图对象。
        /// </summary>
        /// <param name="node">待销毁节点。</param>
        private void DestroyNode(BinaryTreeNode node)
        {
            if (node?.View == null) return;
            DetachTitleFromSpecificNode(node);
            allNodes.Remove(node);
            DestroyImmediateOrRuntime(node.View.gameObject);
        }

        /// <summary>
        /// 按给定路径逐个高亮节点，播放访问过程动画。
        /// </summary>
        /// <param name="path">待播放的访问路径。</param>
        /// <param name="stepInterval">单步间隔时间。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator AnimatePath(List<BinaryTreeNode> path, float stepInterval)
        {
            ApplyBasePaletteToAllNodes();
            for (int i = 0; i < path.Count; i++)
            {
                if (stopRequested) yield break;
                BinaryTreeNode node = path[i];
                if (activeNode != null && activeNode != node)
                {
                    visitedNodes.Add(activeNode);
                    if (implementationType == BinaryTreeImplementationType.RedBlackTree) ApplyBaseNodeVisual(activeNode);
                    else ApplyNodeColor(activeNode, visitedNodeColor);
                }

                activeNode = node;
                ApplyNodeColor(node, activeNodeColor);
                yield return WaitControlled(stepInterval);
            }
        }

        /// <summary>
        /// 将全部节点平滑移动到当前树结构对应的布局位置。
        /// </summary>
        /// <param name="stepInterval">本次操作间隔。</param>
        /// <returns>协程迭代器。</returns>
        internal IEnumerator AnimateLayout(float stepInterval)
        {
            Dictionary<BinaryTreeNode, Vector3> targetPositions = BuildLayoutMap();
            Dictionary<BinaryTreeNode, Vector3> startPositions = new(targetPositions.Count);
            foreach (KeyValuePair<BinaryTreeNode, Vector3> pair in targetPositions) startPositions[pair.Key] = pair.Key.View.transform.localPosition;

            float duration = Mathf.Min(layoutAnimationDuration, Mathf.Max(0.12f, stepInterval * 0.9f));
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (stopRequested) yield break;
                while (pauseRequested && !stopRequested) yield return null;
                elapsed += GetFrameDeltaTime();
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                foreach (KeyValuePair<BinaryTreeNode, Vector3> pair in targetPositions)
                {
                    pair.Key.View.transform.localPosition = Vector3.Lerp(startPositions[pair.Key], pair.Value, t);
                }

                RefreshEdges();
                RefreshTreeBounds();
                RefreshTitle();
                yield return null;
            }

            foreach (KeyValuePair<BinaryTreeNode, Vector3> pair in targetPositions) pair.Key.View.transform.localPosition = pair.Value;
            RefreshEdges();
            RefreshTreeBounds();
            RefreshTitle();
            ApplyBasePaletteToAllNodes();
        }

        /// <summary>
        /// 在支持暂停与停止的前提下等待指定时长。
        /// </summary>
        /// <param name="duration">等待时长。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator WaitControlled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (stopRequested) yield break;
                while (pauseRequested && !stopRequested) yield return null;
                elapsed += GetFrameDeltaTime();
                yield return null;
            }
        }

        /// <summary>
        /// 获取当前帧用于协程动画推进的时间步长。
        /// </summary>
        /// <returns>当前帧时间增量。</returns>
        private float GetFrameDeltaTime()
        {
            return Application.isPlaying ? Time.deltaTime : 0.016f;
        }

        /// <summary>
        /// 结束当前树操作并恢复到空闲状态。
        /// </summary>
        private void FinishOperation()
        {
            ClearVisualStates();
            pauseRequested = false;
            stopRequested = false;
            operationRoutine = null;
            isBusy = false;
        }

        /// <summary>
        /// 根据当前树结构构建节点目标布局表。
        /// </summary>
        /// <returns>节点到目标局部坐标的映射。</returns>
        private Dictionary<BinaryTreeNode, Vector3> BuildLayoutMap()
        {
            Dictionary<BinaryTreeNode, Vector3> layoutMap = new();
            int nodeCount = allNodes.Count;
            if (nodeCount == 0 || rootNode == null) return layoutMap;
            int inorderIndex = 0;
            AssignLayout(rootNode, 0, ref inorderIndex, nodeCount, layoutMap);
            return layoutMap;
        }

        /// <summary>
        /// 按中序顺序递归计算每个节点的布局位置。
        /// </summary>
        /// <param name="node">当前处理节点。</param>
        /// <param name="depth">当前深度。</param>
        /// <param name="inorderIndex">当前中序编号。</param>
        /// <param name="nodeCount">总节点数。</param>
        /// <param name="layoutMap">布局结果表。</param>
        private void AssignLayout(BinaryTreeNode node, int depth, ref int inorderIndex, int nodeCount, Dictionary<BinaryTreeNode, Vector3> layoutMap)
        {
            if (node == null) return;
            AssignLayout(node.Left, depth + 1, ref inorderIndex, nodeCount, layoutMap);
            float centerOffset = (nodeCount - 1) * 0.5f;
            float x = (inorderIndex - centerOffset) * horizontalSpacing;
            float y = -depth * verticalSpacing;
            layoutMap[node] = new Vector3(x, y, 0f);
            inorderIndex++;
            AssignLayout(node.Right, depth + 1, ref inorderIndex, nodeCount, layoutMap);
        }

        /// <summary>
        /// 刷新全部父子节点之间的连线位置与显隐。
        /// </summary>
        private void RefreshEdges()
        {
            EnsureLineRoot();
            if (edgesRoot == null) return;
            EnsureEdgeRendererCount(Mathf.Max(0, allNodes.Count - (rootNode == null ? 0 : 1)));
            int edgeIndex = 0;
            for (int i = 0; i < allNodes.Count; i++)
            {
                BinaryTreeNode node = allNodes[i];
                if (node.Parent == null || node.View == null || node.Parent.View == null) continue;
                LineRenderer renderer = edgeRenderers[edgeIndex++];
                ConfigureLineRenderer(renderer, 0.06f, Color.black, 3);
                renderer.positionCount = 2;
                renderer.useWorldSpace = true;
                renderer.SetPosition(0, node.Parent.View.transform.position);
                renderer.SetPosition(1, node.View.transform.position);
                renderer.enabled = true;
            }

            for (int i = edgeIndex; i < edgeRenderers.Count; i++) edgeRenderers[i].enabled = false;
        }

        /// <summary>
        /// 刷新树边框显示状态。
        /// </summary>
        private void RefreshTreeBounds()
        {
        }

        /// <summary>
        /// 计算当前整棵树的世界包围盒。
        /// </summary>
        /// <returns>树的包围盒。</returns>
        private Bounds CalculateTreeBounds()
        {
            if (allNodes.Count == 0)
            {
                Vector3 center = transform.position;
                return new Bounds(center, new Vector3(3f, 2.4f, 0.1f));
            }

            Vector3 min = allNodes[0].View.transform.position;
            Vector3 max = min;
            for (int i = 0; i < allNodes.Count; i++)
            {
                Vector3 position = allNodes[i].View.transform.position;
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);
            }

            Vector3 titlePosition = transform.position + Vector3.up * (titleOffset + 0.2f);
            max = Vector3.Max(max, titlePosition);
            min -= new Vector3(borderPadding, borderPadding, 0f);
            max += new Vector3(borderPadding, borderPadding, 0f);
            return new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>
        /// 刷新树标题内容与位置。
        /// </summary>
        private void RefreshTitle()
        {
            EnsureTitleText();
            if (titleText == null) return;
            if (rootNode?.View != null)
            {
                if (!hasCachedTitleOffset)
                {
                    titleLocalOffset = new Vector3(0f, titleOffset, 0f);
                    hasCachedTitleOffset = true;
                }

                if (titleText.transform.parent != rootNode.View.transform) titleText.transform.SetParent(rootNode.View.transform, false);
                titleText.transform.localPosition = titleLocalOffset;
                titleText.transform.localRotation = Quaternion.identity;
            }

            titleText.text = $"树 {treeIndex} - {BinaryTreeModeUtility.GetDisplayName(implementationType)}";
        }

        /// <summary>
        /// 清空当前操作产生的临时高亮状态。
        /// </summary>
        private void ClearVisualStates()
        {
            activeNode = null;
            visitedNodes.Clear();
            ApplyBasePaletteToAllNodes();
        }

        /// <summary>
        /// 刷新选中树时所有节点的高亮显示。
        /// </summary>
        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                BinaryTreeNode node = allNodes[i];
                if (node?.View != null) node.View.SetSelected(isSelected);
            }
        }

        /// <summary>
        /// 将整棵树恢复到当前实现模式下的基础配色。
        /// </summary>
        private void ApplyBasePaletteToAllNodes()
        {
            for (int i = 0; i < allNodes.Count; i++) ApplyBaseNodeVisual(allNodes[i]);
        }

        /// <summary>
        /// 应用单个节点的基础颜色与选中状态。
        /// </summary>
        /// <param name="node">目标节点。</param>
        private void ApplyBaseNodeVisual(BinaryTreeNode node)
        {
            if (node?.View == null) return;
            Color bodyColor = GetBaseNodeColor(node);
            node.View.SetColor(bodyColor);
            node.View.SetTextColor(GetContrastingTextColor(bodyColor));
            node.View.SetSelected(isSelected);
        }

        /// <summary>
        /// 获取节点在当前树模式下应显示的基础颜色。
        /// </summary>
        /// <param name="node">目标节点。</param>
        /// <returns>基础颜色。</returns>
        private Color GetBaseNodeColor(BinaryTreeNode node)
        {
            if (node == null) return normalNodeColor;
            if (node.IsPlaceholder) return placeholderNodeColor;
            if (implementationType == BinaryTreeImplementationType.RedBlackTree) return node.IsRed ? redBlackRedNodeColor : redBlackBlackNodeColor;
            return normalNodeColor;
        }

        /// <summary>
        /// 直接设置单个节点的显示颜色。
        /// </summary>
        /// <param name="node">目标节点。</param>
        /// <param name="color">目标颜色。</param>
        private void ApplyNodeColor(BinaryTreeNode node, Color color)
        {
            if (node?.View == null) return;
            node.View.SetColor(color);
            node.View.SetTextColor(GetContrastingTextColor(color));
            node.View.SetSelected(isSelected);
        }

        /// <summary>
        /// 根据背景色计算可读性更好的文字颜色。
        /// </summary>
        /// <param name="backgroundColor">背景色。</param>
        /// <returns>建议使用的文字颜色。</returns>
        private Color GetContrastingTextColor(Color backgroundColor)
        {
            float luminance = backgroundColor.r * 0.299f + backgroundColor.g * 0.587f + backgroundColor.b * 0.114f;
            return luminance < 0.52f ? Color.white : Color.black;
        }

        /// <summary>
        /// 保证标题文本对象存在并完成基础配置。
        /// </summary>
        private void EnsureTitleText()
        {
            if (titleText != null) return;
            TextMeshPro[] titleCandidates = GetComponentsInChildren<TextMeshPro>(true);
            for (int i = 0; i < titleCandidates.Length; i++)
            {
                if (titleCandidates[i] == null || titleCandidates[i].name != "Title") continue;
                titleText = titleCandidates[i];
                CacheTitleLocalOffset();
                break;
            }
        }

        /// <summary>
        /// 保证连线父节点存在。
        /// </summary>
        private void EnsureLineRoot()
        {
            if (edgesRoot != null) return;
            Transform found = transform.Find("Edges");
            if (found != null) edgesRoot = found;
        }

        /// <summary>
        /// 保证边框渲染器缓存完整。
        /// </summary>
        private void EnsureBorderRenderers()
        {
            if (borderRoot == null)
            {
                Transform found = transform.Find("Border");
                if (found != null) borderRoot = found;
            }

            if (borderRoot == null) return;

            while (borderRenderers.Count < 4)
            {
                GameObject lineObject = new($"Border Line {borderRenderers.Count}");
                lineObject.transform.SetParent(borderRoot, false);
                LineRenderer renderer = lineObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer(renderer, 0.08f, Color.black, 5);
                borderRenderers.Add(renderer);
            }
        }

        /// <summary>
        /// 确保连线渲染器数量满足当前树结构需要。
        /// </summary>
        /// <param name="requiredCount">所需渲染器数量。</param>
        private void EnsureEdgeRendererCount(int requiredCount)
        {
            while (edgeRenderers.Count < requiredCount)
            {
                GameObject lineObject = new($"Edge {edgeRenderers.Count}");
                lineObject.transform.SetParent(edgesRoot, false);
                LineRenderer renderer = lineObject.AddComponent<LineRenderer>();
                ConfigureLineRenderer(renderer, 0.06f, Color.black, 3);
                edgeRenderers.Add(renderer);
            }
        }

        /// <summary>
        /// 配置线渲染器的通用显示参数。
        /// </summary>
        /// <param name="renderer">目标线渲染器。</param>
        /// <param name="width">线宽。</param>
        /// <param name="color">颜色。</param>
        /// <param name="sortingOrder">渲染层级。</param>
        private void ConfigureLineRenderer(LineRenderer renderer, float width, Color color, int sortingOrder)
        {
            if (renderer == null) return;
            renderer.sharedMaterial = GetOrCreateLineMaterial();
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.alignment = LineAlignment.TransformZ;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.loop = false;
            renderer.numCapVertices = 4;
            renderer.startWidth = width;
            renderer.endWidth = width;
            renderer.startColor = color;
            renderer.endColor = color;
            renderer.sortingOrder = sortingOrder;
        }

        /// <summary>
        /// 获取或创建供线渲染器共用的材质。
        /// </summary>
        /// <returns>线材质。</returns>
        private Material GetOrCreateLineMaterial()
        {
            if (lineMaterial != null) return lineMaterial;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;
            lineMaterial = new Material(shader) { name = "BinaryTree Line Material" };
            return lineMaterial;
        }

        /// <summary>
        /// 补全树控制器依赖的场景对象引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (nodesRoot == null) nodesRoot = transform.Find("Nodes");
            if (edgesRoot == null) edgesRoot = transform.Find("Edges");
            if (borderRoot == null) borderRoot = transform.Find("Border");
            if (titleText == null) EnsureTitleText();
            CacheTitleLocalOffset();
        }

        /// <summary>
        /// 根据运行环境销毁指定对象。
        /// </summary>
        /// <param name="target">待销毁对象。</param>
        private void DestroyImmediateOrRuntime(Object target)
        {
            if (target == null) return;
            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }

        /// <summary>
        /// 将一组节点的颜色在给定时长内插值过渡到各自的基础颜色。
        /// </summary>
        /// <param name="duration">过渡时长。</param>
        /// <param name="nodes">待过渡节点集合。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator AnimateNodesToBaseColors(float duration, params BinaryTreeNode[] nodes)
        {
            if (nodes == null || nodes.Length == 0) yield break;

            List<BinaryTreeNode> uniqueNodes = new();
            List<Color> startBodyColors = new();
            List<Color> startTextColors = new();
            List<Color> targetBodyColors = new();
            List<Color> targetTextColors = new();

            for (int i = 0; i < nodes.Length; i++)
            {
                BinaryTreeNode node = nodes[i];
                if (node?.View == null || uniqueNodes.Contains(node)) continue;
                uniqueNodes.Add(node);
                startBodyColors.Add(node.View.GetColor());
                startTextColors.Add(node.View.GetTextColor());
                Color bodyColor = GetBaseNodeColor(node);
                targetBodyColors.Add(bodyColor);
                targetTextColors.Add(GetContrastingTextColor(bodyColor));
            }

            if (uniqueNodes.Count == 0) yield break;

            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.05f, duration);
            while (elapsed < safeDuration)
            {
                if (stopRequested) yield break;
                while (pauseRequested && !stopRequested) yield return null;
                elapsed += GetFrameDeltaTime();
                float t = Mathf.Clamp01(elapsed / safeDuration);
                t = t * t * (3f - 2f * t);

                for (int i = 0; i < uniqueNodes.Count; i++)
                {
                    BinaryTreeNode node = uniqueNodes[i];
                    if (node?.View == null) continue;
                    node.View.SetColor(Color.Lerp(startBodyColors[i], targetBodyColors[i], t));
                    node.View.SetTextColor(Color.Lerp(startTextColors[i], targetTextColors[i], t));
                    node.View.SetSelected(isSelected);
                }

                yield return null;
            }

            for (int i = 0; i < uniqueNodes.Count; i++) ApplyBaseNodeVisual(uniqueNodes[i]);
        }

        /// <summary>
        /// 使用淡出与淡入动画切换整棵树的实现模式，并按升序值重建树结构。
        /// </summary>
        /// <param name="mode">目标实现模式。</param>
        /// <param name="stepInterval">渐变时长基准。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator AnimateImplementationTypeTransition(BinaryTreeImplementationType mode, float stepInterval)
        {
            isBusy = true;
            ClearVisualStates();
            List<int> values = CollectSortedNodeValues();
            yield return AnimateTreeFade(1f, selectionColor.a, 0f, 0f, stepInterval);
            if (stopRequested) { FinishOperation(); yield break; }

            implementationType = mode;
            treeStrategy = BinaryTreeStrategyFactory.Create(implementationType);
            ResetRuntimeTreeVisuals();
            EnsureDefaultRootNode();
            RebuildTreeImmediatelyFromSortedValues(values);
            RefreshEdges();
            RefreshTreeBounds();
            RefreshTitle();
            ApplyBasePaletteToAllNodes();
            SetTreeVisualAlpha(0f, 0f);
            yield return AnimateTreeFade(0f, 0f, 1f, selectionColor.a, stepInterval);
            FinishOperation();
        }

        /// <summary>
        /// 收集当前树中的全部真实节点值，并按从小到大排序。
        /// </summary>
        /// <returns>升序节点值列表。</returns>
        private List<int> CollectSortedNodeValues()
        {
            List<int> values = new();
            for (int i = 0; i < allNodes.Count; i++)
            {
                BinaryTreeNode node = allNodes[i];
                if (node == null || node.IsPlaceholder) continue;
                values.Add(node.Value);
            }

            values.Sort();
            return values;
        }

        /// <summary>
        /// 按当前实现模式立即重建整棵树，不播放结构动画。
        /// </summary>
        /// <param name="sortedValues">已排序节点值列表。</param>
        private void RebuildTreeImmediatelyFromSortedValues(IReadOnlyList<int> sortedValues)
        {
            if (sortedValues == null || sortedValues.Count == 0)
            {
                ApplyBasePaletteToAllNodes();
                return;
            }

            ResetRuntimeTreeVisuals();
            rootNode = null;
            for (int i = 0; i < sortedValues.Count; i++) InsertValueImmediatelyForCurrentMode(sortedValues[i]);
            if (rootNode == null) EnsureDefaultRootNode();
            RecomputeHeights(rootNode);
            Dictionary<BinaryTreeNode, Vector3> layoutMap = BuildLayoutMap();
            foreach (KeyValuePair<BinaryTreeNode, Vector3> pair in layoutMap)
            {
                if (pair.Key?.View != null) pair.Key.View.transform.localPosition = pair.Value;
            }

            ApplyBasePaletteToAllNodes();
        }

        /// <summary>
        /// 按当前模式立即插入单个值，不播放可视动画。
        /// </summary>
        /// <param name="value">待插入值。</param>
        private void InsertValueImmediatelyForCurrentMode(int value)
        {
            switch (implementationType)
            {
                case BinaryTreeImplementationType.BalancedBinarySearchTree:
                    {
                        BinaryTreeNode insertedNode = InsertValueAsBst(value, false);
                        RebalanceAvlImmediately(insertedNode?.Parent);
                        break;
                    }
                case BinaryTreeImplementationType.RedBlackTree:
                    {
                        BinaryTreeNode insertedNode = InsertValueAsBst(value, true);
                        FixRedBlackInsertImmediately(insertedNode);
                        break;
                    }
                default:
                    InsertValueAsBst(value, false);
                    break;
            }
        }

        /// <summary>
        /// 立即执行 AVL 平衡修复。
        /// </summary>
        /// <param name="startNode">起始检查节点。</param>
        private void RebalanceAvlImmediately(BinaryTreeNode startNode)
        {
            BinaryTreeNode current = startNode;
            while (current != null)
            {
                RecomputeHeights(rootNode);
                int balance = GetBalance(current);
                if (balance > 1)
                {
                    if (GetBalance(current.Left) < 0) RotateLeft(current.Left);
                    BinaryTreeNode rotatedRoot = RotateRight(current);
                    current = rotatedRoot?.Parent;
                    continue;
                }

                if (balance < -1)
                {
                    if (GetBalance(current.Right) > 0) RotateRight(current.Right);
                    BinaryTreeNode rotatedRoot = RotateLeft(current);
                    current = rotatedRoot?.Parent;
                    continue;
                }

                current = current.Parent;
            }
        }

        /// <summary>
        /// 立即执行红黑树插入修复。
        /// </summary>
        /// <param name="node">新插入节点。</param>
        private void FixRedBlackInsertImmediately(BinaryTreeNode node)
        {
            if (node == null) return;
            if (node == rootNode)
            {
                node.IsRed = false;
                return;
            }

            while (node != rootNode && IsNodeRed(node.Parent))
            {
                BinaryTreeNode parent = node.Parent;
                BinaryTreeNode grandParent = parent?.Parent;
                if (grandParent == null) break;
                bool parentOnLeft = parent == grandParent.Left;
                BinaryTreeNode uncle = parentOnLeft ? grandParent.Right : grandParent.Left;

                if (IsNodeRed(uncle))
                {
                    parent.IsRed = false;
                    uncle.IsRed = false;
                    grandParent.IsRed = true;
                    node = grandParent;
                    continue;
                }

                if (parentOnLeft)
                {
                    if (node == parent.Right)
                    {
                        RotateLeft(parent);
                        node = parent;
                        parent = node.Parent;
                        grandParent = parent?.Parent;
                    }

                    if (parent != null) parent.IsRed = false;
                    if (grandParent != null) grandParent.IsRed = true;
                    RotateRight(grandParent);
                }
                else
                {
                    if (node == parent.Left)
                    {
                        RotateRight(parent);
                        node = parent;
                        parent = node.Parent;
                        grandParent = parent?.Parent;
                    }

                    if (parent != null) parent.IsRed = false;
                    if (grandParent != null) grandParent.IsRed = true;
                    RotateLeft(grandParent);
                }
            }

            if (rootNode != null && !rootNode.IsPlaceholder) rootNode.IsRed = false;
            RecomputeHeights(rootNode);
        }

        /// <summary>
        /// 统一设置整棵树节点、连线与标题的透明度。
        /// </summary>
        /// <param name="alpha">目标透明度。</param>
        /// <param name="selectionAlpha">选中状态的透明度。</param>
        private void SetTreeVisualAlpha(float alpha, float selectionAlpha)
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i]?.View != null) allNodes[i].View.SetVisualAlpha(alpha, selectionAlpha);
            }

            for (int i = 0; i < edgeRenderers.Count; i++)
            {
                LineRenderer renderer = edgeRenderers[i];
                if (renderer == null) continue;
                Color startColor = renderer.startColor;
                Color endColor = renderer.endColor;
                startColor.a = alpha;
                endColor.a = alpha;
                renderer.startColor = startColor;
                renderer.endColor = endColor;
            }

            if (titleText != null)
            {
                Color titleColor = titleText.color;
                titleColor.a = alpha;
                titleText.color = titleColor;
            }
        }

        /// <summary>
        /// 在给定时长内对整棵树执行透明度渐变。
        /// </summary>
        /// <param name="fromAlpha">起始透明度。</param>
        /// <param name="fromSelectionAlpha">选中状态起始透明度。</param>
        /// <param name="toAlpha">目标透明度。</param>
        /// <param name="toSelectionAlpha">选中状态目标透明度。</param>
        /// <param name="duration">渐变时长。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator AnimateTreeFade(float fromAlpha, float fromSelectionAlpha, float toAlpha, float toSelectionAlpha, float duration)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.05f, duration);
            SetTreeVisualAlpha(fromAlpha, fromSelectionAlpha);
            while (elapsed < safeDuration)
            {
                if (stopRequested) yield break;
                while (pauseRequested && !stopRequested) yield return null;
                elapsed += GetFrameDeltaTime();
                float t = Mathf.Clamp01(elapsed / safeDuration);
                t = t * t * (3f - 2f * t);
                SetTreeVisualAlpha(Mathf.Lerp(fromAlpha, toAlpha, t), Mathf.Lerp(fromSelectionAlpha, toSelectionAlpha, t));
                yield return null;
            }

            SetTreeVisualAlpha(toAlpha, toSelectionAlpha);
        }

        /// <summary>
        /// 若标题当前挂在节点下方，则在销毁节点前先将其临时移出。
        /// </summary>
        private void DetachTitleFromNodeRoot()
        {
            if (titleText == null || nodesRoot == null) return;
            if (!titleText.transform.IsChildOf(nodesRoot)) return;
            CacheTitleLocalOffset();
            titleText.transform.SetParent(transform, true);
        }

        /// <summary>
        /// 若标题当前挂在指定节点下方，则在销毁该节点前先将其临时移出。
        /// </summary>
        /// <param name="node">即将被销毁的节点。</param>
        private void DetachTitleFromSpecificNode(BinaryTreeNode node)
        {
            if (titleText == null || node?.View == null) return;
            if (!titleText.transform.IsChildOf(node.View.transform)) return;
            CacheTitleLocalOffset();
            titleText.transform.SetParent(transform, true);
        }

        /// <summary>
        /// 缓存标题相对根节点的局部偏移。
        /// </summary>
        private void CacheTitleLocalOffset()
        {
            if (titleText == null || hasCachedTitleOffset) return;
            titleLocalOffset = titleText.transform.localPosition;
            if (titleLocalOffset == Vector3.zero) titleLocalOffset = new Vector3(0f, titleOffset, 0f);
            hasCachedTitleOffset = true;
        }
    }
}
