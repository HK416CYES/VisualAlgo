using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualAlgo.Managers.UI.StartMenu
{
    /// <summary>
    /// 控制开始菜单寻路按钮上方预览图的悬停动画进度。
    /// </summary>
    public sealed class StartMenuGridMapButtonVisualizer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 预览图绘制组件。
        /// </summary>
        [SerializeField] private StartMenuGridMapPreviewGraphic previewGraphic;

        /// <summary>
        /// 鼠标移入时路径动画的平滑时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float enterSmoothTime = 0.14f;

        /// <summary>
        /// 鼠标移出时路径动画的平滑时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float exitSmoothTime = 0.2f;

        /// <summary>
        /// 起点圆在鼠标移出时的缩小平滑时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float startNodeExitSmoothTime = 0.16f;

        /// <summary>
        /// 起点圆在最终收尾阶段使用的更快平滑时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float startNodeFinalExitSmoothTime = 0.1f;

        /// <summary>
        /// 路径回收末段内，起点圆开始同步缩小的进度范围。
        /// </summary>
        [SerializeField, Range(0.01f, 0.5f)] private float startNodeOverlapRange = 0.18f;

        /// <summary>
        /// 当前鼠标是否悬停在按钮上。
        /// </summary>
        private bool isHovered;

        /// <summary>
        /// 路径动画进度的当前速度缓存。
        /// </summary>
        private float progressVelocity;

        /// <summary>
        /// 起点圆独立显隐进度的速度缓存。
        /// </summary>
        private float startNodeVelocity;

        /// <summary>
        /// 在脚本启用时补全引用并同步初始状态。
        /// </summary>
        private void Awake()
        {
            if (previewGraphic == null) previewGraphic = GetComponentInChildren<StartMenuGridMapPreviewGraphic>(true);
            if (previewGraphic == null) return;
            previewGraphic.Progress = 0f;
            previewGraphic.StartNodeProgress = 0f;
        }

        /// <summary>
        /// 每帧平滑更新路径绘制进度。
        /// </summary>
        private void Update()
        {
            if (previewGraphic == null) return;
            float targetProgress = isHovered ? 1f : 0f;
            float smoothTime = isHovered ? enterSmoothTime : exitSmoothTime;
            float nextProgress = Mathf.SmoothDamp(previewGraphic.Progress, targetProgress, ref progressVelocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (!isHovered && nextProgress <= 0.001f)
            {
                nextProgress = 0f;
                progressVelocity = 0f;
            }

            float startNodeTarget = ResolveStartNodeTarget(nextProgress);
            float startNodeSmoothTime = ResolveStartNodeSmoothTime(nextProgress);
            float nextStartNodeProgress = Mathf.SmoothDamp(previewGraphic.StartNodeProgress, startNodeTarget, ref startNodeVelocity, startNodeSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            if (!isHovered && nextStartNodeProgress <= 0.001f)
            {
                nextStartNodeProgress = 0f;
                startNodeVelocity = 0f;
            }

            previewGraphic.Progress = nextProgress;
            previewGraphic.StartNodeProgress = nextStartNodeProgress;
        }

        /// <summary>
        /// 计算起点圆当前应追踪的目标显隐值。
        /// </summary>
        /// <param name="nextProgress">本帧主路径进度。</param>
        /// <returns>起点圆目标显隐值。</returns>
        private float ResolveStartNodeTarget(float nextProgress)
        {
            if (isHovered) return 1f;
            if (nextProgress <= 0f) return 0f;

            float overlapT = Mathf.Clamp01(nextProgress / Mathf.Max(startNodeOverlapRange, 0.01f));
            return 0.5f + 0.5f * overlapT;
        }

        /// <summary>
        /// 计算起点圆当前应使用的平滑时间。
        /// </summary>
        /// <param name="nextProgress">本帧主路径进度。</param>
        /// <returns>平滑时间。</returns>
        private float ResolveStartNodeSmoothTime(float nextProgress)
        {
            if (isHovered) return enterSmoothTime;
            return nextProgress <= 0.05f ? startNodeFinalExitSmoothTime : startNodeExitSmoothTime;
        }

        /// <summary>
        /// 鼠标进入按钮区域时开始绘制路径。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        /// <summary>
        /// 鼠标离开按钮区域时收回路径。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }
    }
}
