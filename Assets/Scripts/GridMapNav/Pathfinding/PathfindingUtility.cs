using System.Collections.Generic;

namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 提供寻路算法通用的启发式与开放表辅助方法。
    /// </summary>
    public static class PathfindingUtility
    {
        /// <summary>
        /// 按 UI 显示顺序返回全部算法类型。
        /// </summary>
        public static readonly PathfindAlgorithmType[] AlgorithmOrder =
        {
            PathfindAlgorithmType.BFS,
            PathfindAlgorithmType.DFS,
            PathfindAlgorithmType.BestFS,
            PathfindAlgorithmType.AStar,
            PathfindAlgorithmType.IDAStar,
            PathfindAlgorithmType.JPS,
            PathfindAlgorithmType.BidirectionalBFS
        };

        /// <summary>
        /// 计算两个方格之间的曼哈顿距离。
        /// </summary>
        /// <param name="from">起始坐标。</param>
        /// <param name="to">目标坐标。</param>
        /// <returns>曼哈顿距离。</returns>
        public static int GetManhattanDistance(GridCoordinate from, GridCoordinate to)
        {
            return System.Math.Abs(from.X - to.X) + System.Math.Abs(from.Y - to.Y);
        }

        /// <summary>
        /// 从开放表中取出当前评价值最小的坐标。
        /// </summary>
        /// <param name="openList">开放表。</param>
        /// <param name="priorityMap">优先级映射。</param>
        /// <returns>被取出的最佳坐标。</returns>
        public static GridCoordinate PopBestNode(List<GridCoordinate> openList, Dictionary<GridCoordinate, int> priorityMap)
        {
            int bestIndex = 0;
            int bestPriority = int.MaxValue;

            for (int i = 0; i < openList.Count; i++)
            {
                GridCoordinate candidate = openList[i];
                int candidatePriority = priorityMap.TryGetValue(candidate, out int value) ? value : int.MaxValue;
                if (candidatePriority < bestPriority)
                {
                    bestPriority = candidatePriority;
                    bestIndex = i;
                }
            }

            GridCoordinate bestNode = openList[bestIndex];
            openList.RemoveAt(bestIndex);
            return bestNode;
        }

        /// <summary>
        /// 从开放表中取出当前评价值最小的坐标；评价相同则使用第二优先级打破平局。
        /// </summary>
        /// <param name="openList">开放表。</param>
        /// <param name="priorityMap">主优先级映射。</param>
        /// <param name="tieBreakMap">平局优先级映射。</param>
        /// <returns>被取出的最佳坐标。</returns>
        public static GridCoordinate PopBestNode(
            List<GridCoordinate> openList,
            Dictionary<GridCoordinate, int> priorityMap,
            Dictionary<GridCoordinate, int> tieBreakMap)
        {
            int bestIndex = 0;
            int bestPriority = int.MaxValue;
            int bestTieBreak = int.MaxValue;

            for (int i = 0; i < openList.Count; i++)
            {
                GridCoordinate candidate = openList[i];
                int candidatePriority = priorityMap.TryGetValue(candidate, out int value) ? value : int.MaxValue;
                int candidateTieBreak = tieBreakMap != null && tieBreakMap.TryGetValue(candidate, out int tieValue) ? tieValue : int.MaxValue;
                if (candidatePriority < bestPriority || candidatePriority == bestPriority && candidateTieBreak < bestTieBreak)
                {
                    bestPriority = candidatePriority;
                    bestTieBreak = candidateTieBreak;
                    bestIndex = i;
                }
            }

            GridCoordinate bestNode = openList[bestIndex];
            openList.RemoveAt(bestIndex);
            return bestNode;
        }

        /// <summary>
        /// 获取算法在 UI 中的显示名称。
        /// </summary>
        /// <param name="algorithmType">算法类型。</param>
        /// <returns>显示名称。</returns>
        public static string GetDisplayName(PathfindAlgorithmType algorithmType)
        {
            return algorithmType switch
            {
                PathfindAlgorithmType.DFS => "DFS",
                PathfindAlgorithmType.BestFS => "BestFS",
                PathfindAlgorithmType.AStar => "A*",
                PathfindAlgorithmType.IDAStar => "IDA*",
                PathfindAlgorithmType.JPS => "JPS",
                PathfindAlgorithmType.BidirectionalBFS => "双向BFS",
                _ => "BFS"
            };
        }

        /// <summary>
        /// 根据下拉框索引获取算法类型。
        /// </summary>
        /// <param name="index">下拉框索引。</param>
        /// <returns>对应算法类型。</returns>
        public static PathfindAlgorithmType GetAlgorithmByIndex(int index)
        {
            if (index < 0 || index >= AlgorithmOrder.Length)
            {
                return PathfindAlgorithmType.BFS;
            }

            return AlgorithmOrder[index];
        }

        /// <summary>
        /// 获取指定算法在下拉框中的索引。
        /// </summary>
        /// <param name="algorithmType">算法类型。</param>
        /// <returns>下拉框索引。</returns>
        public static int GetAlgorithmIndex(PathfindAlgorithmType algorithmType)
        {
            for (int i = 0; i < AlgorithmOrder.Length; i++)
            {
                if (AlgorithmOrder[i] == algorithmType)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// 构建供下拉框使用的显示名称列表。
        /// </summary>
        /// <returns>名称列表。</returns>
        public static List<string> BuildDropdownOptions()
        {
            List<string> options = new(AlgorithmOrder.Length);
            for (int i = 0; i < AlgorithmOrder.Length; i++)
            {
                options.Add(GetDisplayName(AlgorithmOrder[i]));
            }

            return options;
        }
    }
}
