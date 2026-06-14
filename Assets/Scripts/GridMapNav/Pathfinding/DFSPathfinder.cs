using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 深度优先搜索遍历实现。
    /// </summary>
    public sealed class DFSPathfinder : IPathfinder
    {
        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        public string DisplayName => "DFS";

        /// <summary>
        /// 逐步输出 DFS 实际处理的方格序列。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="start">起点坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <returns>处理顺序坐标序列。</returns>
        public IEnumerable<GridTraversalStep> EnumerateTraversal(SharedGridData gridData, GridCoordinate start, GridCoordinate goal)
        {
            if (gridData == null) yield break;

            Stack<GridCoordinate> stack = new();
            HashSet<GridCoordinate> visited = new();

            stack.Push(start);

            while (stack.Count > 0)
            {
                GridCoordinate current = stack.Pop();
                if (visited.Contains(current)) continue;

                visited.Add(current);
                yield return new GridTraversalStep(current, false, current == goal);

                if (current == goal) yield break;

                List<GridCoordinate> neighbors = new(gridData.GetWalkableNeighbors(current));
                for (int i = neighbors.Count - 1; i >= 0; i--)
                {
                    GridCoordinate neighbor = neighbors[i];
                    if (!visited.Contains(neighbor)) stack.Push(neighbor);
                }
            }
        }
    }
}
