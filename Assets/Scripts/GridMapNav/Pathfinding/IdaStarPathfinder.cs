using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// IDA* 搜索遍历实现。
    /// </summary>
    public sealed class IdaStarPathfinder : IPathfinder
    {
        /// <summary>
        /// 当前搜索是否已经找到终点。
        /// </summary>
        private bool goalFound;

        /// <summary>
        /// 当前一轮搜索建议的下一阈值。
        /// </summary>
        private int nextBoundCandidate;

        /// <summary>
        /// 当前搜索过程中已经输出过的可视化节点。
        /// </summary>
        private HashSet<GridCoordinate> emittedCells;

        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        public string DisplayName => "IDA*";

        /// <summary>
        /// 逐步输出 IDA* 搜索的处理方格序列。
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

            goalFound = false;
            emittedCells = new HashSet<GridCoordinate>();
            int bound = PathfindingUtility.GetManhattanDistance(start, goal);

            while (!goalFound)
            {
                HashSet<GridCoordinate> pathSet = new() { start };
                Dictionary<GridCoordinate, int> bestDepthMap = new()
                {
                    [start] = 0
                };
                nextBoundCandidate = int.MaxValue;
                List<GridTraversalStep> steps = new();
                DepthLimitedSearch(gridData, start, goal, 0, bound, pathSet, bestDepthMap, steps);
                for (int i = 0; i < steps.Count; i++)
                {
                    yield return steps[i];
                }

                if (goalFound)
                {
                    yield break;
                }

                if (bound == nextBoundCandidate || nextBoundCandidate == int.MaxValue)
                {
                    yield break;
                }

                bound = nextBoundCandidate;
            }
        }

        /// <summary>
        /// 执行一次基于当前阈值的深度优先迭代。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="current">当前坐标。</param>
        /// <param name="goal">终点坐标。</param>
        /// <param name="gScore">当前实际代价。</param>
        /// <param name="bound">本轮阈值。</param>
        /// <param name="pathSet">当前路径上的节点集合。</param>
        /// <param name="nextBound">下一轮建议阈值。</param>
        /// <returns>本轮产生的步骤序列。</returns>
        private void DepthLimitedSearch(
            SharedGridData gridData,
            GridCoordinate current,
            GridCoordinate goal,
            int gScore,
            int bound,
            HashSet<GridCoordinate> pathSet,
            Dictionary<GridCoordinate, int> bestDepthMap,
            List<GridTraversalStep> steps)
        {
            int fScore = gScore + PathfindingUtility.GetManhattanDistance(current, goal);
            if (fScore > bound)
            {
                if (fScore < nextBoundCandidate)
                {
                    nextBoundCandidate = fScore;
                }

                return;
            }

            if (emittedCells.Add(current) || current == goal)
            {
                steps.Add(new GridTraversalStep(current, false, current == goal));
            }

            if (current == goal)
            {
                goalFound = true;
                return;
            }

            List<GridCoordinate> neighbors = new(gridData.GetWalkableNeighbors(current));
            neighbors.Sort((left, right) =>
                PathfindingUtility.GetManhattanDistance(left, goal)
                    .CompareTo(PathfindingUtility.GetManhattanDistance(right, goal)));

            foreach (GridCoordinate neighbor in neighbors)
            {
                if (pathSet.Contains(neighbor))
                {
                    continue;
                }

                int tentativeDepth = gScore + 1;
                if (bestDepthMap.TryGetValue(neighbor, out int recordedDepth) && tentativeDepth >= recordedDepth)
                {
                    continue;
                }

                bestDepthMap[neighbor] = tentativeDepth;
                pathSet.Add(neighbor);
                DepthLimitedSearch(gridData, neighbor, goal, tentativeDepth, bound, pathSet, bestDepthMap, steps);
                pathSet.Remove(neighbor);

                if (goalFound)
                {
                    return;
                }
            }
        }
    }
}
