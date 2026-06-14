using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VisualAlgo.Managers.UI
{
    /// <summary>
    /// 控制 ESC 暂停面板、返回游戏、返回主菜单、退出游戏、缩放灵敏度和屏幕设置。
    /// </summary>
    public sealed class PauseMenuController : MonoBehaviour
    {
        /// <summary>
        /// 暂停时显示的 UI 根物体。
        /// </summary>
        [Header("暂停面板")][SerializeField] private GameObject pausePanel;

        /// <summary>
        /// 控制滚轮缩放灵敏度的滑条。
        /// </summary>
        [SerializeField] private Slider zoomSensitivitySlider;

        /// <summary>
        /// 控制屏幕模式的下拉框。
        /// </summary>
        [SerializeField] private TMP_Dropdown screenModeDropdown;

        /// <summary>
        /// 控制窗口模式分辨率的下拉框。
        /// </summary>
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        /// <summary>
        /// 返回游戏按钮。
        /// </summary>
        [Header("按钮")][SerializeField] private Button resumeButton;

        /// <summary>
        /// 返回主菜单按钮。
        /// </summary>
        [SerializeField] private Button mainMenuButton;

        /// <summary>
        /// 退出游戏按钮。
        /// </summary>
        [SerializeField] private Button quitButton;

        /// <summary>
        /// 被暂停面板调节的相机控制器。
        /// </summary>
        [Header("相机控制器")][SerializeField] private CameraManager cameraManager;

        /// <summary>
        /// 返回主菜单时加载的场景序号。
        /// </summary>
        [Header("主菜单序号")][SerializeField] private int mainMenuSceneIndex;

        /// <summary>
        /// 当前游戏是否处于暂停状态。
        /// </summary>
        private bool isPaused;

        /// <summary>
        /// 当前缓存的玩家 UI 配置。
        /// </summary>
        private UiPlayerSettingsData cachedSettings;

        /// <summary>
        /// 设备支持的分辨率列表。
        /// </summary>
        private ResolutionOption[] resolutionOptions = System.Array.Empty<ResolutionOption>();

        /// <summary>
        /// 是否正在由代码同步 UI，避免递归触发回调。
        /// </summary>
        private bool suppressDisplayCallbacks;

        /// <summary>
        /// 当前屏幕设置应用协程。
        /// </summary>
        private Coroutine applyDisplayRoutine;

        /// <summary>
        /// 屏幕模式枚举。
        /// </summary>
        private enum ScreenModeOption
        {
            FullScreen = 0,
            Windowed = 1
        }

        /// <summary>
        /// 单个分辨率选项。
        /// </summary>
        private readonly struct ResolutionOption
        {
            /// <summary>
            /// 宽度。
            /// </summary>
            public readonly int Width;

            /// <summary>
            /// 高度。
            /// </summary>
            public readonly int Height;

            /// <summary>
            /// 构造分辨率选项。
            /// </summary>
            /// <param name="width">宽度。</param>
            /// <param name="height">高度。</param>
            public ResolutionOption(int width, int height)
            {
                Width = width;
                Height = height;
            }

            /// <summary>
            /// 返回显示文本。
            /// </summary>
            /// <returns>格式化分辨率文本。</returns>
            public override string ToString() => $"{Width} x {Height}";
        }

        /// <summary>
        /// 初始化引用、配置和 UI 事件。
        /// </summary>
        private void Awake()
        {
            if (cameraManager == null && Camera.main != null)
                cameraManager = Camera.main.GetComponent<CameraManager>();

            cachedSettings = UiPlayerSettingsStore.Load();
            BuildScreenModeOptions();
            BuildResolutionOptions();
            WireUi();
            SyncZoomUiFromSettings();
            SyncDisplayUiFromSettings();
            ApplyLoadedDisplaySettings();
            SetPaused(false);
        }

        /// <summary>
        /// 销毁时释放 UI 事件并恢复时间缩放。
        /// </summary>
        private void OnDestroy()
        {
            UnwireUi();
            if (isPaused) Time.timeScale = 1f;
        }

        /// <summary>
        /// 监听 ESC 键切换暂停状态。
        /// </summary>
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                SetPaused(!isPaused);
        }

        /// <summary>
        /// 绑定暂停面板 UI 事件。
        /// </summary>
        private void WireUi()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
            if (zoomSensitivitySlider != null) zoomSensitivitySlider.onValueChanged.AddListener(SetZoomSensitivity);
            if (screenModeDropdown != null) screenModeDropdown.onValueChanged.AddListener(HandleScreenModeChanged);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.AddListener(HandleResolutionChanged);
        }

        /// <summary>
        /// 解绑暂停面板 UI 事件。
        /// </summary>
        private void UnwireUi()
        {
            if (resumeButton != null) resumeButton.onClick.RemoveListener(ResumeGame);
            if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
            if (zoomSensitivitySlider != null) zoomSensitivitySlider.onValueChanged.RemoveListener(SetZoomSensitivity);
            if (screenModeDropdown != null) screenModeDropdown.onValueChanged.RemoveListener(HandleScreenModeChanged);
            if (resolutionDropdown != null) resolutionDropdown.onValueChanged.RemoveListener(HandleResolutionChanged);
        }

        /// <summary>
        /// 构建屏幕模式下拉框选项。
        /// </summary>
        private void BuildScreenModeOptions()
        {
            if (screenModeDropdown == null) return;
            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(new List<string> { "全屏模式", "窗口模式" });
        }

        /// <summary>
        /// 构建设备支持的分辨率下拉框选项。
        /// </summary>
        private void BuildResolutionOptions()
        {
            List<ResolutionOption> options = new();
            Resolution[] supportedResolutions = Screen.resolutions;
            for (int index = 0; index < supportedResolutions.Length; index++)
            {
                Resolution resolution = supportedResolutions[index];
                bool exists = false;
                for (int optionIndex = 0; optionIndex < options.Count; optionIndex++)
                {
                    if (options[optionIndex].Width != resolution.width || options[optionIndex].Height != resolution.height) continue;
                    exists = true;
                    break;
                }

                if (!exists) options.Add(new ResolutionOption(resolution.width, resolution.height));
            }

            if (options.Count == 0) options.Add(new ResolutionOption(Screen.currentResolution.width, Screen.currentResolution.height));
            resolutionOptions = options.ToArray();

            if (resolutionDropdown == null) return;
            resolutionDropdown.ClearOptions();
            List<string> labels = new(resolutionOptions.Length);
            for (int index = 0; index < resolutionOptions.Length; index++) labels.Add(resolutionOptions[index].ToString());
            resolutionDropdown.AddOptions(labels);
        }

        /// <summary>
        /// 根据配置同步缩放灵敏度 UI。
        /// </summary>
        private void SyncZoomUiFromSettings()
        {
            if (cameraManager != null) cameraManager.ZoomSensitivity = cachedSettings.ZoomSensitivity;
            if (zoomSensitivitySlider != null) zoomSensitivitySlider.SetValueWithoutNotify(cachedSettings.ZoomSensitivity);
        }

        /// <summary>
        /// 根据配置同步显示设置 UI。
        /// </summary>
        private void SyncDisplayUiFromSettings()
        {
            suppressDisplayCallbacks = true;

            ScreenModeOption mode = NormalizeStoredMode(cachedSettings.ScreenMode);
            if (screenModeDropdown != null)
            {
                screenModeDropdown.SetValueWithoutNotify((int)mode);
                screenModeDropdown.RefreshShownValue();
            }

            ResolutionOption displayedResolution = mode == ScreenModeOption.FullScreen
                ? GetCurrentDesktopResolutionOption()
                : GetStoredWindowedResolutionOption();

            if (resolutionDropdown != null)
            {
                resolutionDropdown.SetValueWithoutNotify(FindResolutionOptionIndex(displayedResolution.Width, displayedResolution.Height));
                resolutionDropdown.RefreshShownValue();
                resolutionDropdown.interactable = mode == ScreenModeOption.Windowed;
            }

            suppressDisplayCallbacks = false;
        }

        /// <summary>
        /// 在启动时应用缓存的显示设置。
        /// </summary>
        private void ApplyLoadedDisplaySettings()
        {
            ApplyDisplaySettings(false);
        }

        /// <summary>
        /// 设置暂停状态和暂停面板显示状态。
        /// </summary>
        /// <param name="paused">是否暂停。</param>
        private void SetPaused(bool paused)
        {
            isPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(paused);
        }

        /// <summary>
        /// 返回游戏。
        /// </summary>
        public void ResumeGame()
        {
            SetPaused(false);
        }

        /// <summary>
        /// 返回主菜单场景。
        /// </summary>
        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneIndex);
        }

        /// <summary>
        /// 退出游戏；在编辑器内则停止播放。
        /// </summary>
        public void QuitGame()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 设置滚轮缩放灵敏度并保存。
        /// </summary>
        /// <param name="value">新的灵敏度值。</param>
        private void SetZoomSensitivity(float value)
        {
            if (cameraManager != null) cameraManager.ZoomSensitivity = value;
            cachedSettings.ZoomSensitivity = cameraManager != null ? cameraManager.ZoomSensitivity : value;
            SaveSettings();
        }

        /// <summary>
        /// 在屏幕模式变更时立即应用新设置。
        /// </summary>
        /// <param name="optionIndex">新模式序号。</param>
        private void HandleScreenModeChanged(int optionIndex)
        {
            if (suppressDisplayCallbacks) return;
            cachedSettings.ScreenMode = Mathf.Clamp(optionIndex, 0, 1);
            SaveSettings();
            ApplyDisplaySettings(true);
        }

        /// <summary>
        /// 在分辨率变更时立即应用窗口模式分辨率。
        /// </summary>
        /// <param name="optionIndex">新分辨率序号。</param>
        private void HandleResolutionChanged(int optionIndex)
        {
            if (suppressDisplayCallbacks) return;

            int safeIndex = Mathf.Clamp(optionIndex, 0, resolutionOptions.Length - 1);
            ResolutionOption option = resolutionOptions.Length > 0 ? resolutionOptions[safeIndex] : GetCurrentDesktopResolutionOption();
            cachedSettings.WindowedResolutionWidth = option.Width;
            cachedSettings.WindowedResolutionHeight = option.Height;
            SaveSettings();

            if (GetSelectedMode() == ScreenModeOption.Windowed)
                ApplyDisplaySettings(true);
        }

        /// <summary>
        /// 应用当前缓存的显示设置。
        /// </summary>
        /// <param name="syncUiAfterApply">应用完成后是否根据实际状态刷新 UI。</param>
        private void ApplyDisplaySettings(bool syncUiAfterApply)
        {
            if (applyDisplayRoutine != null) StopCoroutine(applyDisplayRoutine);
            applyDisplayRoutine = StartCoroutine(ApplyDisplaySettingsRoutine(syncUiAfterApply));
        }

        /// <summary>
        /// 分帧应用显示设置。
        /// </summary>
        /// <param name="syncUiAfterApply">应用完成后是否刷新 UI。</param>
        /// <returns>协程枚举器。</returns>
        private IEnumerator ApplyDisplaySettingsRoutine(bool syncUiAfterApply)
        {
            ScreenModeOption mode = GetSelectedMode();
            if (mode == ScreenModeOption.FullScreen)
            {
                ResolutionOption fullScreenResolution = GetCurrentDesktopResolutionOption();
                Screen.SetResolution(fullScreenResolution.Width, fullScreenResolution.Height, FullScreenMode.FullScreenWindow);
            }
            else
            {
                ResolutionOption windowedResolution = GetStoredWindowedResolutionOption();
                Screen.SetResolution(windowedResolution.Width, windowedResolution.Height, FullScreenMode.Windowed);
            }

            yield return new WaitForEndOfFrame();
            yield return null;

            if (syncUiAfterApply) SyncDisplayUiFromActualState();
            applyDisplayRoutine = null;
        }

        /// <summary>
        /// 根据当前实际显示状态刷新 UI 和缓存设置。
        /// </summary>
        private void SyncDisplayUiFromActualState()
        {
            suppressDisplayCallbacks = true;

            ScreenModeOption actualMode = Screen.fullScreenMode == FullScreenMode.Windowed ? ScreenModeOption.Windowed : ScreenModeOption.FullScreen;
            cachedSettings.ScreenMode = (int)actualMode;

            if (actualMode == ScreenModeOption.Windowed)
            {
                cachedSettings.WindowedResolutionWidth = Screen.width;
                cachedSettings.WindowedResolutionHeight = Screen.height;
            }

            if (screenModeDropdown != null)
            {
                screenModeDropdown.SetValueWithoutNotify((int)actualMode);
                screenModeDropdown.RefreshShownValue();
            }

            ResolutionOption displayedResolution = actualMode == ScreenModeOption.FullScreen
                ? GetCurrentDesktopResolutionOption()
                : new ResolutionOption(Screen.width, Screen.height);

            if (resolutionDropdown != null)
            {
                resolutionDropdown.SetValueWithoutNotify(FindResolutionOptionIndex(displayedResolution.Width, displayedResolution.Height));
                resolutionDropdown.RefreshShownValue();
                resolutionDropdown.interactable = actualMode == ScreenModeOption.Windowed;
            }

            suppressDisplayCallbacks = false;
            SaveSettings();
        }

        /// <summary>
        /// 获取当前下拉框选中的模式。
        /// </summary>
        /// <returns>当前选中的屏幕模式。</returns>
        private ScreenModeOption GetSelectedMode()
        {
            if (screenModeDropdown == null) return NormalizeStoredMode(cachedSettings.ScreenMode);
            return (ScreenModeOption)Mathf.Clamp(screenModeDropdown.value, 0, 1);
        }

        /// <summary>
        /// 获取当前桌面分辨率。
        /// </summary>
        /// <returns>桌面分辨率选项。</returns>
        private static ResolutionOption GetCurrentDesktopResolutionOption()
        {
            Resolution resolution = Screen.currentResolution;
            return new ResolutionOption(resolution.width, resolution.height);
        }

        /// <summary>
        /// 获取缓存的窗口模式分辨率；若无效则退回当前桌面分辨率。
        /// </summary>
        /// <returns>窗口模式分辨率。</returns>
        private ResolutionOption GetStoredWindowedResolutionOption()
        {
            int width = cachedSettings.WindowedResolutionWidth;
            int height = cachedSettings.WindowedResolutionHeight;
            if (width <= 0 || height <= 0) return GetCurrentDesktopResolutionOption();
            return new ResolutionOption(width, height);
        }

        /// <summary>
        /// 在分辨率选项中查找最匹配的序号。
        /// </summary>
        /// <param name="width">宽度。</param>
        /// <param name="height">高度。</param>
        /// <returns>最匹配的分辨率序号。</returns>
        private int FindResolutionOptionIndex(int width, int height)
        {
            if (resolutionOptions.Length == 0) return 0;

            for (int index = 0; index < resolutionOptions.Length; index++)
            {
                if (resolutionOptions[index].Width == width && resolutionOptions[index].Height == height)
                    return index;
            }

            int bestIndex = 0;
            int bestDistance = int.MaxValue;
            for (int index = 0; index < resolutionOptions.Length; index++)
            {
                int distance = Mathf.Abs(resolutionOptions[index].Width - width) + Mathf.Abs(resolutionOptions[index].Height - height);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestIndex = index;
            }

            return bestIndex;
        }

        /// <summary>
        /// 兼容旧配置中的模式序号。
        /// </summary>
        /// <param name="storedModeIndex">旧模式序号。</param>
        /// <returns>当前两档模式下的合法值。</returns>
        private static ScreenModeOption NormalizeStoredMode(int storedModeIndex)
        {
            return storedModeIndex == 1 || storedModeIndex == 2 ? ScreenModeOption.Windowed : ScreenModeOption.FullScreen;
        }

        /// <summary>
        /// 保存当前配置。
        /// </summary>
        private void SaveSettings()
        {
            UiPlayerSettingsStore.Save(cachedSettings);
        }
    }
}
