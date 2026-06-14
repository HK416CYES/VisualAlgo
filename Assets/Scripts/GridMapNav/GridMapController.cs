using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VisualAlgo.GridMapNav.Pathfinding;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 单个网格图的控制器，负责网格单元渲染、选中态边框以及单图寻路可视化。
    /// </summary>
    public sealed class GridMapController : MonoBehaviour
    {
        #region 字段及属性定义

        /// <summary>
        /// 用于生成单元格的预制体。
        /// </summary>
        [Header("网格资源")][SerializeField] private GridCellView cellPrefab;

        /// <summary>
        /// 网格单元的父节点。
        /// </summary>
        [SerializeField] private Transform cellsRoot;

        /// <summary>
        /// 网格内部细分边线的父节点。
        /// </summary>
        [SerializeField] private Transform gridLinesRoot;

        /// <summary>
        /// 外围边框线段的父节点。
        /// </summary>
        [Header("边框资源")][SerializeField] private Transform outerBorderRoot;

        /// <summary>
        /// 用于绘制边框的线材质。
        /// </summary>
        [SerializeField] private Material lineMaterial;

        /// <summary>
        /// 显示当前寻路方式的文本组件。
        /// </summary>
        [SerializeField] private TextMeshPro algorithmLabelText;

        /// <summary>
        /// 单个方格的边长。
        /// </summary>
        [Header("网格布局")][SerializeField, Min(0.1f)] private float cellSize = 0.85f;

        /// <summary>
        /// 方格之间的水平与垂直间距。
        /// </summary>
        [SerializeField, Min(0f)] private float cellGap = 0f;

        /// <summary>
        /// 边框相对网格内容的额外留白。
        /// </summary>
        [SerializeField, Min(0f)] private float borderPadding = 0.02f;

        /// <summary>
        /// 边框线条厚度。
        /// </summary>
        [SerializeField, Min(0.01f)] private float borderThickness = 0.16f;

        /// <summary>
        /// 网格内部细边框的线条厚度。
        /// </summary>
        [SerializeField, Min(0.005f)] private float innerLineThickness = 0.03f;

        /// <summary>
        /// 普通空白方格的颜色。
        /// </summary>
        [Header("颜色设置")][SerializeField] private Color emptyColor = new(0.95f, 0.95f, 0.95f, 1f);

        /// <summary>
        /// 棋盘交错时使用的第二种空白方格颜色。
        /// </summary>
        [SerializeField] private Color alternateEmptyColor = new(0.88f, 0.88f, 0.88f, 1f);

        /// <summary>
        /// 墙壁方格的颜色。
        /// </summary>
        [SerializeField] private Color wallColor = new(0.12f, 0.12f, 0.12f, 1f);

        /// <summary>
        /// 起点方格的颜色。
        /// </summary>
        [SerializeField] private Color startColor = new(0.16f, 0.44f, 0.93f, 1f);

        /// <summary>
        /// 终点方格的颜色。
        /// </summary>
        [SerializeField] private Color goalColor = new(0.89f, 0.25f, 0.26f, 1f);

        /// <summary>
        /// 当前正在处理的活动方格颜色。
        /// </summary>
        [SerializeField] private Color activeColor = new(1f, 0.79f, 0.16f, 1f);

        /// <summary>
        /// 已经访问过的方格颜色。
        /// </summary>
        [SerializeField] private Color visitedColor = new(0.72f, 0.78f, 0.85f, 1f);

        /// <summary>
        /// 最短路径高亮颜色。
        /// </summary>
        [SerializeField] private Color pathColor = new(0.25f, 0.8f, 0.38f, 1f);

        /// <summary>
        /// 跳点搜索中跳点的特殊颜色。
        /// </summary>
        [SerializeField] private Color jumpPointColor = new(0.67f, 0.31f, 0.95f, 1f);

        /// <summary>
        /// 未选中时的边框颜色。
        /// </summary>
        [SerializeField] private Color normalBorderColor = Color.black;

        /// <summary>
        /// 选中时的边框颜色。
        /// </summary>
        [SerializeField] private Color selectedBorderColor = new(0.21f, 0.52f, 0.96f, 1f);

        /// <summary>
        /// 该网格图当前使用的寻路算法。
        /// </summary>
        [Header("算法设置")][SerializeField] private PathfindAlgorithmType algorithmType = PathfindAlgorithmType.BFS;

        /// <summary>
        /// 当前网格图对应的共享网格数据。
        /// </summary>
        private SharedGridData sharedGridData;

        /// <summary>
        /// 当前网格图中实例化出的所有单元格视图。
        /// </summary>
        private GridCellView[,] cellViews;

        /// <summary>
        /// 当前网格图已经访问过的方格集合。
        /// </summary>
        private readonly HashSet<GridCoordinate> visitedCells = new();

        /// <summary>
        /// 当前网格图被标记为最短路径的方格集合。
        /// </summary>
        private readonly HashSet<GridCoordinate> pathCells = new();

        /// <summary>
        /// 当前网格图被标记为跳点的方格集合。
        /// </summary>
        private readonly HashSet<GridCoordinate> jumpPointCells = new();

        /// <summary>
        /// 当前算法正在处理的活动方格。
        /// </summary>
        private GridCoordinate? activeCell;

        /// <summary>
        /// 当前寻路协程的引用。
        /// </summary>
        private Coroutine simulationRoutine;

        /// <summary>
        /// 当前内部边线的渲染器集合。
        /// </summary>
        private readonly List<LineRenderer> innerLineRenderers = new();

        /// <summary>
        /// 当前外围边框线段集合。
        /// </summary>
        private readonly List<LineRenderer> outerBorderRenderers = new();

        /// <summary>
        /// 当前网格图的编号。
        /// </summary>
        private int mapIndex = 1;

        /// <summary>
        /// 当前网格图是否处于选中状态。
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// 当前网格图是否处于运行状态。
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// 获取该网格图的编号。
        /// </summary>
        public int MapIndex => mapIndex;

        /// <summary>
        /// 获取该网格图当前使用的算法类型。
        /// </summary>
        public PathfindAlgorithmType AlgorithmType => algorithmType;

        /// <summary>
        /// 获取该网格图当前是否处于运行中。
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// 获取该网格图当前内容区域的世界宽度。
        /// </summary>
        public float ContentWidth => sharedGridData == null ? 0f : CalculateGridWidth(sharedGridData.Width);

        /// <summary>
        /// 获取该网格图当前内容区域的世界高度。
        /// </summary>
        public float ContentHeight => sharedGridData == null ? 0f : CalculateGridHeight(sharedGridData.Height);

        /// <summary>
        /// 获取该网格图带边框留白后的世界宽度。
        /// </summary>
        public float OuterWidth => ContentWidth + borderPadding * 2f;

        /// <summary>
        /// 获取该网格图带边框留白后的世界高度。
        /// </summary>
        public float OuterHeight => ContentHeight + borderPadding * 2f;

        #endregion

        /// <summary>
        /// 初始化该网格图控制器所需的核心依赖。
        /// </summary>
        /// <param name="data">共享网格数据。</param>
        /// <param name="prefab">单元格预制体。</param>
        /// <param name="index">网格图编号。</param>
        public void Configure(SharedGridData data, GridCellView prefab, int index)
        {
            sharedGridData = data;
            cellPrefab = prefab;
            mapIndex = Mathf.Max(1, index);
            ResolveReferences();
            RebindSharedData();
            RebuildGrid();
        }

        /// <summary>
        /// 设置该网格图的编号。
        /// </summary>
        /// <param name="index">新的编号。</param>
        public void SetMapIndex(int index)
        {
            mapIndex = Mathf.Max(1, index);
        }

        /// <summary>
        /// 设置该网格图当前使用的算法类型。
        /// </summary>
        /// <param name="type">新的算法类型。</param>
        public void SetAlgorithm(PathfindAlgorithmType type)
        {
            algorithmType = type;
            RefreshAlgorithmLabel();
        }

        /// <summary>
        /// 设置该网格图是否处于选中状态。
        /// </summary>
        /// <param name="selected">是否选中。</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            RefreshBorderVisual();
        }

        /// <summary>
        /// 设置外围边框粗细，并同步更新内部细线粗细。
        /// </summary>
        /// <param name="thickness">新的外围边框粗细。</param>
        public void SetBorderThickness(float thickness)
        {
            borderThickness = Mathf.Max(0.01f, thickness);
            innerLineThickness = Mathf.Clamp(borderThickness * 0.2f, 0.01f, borderThickness);
            RefreshBorderVisual();
            RefreshInnerGridLinesVisual();
        }

        /// <summary>
        /// 重新构建该网格图的所有单元格视图。
        /// </summary>
        public void RebuildGrid()
        {
            if (sharedGridData == null || cellPrefab == null) return;

            ResolveReferences();
            ClearInstancedCells();

            int width = sharedGridData.Width;
            int height = sharedGridData.Height;
            cellViews = new GridCellView[width, height];

            float gridWidth = CalculateGridWidth(width);
            float gridHeight = CalculateGridHeight(height);
            float minX = -gridWidth * 0.5f + cellSize * 0.5f;
            float minY = -gridHeight * 0.5f + cellSize * 0.5f;
            float step = cellSize + cellGap;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCellView instance = Instantiate(cellPrefab, cellsRoot);
                    instance.name = $"Cell_{x}_{y}";
                    instance.Configure(new GridCoordinate(x, y), cellSize);
                    instance.transform.localPosition = new Vector3(minX + x * step, minY + y * step, 0f);
                    cellViews[x, y] = instance;
                }
            }

            RebuildInnerGridLines();
            ClearSearchVisuals();
            RefreshAllCellVisuals();
            RefreshBorderVisual();
            RefreshAlgorithmLabel();
        }

        /// <summary>
        /// 停止当前网格图的寻路过程，并清空运行状态。
        /// </summary>
        public void StopSimulation(bool refreshVisuals = true)
        {
            if (simulationRoutine != null)
            {
                StopCoroutine(simulationRoutine);
                simulationRoutine = null;
            }

            isRunning = false;
            ClearSearchVisuals();
            if (refreshVisuals) RefreshAllCellVisuals();
        }

        /// <summary>
        /// 启动该网格图的寻路可视化流程。
        /// </summary>
        /// <param name="stepIntervalGetter">用于读取实时步进间隔的委托。</param>
        public void StartSimulation(System.Func<float> stepIntervalGetter)
        {
            StopSimulation();

            if (sharedGridData == null || !sharedGridData.HasStart || !sharedGridData.HasGoal) return;

            IPathfinder pathfinder = PathfindingFactory.Create(algorithmType);
            if (pathfinder == null) return;

            simulationRoutine = StartCoroutine(RunSimulation(pathfinder, stepIntervalGetter));
        }

        /// <summary>
        /// 判断指定世界坐标是否位于该网格图的边框范围内。
        /// </summary>
        /// <param name="worldPosition">待检测的世界坐标。</param>
        /// <returns>若位于边框范围内则返回真。</returns>
        public bool ContainsWorldPoint(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return local.x >= -OuterWidth * 0.5f
                && local.x <= OuterWidth * 0.5f
                && local.y >= -OuterHeight * 0.5f
                && local.y <= OuterHeight * 0.5f;
        }

        /// <summary>
        /// 将世界坐标转换为网格坐标，并判断是否点击到了有效方格。
        /// </summary>
        /// <param name="worldPosition">世界坐标。</param>
        /// <param name="coordinate">转换出的网格坐标。</param>
        /// <returns>若点击到了有效方格则返回真。</returns>
        public bool TryGetCellCoordinate(Vector3 worldPosition, out GridCoordinate coordinate)
        {
            coordinate = default;

            if (sharedGridData == null) return false;

            Vector3 local = transform.InverseTransformPoint(worldPosition);
            float gridWidth = CalculateGridWidth(sharedGridData.Width);
            float gridHeight = CalculateGridHeight(sharedGridData.Height);
            float startX = -gridWidth * 0.5f;
            float startY = -gridHeight * 0.5f;
            float step = cellSize + cellGap;

            float localX = local.x - startX;
            float localY = local.y - startY;

            if (localX < 0f || localY < 0f) return false;

            int x = Mathf.FloorToInt(localX / step);
            int y = Mathf.FloorToInt(localY / step);

            if (!sharedGridData.IsInside(x, y)) return false;

            float remainderX = localX - x * step;
            float remainderY = localY - y * step;
            if (remainderX > cellSize || remainderY > cellSize) return false;

            coordinate = new GridCoordinate(x, y);
            return true;
        }

        /// <summary>
        /// 将共享网格数据中的某个方格修改应用到本图显示。
        /// </summary>
        /// <param name="coordinate">发生变化的方格坐标。</param>
        public void RefreshCell(GridCoordinate coordinate)
        {
            if (cellViews == null || !sharedGridData.IsInside(coordinate.X, coordinate.Y)) return;

            if (coordinate.X < 0 || coordinate.X >= cellViews.GetLength(0) || coordinate.Y < 0 || coordinate.Y >= cellViews.GetLength(1))
                return;

            RefreshCellVisual(coordinate);
        }

        /// <summary>
        /// 在共享网格数据变化后重置该网格图的寻路可视化状态。
        /// </summary>
        public void ResetSearchVisuals()
        {
            ClearSearchVisuals();
            RefreshAllCellVisuals();
        }

        /// <summary>
        /// 在脚本启用时尝试补全组件引用。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>
        /// 在对象销毁时解除共享数据事件订阅。
        /// </summary>
        private void OnDestroy()
        {
            if (sharedGridData != null)
            {
                sharedGridData.OnGridRebuilt -= HandleGridRebuilt;
                sharedGridData.OnCellChanged -= HandleCellChanged;
            }
        }

        /// <summary>
        /// 重新绑定共享数据事件，保证只有一份有效订阅。
        /// </summary>
        private void RebindSharedData()
        {
            if (sharedGridData == null)
            {
                return;
            }

            sharedGridData.OnGridRebuilt -= HandleGridRebuilt;
            sharedGridData.OnCellChanged -= HandleCellChanged;
            sharedGridData.OnGridRebuilt += HandleGridRebuilt;
            sharedGridData.OnCellChanged += HandleCellChanged;
        }

        /// <summary>
        /// 当共享网格被重建时，重建本图的可视对象。
        /// </summary>
        private void HandleGridRebuilt()
        {
            StopSimulation(false);
            RebuildGrid();
        }

        /// <summary>
        /// 当共享网格中的某个方格发生变化时，刷新本图对应的单元格颜色。
        /// </summary>
        /// <param name="coordinate">发生变化的方格坐标。</param>
        private void HandleCellChanged(GridCoordinate coordinate)
        {
            StopSimulation();
            RefreshCell(coordinate);
        }

        /// <summary>
        /// 执行该网格图的逐步寻路协程。
        /// </summary>
        /// <param name="pathfinder">实际执行遍历的算法实例。</param>
        /// <param name="stepIntervalGetter">实时读取步进间隔的委托。</param>
        /// <returns>协程迭代器。</returns>
        private IEnumerator RunSimulation(IPathfinder pathfinder, System.Func<float> stepIntervalGetter)
        {
            isRunning = true;
            ClearSearchVisuals();
            RefreshAllCellVisuals();

            GridCoordinate start = sharedGridData.StartCoordinate;
            GridCoordinate goal = sharedGridData.GoalCoordinate;

            foreach (GridTraversalStep step in pathfinder.EnumerateTraversal(sharedGridData, start, goal))
            {
                GridCoordinate coordinate = step.Coordinate;
                if (activeCell.HasValue)
                {
                    visitedCells.Add(activeCell.Value);
                    activeCell = null;
                }

                if (step.IsJumpPoint)
                {
                    jumpPointCells.Add(coordinate);
                }

                activeCell = coordinate;
                RefreshAllCellVisuals();

                if (step.HasReachedGoal)
                {
                    HashSet<GridCoordinate> searchedNodes = new(visitedCells);
                    searchedNodes.Add(coordinate);
                    IReadOnlyList<GridCoordinate> shortestPath = GridShortestPathUtility.FindShortestPath(sharedGridData, start, goal, searchedNodes);
                    if (shortestPath != null)
                    {
                        pathCells.Clear();
                        for (int i = 0; i < shortestPath.Count; i++)
                        {
                            pathCells.Add(shortestPath[i]);
                        }
                    }

                    activeCell = null;
                    RefreshAllCellVisuals();
                    isRunning = false;
                    simulationRoutine = null;
                    yield break;
                }

                float interval = stepIntervalGetter == null ? 0.1f : Mathf.Max(0.01f, stepIntervalGetter.Invoke());
                yield return new WaitForSeconds(interval);
            }

            if (activeCell.HasValue)
            {
                visitedCells.Add(activeCell.Value);
                activeCell = null;
            }

            RefreshAllCellVisuals();
            isRunning = false;
            simulationRoutine = null;
        }

        /// <summary>
        /// 计算给定列数下的网格总宽度。
        /// </summary>
        /// <param name="width">列数。</param>
        /// <returns>世界空间中的总宽度。</returns>
        private float CalculateGridWidth(int width)
        {
            return width <= 0 ? 0f : width * cellSize + Mathf.Max(0, width - 1) * cellGap;
        }

        /// <summary>
        /// 计算给定行数下的网格总高度。
        /// </summary>
        /// <param name="height">行数。</param>
        /// <returns>世界空间中的总高度。</returns>
        private float CalculateGridHeight(int height)
        {
            return height <= 0 ? 0f : height * cellSize + Mathf.Max(0, height - 1) * cellGap;
        }

        /// <summary>
        /// 清理旧的单元格实例对象。
        /// </summary>
        private void ClearInstancedCells()
        {
            if (cellsRoot == null)
            {
                return;
            }

            for (int i = cellsRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = cellsRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        /// <summary>
        /// 清理旧的内部网格线对象。
        /// </summary>
        private void ClearInnerGridLines()
        {
            innerLineRenderers.Clear();

            if (gridLinesRoot == null)
            {
                return;
            }

            for (int i = gridLinesRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = gridLinesRoot.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        /// <summary>
        /// 清空当前网格图的搜索状态缓存。
        /// </summary>
        private void ClearSearchVisuals()
        {
            visitedCells.Clear();
            pathCells.Clear();
            jumpPointCells.Clear();
            activeCell = null;
        }

        /// <summary>
        /// 刷新整个网格图中所有方格的显示颜色。
        /// </summary>
        private void RefreshAllCellVisuals()
        {
            if (sharedGridData == null || cellViews == null)
            {
                return;
            }

            int width = cellViews.GetLength(0);
            int height = cellViews.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    RefreshCellVisual(new GridCoordinate(x, y));
                }
            }
        }

        /// <summary>
        /// 刷新单个方格的显示颜色。
        /// </summary>
        /// <param name="coordinate">目标方格坐标。</param>
        private void RefreshCellVisual(GridCoordinate coordinate)
        {
            if (cellViews == null
                || coordinate.X < 0
                || coordinate.Y < 0
                || coordinate.X >= cellViews.GetLength(0)
                || coordinate.Y >= cellViews.GetLength(1))
            {
                return;
            }

            GridCellView view = cellViews[coordinate.X, coordinate.Y];
            if (view == null)
            {
                return;
            }

            Color color = ResolveCellColor(coordinate);
            view.SetColor(color);
            GridCellType cellType = sharedGridData.GetCellType(coordinate.X, coordinate.Y);
            bool isStartOrGoal = cellType == GridCellType.Start || cellType == GridCellType.Goal;
            view.SetSortingOrder(isStartOrGoal ? 25 : pathCells.Contains(coordinate) ? 20 : activeCell == coordinate ? 15 : 0);
        }

        /// <summary>
        /// 根据共享数据与运行态叠加信息求出某个方格的最终显示颜色。
        /// </summary>
        /// <param name="coordinate">目标方格坐标。</param>
        /// <returns>最终颜色。</returns>
        private Color ResolveCellColor(GridCoordinate coordinate)
        {
            GridCellType cellType = sharedGridData.GetCellType(coordinate.X, coordinate.Y);
            if (cellType == GridCellType.Start)
            {
                return startColor;
            }

            if (cellType == GridCellType.Goal)
            {
                return goalColor;
            }

            if (pathCells.Contains(coordinate))
            {
                return pathColor;
            }

            if (activeCell.HasValue && activeCell.Value == coordinate)
            {
                return activeColor;
            }

            if (jumpPointCells.Contains(coordinate))
            {
                return jumpPointColor;
            }

            if (visitedCells.Contains(coordinate))
            {
                return visitedColor;
            }

            return cellType switch
            {
                GridCellType.Wall => wallColor,
                _ => ((coordinate.X + coordinate.Y) & 1) == 0 ? emptyColor : alternateEmptyColor
            };
        }

        /// <summary>
        /// 重建棋盘内部的细边框线条。
        /// </summary>
        private void RebuildInnerGridLines()
        {
            ResolveReferences();
            ClearInnerGridLines();

            if (gridLinesRoot == null || sharedGridData == null)
            {
                return;
            }

            int width = sharedGridData.Width;
            int height = sharedGridData.Height;
            float gridWidth = CalculateGridWidth(width);
            float gridHeight = CalculateGridHeight(height);
            float step = cellSize + cellGap;
            float minX = -gridWidth * 0.5f;
            float minY = -gridHeight * 0.5f;
            for (int x = 1; x < width; x++)
            {
                float xPosition = minX + x * step - cellGap * 0.5f;
                CreateInnerLine(
                    $"Vertical Line {x}",
                    new Vector3(xPosition, 0f, 0f),
                    new Vector3(0f, (gridHeight + innerLineThickness) * 0.5f, 0f),
                    new Vector3(0f, -(gridHeight + innerLineThickness) * 0.5f, 0f));
            }

            for (int y = 1; y < height; y++)
            {
                float yPosition = minY + y * step - cellGap * 0.5f;
                CreateInnerLine(
                    $"Horizontal Line {y}",
                    new Vector3(0f, yPosition, 0f),
                    new Vector3(-(gridWidth + innerLineThickness) * 0.5f, 0f, 0f),
                    new Vector3((gridWidth + innerLineThickness) * 0.5f, 0f, 0f));
            }
        }

        /// <summary>
        /// 创建一条内部细边框线。
        /// </summary>
        /// <param name="lineName">线条对象名称。</param>
        /// <param name="localPosition">局部坐标。</param>
        /// <param name="startPoint">局部起点。</param>
        /// <param name="endPoint">局部终点。</param>
        private void CreateInnerLine(string lineName, Vector3 localPosition, Vector3 startPoint, Vector3 endPoint)
        {
            GameObject lineObject = new(lineName);
            lineObject.transform.SetParent(gridLinesRoot, false);
            lineObject.transform.localPosition = localPosition;

            LineRenderer renderer = lineObject.AddComponent<LineRenderer>();
            ConfigureLineRenderer(renderer, innerLineThickness, Color.black, 30);
            renderer.positionCount = 2;
            renderer.useWorldSpace = false;
            renderer.SetPosition(0, startPoint);
            renderer.SetPosition(1, endPoint);
            innerLineRenderers.Add(renderer);
        }

        /// <summary>
        /// 刷新边框线条的尺寸、位置与颜色。
        /// </summary>
        private void RefreshBorderVisual()
        {
            ResolveReferences();
            EnsureOuterBorderRenderers();

            float width = OuterWidth;
            float height = OuterHeight;
            Color color = isSelected ? selectedBorderColor : normalBorderColor;

            if (outerBorderRenderers.Count < 4) return;

            ConfigureBorderLine(
                outerBorderRenderers[0],
                new Vector3(-width * 0.5f, height * 0.5f, 0f),
                new Vector3(width * 0.5f, height * 0.5f, 0f),
                color);
            ConfigureBorderLine(
                outerBorderRenderers[1],
                new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, -height * 0.5f, 0f),
                color);
            ConfigureBorderLine(
                outerBorderRenderers[2],
                new Vector3(-width * 0.5f, -height * 0.5f, 0f),
                new Vector3(-width * 0.5f, height * 0.5f, 0f),
                color);
            ConfigureBorderLine(
                outerBorderRenderers[3],
                new Vector3(width * 0.5f, -height * 0.5f, 0f),
                new Vector3(width * 0.5f, height * 0.5f, 0f),
                color);
        }

        /// <summary>
        /// 刷新网格图上方的算法名称显示。
        /// </summary>
        private void RefreshAlgorithmLabel()
        {
            EnsureAlgorithmLabel();
            if (algorithmLabelText == null)
            {
                return;
            }

            algorithmLabelText.text = PathfindingUtility.GetDisplayName(algorithmType);
            algorithmLabelText.transform.localPosition = new Vector3(0f, OuterHeight * 0.5f + 0.5f, 0f);
        }

        /// <summary>
        /// 刷新内部细边线的粗细。
        /// </summary>
        private void RefreshInnerGridLinesVisual()
        {
            for (int i = 0; i < innerLineRenderers.Count; i++)
            {
                if (innerLineRenderers[i] != null)
                {
                    innerLineRenderers[i].startWidth = innerLineThickness;
                    innerLineRenderers[i].endWidth = innerLineThickness;
                }
            }
        }

        /// <summary>
        /// 保证外围四条边框线已创建完成。
        /// </summary>
        private void EnsureOuterBorderRenderers()
        {
            ResolveReferences();
            if (outerBorderRoot == null)
            {
                return;
            }

            outerBorderRenderers.Clear();
            string[] borderNames = { "Top Border", "Bottom Border", "Left Border", "Right Border" };
            for (int i = 0; i < borderNames.Length; i++)
            {
                Transform child = outerBorderRoot.Find(borderNames[i]);
                if (child == null)
                {
                    GameObject borderObject = new(borderNames[i]);
                    borderObject.transform.SetParent(outerBorderRoot, false);
                    child = borderObject.transform;
                }

                LineRenderer renderer = child.GetComponent<LineRenderer>();
                if (renderer == null)
                {
                    renderer = child.gameObject.AddComponent<LineRenderer>();
                }

                ConfigureLineRenderer(renderer, borderThickness, normalBorderColor, 40);
                outerBorderRenderers.Add(renderer);
            }
        }

        /// <summary>
        /// 配置单条外围边框线段的位置、颜色与粗细。
        /// </summary>
        /// <param name="renderer">目标线渲染器。</param>
        /// <param name="startPoint">局部起点。</param>
        /// <param name="endPoint">局部终点。</param>
        /// <param name="color">颜色。</param>
        private void ConfigureBorderLine(LineRenderer renderer, Vector3 startPoint, Vector3 endPoint, Color color)
        {
            if (renderer == null) return;

            ConfigureLineRenderer(renderer, borderThickness, color, 40);
            renderer.positionCount = 2;
            renderer.useWorldSpace = false;
            renderer.SetPosition(0, startPoint);
            renderer.SetPosition(1, endPoint);
        }

        /// <summary>
        /// 对指定线渲染器应用统一的显示配置。
        /// </summary>
        /// <param name="renderer">目标线渲染器。</param>
        /// <param name="thickness">线宽。</param>
        /// <param name="color">颜色。</param>
        /// <param name="sortingOrder">排序层级。</param>
        private void ConfigureLineRenderer(LineRenderer renderer, float thickness, Color color, int sortingOrder)
        {
            if (renderer == null) return;

            renderer.sharedMaterial = GetOrCreateLineMaterial();
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.alignment = LineAlignment.TransformZ;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.loop = false;
            renderer.numCapVertices = 6;
            renderer.numCornerVertices = 0;
            renderer.startWidth = thickness;
            renderer.endWidth = thickness;
            renderer.startColor = color;
            renderer.endColor = color;
            renderer.sortingOrder = sortingOrder;
        }

        /// <summary>
        /// 获取或创建边框线条共用材质。
        /// </summary>
        /// <returns>线条材质。</returns>
        private Material GetOrCreateLineMaterial()
        {
            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                lineMaterial = shader == null ? null : new Material(shader)
                {
                    name = "Grid Line Material"
                };
            }

            return lineMaterial;
        }

        /// <summary>
        /// 解析并补全核心子节点与组件引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (cellsRoot == null)
            {
                Transform found = transform.Find("Cells");
                if (found != null)
                {
                    cellsRoot = found;
                }
            }

            if (gridLinesRoot == null)
            {
                Transform found = transform.Find("Grid Lines");
                if (found != null)
                {
                    gridLinesRoot = found;
                }
            }

            if (outerBorderRoot == null)
            {
                Transform found = transform.Find("Outer Borders");
                if (found != null)
                {
                    outerBorderRoot = found;
                }
            }

            if (algorithmLabelText == null)
            {
                Transform found = transform.Find("Algorithm Label");
                if (found != null)
                {
                    algorithmLabelText = found.GetComponent<TextMeshPro>();
                }
            }
        }

        /// <summary>
        /// 保证算法显示文本存在并完成基础配置。
        /// </summary>
        private void EnsureAlgorithmLabel()
        {
            if (algorithmLabelText == null)
            {
                GameObject labelObject = new("Algorithm Label");
                labelObject.transform.SetParent(transform, false);
                algorithmLabelText = labelObject.AddComponent<TextMeshPro>();
            }

            algorithmLabelText.alignment = TextAlignmentOptions.Center;
            if (algorithmLabelText.font == null)
            {
                algorithmLabelText.font = TMP_Settings.defaultFontAsset;
            }
            algorithmLabelText.fontSize = 4.8f;
            algorithmLabelText.color = Color.black;
            algorithmLabelText.textWrappingMode = TextWrappingModes.NoWrap;
            algorithmLabelText.sortingOrder = 50;
        }
    }
}
