using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualAlgo.Managers.UI.StartMenu
{
    /// <summary>
    /// 控制挂载它的按钮对象在鼠标悬停时按固定时长放大，并在移开时按固定时长恢复。
    /// </summary>
    public sealed class StartMenuHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 悬停时相对于初始缩放的倍率。
        /// </summary>
        [SerializeField, Min(0.01f)] private float hoverScaleMultiplier = 1.06f;

        /// <summary>
        /// 缩放过渡所需的固定时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float duration = 0.12f;

        /// <summary>
        /// 按钮初始缩放。
        /// </summary>
        private Vector3 initialScale;

        /// <summary>
        /// 当前过渡的起始缩放。
        /// </summary>
        private Vector3 transitionStartScale;

        /// <summary>
        /// 当前过渡的目标缩放。
        /// </summary>
        private Vector3 transitionTargetScale;

        /// <summary>
        /// 当前过渡已进行的时间。
        /// </summary>
        private float transitionElapsed;

        /// <summary>
        /// 在脚本启用时缓存初始缩放。
        /// </summary>
        private void Awake()
        {
            initialScale = transform.localScale;
            transitionStartScale = initialScale;
            transitionTargetScale = initialScale;
        }

        /// <summary>
        /// 每帧按固定时长插值更新按钮整体缩放。
        /// </summary>
        private void Update()
        {
            if (transform.localScale == transitionTargetScale) return;
            transitionElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(transitionElapsed / duration);
            float easedT = t * t * (3f - 2f * t);
            transform.localScale = Vector3.LerpUnclamped(transitionStartScale, transitionTargetScale, easedT);
        }

        /// <summary>
        /// 鼠标进入按钮区域时开始放大。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            BeginTransition(initialScale * hoverScaleMultiplier);
        }

        /// <summary>
        /// 鼠标离开按钮区域时恢复原始大小。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            BeginTransition(initialScale);
        }

        /// <summary>
        /// 以当前缩放为起点开启新的缩放过渡。
        /// </summary>
        /// <param name="targetScale">目标缩放。</param>
        private void BeginTransition(Vector3 targetScale)
        {
            transitionStartScale = transform.localScale;
            transitionTargetScale = targetScale;
            transitionElapsed = 0f;
        }
    }
}
