using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualAlgo.Managers.UI.StartMenu
{
    /// <summary>
    /// 控制开始菜单二叉树按钮上方红黑树预览的悬停过渡。
    /// </summary>
    public sealed class StartMenuBinaryTreeButtonVisualizer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 预览图绘制组件。
        /// </summary>
        [SerializeField] private StartMenuBinaryTreePreviewGraphic previewGraphic;

        /// <summary>
        /// 鼠标移入时的过渡平滑时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float enterSmoothTime = 0.18f;

        /// <summary>
        /// 鼠标移出时的过渡平滑时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float exitSmoothTime = 0.2f;

        /// <summary>
        /// 当前鼠标是否悬停在按钮上。
        /// </summary>
        private bool isHovered;

        /// <summary>
        /// 进度平滑缓存。
        /// </summary>
        private float progressVelocity;

        /// <summary>
        /// 在脚本启用时补全引用并同步初始状态。
        /// </summary>
        private void Awake()
        {
            if (previewGraphic == null) previewGraphic = GetComponentInChildren<StartMenuBinaryTreePreviewGraphic>(true);
            if (previewGraphic != null) previewGraphic.Progress = 0f;
        }

        /// <summary>
        /// 每帧平滑更新预览进度。
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

            previewGraphic.Progress = nextProgress;
        }

        /// <summary>
        /// 鼠标进入按钮区域时开始播放调整动画。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        /// <summary>
        /// 鼠标离开按钮区域时恢复到未调整状态。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }
    }
}
