using UnityEngine;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 单个网格单元的纯视图组件，负责颜色与尺寸更新。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GridCellView : MonoBehaviour
    {
        /// <summary>
        /// 当前方格的坐标。
        /// </summary>
        private GridCoordinate coordinate;

        /// <summary>
        /// 方格的精灵渲染器。
        /// </summary>
        private SpriteRenderer cachedRenderer;

        /// <summary>
        /// 获取当前方格的网格坐标。
        /// </summary>
        public GridCoordinate Coordinate => coordinate;

        /// <summary>
        /// 初始化方格的坐标与尺寸。
        /// </summary>
        /// <param name="gridCoordinate">网格坐标。</param>
        /// <param name="size">显示边长。</param>
        public void Configure(GridCoordinate gridCoordinate, float size)
        {
            coordinate = gridCoordinate;
            CacheRenderer();
            transform.localScale = new Vector3(size, size, 1f);
        }

        /// <summary>
        /// 设置方格的显示颜色。
        /// </summary>
        /// <param name="color">目标颜色。</param>
        public void SetColor(Color color)
        {
            CacheRenderer();
            if (cachedRenderer != null)
            {
                cachedRenderer.color = color;
            }
        }

        /// <summary>
        /// 设置方格的排序层级。
        /// </summary>
        /// <param name="sortingOrder">新的排序层级。</param>
        public void SetSortingOrder(int sortingOrder)
        {
            CacheRenderer();
            if (cachedRenderer != null)
            {
                cachedRenderer.sortingOrder = sortingOrder;
            }
        }

        /// <summary>
        /// 缓存精灵渲染器引用。
        /// </summary>
        private void CacheRenderer()
        {
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponent<SpriteRenderer>();
            }
        }
    }
}
