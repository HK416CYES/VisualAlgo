using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualAlgo.Managers.UI.StartMenu
{
    /// <summary>
    /// 控制开始菜单中排序按钮上方四个柱状图的悬停排序动画。
    /// </summary>
    public sealed class StartMenuSortButtonVisualizer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 四个柱子的矩形变换引用，顺序对应初始值 3、2、4、1。
        /// </summary>
        [SerializeField] private RectTransform[] barRects = new RectTransform[4];

        /// <summary>
        /// X 轴平滑过渡时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float moveSmoothTime = 0.12f;

        /// <summary>
        /// 柱子初始的 X 轴位置。
        /// </summary>
        private readonly float[] initialXs = new float[4];

        /// <summary>
        /// 柱子排序后的目标 X 轴位置。
        /// </summary>
        private readonly float[] sortedXs = new float[4];

        /// <summary>
        /// 每个柱子当前的平滑速度缓存。
        /// </summary>
        private readonly float[] barVelocities = new float[4];

        /// <summary>
        /// 当前鼠标是否悬停在按钮上。
        /// </summary>
        private bool isHovered;

        /// <summary>
        /// 在脚本启用时补全引用并缓存动画目标位置。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            CacheBarPositions();
            SnapToCurrentState();
        }

        /// <summary>
        /// 每帧更新柱子的平滑横向移动。
        /// </summary>
        private void Update()
        {
            for (int i = 0; i < barRects.Length; i++)
            {
                RectTransform barRect = barRects[i];
                if (barRect == null) continue;

                Vector2 anchoredPosition = barRect.anchoredPosition;
                float targetX = isHovered ? sortedXs[i] : initialXs[i];
                anchoredPosition.x = Mathf.SmoothDamp(anchoredPosition.x, targetX, ref barVelocities[i], moveSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
                barRect.anchoredPosition = anchoredPosition;
            }
        }

        /// <summary>
        /// 鼠标进入按钮区域时切换到排序后状态。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        /// <summary>
        /// 鼠标离开按钮区域时恢复到初始状态。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }

        /// <summary>
        /// 缓存柱子的初始位置和排序后目标位置。
        /// </summary>
        private void CacheBarPositions()
        {
            if (barRects == null || barRects.Length < 4) return;

            float[] slotXs = new float[barRects.Length];
            int[] orderedIndices = new int[barRects.Length];
            for (int i = 0; i < barRects.Length; i++)
            {
                if (barRects[i] == null) continue;
                initialXs[i] = barRects[i].anchoredPosition.x;
                slotXs[i] = initialXs[i];
                orderedIndices[i] = i;
            }

            Array.Sort(slotXs);
            Array.Sort(orderedIndices, CompareBarHeight);
            for (int slotIndex = 0; slotIndex < orderedIndices.Length; slotIndex++)
            {
                int barIndex = orderedIndices[slotIndex];
                sortedXs[barIndex] = slotXs[slotIndex];
            }
        }

        /// <summary>
        /// 将当前柱子位置立即对齐到当前状态，避免初始化瞬间跳动。
        /// </summary>
        private void SnapToCurrentState()
        {
            for (int i = 0; i < barRects.Length; i++)
            {
                if (barRects[i] == null) continue;
                Vector2 anchoredPosition = barRects[i].anchoredPosition;
                anchoredPosition.x = isHovered ? sortedXs[i] : initialXs[i];
                barRects[i].anchoredPosition = anchoredPosition;
                barVelocities[i] = 0f;
            }
        }

        /// <summary>
        /// 从当前按钮子物体中补全标题和柱子引用。
        /// </summary>
        private void ResolveReferences()
        {
            for (int i = 0; i < barRects.Length; i++)
            {
                if (barRects[i] == null) barRects[i] = transform.Find($"Bar_{i}") as RectTransform;
            }
        }

        /// <summary>
        /// 比较两个柱子的当前高度，用于生成升序排列结果。
        /// </summary>
        /// <param name="leftIndex">左侧柱子索引。</param>
        /// <param name="rightIndex">右侧柱子索引。</param>
        /// <returns>高度比较结果。</returns>
        private int CompareBarHeight(int leftIndex, int rightIndex)
        {
            float leftHeight = barRects[leftIndex] == null ? 0f : barRects[leftIndex].sizeDelta.y;
            float rightHeight = barRects[rightIndex] == null ? 0f : barRects[rightIndex].sizeDelta.y;
            int compareResult = leftHeight.CompareTo(rightHeight);
            return compareResult != 0 ? compareResult : leftIndex.CompareTo(rightIndex);
        }
    }
}
