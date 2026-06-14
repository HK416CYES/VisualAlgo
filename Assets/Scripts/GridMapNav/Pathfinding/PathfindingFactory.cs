namespace VisualAlgo.GridMapNav.Pathfinding
{
    /// <summary>
    /// 负责根据算法类型创建具体寻路器实例。
    /// </summary>
    public static class PathfindingFactory
    {
        /// <summary>
        /// 创建指定类型的寻路器实例。
        /// </summary>
        /// <param name="algorithmType">算法类型。</param>
        /// <returns>具体寻路器实例。</returns>
        public static IPathfinder Create(PathfindAlgorithmType algorithmType)
        {
            return algorithmType switch
            {
                PathfindAlgorithmType.BestFS => new BestFirstPathfinder(),
                PathfindAlgorithmType.AStar => new AStarPathfinder(),
                PathfindAlgorithmType.IDAStar => new IdaStarPathfinder(),
                PathfindAlgorithmType.JPS => new JpsPathfinder(),
                PathfindAlgorithmType.BidirectionalBFS => new BidirectionalBfsPathfinder(),
                PathfindAlgorithmType.DFS => new DFSPathfinder(),
                _ => new BFSPathfinder()
            };
        }
    }
}
