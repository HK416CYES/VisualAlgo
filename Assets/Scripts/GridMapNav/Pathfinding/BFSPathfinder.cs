using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 广度优先搜索遍历实现。
    /// </summary>
    public sealed class BFSPathfinder : IPathfinder
    {
        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        public string DisplayName => "BFS";

        /// <summary>
        /// 逐步输出 BFS 实际处理的方格序列。
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

            Queue<GridCoordinate> queue = new();
            HashSet<GridCoordinate> visited = new();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                GridCoordinate current = queue.Dequeue();
                yield return new GridTraversalStep(current, false, current == goal);

                if (current == goal)
                {
                    yield break;
                }

                foreach (GridCoordinate neighbor in gridData.GetWalkableNeighbors(current))
                {
                    if (visited.Contains(neighbor))
                    {
                        continue;
                    }

                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }
}
