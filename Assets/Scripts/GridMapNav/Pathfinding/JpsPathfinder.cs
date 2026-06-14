using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 四方向网格的 JPS 可视化实现。保留跳跃扫描与强迫跳点标记，同时保留普通邻居扩展来保证四联通网格中的完备性。
    /// </summary>
    public sealed class JpsPathfinder : IPathfinder
    {
        /// <summary>
        /// 四联通方向。
        /// </summary>
        private static readonly GridCoordinate[] Directions =
        {
            new(0, 1),
            new(1, 0),
            new(0, -1),
            new(-1, 0)
        };

        /// <summary>
        /// 获取算法显示名称。
        /// </summary>
        public string DisplayName => "JPS";

        /// <summary>
        /// 逐步输出 JPS 的连续可视化搜索序列。
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
            HashSet<GridCoordinate> visualizedCells = new();
            HashSet<GridCoordinate> forcedJumpPoints = new();
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

                if (visualizedCells.Add(current))
                {
                    yield return new GridTraversalStep(current, forcedJumpPoints.Contains(current), current == goal);
                    if (current == goal)
                    {
                        yield break;
                    }
                }
                else if (current == goal)
                {
                    yield return new GridTraversalStep(current, false, true);
                    yield break;
                }

                foreach (GridCoordinate direction in Directions)
                {
                    JumpResult jumpResult = Jump(gridData, current, direction, goal);
                    for (int i = 0; i < jumpResult.ScannedCells.Count; i++)
                    {
                        GridCoordinate scanned = jumpResult.ScannedCells[i];
                        bool isForcedJumpPoint = jumpResult.Found
                            && jumpResult.IsForcedJumpPoint
                            && scanned == jumpResult.JumpPoint;

                        if (isForcedJumpPoint)
                        {
                            forcedJumpPoints.Add(scanned);
                        }

                        if (!visualizedCells.Add(scanned))
                        {
                            continue;
                        }

                        yield return new GridTraversalStep(scanned, isForcedJumpPoint, scanned == goal);
                        if (scanned == goal)
                        {
                            yield break;
                        }
                    }

                    if (jumpResult.Found && !closedSet.Contains(jumpResult.JumpPoint))
                    {
                        TryAddOrRelax(
                            jumpResult.JumpPoint,
                            gScores[current] + jumpResult.Distance,
                            goal,
                            openList,
                            gScores,
                            priorities,
                            tieBreaks);
                    }
                }

                foreach (GridCoordinate neighbor in gridData.GetWalkableNeighbors(current))
                {
                    if (closedSet.Contains(neighbor))
                    {
                        continue;
                    }

                    TryAddOrRelax(
                        neighbor,
                        gScores[current] + 1,
                        goal,
                        openList,
                        gScores,
                        priorities,
                        tieBreaks);
                }
            }
        }

        /// <summary>
        /// 沿指定方向扫描，直到遇到终点、强迫跳点、边界或障碍。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="origin">扫描起点。</param>
        /// <param name="direction">扫描方向。</param>
        /// <param name="goal">终点。</param>
        /// <returns>扫描结果。</returns>
        private static JumpResult Jump(
            SharedGridData gridData,
            GridCoordinate origin,
            GridCoordinate direction,
            GridCoordinate goal)
        {
            JumpResult result = new();
            GridCoordinate current = origin;
            int distance = 0;
            GridCoordinate lastWalkable = origin;

            while (true)
            {
                current = new GridCoordinate(current.X + direction.X, current.Y + direction.Y);
                distance++;

                if (!IsWalkable(gridData, current.X, current.Y))
                {
                    if (lastWalkable != origin)
                    {
                        result.Found = true;
                        result.JumpPoint = lastWalkable;
                        result.Distance = distance - 1;
                    }

                    return result;
                }

                result.ScannedCells.Add(current);
                lastWalkable = current;

                if (current == goal)
                {
                    result.Found = true;
                    result.JumpPoint = current;
                    result.Distance = distance;
                    return result;
                }

                if (HasForcedNeighbor(gridData, current, direction))
                {
                    result.Found = true;
                    result.JumpPoint = current;
                    result.Distance = distance;
                    result.IsForcedJumpPoint = true;
                    return result;
                }
            }
        }

        /// <summary>
        /// 判断当前点沿指定方向是否存在由障碍造成的强迫邻居。
        /// </summary>
        /// <param name="gridData">共享网格数据。</param>
        /// <param name="current">当前点。</param>
        /// <param name="direction">移动方向。</param>
        /// <returns>若存在强迫邻居则返回真。</returns>
        private static bool HasForcedNeighbor(SharedGridData gridData, GridCoordinate current, GridCoordinate direction)
        {
            if (direction.X != 0)
            {
                return IsBlocked(gridData, current.X, current.Y + 1)
                    && IsWalkable(gridData, current.X + direction.X, current.Y + 1)
                    || IsBlocked(gridData, current.X, current.Y - 1)
                    && IsWalkable(gridData, current.X + direction.X, current.Y - 1);
            }

            if (direction.Y != 0)
            {
                return IsBlocked(gridData, current.X + 1, current.Y)
                    && IsWalkable(gridData, current.X + 1, current.Y + direction.Y)
                    || IsBlocked(gridData, current.X - 1, current.Y)
                    && IsWalkable(gridData, current.X - 1, current.Y + direction.Y);
            }

            return false;
        }

        /// <summary>
        /// 尝试加入新节点或用更低代价更新已有节点。
        /// </summary>
        private static void TryAddOrRelax(
            GridCoordinate coordinate,
            int tentativeG,
            GridCoordinate goal,
            List<GridCoordinate> openList,
            Dictionary<GridCoordinate, int> gScores,
            Dictionary<GridCoordinate, int> priorities,
            Dictionary<GridCoordinate, int> tieBreaks)
        {
            if (gScores.TryGetValue(coordinate, out int existingG) && tentativeG >= existingG)
            {
                return;
            }

            int heuristic = PathfindingUtility.GetManhattanDistance(coordinate, goal);
            gScores[coordinate] = tentativeG;
            priorities[coordinate] = tentativeG + heuristic;
            tieBreaks[coordinate] = heuristic;
            if (!openList.Contains(coordinate))
            {
                openList.Add(coordinate);
            }
        }

        /// <summary>
        /// 判断指定坐标是否被阻挡。
        /// </summary>
        private static bool IsBlocked(SharedGridData gridData, int x, int y)
        {
            return !gridData.IsInside(x, y) || gridData.GetCellType(x, y) == GridCellType.Wall;
        }

        /// <summary>
        /// 判断指定坐标是否可通行。
        /// </summary>
        private static bool IsWalkable(SharedGridData gridData, int x, int y)
        {
            return gridData.IsInside(x, y) && gridData.GetCellType(x, y) != GridCellType.Wall;
        }

        /// <summary>
        /// 记录一次跳跃扫描结果。
        /// </summary>
        private sealed class JumpResult
        {
            /// <summary>
            /// 该次扫描经过的连续可通行格子。
            /// </summary>
            public readonly List<GridCoordinate> ScannedCells = new();

            /// <summary>
            /// 是否找到跳点候选或终点。
            /// </summary>
            public bool Found;

            /// <summary>
            /// 跳点候选或终点坐标。
            /// </summary>
            public GridCoordinate JumpPoint;

            /// <summary>
            /// 从扫描起点到跳点候选的距离。
            /// </summary>
            public int Distance;

            /// <summary>
            /// 是否为由障碍造成的强迫跳点。
            /// </summary>
            public bool IsForcedJumpPoint;
        }
    }
}
