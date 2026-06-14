using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 贪心最佳优先搜索遍历实现。
    /// </summary>
    public sealed class BestFirstPathfinder : IPathfinder
    {
        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        public string DisplayName => "BestFS";

        /// <summary>
        /// 逐步输出贪心最佳优先搜索的处理方格序列。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="start">起点坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <returns>处理顺序坐标序列。</returns>
        public IEnumerable<GridTraversalStep> EnumerateTraversal(SharedGridData gridData, GridCoordinate start, GridCoordinate goal)
        {
            if (gridData == null)
            {
                yield break;
            }

            List<GridCoordinate> openList = new() { start };
            Dictionary<GridCoordinate, int> priorities = new()
            {
                [start] = PathfindingUtility.GetManhattanDistance(start, goal)
            };
            HashSet<GridCoordinate> visited = new();

            while (openList.Count > 0)
            {
                GridCoordinate current = PathfindingUtility.PopBestNode(openList, priorities);
                if (!visited.Add(current))
                {
                    continue;
                }

                yield return new GridTraversalStep(current, false, current == goal);

                if (current == goal)
                {
                    yield break;
                }

                foreach (GridCoordinate neighbor in gridData.GetWalkableNeighbors(current))
                {
                    if (visited.Contains(neighbor) || openList.Contains(neighbor))
                    {
                        continue;
                    }

                    priorities[neighbor] = PathfindingUtility.GetManhattanDistance(neighbor, goal);
                    openList.Add(neighbor);
                }
            }
        }
    }
}
