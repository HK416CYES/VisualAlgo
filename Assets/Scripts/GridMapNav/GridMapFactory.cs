using UnityEngine;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 负责实例化单个网格图预制体的工厂类。
    /// </summary>
    public sealed class GridMapFactory : MonoBehaviour
    {
        /// <summary>
        /// 网格图预制体。
        /// </summary>
        [SerializeField] private GridMapController gridMapPrefab;

        /// <summary>
        /// 所有网格图实例的父节点。
        /// </summary>
        [SerializeField] private Transform mapsRoot;

        /// <summary>
        /// 配置工厂依赖的预制体与容器节点。
        /// </summary>
        /// <param name="prefab">网格图预制体。</param>
        /// <param name="root">实例容器节点。</param>
        public void Configure(GridMapController prefab, Transform root)
        {
            gridMapPrefab = prefab;
            mapsRoot = root;
        }

        /// <summary>
        /// 创建一个新的网格图实例。
        /// </summary>
        /// <param name="mapName">实例名称。</param>
        /// <returns>新建的网格图控制器。</returns>
        public GridMapController CreateMap(string mapName)
        {
            if (gridMapPrefab == null)
            {
                return null;
            }

            Transform parent = mapsRoot == null ? transform : mapsRoot;
            GridMapController instance = Instantiate(gridMapPrefab, parent);
            instance.name = string.IsNullOrWhiteSpace(mapName) ? gridMapPrefab.name : mapName;
            return instance;
        }
    }
}
