using System;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 表示单个方格在共享网格中的基础类型。
    /// </summary>
    public enum GridCellType
    {
        /// <summary>
        /// 普通可通行方格。
        /// </summary>
        Empty,

        /// <summary>
        /// 障碍物方格。
        /// </summary>
        Wall,

        /// <summary>
        /// 起点方格。
        /// </summary>
        Start,

        /// <summary>
        /// 终点方格。
        /// </summary>
        Goal
    }

    /// <summary>
    /// 表示当前全局编辑模式。
    /// </summary>
    public enum GridEditMode
    {
        /// <summary>
        /// 将目标方格设置为墙壁。
        /// </summary>
        PaintWall,

        /// <summary>
        /// 将目标方格恢复为空白。
        /// </summary>
        Erase,

        /// <summary>
        /// 将目标方格设置为起点。
        /// </summary>
        SetStart,

        /// <summary>
        /// 将目标方格设置为终点。
        /// </summary>
        SetGoal
    }

    /// <summary>
    /// 表示网格中的整数坐标。
    /// </summary>
    [Serializable]
    public readonly struct GridCoordinate : IEquatable<GridCoordinate>
    {
        /// <summary>
        /// 水平方向坐标。
        /// </summary>
        public int X { get; }

        /// <summary>
        /// 垂直方向坐标。
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// 构造一个新的网格坐标。
        /// </summary>
        /// <param name="x">水平方向坐标。</param>
        /// <param name="y">垂直方向坐标。</param>
        public GridCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// 判断两个网格坐标是否相等。
        /// </summary>
        /// <param name="other">另一网格坐标。</param>
        /// <returns>若坐标完全相同则返回真。</returns>
        public bool Equals(GridCoordinate other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// 判断当前坐标是否与另一对象表示同一网格坐标。
        /// </summary>
        /// <param name="obj">待比较对象。</param>
        /// <returns>若对象为相同坐标则返回真。</returns>
        public override bool Equals(object obj)
        {
            return obj is GridCoordinate other && Equals(other);
        }

        /// <summary>
        /// 获取当前坐标的哈希值。
        /// </summary>
        /// <returns>哈希值。</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        /// <summary>
        /// 将坐标格式化为文本。
        /// </summary>
        /// <returns>形如 (x, y) 的文本。</returns>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        /// <summary>
        /// 判断两个坐标是否相同。
        /// </summary>
        /// <param name="left">左值。</param>
        /// <param name="right">右值。</param>
        /// <returns>若相同则返回真。</returns>
        public static bool operator ==(GridCoordinate left, GridCoordinate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个坐标是否不同。
        /// </summary>
        /// <param name="left">左值。</param>
        /// <param name="right">右值。</param>
        /// <returns>若不同则返回真。</returns>
        public static bool operator !=(GridCoordinate left, GridCoordinate right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// 表示共享网格中的单个节点数据。
    /// </summary>
    [Serializable]
    public sealed class GridNodeData
    {
        /// <summary>
        /// 节点的水平坐标。
        /// </summary>
        public int X { get; }

        /// <summary>
        /// 节点的垂直坐标。
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// 节点当前的基础类型。
        /// </summary>
        public GridCellType CellType { get; set; }

        /// <summary>
        /// 构造一个新的节点数据对象。
        /// </summary>
        /// <param name="x">水平坐标。</param>
        /// <param name="y">垂直坐标。</param>
        public GridNodeData(int x, int y)
        {
            X = x;
            Y = y;
            CellType = GridCellType.Empty;
        }
    }
}
