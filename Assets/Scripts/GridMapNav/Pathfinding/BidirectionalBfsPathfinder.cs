using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 双向广度优先搜索遍历实现。
    /// </summary>
    public sealed class BidirectionalBfsPathfinder : IPathfinder
    {
        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        public string DisplayName => "双向BFS";

        /// <summary>
        /// 逐步输出双向广度优先搜索的处理方格序列。
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

            if (start == goal)
            {
                yield return new GridTraversalStep(start, false, true);
                yield break;
            }

            Queue<GridCoordinate> forwardQueue = new();
            Queue<GridCoordinate> backwardQueue = new();
            HashSet<GridCoordinate> forwardVisited = new() { start };
            HashSet<GridCoordinate> backwardVisited = new() { goal };

            forwardQueue.Enqueue(start);
            backwardQueue.Enqueue(goal);
            bool expandForward = true;

            while (forwardQueue.Count > 0 || backwardQueue.Count > 0)
            {
                bool canExpandForward = forwardQueue.Count > 0;
                bool canExpandBackward = backwardQueue.Count > 0;
                bool useForward = canExpandForward && (!canExpandBackward || expandForward);

                if (useForward)
                {
                    GridCoordinate current = forwardQueue.Dequeue();
                    bool reachedGoal = backwardVisited.Contains(current) && current != start;
                    yield return new GridTraversalStep(current, false, reachedGoal);
                    if (reachedGoal) yield break;

                    foreach (GridCoordinate neighbor in gridData.GetWalkableNeighbors(current))
                    {
                        if (forwardVisited.Add(neighbor))
                        {
                            if (backwardVisited.Contains(neighbor))
                            {
                                yield return new GridTraversalStep(neighbor, false, true);
                                yield break;
                            }

                            forwardQueue.Enqueue(neighbor);
                        }
                    }

                    expandForward = false;
                    continue;
                }

                if (backwardQueue.Count > 0)
                {
                    GridCoordinate current = backwardQueue.Dequeue();
                    yield return new GridTraversalStep(current);

                    foreach (GridCoordinate neighbor in gridData.GetWalkableNeighbors(current))
                    {
                        if (backwardVisited.Add(neighbor))
                        {
                            if (forwardVisited.Contains(neighbor) && neighbor != goal)
                            {
                                yield return new GridTraversalStep(neighbor, false, true);
                                yield break;
                            }

                            backwardQueue.Enqueue(neighbor);
                        }
                    }

                    expandForward = true;
                }
            }
        }
    }
}
