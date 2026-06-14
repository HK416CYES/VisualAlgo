using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;

namespace VisualAlgo.Managers
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraManager : MonoBehaviour
    {
        /// <summary>
        /// 滚轮缩放时的灵敏度。
        /// </summary>
        [SerializeField] private float zoomSensitivity = 0.15f;

        /// <summary>
        /// 鼠标右键拖拽平移时的旧灵敏度乘数；为兼容旧配置保留，但不再参与平移计算。
        /// </summary>
        [SerializeField] private float panSensitivity = 1f;

        /// <summary>
        /// 允许缩放的最小相机正交大小。
        /// </summary>
        [SerializeField] private float minOrthographicSize = 2f;

        /// <summary>
        /// 允许缩放的最大相机正交大小。
        /// </summary>
        [SerializeField] private float maxOrthographicSize = 20f;

        /// <summary>
        /// 当鼠标悬停在 UI 元素上时，是否忽略缩放和平移操作。
        /// </summary>
        [SerializeField] private bool ignorePointerOverUi = true;

        /// <summary>
        /// 自动移动相机到目标位置时的持续时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float focusMoveDuration = 0.45f;

        /// <summary>
        /// 当前脚本控制的目标相机组件。
        /// </summary>
        private Camera controlledCamera;

        /// <summary>
        /// 当前正在执行的自动平移协程。
        /// </summary>
        private Coroutine focusRoutine;

        /// <summary>
        /// 当前是否正在进行右键拖拽。
        /// </summary>
        private bool isPanning;

        /// <summary>
        /// 右键拖拽时上一帧鼠标的屏幕坐标。
        /// </summary>
        private Vector2 lastPanScreenPosition;

        /// <summary>
        /// 获取或设置滚轮缩放灵敏度。
        /// </summary>
        public float ZoomSensitivity
        {
            get => zoomSensitivity;
            set => zoomSensitivity = Mathf.Max(0.01f, value);
        }

        /// <summary>
        /// 获取或设置旧的右键平移灵敏度兼容值。
        /// </summary>
        public float PanSensitivity
        {
            get => panSensitivity;
            set => panSensitivity = Mathf.Max(0.01f, value);
        }

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
        }

        private void Update()
        {
            if (Mathf.Approximately(Time.timeScale, 0f)) return;

            Mouse mouse = Mouse.current;
            if (mouse == null || controlledCamera == null)
            {
                return;
            }

            // 判断鼠标指针是否在 UI 元素上
            bool pointerOverUi = ignorePointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!pointerOverUi)
            {
                ApplyZoom(mouse);
            }

            ApplyPan(mouse);
        }

        /// <summary>
        /// 根据鼠标滚轮输入应用缩放效果。
        /// </summary>
        /// <param name="mouse">当前的鼠标输入设​​备。</param>
        private void ApplyZoom(Mouse mouse)
        {
            float scrollY = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(scrollY, 0f)) return;

            // 将相机当前的 Size 减去按灵敏度换算后的缩放量，并把结果限制在最大最小值范围内
            controlledCamera.orthographicSize = Mathf.Clamp(
                controlledCamera.orthographicSize - scrollY * zoomSensitivity,
                minOrthographicSize,
                maxOrthographicSize);
        }

        /// <summary>
        /// 根据鼠标右键的拖拽输入平移相机的位置。
        /// </summary>
        /// <param name="mouse">当前的鼠标输入设备。</param>
        private void ApplyPan(Mouse mouse)
        {
            if (!mouse.rightButton.isPressed)
            {
                isPanning = false;
                return;
            }

            Vector2 currentScreenPosition = mouse.position.ReadValue();
            if (!isPanning)
            {
                isPanning = true;
                lastPanScreenPosition = currentScreenPosition;
                return;
            }

            Vector3 previousWorldPoint = controlledCamera.ScreenToWorldPoint(new Vector3(lastPanScreenPosition.x, lastPanScreenPosition.y, -transform.position.z));
            Vector3 currentWorldPoint = controlledCamera.ScreenToWorldPoint(new Vector3(currentScreenPosition.x, currentScreenPosition.y, -transform.position.z));
            Vector3 pan = previousWorldPoint - currentWorldPoint;
            pan.z = 0f;
            transform.position += pan;
            lastPanScreenPosition = currentScreenPosition;
        }

        /// <summary>
        /// 平滑移动相机，使目标世界坐标位于屏幕中心。
        /// </summary>
        /// <param name="targetWorldPosition">目标世界坐标。</param>
        public void FocusOn(Vector3 targetWorldPosition)
        {
            if (controlledCamera == null)
                controlledCamera = GetComponent<Camera>();

            if (focusRoutine != null)
                StopCoroutine(focusRoutine);

            focusRoutine = StartCoroutine(FocusRoutine(targetWorldPosition));
        }

        /// <summary>
        /// 执行相机自动平移过程。
        /// </summary>
        /// <param name="targetWorldPosition">目标世界坐标。</param>
        private IEnumerator FocusRoutine(Vector3 targetWorldPosition)
        {
            Vector3 startPosition = transform.position;
            Vector3 endPosition = new(targetWorldPosition.x, targetWorldPosition.y, startPosition.z);
            float elapsed = 0f;

            while (elapsed < focusMoveDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / focusMoveDuration);
                t = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(startPosition, endPosition, t);
                yield return null;
            }

            transform.position = endPosition;
            focusRoutine = null;
        }
    }
}
