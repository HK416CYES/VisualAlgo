using System.Collections.Generic;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 提供统一的最短路径计算工具，用于在找到终点后高亮全局最短路径。
    /// </summary>
    public static class GridShortestPathUtility
    {
        /// <summary>
        /// 使用广度优先搜索计算起点到终点的最短路径。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="start">起点坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <returns>最短路径坐标序列；若无路径则返回空。</returns>
        public static IReadOnlyList<GridCoordinate> FindShortestPath(SharedGridData gridData, GridCoordinate start, GridCoordinate goal)
        {
            return FindShortestPath(gridData, start, goal, null);
        }

        /// <summary>
        /// 使用广度优先搜索计算起点到终点的最短路径，并可限制在指定可用节点集合内搜索。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="start">起点坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <param name="allowedNodes">允许参与路径计算的节点集合；为空时表示不限。</param>
        /// <returns>最短路径坐标序列；若无路径则返回空。</returns>
        public static IReadOnlyList<GridCoordinate> FindShortestPath(
            SharedGridData gridData,
            GridCoordinate start,
            GridCoordinate goal,
            IReadOnlyCollection<GridCoordinate> allowedNodes)
        {
            if (gridData == null || !gridData.IsInside(start.X, start.Y) || !gridData.IsInside(goal.X, goal.Y))
            {
                return null;
            }

            HashSet<GridCoordinate> allowedSet = null;
            if (allowedNodes != null)
            {
                allowedSet = new HashSet<GridCoordinate>(allowedNodes);
                allowedSet.Add(start);
                allowedSet.Add(goal);
            }

            Queue<GridCoordinate> queue = new();
            Dictionary<GridCoordinate, GridCoordinate> parentMap = new();
            HashSet<GridCoordinate> visited = new();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                GridCoordinate current = queue.Dequeue();
                if (current == goal)
                {
                    return ReconstructPath(parentMap, start, goal);
                }

                foreach (GridCoordinate neighbor in gridData.GetWalkableNeighbors(current))
                {
                    if (allowedSet != null && !allowedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    if (visited.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    parentMap[neighbor] = current;
                    queue.Enqueue(neighbor);
                }
            }

            return null;
        }

        /// <summary>
        /// 根据父节点映射回溯最终路径。
        /// </summary>
        /// <param name="parentMap">父节点映射表。</param>
        /// <param name="start">起点坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <returns>按起点到终点顺序排列的坐标列表。</returns>
        private static IReadOnlyList<GridCoordinate> ReconstructPath(Dictionary<GridCoordinate, GridCoordinate> parentMap, GridCoordinate start, GridCoordinate goal)
        {
            List<GridCoordinate> path = new();
            GridCoordinate current = goal;
            path.Add(current);

            while (current != start && parentMap.TryGetValue(current, out GridCoordinate parent))
            {
                current = parent;
                path.Add(current);
            }

            path.Reverse();
            return path;
        }
    }
}
