using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VisualAlgo.Managers.UI
{
    /// <summary>
    /// 控制左侧面板的展开与收起，并在面板收起时使展开按钮保持可见。
    /// 面板展开时，原本的展开按钮会隐藏，并在面板右上角生成一个“X”关闭按钮用于收起面板。
    /// </summary>
    public sealed class PanelController : MonoBehaviour
    {
        #region 字段与属性定义

        [Header("控制面板")][SerializeField] private RectTransform panel;

        [Header("展开按钮")][SerializeField] private Button openButton;

        [Header("关闭按钮")][SerializeField] private Button closeButton;

        [Header("展开时的X轴位置")][SerializeField] private float visibleX;

        [Header("收起时的X轴位置")][SerializeField] private float hiddenX = -320f;

        [Header("展开/收起动画持续时间")][SerializeField, Min(0.01f)] private float animationDuration = 0.22f;

        [Header("展开按钮边距")][SerializeField, Min(0f)] private float toggleMargin = 8f;

        /// <summary>
        /// 当前正则播放动画的协程引用，用于停止被覆盖的动画。
        /// </summary>
        private Coroutine animationRoutine;

        public static PanelController Instance { get; private set; }

        #endregion

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            PlacePanelAndToggle(visibleX);

            if (openButton != null)
            {
                openButton.onClick.AddListener(() => SetExpanded(true));
                openButton.gameObject.SetActive(false);
            }
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => SetExpanded(false));
                closeButton.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 在脚本销毁时注销事件监听器，避免内存泄漏。
        /// </summary>
        private void OnDestroy()
        {
            if (openButton != null)
                openButton.onClick.RemoveListener(() => SetExpanded(true));

            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 配置面板控制器所需的核心依赖项。
        /// </summary>
        /// <param name="targetPanel">目标面板对象</param>
        /// <param name="toggle">用于控制展开/收起的侧边按钮</param>
        /// <param name="expandedX">面板展开时的X轴坐标</param>
        /// <param name="collapsedX">面板收起时的X轴坐标</param>
        public void Configure(RectTransform targetPanel, float expandedX, float collapsedX)
        {
            panel = targetPanel;
            visibleX = expandedX;
            hiddenX = collapsedX;
            //ResolveReferences();
        }

        /// <summary>
        /// 设置面板的展开或收起状态，并启动相应的过渡动画。
        /// </summary>
        /// <param name="expanded">是否展开</param>
        public void SetExpanded(bool expanded)
        {
            if (animationRoutine != null) StopCoroutine(animationRoutine);

            if (openButton != null && expanded) openButton.gameObject.SetActive(false);
            if (closeButton != null) closeButton.gameObject.SetActive(expanded);

            animationRoutine = StartCoroutine(AnimatePanel(expanded ? visibleX : GetHiddenX(), expanded));
        }

        /// <summary>
        /// 平滑插值面板X轴坐标和按钮位置的协程动画。
        /// </summary>
        /// <param name="targetX">目标X轴坐标</param>
        /// <returns>IEnumerator 协程迭代器</returns>
        private IEnumerator AnimatePanel(float targetX, bool expanded = true)
        {
            if (panel == null) yield break;

            Vector2 startPosition = panel.anchoredPosition;
            Vector2 targetPosition = new Vector2(targetX, startPosition.y);
            float elapsed = 0f;

            while (elapsed < animationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / animationDuration);
                t = Mathf.SmoothStep(0f, 1f, t);
                panel.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);
                yield return null;
            }

            if (openButton != null && !expanded) openButton.gameObject.SetActive(true);
            panel.anchoredPosition = targetPosition;
            animationRoutine = null;
        }

        /// <summary>
        /// 直接定位面板与按钮的位置到指定的X坐标。
        /// </summary>
        /// <param name="panelX">面板的起始X坐标</param>
        private void PlacePanelAndToggle(float panelX)
        {
            if (panel == null) return;

            Vector2 panelPosition = panel.anchoredPosition;
            panelPosition.x = panelX;
            panel.anchoredPosition = panelPosition;
        }

        /// <summary>
        /// 获取面板收起时的最终X轴坐标，通过面板当前计算得出以应对布局变化。
        /// </summary>
        /// <returns>收起时的X轴坐标</returns>
        private float GetHiddenX()
        {
            // 通过当前宽度重新计算，因为布局的缩放可能在反序列化后发生改变
            return Mathf.Min(hiddenX, -GetPanelWidth() - 2f);
        }

        /// <summary>
        /// 安全获取面板宽度（若面板为空则返回默认320）。
        /// </summary>
        /// <returns>面板的计算宽度</returns>
        private float GetPanelWidth()
        {
            if (panel == null) return 320f;
            return Mathf.Max(panel.rect.width, panel.sizeDelta.x);
        }
    }
}
