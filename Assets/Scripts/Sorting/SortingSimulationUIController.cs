using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 连接 UI 与多排序组管理器。面板控制当前选中组，顶部全局控件控制全部组。
    /// </summary>
    public sealed class SortingSimulationUIController : MonoBehaviour
    {
        /// <summary>
        /// 场景中负责多排序组数据和状态的管理器。
        /// </summary>
        [Header("多组排序管理器")][SerializeField] private SortComparisonManager comparisonManager;

        /// <summary>
        /// 全局面板中用于设置每组排序块数量的输入框。
        /// </summary>
        [Header("排序块数量输入框")][SerializeField] private TMP_InputField countInput;

        /// <summary>
        /// 单组面板中用于修改当前选中块高度的输入框。
        /// </summary>
        [Header("选中块高度输入框")][SerializeField] public TMP_InputField barHeightInput;

        /// <summary>
        /// 全局面板中用于实时修改单步操作间隔的输入框。
        /// </summary>
        [Header("全局操作间隔输入框")][SerializeField] private TMP_InputField globalIntervalInput;

        /// <summary>
        /// 全局随机高度时使用的最小高度输入框。
        /// </summary>
        [Header("全局随机最小高度输入框")][SerializeField] private TMP_InputField randomMinHeightInput;

        /// <summary>
        /// 全局随机高度时使用的最大高度输入框。
        /// </summary>
        [Header("全局随机最大高度输入框")][SerializeField] private TMP_InputField randomMaxHeightInput;

        /// <summary>
        /// 全局面板中用于设置排序块宽度的输入框。
        /// </summary>
        [Header("全局块宽度输入框")][SerializeField] private TMP_InputField barWidthInput;

        /// <summary>
        /// 全局面板中用于设置排序块水平间距的输入框。
        /// </summary>
        [Header("全局块间距输入框")][SerializeField] private TMP_InputField barGapInput;

        /// <summary>
        /// 单组面板中用于选择当前组排序算法的下拉框。
        /// </summary>
        [Header("算法下拉选择框")][SerializeField] private TMP_Dropdown algorithmDropdown;

        /// <summary>
        /// 单组面板中显示当前选中组状态的文本。
        /// </summary>
        [Header("选中组信息文本")][SerializeField] private TMP_Text selectedGroupInfoText;

        /// <summary>
        /// 状态提示文本。
        /// </summary>
        [Header("状态文本")][SerializeField] private TMP_Text statusText;

        /// <summary>
        /// 顶部单组控制面板，没有选中组时自动隐藏。
        /// </summary>
        [Header("顶部单组控制面板")][SerializeField] private GameObject selectedGroupPanel;

        /// <summary>
        /// 全局面板中重新生成所有组排序块的按钮。
        /// </summary>
        [Header("面板按钮")][SerializeField] private Button rebuildButton;

        /// <summary>
        /// 全局面板中随机化所有组排序块高度的按钮。
        /// </summary>
        [SerializeField] private Button randomizeButton;

        /// <summary>
        /// 单组面板中应用当前选中块高度的按钮。
        /// </summary>
        [SerializeField] private Button applyHeightButton;

        /// <summary>
        /// 单组面板中开始或停止当前组排序的按钮。
        /// </summary>
        [SerializeField] private Button startButton;

        /// <summary>
        /// 单组面板中暂停或恢复当前组排序的按钮。
        /// </summary>
        [SerializeField] private Button pauseButton;

        /// <summary>
        /// 全局面板中开始或停止全部组排序的按钮。
        /// </summary>
        [Header("全局按钮")][SerializeField] private Button globalStartButton;

        /// <summary>
        /// 全局面板中暂停或恢复全部组排序的按钮。
        /// </summary>
        [SerializeField] private Button globalPauseButton;

        /// <summary>
        /// 全局区域中用于新增排序组的按钮。
        /// </summary>
        [SerializeField] private Button addGroupButton;

        /// <summary>
        /// 全局区域中用于删除当前选中排序组的按钮。
        /// </summary>
        [SerializeField] private Button removeGroupButton;

        /// <summary>
        /// 当前支持的排序算法列表。
        /// </summary>
        private readonly IReadOnlyList<ISortAlgorithm> algorithms = SortAlgorithmFactory.CreateAll();

        /// <summary>
        /// 开始按钮显示文本。
        /// </summary>
        private const string StartText = "单组开始";

        /// <summary>
        /// 停止按钮显示文本。
        /// </summary>
        private const string StopText = "单组停止";

        /// <summary>
        /// 暂停按钮显示文本。
        /// </summary>
        private const string PauseText = "单组暂停";

        /// <summary>
        /// 恢复按钮显示文本。
        /// </summary>
        private const string ResumeText = "单组恢复";

        /// <summary>
        /// 全局开始按钮显示文本。
        /// </summary>
        private const string GlobalStartText = "全部开始";

        /// <summary>
        /// 全局停止按钮显示文本。
        /// </summary>
        private const string GlobalStopText = "全部停止";

        /// <summary>
        /// 全局暂停按钮显示文本。
        /// </summary>
        private const string GlobalPauseText = "全部暂停";

        /// <summary>
        /// 全局恢复按钮显示文本。
        /// </summary>
        private const string GlobalResumeText = "全部恢复";

        /// <summary>
        /// 当前 UI 控制器单例，供排序组状态变化时刷新面板。
        /// </summary>
        public static SortingSimulationUIController Instance { get; private set; }

        /// <summary>
        /// 上一帧记录的全局运行锁定状态，用于检测状态变化后主动刷新 UI。
        /// </summary>
        private bool lastGlobalEditLocked;

        /// <summary>
        /// 上一帧记录的当前选中组运行锁定状态，用于检测状态变化后主动刷新 UI。
        /// </summary>
        private bool lastSelectedGroupEditLocked;

        /// <summary>
        /// 上一帧记录的是否存在任意运行中的排序组。
        /// </summary>
        private bool lastAnyRunning;

        /// <summary>
        /// 上一帧记录的是否存在任意运行且未暂停的排序组。
        /// </summary>
        private bool lastAnyRunningNotPaused;

        /// <summary>
        /// 上一帧记录的当前选中排序组引用。
        /// </summary>
        private SortGroupController lastSelectedGroup;

        /// <summary>
        /// 获取当前选中的排序组。
        /// </summary>
        private SortGroupController SelectedGroup => comparisonManager != null ? comparisonManager.SelectedGroup : null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (comparisonManager == null)
                comparisonManager = FindFirstObjectByType<SortComparisonManager>();

            WireUi();
            RefreshDefaultInputs();
            RefreshSelectedGroupUI();
            SetStatus("就绪");
        }

        /// <summary>
        /// 监视排序组运行状态变化，并在状态切换时主动刷新 UI。
        /// </summary>
        private void Update()
        {
            if (comparisonManager == null) return;

            bool globalEditLocked = IsGlobalEditLocked();
            bool selectedGroupEditLocked = IsSelectedGroupEditLocked();
            bool anyRunning = HasAnyRunningGroup();
            bool anyRunningNotPaused = HasAnyRunningNotPausedGroup();
            SortGroupController group = SelectedGroup;

            if (group == lastSelectedGroup &&
                globalEditLocked == lastGlobalEditLocked &&
                selectedGroupEditLocked == lastSelectedGroupEditLocked &&
                anyRunning == lastAnyRunning &&
                anyRunningNotPaused == lastAnyRunningNotPaused)
            {
                return;
            }

            lastSelectedGroup = group;
            lastGlobalEditLocked = globalEditLocked;
            lastSelectedGroupEditLocked = selectedGroupEditLocked;
            lastAnyRunning = anyRunning;
            lastAnyRunningNotPaused = anyRunningNotPaused;
            RefreshSelectedGroupUI();
        }

        private void OnDestroy()
        {
            UnwireUi();
            if (Instance == this) Instance = null;
        }

        public void Configure(
            SortComparisonManager manager,
            TMP_InputField count,
            TMP_InputField barHeight,
            TMP_InputField globalInterval,
            TMP_Dropdown dropdown,
            TMP_Text groupInfo,
            TMP_Text status,
            Button rebuild,
            Button randomize,
            Button applyHeight,
            Button start,
            Button pause,
            Button globalStart,
            Button globalPause)
        {
            comparisonManager = manager;
            countInput = count;
            barHeightInput = barHeight;
            globalIntervalInput = globalInterval;
            algorithmDropdown = dropdown;
            selectedGroupInfoText = groupInfo;
            statusText = status;
            rebuildButton = rebuild;
            randomizeButton = randomize;
            applyHeightButton = applyHeight;
            startButton = start;
            pauseButton = pause;
            globalStartButton = globalStart;
            globalPauseButton = globalPause;
        }

        public void Btn_RebuildBarsFromInput()
        {
            if (comparisonManager == null) return;
            if (IsGlobalEditLocked()) return;

            int barCount = ParseInt(countInput, 12);
            comparisonManager.ResetSharedValues(barCount);
            RefreshSelectedGroupUI();
            SetStatus($"已为所有组生成 {barCount} 个块");
        }

        /// <summary>
        /// 由 UI 调用：新增一个排序组。
        /// </summary>
        public void Btn_AddGroup()
        {
            if (comparisonManager == null) return;
            if (IsGlobalEditLocked()) return;

            comparisonManager.AddGroupAndSelect();
            RefreshSelectedGroupUI();
            SetStatus(comparisonManager.SelectedGroup != null ? $"已新增第 {comparisonManager.SelectedGroup.GroupNumber} 组" : "新增组失败");
        }

        /// <summary>
        /// 由 UI 调用：删除当前选中的排序组。
        /// </summary>
        public void Btn_RemoveSelectedGroup()
        {
            if (comparisonManager == null) return;
            if (IsGlobalEditLocked()) return;

            comparisonManager.RemoveSelectedGroup();
            RefreshSelectedGroupUI();
            SetStatus("已删除当前组并重新编号");
        }

        public void Btn_RandomizeBars()
        {
            if (comparisonManager == null) return;
            if (IsGlobalEditLocked()) return;

            float randomMin = ParseFloat(randomMinHeightInput, comparisonManager.MinHeight);
            float randomMax = ParseFloat(randomMaxHeightInput, comparisonManager.MaxHeight);
            comparisonManager.RandomizeSharedValues(randomMin, randomMax);
            RefreshSelectedGroupUI();
            SetStatus("所有组已同步随机高度");
        }

        public void Btn_ApplyManualHeight()
        {
            if (comparisonManager == null) return;
            if (IsSelectedGroupEditLocked()) return;

            float height = ParseFloat(barHeightInput, 1f);
            if (comparisonManager.TrySetSelectedHeight(height))
                SetStatus("已同步修改所有组的对应块高度");
            else
                SetStatus("请先选择一个组内的块");

            RefreshSelectedGroupUI();
        }

        public void Btn_ToggleStartStopSimulation()
        {
            if (comparisonManager == null) return;

            UpdateGlobalIntervalFromInput();
            comparisonManager.ToggleSelectedStartStop();
            RefreshSelectedGroupUI();
        }

        public void Btn_TogglePauseResumeSimulation()
        {
            comparisonManager?.ToggleSelectedPauseResume();
            RefreshSelectedGroupUI();
        }

        public void Btn_ToggleGlobalStartStop()
        {
            if (comparisonManager == null) return;

            UpdateGlobalIntervalFromInput();
            comparisonManager.ToggleAllStartStop();
            RefreshSelectedGroupUI();
        }

        public void Btn_ToggleGlobalPauseResume()
        {
            comparisonManager?.ToggleAllPauseResume();
            RefreshSelectedGroupUI();
        }

        public void RefreshSelectedGroupUI()
        {
            RefreshAlgorithmDropdownUI();

            SortGroupController group = SelectedGroup;
            if (selectedGroupPanel != null)
                selectedGroupPanel.SetActive(group != null);

            if (selectedGroupInfoText != null)
                selectedGroupInfoText.text = group != null ? group.BuildInfoText() : string.Empty;

            if (group == null && barHeightInput != null)
                barHeightInput.text = string.Empty;

            if (group != null && barHeightInput != null && group.SelectedBarIndex >= 0)
            {
                float value = group.BarsController.Values[group.SelectedBarIndex];
                barHeightInput.text = value.ToString("F2", CultureInfo.InvariantCulture);
            }

            SetButtonText(startButton, group != null && group.IsRunning ? StopText : StartText);
            SetButtonInteractable(startButton, group != null);
            SetButtonInteractable(pauseButton, group != null && group.IsRunning);
            SetButtonText(pauseButton, group != null && group.IsPaused ? ResumeText : PauseText);
            SetButtonInteractable(applyHeightButton, group != null);
            if (algorithmDropdown != null)
                algorithmDropdown.interactable = group != null && !group.IsRunning;

            bool anyRunning = comparisonManager != null && HasAnyRunningGroup();
            bool anyRunningNotPaused = comparisonManager != null && HasAnyRunningNotPausedGroup();
            SetButtonText(globalStartButton, anyRunning ? GlobalStopText : GlobalStartText);
            SetButtonText(globalPauseButton, anyRunningNotPaused ? GlobalPauseText : GlobalResumeText);
            SetButtonInteractable(globalPauseButton, anyRunning);
            SetButtonInteractable(removeGroupButton, comparisonManager != null && group != null && comparisonManager.Groups.Count > 1);
            RefreshRuntimeEditableControls();
            CacheRuntimeUiState(group);
        }

        private void WireUi()
        {
            if (algorithmDropdown != null) algorithmDropdown.onValueChanged.AddListener(OnAlgorithmDropdownChanged);
            if (rebuildButton != null) rebuildButton.onClick.AddListener(Btn_RebuildBarsFromInput);
            if (randomizeButton != null) randomizeButton.onClick.AddListener(Btn_RandomizeBars);
            if (applyHeightButton != null) applyHeightButton.onClick.AddListener(Btn_ApplyManualHeight);
            if (startButton != null) startButton.onClick.AddListener(Btn_ToggleStartStopSimulation);
            if (pauseButton != null) pauseButton.onClick.AddListener(Btn_TogglePauseResumeSimulation);
            if (globalStartButton != null) globalStartButton.onClick.AddListener(Btn_ToggleGlobalStartStop);
            if (globalPauseButton != null) globalPauseButton.onClick.AddListener(Btn_ToggleGlobalPauseResume);
            if (addGroupButton != null) addGroupButton.onClick.AddListener(Btn_AddGroup);
            if (removeGroupButton != null) removeGroupButton.onClick.AddListener(Btn_RemoveSelectedGroup);
            if (globalIntervalInput != null) globalIntervalInput.onValueChanged.AddListener(OnGlobalIntervalEdited);
            if (barWidthInput != null) barWidthInput.onEndEdit.AddListener(OnBarLayoutEdited);
            if (barGapInput != null) barGapInput.onEndEdit.AddListener(OnBarLayoutEdited);
        }

        private void UnwireUi()
        {
            if (algorithmDropdown != null) algorithmDropdown.onValueChanged.RemoveListener(OnAlgorithmDropdownChanged);
            if (rebuildButton != null) rebuildButton.onClick.RemoveListener(Btn_RebuildBarsFromInput);
            if (randomizeButton != null) randomizeButton.onClick.RemoveListener(Btn_RandomizeBars);
            if (applyHeightButton != null) applyHeightButton.onClick.RemoveListener(Btn_ApplyManualHeight);
            if (startButton != null) startButton.onClick.RemoveListener(Btn_ToggleStartStopSimulation);
            if (pauseButton != null) pauseButton.onClick.RemoveListener(Btn_TogglePauseResumeSimulation);
            if (globalStartButton != null) globalStartButton.onClick.RemoveListener(Btn_ToggleGlobalStartStop);
            if (globalPauseButton != null) globalPauseButton.onClick.RemoveListener(Btn_ToggleGlobalPauseResume);
            if (addGroupButton != null) addGroupButton.onClick.RemoveListener(Btn_AddGroup);
            if (removeGroupButton != null) removeGroupButton.onClick.RemoveListener(Btn_RemoveSelectedGroup);
            if (globalIntervalInput != null) globalIntervalInput.onValueChanged.RemoveListener(OnGlobalIntervalEdited);
            if (barWidthInput != null) barWidthInput.onEndEdit.RemoveListener(OnBarLayoutEdited);
            if (barGapInput != null) barGapInput.onEndEdit.RemoveListener(OnBarLayoutEdited);
        }

        private void SelectBubbleAlgorithm()
        {
            SelectAlgorithm(SortAlgorithmType.Bubble);
        }

        private void SelectSelectionAlgorithm()
        {
            SelectAlgorithm(SortAlgorithmType.Selection);
        }

        private void SelectAlgorithm(SortAlgorithmType type)
        {
            for (int i = 0; i < algorithms.Count; i++)
            {
                if (algorithms[i].Type != type) continue;
                SelectedGroup?.SetAlgorithmIndex(i);
                RefreshSelectedGroupUI();
                return;
            }
        }

        private void OnAlgorithmDropdownChanged(int optionIndex)
        {
            if (IsSelectedGroupEditLocked())
            {
                RefreshAlgorithmDropdownUI();
                return;
            }

            SelectedGroup?.SetAlgorithmIndex(optionIndex);
            RefreshSelectedGroupUI();
        }

        private void RefreshAlgorithmDropdownUI()
        {
            if (algorithmDropdown == null) return;

            if (algorithmDropdown.options.Count != algorithms.Count)
            {
                List<string> optionNames = new();
                for (int i = 0; i < algorithms.Count; i++)
                    optionNames.Add(algorithms[i].DisplayName);

                algorithmDropdown.ClearOptions();
                algorithmDropdown.AddOptions(optionNames);
            }

            int selectedIndex = SelectedGroup != null ? SelectedGroup.SelectedAlgorithmIndex : 0;
            algorithmDropdown.SetValueWithoutNotify(Mathf.Clamp(selectedIndex, 0, algorithms.Count - 1));
            algorithmDropdown.RefreshShownValue();
        }

        private void RefreshDefaultInputs()
        {
            SetInputTextIfEmpty(countInput, "12");
            SetInputTextIfEmpty(barHeightInput, "3");
            SetInputTextIfEmpty(globalIntervalInput, comparisonManager != null ? comparisonManager.GlobalInterval.ToString("0.##", CultureInfo.InvariantCulture) : "0.25");
            SetInputTextIfEmpty(randomMinHeightInput, comparisonManager != null ? comparisonManager.MinHeight.ToString("0.##", CultureInfo.InvariantCulture) : "0.8");
            SetInputTextIfEmpty(randomMaxHeightInput, comparisonManager != null ? comparisonManager.MaxHeight.ToString("0.##", CultureInfo.InvariantCulture) : "12");
            SetInputTextIfEmpty(barWidthInput, comparisonManager != null ? comparisonManager.SharedBarWidth.ToString("0.##", CultureInfo.InvariantCulture) : "0.6");
            SetInputTextIfEmpty(barGapInput, comparisonManager != null ? comparisonManager.SharedBarGap.ToString("0.##", CultureInfo.InvariantCulture) : "0.15");
        }

        private void OnGlobalIntervalEdited(string value)
        {
            UpdateGlobalIntervalFromInput();
        }

        /// <summary>
        /// 处理排序块宽度或间距输入框结束编辑事件。
        /// </summary>
        /// <param name="value">当前输入框文本，统一从两个输入框重新读取。</param>
        private void OnBarLayoutEdited(string value)
        {
            if (IsGlobalEditLocked()) return;
            UpdateBarLayoutFromInput();
        }

        private void UpdateGlobalIntervalFromInput()
        {
            if (comparisonManager == null) return;
            comparisonManager.SetGlobalInterval(ParseFloat(globalIntervalInput, comparisonManager.GlobalInterval));
        }

        /// <summary>
        /// 从全局输入框读取排序块宽度和块间距，并同步到所有排序组。
        /// </summary>
        private void UpdateBarLayoutFromInput()
        {
            if (comparisonManager == null) return;

            float barWidth = ParseFloat(barWidthInput, comparisonManager.SharedBarWidth);
            float barGap = ParseFloat(barGapInput, comparisonManager.SharedBarGap);
            comparisonManager.SetSharedBarLayout(barWidth, barGap);
        }

        /// <summary>
        /// 获取当前是否应锁定排序块编辑和结构编辑相关控件。
        /// </summary>
        /// <returns>若任一排序组正在运行且未暂停则返回真。</returns>
        private bool IsGlobalEditLocked()
        {
            return comparisonManager != null && comparisonManager.HasAnyActiveSimulation;
        }

        /// <summary>
        /// 获取当前选中排序组是否处于运行锁定状态。
        /// </summary>
        /// <returns>若当前选中组正在运行且未暂停则返回真。</returns>
        private bool IsSelectedGroupEditLocked()
        {
            SortGroupController group = SelectedGroup;
            return group != null && group.IsRunning && !group.IsPaused;
        }

        private bool HasAnyRunningGroup()
        {
            foreach (SortGroupController group in comparisonManager.Groups)
            {
                if (group.IsRunning) return true;
            }

            return false;
        }

        private bool HasAnyRunningNotPausedGroup()
        {
            foreach (SortGroupController group in comparisonManager.Groups)
            {
                if (group.IsRunning && !group.IsPaused) return true;
            }

            return false;
        }

        /// <summary>
        /// 缓存当前 UI 运行状态，避免下一帧因同一状态重复刷新。
        /// </summary>
        /// <param name="group">当前选中的排序组。</param>
        private void CacheRuntimeUiState(SortGroupController group)
        {
            lastSelectedGroup = group;
            lastGlobalEditLocked = IsGlobalEditLocked();
            lastSelectedGroupEditLocked = IsSelectedGroupEditLocked();
            lastAnyRunning = comparisonManager != null && HasAnyRunningGroup();
            lastAnyRunningNotPaused = comparisonManager != null && HasAnyRunningNotPausedGroup();
        }

        /// <summary>
        /// 刷新运行时需要锁定的输入控件和按钮状态。
        /// </summary>
        private void RefreshRuntimeEditableControls()
        {
            bool globalEditLocked = IsGlobalEditLocked();
            bool selectedGroupEditLocked = IsSelectedGroupEditLocked();
            SortGroupController group = SelectedGroup;

            if (countInput != null) countInput.interactable = !globalEditLocked;
            if (barWidthInput != null) barWidthInput.interactable = !globalEditLocked;
            if (barGapInput != null) barGapInput.interactable = !globalEditLocked;
            if (randomMinHeightInput != null) randomMinHeightInput.interactable = !globalEditLocked;
            if (randomMaxHeightInput != null) randomMaxHeightInput.interactable = !globalEditLocked;
            if (rebuildButton != null) rebuildButton.interactable = !globalEditLocked;
            if (randomizeButton != null) randomizeButton.interactable = !globalEditLocked;
            if (addGroupButton != null) addGroupButton.interactable = !globalEditLocked;
            if (removeGroupButton != null) removeGroupButton.interactable = !globalEditLocked && comparisonManager != null && group != null && comparisonManager.Groups.Count > 1;
            if (barHeightInput != null) barHeightInput.interactable = group != null && !selectedGroupEditLocked;
            if (applyHeightButton != null) applyHeightButton.interactable = group != null && !selectedGroupEditLocked;
            if (algorithmDropdown != null) algorithmDropdown.interactable = group != null && !selectedGroupEditLocked;
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
        }

        private static int ParseInt(TMP_InputField input, int fallback)
        {
            if (input == null || !int.TryParse(input.text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return fallback;

            return value;
        }

        private static float ParseFloat(TMP_InputField input, float fallback)
        {
            if (input == null) return fallback;

            string normalized = input.text.Replace(',', '.');
            if (!float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return fallback;

            return value;
        }

        private static void SetInputTextIfEmpty(TMP_InputField input, string value)
        {
            if (input != null && string.IsNullOrWhiteSpace(input.text))
                input.text = value;
        }

        private static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }

        private static void SetButtonText(Button button, string text)
        {
            if (button == null) return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = text;
        }
    }
}
