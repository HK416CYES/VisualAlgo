using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VisualAlgo.GridMapNav.Pathfinding;
using VisualAlgo.Managers.UI;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 负责连接全局控制面板与单图控制面板的 UI 逻辑。
    /// </summary>
    public sealed class GridMapNavUIController : MonoBehaviour
    {
        #region 字段与属性定义

        /// <summary>
        /// 多网格图总管理器。
        /// </summary>
        [Header("核心依赖")][SerializeField] private GridMapComparisonManager comparisonManager;

        /// <summary>
        /// 左侧全局控制面板实例。
        /// </summary>
        [SerializeField] public PanelController globalPanel;

        /// <summary>
        /// 上方单图控制面板实例。
        /// </summary>
        [SerializeField] private RectTransform selectedMapPanel;

        /// <summary>
        /// 宽度输入框。
        /// </summary>
        [Header("全局输入")][SerializeField] private TMP_InputField widthInput;

        /// <summary>
        /// 高度输入框。
        /// </summary>
        [SerializeField] private TMP_InputField heightInput;

        /// <summary>
        /// 间隔输入框。
        /// </summary>
        [SerializeField] private TMP_InputField intervalInput;

        /// <summary>
        /// 边框粗细输入框。
        /// </summary>
        [SerializeField] private TMP_InputField borderThicknessInput;

        /// <summary>
        /// 应用尺寸按钮。
        /// </summary>
        [Header("全局按钮")][SerializeField] private Button applySizeButton;

        /// <summary>
        /// 开始/停止按钮。
        /// </summary>
        [SerializeField] private Button startStopButton;

        /// <summary>
        /// 新建网格图按钮。
        /// </summary>
        [SerializeField] private Button addMapButton;

        /// <summary>
        /// 重置按钮。
        /// </summary>
        [SerializeField] private Button resetButton;

        /// <summary>
        /// 画墙按钮。
        /// </summary>
        [SerializeField] private Button wallModeButton;

        /// <summary>
        /// 设置起点按钮。
        /// </summary>
        [SerializeField] private Button startModeButton;

        /// <summary>
        /// 擦除模式按钮。
        /// </summary>
        [SerializeField] private Button eraseModeButton;

        /// <summary>
        /// 设置终点按钮。
        /// </summary>
        [SerializeField] private Button goalModeButton;

        /// <summary>
        /// 开始/停止按钮上的文本。
        /// </summary>
        [SerializeField] private TMP_Text startStopButtonText;

        /// <summary>
        /// 单图算法下拉框。
        /// </summary>
        [Header("单图控制")][SerializeField] private TMP_Dropdown algorithmDropdown;

        /// <summary>
        /// 删除当前网格图按钮。
        /// </summary>
        [SerializeField] private Button deleteMapButton;

        /// <summary>
        /// 当前选中网格图标题文本。
        /// </summary>
        [SerializeField] private TMP_Text selectedMapTitleText;

        /// <summary>
        /// 当前是否已经完成按钮事件绑定。
        /// </summary>
        private bool listenersBound;

        public static GridMapNavUIController Instance { get; private set; }

        #endregion

        /// <summary>
        /// 在脚本启动时查找并绑定 UI 引用。
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            ResolveReferences();
            BindListeners();
            RefreshGlobalInputs();
            RefreshSelectedMapPanel();
        }

        /// <summary>
        /// 在对象销毁时移除管理器事件监听。
        /// </summary>
        private void OnDestroy()
        {
            if (comparisonManager != null)
                comparisonManager.OnSelectedMapChanged -= HandleSelectedMapChanged;

            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 在每帧刷新运行按钮文案，保证显示状态与实际状态一致。
        /// </summary>
        private void Update()
        {
            RefreshRunButtonText();
        }

        /// <summary>
        /// 刷新左侧面板输入框中的默认值。
        /// </summary>
        private void RefreshGlobalInputs()
        {
            if (comparisonManager == null) return;

            widthInput?.SetTextWithoutNotify(comparisonManager.GetComponentInChildren<SharedGridData>().Width.ToString());
            heightInput?.SetTextWithoutNotify(comparisonManager.GetComponentInChildren<SharedGridData>().Height.ToString());
            intervalInput?.SetTextWithoutNotify(comparisonManager.StepInterval.ToString("0.00"));
            borderThicknessInput?.SetTextWithoutNotify(comparisonManager.BorderThickness.ToString("0.00"));
            RefreshRunButtonText();
            RefreshEditModeButtonStates();
        }

        /// <summary>
        /// 绑定所有 UI 控件的点击与值变化事件。
        /// </summary>
        private void BindListeners()
        {
            if (listenersBound || comparisonManager == null)
            {
                return;
            }

            comparisonManager.OnSelectedMapChanged += HandleSelectedMapChanged;

            if (applySizeButton != null)
            {
                applySizeButton.onClick.AddListener(ApplyGridSizeFromInput);
            }

            if (startStopButton != null)
            {
                startStopButton.onClick.AddListener(ToggleRunState);
            }

            if (addMapButton != null)
            {
                addMapButton.onClick.AddListener(() => comparisonManager.AddMap());
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(ResetAllMaps);
            }

            if (wallModeButton != null)
            {
                wallModeButton.onClick.AddListener(() => ToggleEditMode(GridEditMode.PaintWall));
            }

            if (startModeButton != null)
            {
                startModeButton.onClick.AddListener(() => ToggleEditMode(GridEditMode.SetStart));
            }

            if (eraseModeButton != null)
            {
                eraseModeButton.onClick.AddListener(() => ToggleEditMode(GridEditMode.Erase));
            }

            if (goalModeButton != null)
            {
                goalModeButton.onClick.AddListener(() => ToggleEditMode(GridEditMode.SetGoal));
            }

            if (intervalInput != null)
            {
                intervalInput.onEndEdit.AddListener(ApplyIntervalFromInput);
            }

            if (borderThicknessInput != null)
            {
                borderThicknessInput.onEndEdit.AddListener(ApplyBorderThicknessFromInput);
            }

            if (algorithmDropdown != null)
            {
                algorithmDropdown.onValueChanged.AddListener(ApplySelectedAlgorithm);
                algorithmDropdown.ClearOptions();
                algorithmDropdown.AddOptions(PathfindingUtility.BuildDropdownOptions());
            }

            if (deleteMapButton != null)
            {
                deleteMapButton.onClick.AddListener(DeleteSelectedMap);
            }

            listenersBound = true;
        }

        /// <summary>
        /// 根据宽高输入框内容重建共享网格。
        /// </summary>
        private void ApplyGridSizeFromInput()
        {
            if (comparisonManager == null)
            {
                return;
            }

            int width = ParseIntOrFallback(widthInput, 10);
            int height = ParseIntOrFallback(heightInput, 8);
            comparisonManager.RebuildSharedGrid(width, height);
            RefreshGlobalInputs();
        }

        /// <summary>
        /// 将操作间隔输入框内容应用到全局管理器。
        /// </summary>
        /// <param name="inputValue">输入文本。</param>
        private void ApplyIntervalFromInput(string inputValue)
        {
            if (comparisonManager == null) return;

            float interval = ParseFloatOrFallback(intervalInput, comparisonManager.StepInterval);
            comparisonManager.SetStepInterval(interval);
            intervalInput?.SetTextWithoutNotify(comparisonManager.StepInterval.ToString("0.00"));
        }

        /// <summary>
        /// 将边框粗细输入框内容应用到全局管理器。
        /// </summary>
        /// <param name="inputValue">输入文本。</param>
        private void ApplyBorderThicknessFromInput(string inputValue)
        {
            if (comparisonManager == null)
            {
                return;
            }

            float thickness = ParseFloatOrFallback(borderThicknessInput, comparisonManager.BorderThickness);
            comparisonManager.SetBorderThickness(thickness);
            borderThicknessInput?.SetTextWithoutNotify(comparisonManager.BorderThickness.ToString("0.00"));
        }

        /// <summary>
        /// 切换所有网格图的运行状态。
        /// </summary>
        private void ToggleRunState()
        {
            if (comparisonManager == null) return;

            if (comparisonManager.AnyMapRunning)
            {
                ToggleUIEnableOnStartOrStop(true);
                comparisonManager.StopAllMaps();
            }
            else
            {
                comparisonManager.StartAllMaps();
            }

            RefreshRunButtonText();
        }

        /// <summary>
        /// 重置所有网格图的寻路可视化状态。
        /// </summary>
        private void ResetAllMaps()
        {
            if (comparisonManager == null) return;

            ToggleUIEnableOnStartOrStop(true);
            comparisonManager.ResetAllMaps();
            RefreshRunButtonText();
        }

        /// <summary>
        /// 在开始或停止时切换按钮的可交互状态。
        /// </summary>
        /// <param name="isEnable">是否启用</param>
        public void ToggleUIEnableOnStartOrStop(bool isEnable)
        {
            comparisonManager.SetEditModeEnabled(isEnable);
            algorithmDropdown.interactable = isEnable;
            deleteMapButton.interactable = isEnable;
            wallModeButton.interactable = isEnable;
            startModeButton.interactable = isEnable;
            if (eraseModeButton != null) eraseModeButton.interactable = isEnable;
            goalModeButton.interactable = isEnable;
            widthInput.interactable = isEnable;
            heightInput.interactable = isEnable;
            applySizeButton.interactable = isEnable;
            addMapButton.interactable = isEnable;
        }

        /// <summary>
        /// 切换全局编辑模式，并刷新按钮选中态。
        /// </summary>
        /// <param name="mode">目标编辑模式。</param>
        private void ToggleEditMode(GridEditMode mode)
        {
            if (comparisonManager == null) return;

            comparisonManager.ToggleEditMode(mode);
            RefreshEditModeButtonStates();
        }

        /// <summary>
        /// 将当前下拉框选中的算法应用到选中网格图。
        /// </summary>
        /// <param name="dropdownIndex">下拉框索引。</param>
        private void ApplySelectedAlgorithm(int dropdownIndex)
        {
            GridMapController selectedMap = comparisonManager == null ? null : comparisonManager.SelectedMap;
            if (selectedMap == null)
            {
                return;
            }

            PathfindAlgorithmType type = PathfindingUtility.GetAlgorithmByIndex(dropdownIndex);
            selectedMap.SetAlgorithm(type);
        }

        /// <summary>
        /// 删除当前选中的网格图。
        /// </summary>
        private void DeleteSelectedMap()
        {
            if (comparisonManager == null)
            {
                return;
            }

            comparisonManager.RemoveSelectedMap();
        }

        /// <summary>
        /// 当选中网格图发生变化时刷新顶部面板。
        /// </summary>
        /// <param name="selectedMap">新的选中网格图。</param>
        private void HandleSelectedMapChanged(GridMapController selectedMap)
        {
            RefreshSelectedMapPanel();
        }

        /// <summary>
        /// 刷新顶部单图控制面板的显隐与内容。
        /// </summary>
        private void RefreshSelectedMapPanel()
        {
            GridMapController selectedMap = comparisonManager == null ? null : comparisonManager.SelectedMap;
            bool hasSelection = selectedMap != null;

            if (selectedMapPanel != null)
            {
                selectedMapPanel.gameObject.SetActive(hasSelection);
            }

            if (!hasSelection)
            {
                if (selectedMapTitleText != null)
                {
                    selectedMapTitleText.text = string.Empty;
                }

                return;
            }

            if (selectedMapTitleText != null)
            {
                selectedMapTitleText.text = $"网格图 {selectedMap.MapIndex}";
            }

            if (algorithmDropdown != null)
            {
                int index = PathfindingUtility.GetAlgorithmIndex(selectedMap.AlgorithmType);
                algorithmDropdown.SetValueWithoutNotify(index);
            }
        }

        /// <summary>
        /// 刷新四个编辑模式按钮的选中显示状态。
        /// </summary>
        private void RefreshEditModeButtonStates()
        {
            if (comparisonManager == null) return;

            RefreshEditButtonState(wallModeButton, comparisonManager.IsEditModeEnabled && comparisonManager.EditMode == GridEditMode.PaintWall);
            RefreshEditButtonState(startModeButton, comparisonManager.IsEditModeEnabled && comparisonManager.EditMode == GridEditMode.SetStart);
            RefreshEditButtonState(eraseModeButton, comparisonManager.IsEditModeEnabled && comparisonManager.EditMode == GridEditMode.Erase);
            RefreshEditButtonState(goalModeButton, comparisonManager.IsEditModeEnabled && comparisonManager.EditMode == GridEditMode.SetGoal);
        }

        /// <summary>
        /// 刷新单个编辑按钮的颜色状态。
        /// </summary>
        /// <param name="button">目标按钮。</param>
        /// <param name="selected">是否处于选中状态。</param>
        private void RefreshEditButtonState(Button button, bool selected)
        {
            if (button == null) return;

            ColorBlock colors = button.colors;
            Color normalColor = selected ? new Color(0.16f, 0.5f, 0.85f, 1f) : new Color(0.22f, 0.28f, 0.35f, 1f);
            Color highlightColor = selected ? new Color(0.24f, 0.58f, 0.92f, 1f) : new Color(0.28f, 0.35f, 0.44f, 1f);
            Color pressedColor = selected ? new Color(0.12f, 0.42f, 0.75f, 1f) : new Color(0.14f, 0.18f, 0.24f, 1f);

            colors.normalColor = normalColor;
            colors.highlightedColor = highlightColor;
            colors.selectedColor = highlightColor;
            colors.pressedColor = pressedColor;
            button.colors = colors;
        }

        /// <summary>
        /// 刷新开始/停止按钮上的文案。
        /// </summary>
        private void RefreshRunButtonText()
        {
            if (startStopButtonText != null && comparisonManager != null)
            {
                startStopButtonText.text = comparisonManager.AnyMapRunning ? "停止" : "开始";
            }
        }

        /// <summary>
        /// 将输入框文本解析为整数，失败时返回指定默认值。
        /// </summary>
        /// <param name="inputField">目标输入框。</param>
        /// <param name="fallback">默认值。</param>
        /// <returns>解析得到的整数。</returns>
        private int ParseIntOrFallback(TMP_InputField inputField, int fallback)
        {
            return inputField != null && int.TryParse(inputField.text, out int value) ? Mathf.Max(2, value) : fallback;
        }

        /// <summary>
        /// 将输入框文本解析为浮点数，失败时返回指定默认值。
        /// </summary>
        /// <param name="inputField">目标输入框。</param>
        /// <param name="fallback">默认值。</param>
        /// <returns>解析得到的浮点数。</returns>
        private float ParseFloatOrFallback(TMP_InputField inputField, float fallback)
        {
            return inputField != null && float.TryParse(inputField.text, out float value) ? Mathf.Max(0.01f, value) : fallback;
        }

        /// <summary>
        /// 尝试从场景层级中补全所有 UI 与管理器引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (comparisonManager == null)
                comparisonManager = FindAnyObjectByType<GridMapComparisonManager>();
        }
    }
}
