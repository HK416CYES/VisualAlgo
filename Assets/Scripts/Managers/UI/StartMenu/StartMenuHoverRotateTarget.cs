using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualAlgo.Managers.UI.StartMenu
{
    /// <summary>
    /// 控制按钮在鼠标悬停时驱动指定 UI 物体持续旋转。
    /// </summary>
    public sealed class StartMenuHoverRotateTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>
        /// 需要被旋转的目标 UI。
        /// </summary>
        [SerializeField] private RectTransform targetRect;

        /// <summary>
        /// 绕 Z 轴的旋转速度，单位为度每秒。
        /// </summary>
        [SerializeField] private float rotationSpeed = 180f;

        /// <summary>
        /// 鼠标移开后回到目标朝向所需的减速时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float returnSmoothTime = 0.12f;

        /// <summary>
        /// 目标图案的旋转对称步进角度。五角星可设为 72。
        /// </summary>
        [SerializeField, Min(0.1f)] private float symmetryStepDegrees = 72f;

        /// <summary>
        /// 目标 UI 在悬停时的缩放倍率。
        /// </summary>
        [SerializeField, Min(0.01f)] private float hoverScaleMultiplier = 1.06f;

        /// <summary>
        /// 目标 UI 缩放过渡所需的固定时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float scaleDuration = 0.12f;

        /// <summary>
        /// 目标 UI 的初始本地旋转。
        /// </summary>
        private Quaternion initialLocalRotation;

        /// <summary>
        /// 目标 UI 初始的 Z 轴角度。
        /// </summary>
        private float initialAngle;

        /// <summary>
        /// 目标 UI 的初始本地缩放。
        /// </summary>
        private Vector3 initialScale;

        /// <summary>
        /// 是否已经缓存过初始旋转。
        /// </summary>
        private bool hasInitialRotation;

        /// <summary>
        /// 当前是否正在悬停旋转。
        /// </summary>
        private bool isHovered;

        /// <summary>
        /// 当前是否正在回到初始旋转。
        /// </summary>
        private bool isReturning;

        /// <summary>
        /// 回位过渡目标的 Z 轴角度。
        /// </summary>
        private float returnTargetAngle;

        /// <summary>
        /// 当前不回绕的 Z 轴角度。
        /// </summary>
        private float currentAngle;

        /// <summary>
        /// 回位阶段的速度缓存。
        /// </summary>
        private float returnVelocity;

        /// <summary>
        /// 当前缩放过渡的起始值。
        /// </summary>
        private Vector3 scaleTransitionStart;

        /// <summary>
        /// 当前缩放过渡的目标值。
        /// </summary>
        private Vector3 scaleTransitionTarget;

        /// <summary>
        /// 当前缩放过渡已进行的时间。
        /// </summary>
        private float scaleTransitionElapsed;

        /// <summary>
        /// 在脚本启用时缓存初始旋转。
        /// </summary>
        private void Awake()
        {
            CacheInitialRotation();
        }

        /// <summary>
        /// 每帧在悬停时更新目标旋转。
        /// </summary>
        private void Update()
        {
            if (targetRect == null) return;
            UpdateScale();
            if (isHovered)
            {
                currentAngle += rotationSpeed * Time.unscaledDeltaTime;
                ApplyCurrentAngle();
                return;
            }

            if (!isReturning) return;
            currentAngle = Mathf.SmoothDamp(currentAngle, returnTargetAngle, ref returnVelocity, returnSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            ApplyCurrentAngle();
            float remainingDelta = Mathf.Abs(returnTargetAngle - currentAngle);
            if (remainingDelta <= 0.05f && Mathf.Abs(returnVelocity) <= 0.05f)
            {
                isReturning = false;
                returnVelocity = 0f;
            }
        }

        /// <summary>
        /// 鼠标进入按钮区域时开始旋转目标 UI。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            CacheInitialRotation();
            isHovered = true;
            isReturning = false;
            returnVelocity = 0f;
            BeginScaleTransition(initialScale * hoverScaleMultiplier);
        }

        /// <summary>
        /// 鼠标离开按钮区域时停止旋转目标 UI。
        /// </summary>
        /// <param name="eventData">指针事件数据。</param>
        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (targetRect == null) return;
            isReturning = true;
            returnTargetAngle = ResolveReturnTargetAngle(currentAngle, initialAngle);
            returnVelocity = 0f;
            BeginScaleTransition(initialScale);
        }

        /// <summary>
        /// 缓存目标 UI 的初始本地旋转。
        /// </summary>
        private void CacheInitialRotation()
        {
            if (targetRect == null || hasInitialRotation) return;
            initialLocalRotation = targetRect.localRotation;
            initialAngle = targetRect.localEulerAngles.z;
            initialScale = targetRect.localScale;
            scaleTransitionStart = initialScale;
            scaleTransitionTarget = initialScale;
            currentAngle = initialAngle;
            hasInitialRotation = true;
        }

        /// <summary>
        /// 计算沿当前旋转方向的最近等效回位角度。
        /// </summary>
        /// <param name="currentAngle">当前 Z 轴角度。</param>
        /// <param name="initialAngle">初始 Z 轴角度。</param>
        /// <returns>沿当前方向的最近等效目标角度。</returns>
        private float ResolveReturnTargetAngle(float currentAngle, float initialAngle)
        {
            float step = Mathf.Max(0.1f, symmetryStepDegrees);
            if (rotationSpeed >= 0f)
            {
                float stepIndex = Mathf.Ceil((currentAngle - initialAngle) / step);
                return initialAngle + stepIndex * step;
            }

            float negativeStepIndex = Mathf.Floor((currentAngle - initialAngle) / step);
            return initialAngle + negativeStepIndex * step;
        }

        /// <summary>
        /// 将当前不回绕角度写回目标 UI。
        /// </summary>
        private void ApplyCurrentAngle()
        {
            Vector3 eulerAngles = targetRect.localEulerAngles;
            eulerAngles.z = currentAngle;
            targetRect.localEulerAngles = eulerAngles;
        }

        /// <summary>
        /// 每帧按固定时长插值更新目标 UI 的缩放。
        /// </summary>
        private void UpdateScale()
        {
            if (targetRect.localScale == scaleTransitionTarget) return;
            scaleTransitionElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(scaleTransitionElapsed / scaleDuration);
            float easedT = t * t * (3f - 2f * t);
            targetRect.localScale = Vector3.LerpUnclamped(scaleTransitionStart, scaleTransitionTarget, easedT);
        }

        /// <summary>
        /// 以当前缩放为起点开启新的目标缩放过渡。
        /// </summary>
        /// <param name="targetScale">目标缩放。</param>
        private void BeginScaleTransition(Vector3 targetScale)
        {
            scaleTransitionStart = targetRect.localScale;
            scaleTransitionTarget = targetScale;
            scaleTransitionElapsed = 0f;
        }

    }
}
