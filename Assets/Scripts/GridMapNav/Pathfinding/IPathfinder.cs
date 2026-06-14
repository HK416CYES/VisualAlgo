using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 支持的寻路算法类型。
    /// </summary>
    public enum PathfindAlgorithmType
    {
        /// <summary>
        /// 广度优先搜索。
        /// </summary>
        BFS,

        /// <summary>
        /// 深度优先搜索。
        /// </summary>
        DFS,

        /// <summary>
        /// 贪心最佳优先搜索。
        /// </summary>
        BestFS,

        /// <summary>
        /// A* 搜索。
        /// </summary>
        AStar,

        /// <summary>
        /// IDA* 搜索。
        /// </summary>
        IDAStar,

        /// <summary>
        /// 跳点搜索。
        /// </summary>
        JPS,

        /// <summary>
        /// 双向广度优先搜索。
        /// </summary>
        BidirectionalBFS
    }

    /// <summary>
    /// 表示一次可视化步骤的数据。
    /// </summary>
    public readonly struct GridTraversalStep
    {
        /// <summary>
        /// 当前步骤对应的网格坐标。
        /// </summary>
        public GridCoordinate Coordinate { get; }

        /// <summary>
        /// 当前步骤是否应按跳点特殊颜色显示。
        /// </summary>
        public bool IsJumpPoint { get; }

        /// <summary>
        /// 当前步骤是否表示算法已经真正找到可行路径。
        /// </summary>
        public bool HasReachedGoal { get; }

        /// <summary>
        /// 构造一次新的遍历步骤。
        /// </summary>
        /// <param name="coordinate">目标坐标。</param>
        /// <param name="isJumpPoint">是否为跳点。</param>
        /// <param name="hasReachedGoal">是否已真正找到终点。</param>
        public GridTraversalStep(GridCoordinate coordinate, bool isJumpPoint = false, bool hasReachedGoal = false)
        {
            Coordinate = coordinate;
            IsJumpPoint = isJumpPoint;
            HasReachedGoal = hasReachedGoal;
        }
    }

    /// <summary>
    /// 单个寻路算法的统一接口。
    /// </summary>
    public interface IPathfinder
    {
        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 逐步输出算法实际处理的方格坐标序列。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="start">起点坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <returns>逐步处理的方格序列。</returns>
        IEnumerable<GridTraversalStep> EnumerateTraversal(SharedGridData gridData, GridCoordinate start, GridCoordinate goal);
    }
}
