using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VisualAlgo.Managers.UI
{
    /// <summary>
    /// 控制场景说明面板的显示、关闭和“不再显示”配置持久化。
    /// </summary>
    public sealed class SceneInstructionPanelController : MonoBehaviour
    {
        /// <summary>
        /// 说明面板根物体。
        /// </summary>
        [Header("说明面板")][SerializeField] private GameObject instructionPanel;

        /// <summary>
        /// 关闭说明面板按钮。
        /// </summary>
        [SerializeField] private Button closeButton;

        /// <summary>
        /// 不再显示说明面板勾选框。
        /// </summary>
        [SerializeField] private Toggle hideNextTimeToggle;

        /// <summary>
        /// 场景配置标识；为空时默认使用当前场景名。
        /// </summary>
        [Header("配置")][SerializeField] private string sceneKeyOverride;

        /// <summary>
        /// 当前场景配置标识。
        /// </summary>
        private string SceneKey => string.IsNullOrWhiteSpace(sceneKeyOverride) ? SceneManager.GetActiveScene().name : sceneKeyOverride;

        /// <summary>
        /// 初始化说明面板状态并绑定事件。
        /// </summary>
        private void Awake()
        {
            if (instructionPanel == null)
                instructionPanel = gameObject;

            WireUi();
            ApplyInitialVisibility();
        }

        /// <summary>
        /// 释放按钮和勾选框事件。
        /// </summary>
        private void OnDestroy()
        {
            UnwireUi();
        }

        /// <summary>
        /// 关闭说明面板并保存当前勾选状态。
        /// </summary>
        public void ClosePanel()
        {
            SaveHidePreference();
            if (instructionPanel != null)
                instructionPanel.SetActive(false);
        }

        /// <summary>
        /// 打开说明面板并同步勾选状态。
        /// </summary>
        public void OpenPanel()
        {
            bool hideNextTime = UiPlayerSettingsStore.GetHideInstructionPanel(SceneKey);
            if (hideNextTimeToggle != null)
                hideNextTimeToggle.SetIsOnWithoutNotify(hideNextTime);

            if (instructionPanel != null)
                instructionPanel.SetActive(true);
        }

        /// <summary>
        /// 绑定说明面板 UI 事件。
        /// </summary>
        private void WireUi()
        {
            if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
            if (hideNextTimeToggle != null) hideNextTimeToggle.onValueChanged.AddListener(HandleHideNextTimeToggleChanged);
        }

        /// <summary>
        /// 解绑说明面板 UI 事件。
        /// </summary>
        private void UnwireUi()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
            if (hideNextTimeToggle != null) hideNextTimeToggle.onValueChanged.RemoveListener(HandleHideNextTimeToggleChanged);
        }

        /// <summary>
        /// 按当前场景配置决定说明面板初始显示状态。
        /// </summary>
        private void ApplyInitialVisibility()
        {
            bool hideNextTime = UiPlayerSettingsStore.GetHideInstructionPanel(SceneKey);
            if (hideNextTimeToggle != null)
                hideNextTimeToggle.SetIsOnWithoutNotify(hideNextTime);

            if (instructionPanel != null)
                instructionPanel.SetActive(!hideNextTime);
        }

        /// <summary>
        /// 在勾选框状态变更时立即保存配置。
        /// </summary>
        /// <param name="isOn">是否勾选不再显示。</param>
        private void HandleHideNextTimeToggleChanged(bool isOn)
        {
            UiPlayerSettingsStore.SetHideInstructionPanel(SceneKey, isOn);
        }

        /// <summary>
        /// 保存当前勾选框配置。
        /// </summary>
        private void SaveHidePreference()
        {
            if (hideNextTimeToggle == null) return;
            UiPlayerSettingsStore.SetHideInstructionPanel(SceneKey, hideNextTimeToggle.isOn);
        }
    }
}
