using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// A* 搜索遍历实现。
    /// </summary>
    public sealed class AStarPathfinder : IPathfinder
    {
        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        public string DisplayName => "A*";

        /// <summary>
        /// 逐步输出 A* 搜索的处理方格序列。
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
            HashSet<GridCoordinate> closedSet = new();
            Dictionary<GridCoordinate, int> gScores = new()
            {
                [start] = 0
            };
            Dictionary<GridCoordinate, int> priorities = new()
            {
                [start] = PathfindingUtility.GetManhattanDistance(start, goal)
            };
            Dictionary<GridCoordinate, int> tieBreaks = new()
            {
                [start] = PathfindingUtility.GetManhattanDistance(start, goal)
            };

            while (openList.Count > 0)
            {
                GridCoordinate current = PathfindingUtility.PopBestNode(openList, priorities, tieBreaks);
                if (!closedSet.Add(current))
                {
                    continue;
                }

                yield return new GridTraversalStep(current, false, current == goal);

                if (current == goal)
                {
                    yield break;
                }

                int currentG = gScores[current];
                foreach (GridCoordinate neighbor in gridData.GetWalkableNeighbors(current))
                {
                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    int tentativeG = currentG + 1;
                    if (!gScores.TryGetValue(neighbor, out int existingG) || tentativeG < existingG)
                    {
                        int heuristic = PathfindingUtility.GetManhattanDistance(neighbor, goal);
                        gScores[neighbor] = tentativeG;
                        priorities[neighbor] = tentativeG + heuristic;
                        tieBreaks[neighbor] = heuristic;
                        if (!openList.Contains(neighbor))
                        {
                            openList.Add(neighbor);
                        }
                    }
                }
            }
        }
    }
}
