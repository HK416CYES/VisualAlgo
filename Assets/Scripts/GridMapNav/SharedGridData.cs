using System;
using System.Collections.Generic;
using UnityEngine;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 存放所有网格图共享的基础网格数据。
    /// </summary>
    public sealed class SharedGridData : MonoBehaviour
    {
        /// <summary>
        /// 当前网格宽度。
        /// </summary>
        [SerializeField, Min(2)] private int width = 10;

        /// <summary>
        /// 当前网格高度。
        /// </summary>
        [SerializeField, Min(2)] private int height = 8;

        /// <summary>
        /// 当前网格中的全部节点数据。
        /// </summary>
        private GridNodeData[,] nodes;

        /// <summary>
        /// 当前唯一的起点坐标。
        /// </summary>
        private GridCoordinate startCoordinate;

        /// <summary>
        /// 当前唯一的终点坐标。
        /// </summary>
        private GridCoordinate goalCoordinate;

        /// <summary>
        /// 当前是否已经设置过起点。
        /// </summary>
        private bool hasStart;

        /// <summary>
        /// 当前是否已经设置过终点。
        /// </summary>
        private bool hasGoal;

        /// <summary>
        /// 当网格尺寸发生重建时触发。
        /// </summary>
        public event Action OnGridRebuilt;

        /// <summary>
        /// 当某个方格基础数据发生变化时触发。
        /// </summary>
        public event Action<GridCoordinate> OnCellChanged;

        /// <summary>
        /// 获取当前网格宽度。
        /// </summary>
        public int Width => width;

        /// <summary>
        /// 获取当前网格高度。
        /// </summary>
        public int Height => height;

        /// <summary>
        /// 获取当前起点坐标。
        /// </summary>
        public GridCoordinate StartCoordinate => startCoordinate;

        /// <summary>
        /// 获取当前终点坐标。
        /// </summary>
        public GridCoordinate GoalCoordinate => goalCoordinate;

        /// <summary>
        /// 获取当前是否存在起点。
        /// </summary>
        public bool HasStart => hasStart;

        /// <summary>
        /// 获取当前是否存在终点。
        /// </summary>
        public bool HasGoal => hasGoal;

        /// <summary>
        /// 在对象激活时保证内部网格数组已初始化。
        /// </summary>
        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// 使用指定尺寸重新初始化整个共享网格。
        /// </summary>
        /// <param name="newWidth">新的宽度。</param>
        /// <param name="newHeight">新的高度。</param>
        public void Initialize(int newWidth, int newHeight)
        {
            width = Mathf.Max(2, newWidth);
            height = Mathf.Max(2, newHeight);
            nodes = new GridNodeData[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    nodes[x, y] = new GridNodeData(x, y);
                }
            }

            hasStart = false;
            hasGoal = false;
            startCoordinate = default;
            goalCoordinate = default;
            OnGridRebuilt?.Invoke();
        }

        /// <summary>
        /// 判断指定坐标是否位于当前网格范围内。
        /// </summary>
        /// <param name="x">水平坐标。</param>
        /// <param name="y">垂直坐标。</param>
        /// <returns>若位于范围内则返回真。</returns>
        public bool IsInside(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// 获取指定坐标处的节点数据。
        /// </summary>
        /// <param name="x">水平坐标。</param>
        /// <param name="y">垂直坐标。</param>
        /// <returns>若坐标有效则返回节点对象，否则返回空。</returns>
        public GridNodeData GetNode(int x, int y)
        {
            EnsureInitialized();
            return IsInside(x, y) ? nodes[x, y] : null;
        }

        /// <summary>
        /// 获取指定坐标处的方格类型。
        /// </summary>
        /// <param name="x">水平坐标。</param>
        /// <param name="y">垂直坐标。</param>
        /// <returns>基础方格类型。</returns>
        public GridCellType GetCellType(int x, int y)
        {
            GridNodeData node = GetNode(x, y);
            return node == null ? GridCellType.Empty : node.CellType;
        }

        /// <summary>
        /// 将某个方格修改为指定类型，并维护唯一的起点与终点。
        /// </summary>
        /// <param name="coordinate">目标坐标。</param>
        /// <param name="newType">目标方格类型。</param>
        public void SetCellType(GridCoordinate coordinate, GridCellType newType)
        {
            EnsureInitialized();
            if (!IsInside(coordinate.X, coordinate.Y))
            {
                return;
            }

            GridNodeData node = nodes[coordinate.X, coordinate.Y];
            if (node.CellType == newType)
            {
                return;
            }

            if (node.CellType == GridCellType.Start)
            {
                hasStart = false;
            }
            else if (node.CellType == GridCellType.Goal)
            {
                hasGoal = false;
            }

            if (newType == GridCellType.Start && hasStart)
            {
                SetCellType(startCoordinate, GridCellType.Empty);
            }

            if (newType == GridCellType.Goal && hasGoal)
            {
                SetCellType(goalCoordinate, GridCellType.Empty);
            }

            node.CellType = newType;

            if (newType == GridCellType.Start)
            {
                startCoordinate = coordinate;
                hasStart = true;
            }
            else if (newType == GridCellType.Goal)
            {
                goalCoordinate = coordinate;
                hasGoal = true;
            }

            OnCellChanged?.Invoke(coordinate);
        }

        /// <summary>
        /// 获取指定方格四联通方向上的可通行邻居坐标。
        /// </summary>
        /// <param name="coordinate">中心坐标。</param>
        /// <returns>邻居坐标序列。</returns>
        public IEnumerable<GridCoordinate> GetWalkableNeighbors(GridCoordinate coordinate)
        {
            EnsureInitialized();

            GridCoordinate[] deltas =
            {
                new GridCoordinate(0, 1),
                new GridCoordinate(1, 0),
                new GridCoordinate(0, -1),
                new GridCoordinate(-1, 0)
            };

            for (int i = 0; i < deltas.Length; i++)
            {
                GridCoordinate delta = deltas[i];
                GridCoordinate neighbor = new(coordinate.X + delta.X, coordinate.Y + delta.Y);
                if (IsInside(neighbor.X, neighbor.Y) && GetCellType(neighbor.X, neighbor.Y) != GridCellType.Wall)
                {
                    yield return neighbor;
                }
            }
        }

        /// <summary>
        /// 保证内部网格数组在使用前已经就绪。
        /// </summary>
        private void EnsureInitialized()
        {
            if (nodes == null || nodes.GetLength(0) != width || nodes.GetLength(1) != height)
            {
                Initialize(width, height);
            }
        }
    }
}
