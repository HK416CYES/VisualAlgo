using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VisualAlgo.Managers.UI;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 负责二叉树场景的全局面板与单树面板 UI 逻辑。
    /// </summary>
    public sealed class BinaryTreeUIController : MonoBehaviour
    {
        /// <summary>
        /// 树总管理器。
        /// </summary>
        [Header("核心依赖")][SerializeField] private BinaryTreeComparisonManager comparisonManager;

        /// <summary>
        /// 左侧全局控制面板。
        /// </summary>
        [SerializeField] public PanelController globalPanel;

        /// <summary>
        /// 顶部单树控制面板。
        /// </summary>
        [SerializeField] private RectTransform selectedTreePanel;

        /// <summary>
        /// 全局操作间隔输入框。
        /// </summary>
        [Header("全局输入")][SerializeField] private TMP_InputField intervalInput;

        /// <summary>
        /// 新建树模式下拉框。
        /// </summary>
        [SerializeField] private TMP_Dropdown newTreeModeDropdown;

        /// <summary>
        /// 全局目标值输入框。
        /// </summary>
        [SerializeField] private TMP_InputField globalTargetInput;

        /// <summary>
        /// 全局插入列表控件。
        /// </summary>
        [SerializeField] private BinaryTreeInsertListInput globalInsertListInput;

        /// <summary>
        /// 全局新值输入框。
        /// </summary>
        [SerializeField] private TMP_InputField globalNewValueInput;

        /// <summary>
        /// 新建树按钮。
        /// </summary>
        [Header("全局按钮")][SerializeField] private Button addTreeButton;

        /// <summary>
        /// 全局查找按钮。
        /// </summary>
        [SerializeField] private Button globalSearchButton;

        /// <summary>
        /// 全局插入按钮。
        /// </summary>
        [SerializeField] private Button globalInsertButton;

        /// <summary>
        /// 全局修改按钮。
        /// </summary>
        [SerializeField] private Button globalUpdateButton;

        /// <summary>
        /// 全局删除按钮。
        /// </summary>
        [SerializeField] private Button globalDeleteButton;

        /// <summary>
        /// 全局暂停/恢复按钮。
        /// </summary>
        [SerializeField] private Button pauseResumeButton;

        /// <summary>
        /// 全局停止按钮。
        /// </summary>
        [SerializeField] private Button stopButton;

        /// <summary>
        /// 暂停/恢复按钮文本。
        /// </summary>
        [SerializeField] private TMP_Text pauseResumeButtonText;

        /// <summary>
        /// 当前树模式下拉框。
        /// </summary>
        [SerializeField] private TMP_Dropdown selectedTreeModeDropdown;

        /// <summary>
        /// 当前树目标值输入框。
        /// </summary>
        [SerializeField] private TMP_InputField selectedTargetInput;

        /// <summary>
        /// 当前树新值输入框。
        /// </summary>
        [SerializeField] private TMP_InputField selectedNewValueInput;

        /// <summary>
        /// 当前树查找按钮。
        /// </summary>
        [SerializeField] private Button selectedSearchButton;

        /// <summary>
        /// 当前树插入按钮。
        /// </summary>
        [SerializeField] private Button selectedInsertButton;

        /// <summary>
        /// 当前树修改按钮。
        /// </summary>
        [SerializeField] private Button selectedUpdateButton;

        /// <summary>
        /// 当前树删除按钮。
        /// </summary>
        [SerializeField] private Button selectedDeleteButton;

        /// <summary>
        /// 当前树清空按钮。
        /// </summary>
        [SerializeField] private Button clearSelectedTreeButton;

        /// <summary>
        /// 是否已完成事件绑定。
        /// </summary>
        private bool listenersBound;

        /// <summary>
        /// 在脚本启用时补全引用并绑定事件。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            BindListeners();
            RefreshGlobalInputs();
            RefreshSelectedTreePanel();
        }

        /// <summary>
        /// 在销毁时移除事件订阅。
        /// </summary>
        private void OnDestroy()
        {
            if (comparisonManager != null) comparisonManager.OnSelectedTreeChanged -= HandleSelectedTreeChanged;
        }

        /// <summary>
        /// 每帧刷新暂停文案与交互状态。
        /// </summary>
        private void Update()
        {
            RefreshRuntimeControlStates();
        }

        /// <summary>
        /// 刷新全局输入框默认值。
        /// </summary>
        private void RefreshGlobalInputs()
        {
            if (comparisonManager == null) return;
            intervalInput?.SetTextWithoutNotify(comparisonManager.StepInterval.ToString("0.00"));
            int modeIndex = Mathf.Clamp((int)comparisonManager.NewTreeMode, 0, BuildModeOptions().Count - 1);
            newTreeModeDropdown?.SetValueWithoutNotify(modeIndex);
        }

        /// <summary>
        /// 绑定 UI 事件监听。
        /// </summary>
        private void BindListeners()
        {
            if (listenersBound || comparisonManager == null) return;
            comparisonManager.OnSelectedTreeChanged += HandleSelectedTreeChanged;

            if (intervalInput != null)
            {
                intervalInput.onValueChanged.AddListener(ApplyInterval);
                intervalInput.onEndEdit.AddListener(ApplyInterval);
            }
            if (newTreeModeDropdown != null)
            {
                newTreeModeDropdown.ClearOptions();
                newTreeModeDropdown.AddOptions(BuildModeOptions());
                newTreeModeDropdown.onValueChanged.AddListener(ApplyNewTreeMode);
            }

            if (addTreeButton != null) addTreeButton.onClick.AddListener(() => comparisonManager.AddTree());
            if (globalSearchButton != null) globalSearchButton.onClick.AddListener(() => ExecuteGlobalOperation(BinaryTreeOperationType.Search));
            if (globalInsertButton != null) globalInsertButton.onClick.AddListener(() => ExecuteGlobalOperation(BinaryTreeOperationType.Insert));
            if (globalUpdateButton != null) globalUpdateButton.onClick.AddListener(() => ExecuteGlobalOperation(BinaryTreeOperationType.Update));
            if (globalDeleteButton != null) globalDeleteButton.onClick.AddListener(() => ExecuteGlobalOperation(BinaryTreeOperationType.Delete));
            if (pauseResumeButton != null) pauseResumeButton.onClick.AddListener(TogglePauseResume);
            if (stopButton != null) stopButton.onClick.AddListener(StopAllOperations);

            if (selectedTreeModeDropdown != null)
            {
                selectedTreeModeDropdown.ClearOptions();
                selectedTreeModeDropdown.AddOptions(BuildModeOptions());
                selectedTreeModeDropdown.onValueChanged.AddListener(ApplySelectedTreeMode);
            }

            if (selectedSearchButton != null) selectedSearchButton.onClick.AddListener(() => ExecuteSelectedOperation(BinaryTreeOperationType.Search));
            if (selectedInsertButton != null) selectedInsertButton.onClick.AddListener(() => ExecuteSelectedOperation(BinaryTreeOperationType.Insert));
            if (selectedUpdateButton != null) selectedUpdateButton.onClick.AddListener(() => ExecuteSelectedOperation(BinaryTreeOperationType.Update));
            if (selectedDeleteButton != null) selectedDeleteButton.onClick.AddListener(() => ExecuteSelectedOperation(BinaryTreeOperationType.Delete));
            if (clearSelectedTreeButton != null) clearSelectedTreeButton.onClick.AddListener(() => comparisonManager.ClearSelectedTree());

            listenersBound = true;
        }

        /// <summary>
        /// 应用新的全局操作间隔。
        /// </summary>
        /// <param name="inputValue">输入文本。</param>
        private void ApplyInterval(string inputValue)
        {
            if (comparisonManager == null) return;
            float interval = ParseFloat(intervalInput, comparisonManager.StepInterval);
            comparisonManager.SetStepInterval(interval);
            intervalInput?.SetTextWithoutNotify(comparisonManager.StepInterval.ToString("0.00"));
        }

        /// <summary>
        /// 应用新建树默认模式。
        /// </summary>
        /// <param name="dropdownIndex">下拉框索引。</param>
        private void ApplyNewTreeMode(int dropdownIndex)
        {
            if (comparisonManager == null) return;
            comparisonManager.SetNewTreeMode((BinaryTreeImplementationType)Mathf.Clamp(dropdownIndex, 0, 2));
        }

        /// <summary>
        /// 应用当前选中树模式。
        /// </summary>
        /// <param name="dropdownIndex">下拉框索引。</param>
        private void ApplySelectedTreeMode(int dropdownIndex)
        {
            if (comparisonManager == null) return;
            BinaryTreeImplementationType mode = (BinaryTreeImplementationType)Mathf.Clamp(dropdownIndex, 0, 2);
            comparisonManager.SetSelectedTreeMode(mode);
            selectedTreeModeDropdown?.SetValueWithoutNotify((int)mode);
        }

        /// <summary>
        /// 对所有树执行一次统一操作。
        /// </summary>
        /// <param name="operationType">操作类型。</param>
        private void ExecuteGlobalOperation(BinaryTreeOperationType operationType)
        {
            if (comparisonManager == null) return;
            if (operationType == BinaryTreeOperationType.Insert)
            {
                comparisonManager.InsertListOnAllTrees(globalInsertListInput);
                return;
            }
            comparisonManager.ExecuteOnAllTrees(BuildRequest(operationType, globalTargetInput, globalNewValueInput));
        }

        /// <summary>
        /// 对当前选中树执行一次单独操作。
        /// </summary>
        /// <param name="operationType">操作类型。</param>
        private void ExecuteSelectedOperation(BinaryTreeOperationType operationType)
        {
            if (comparisonManager == null) return;
            if (operationType == BinaryTreeOperationType.Insert)
            {
                comparisonManager.InsertListOnSelectedTree(globalInsertListInput);
                return;
            }
            comparisonManager.ExecuteOnSelectedTree(BuildRequest(operationType, selectedTargetInput, selectedNewValueInput));
        }

        /// <summary>
        /// 切换全局暂停与恢复状态。
        /// </summary>
        private void TogglePauseResume()
        {
            if (comparisonManager == null || !comparisonManager.AnyTreeBusy) return;
            if (comparisonManager.IsPaused) comparisonManager.ResumeAllOperations();
            else comparisonManager.PauseAllOperations();
        }

        /// <summary>
        /// 停止全部树操作。
        /// </summary>
        private void StopAllOperations()
        {
            if (comparisonManager == null) return;
            comparisonManager.StopAllOperations();
        }

        /// <summary>
        /// 构造一次树操作请求。
        /// </summary>
        /// <param name="operationType">操作类型。</param>
        /// <param name="targetInput">目标值输入框。</param>
        /// <param name="newValueInput">新值输入框。</param>
        /// <returns>构造出的操作请求。</returns>
        private BinaryTreeOperationRequest BuildRequest(BinaryTreeOperationType operationType, TMP_InputField targetInput, TMP_InputField newValueInput)
        {
            int targetValue = ParseInt(targetInput, 0);
            int newValue = ParseInt(newValueInput, 0);
            return new BinaryTreeOperationRequest(operationType, targetValue, newValue);
        }

        /// <summary>
        /// 当选中树发生变化时刷新顶部面板。
        /// </summary>
        /// <param name="selectedTree">新的选中树。</param>
        private void HandleSelectedTreeChanged(BinaryTreeController selectedTree)
        {
            RefreshSelectedTreePanel();
        }

        /// <summary>
        /// 刷新顶部单树面板内容。
        /// </summary>
        private void RefreshSelectedTreePanel()
        {
            BinaryTreeController selectedTree = comparisonManager == null ? null : comparisonManager.SelectedTree;
            bool hasSelection = selectedTree != null;
            if (selectedTreePanel != null) selectedTreePanel.gameObject.SetActive(hasSelection);
            if (!hasSelection) return;

            if (selectedTreeModeDropdown != null)
                selectedTreeModeDropdown.SetValueWithoutNotify((int)selectedTree.ImplementationType);
        }

        /// <summary>
        /// 构建模式下拉框选项。
        /// </summary>
        /// <returns>模式名称列表。</returns>
        private List<string> BuildModeOptions()
        {
            return new List<string>
            {
                BinaryTreeModeUtility.GetDisplayName(BinaryTreeImplementationType.NaiveBinarySearchTree),
                BinaryTreeModeUtility.GetDisplayName(BinaryTreeImplementationType.BalancedBinarySearchTree),
                BinaryTreeModeUtility.GetDisplayName(BinaryTreeImplementationType.RedBlackTree)
            };
        }

        /// <summary>
        /// 将输入框文本解析为整数。
        /// </summary>
        /// <param name="inputField">目标输入框。</param>
        /// <param name="fallback">默认值。</param>
        /// <returns>解析值。</returns>
        private int ParseInt(TMP_InputField inputField, int fallback)
        {
            return inputField != null && int.TryParse(inputField.text, out int value) ? value : fallback;
        }

        /// <summary>
        /// 将输入框文本解析为浮点数。
        /// </summary>
        /// <param name="inputField">目标输入框。</param>
        /// <param name="fallback">默认值。</param>
        /// <returns>解析值。</returns>
        private float ParseFloat(TMP_InputField inputField, float fallback)
        {
            return inputField != null && float.TryParse(inputField.text, out float value) ? Mathf.Max(0.05f, value) : fallback;
        }

        /// <summary>
        /// 从场景层级中补全引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (comparisonManager == null) comparisonManager = FindAnyObjectByType<BinaryTreeComparisonManager>();
        }

        /// <summary>
        /// 刷新运行中按钮文案与交互状态。
        /// </summary>
        private void RefreshRuntimeControlStates()
        {
            if (comparisonManager == null) return;
            bool running = comparisonManager.AnyTreeBusy;
            bool paused = comparisonManager.IsPaused;

            if (pauseResumeButtonText != null) pauseResumeButtonText.text = paused ? "恢复" : "暂停";
            if (pauseResumeButton != null) pauseResumeButton.interactable = running;
            if (stopButton != null) stopButton.interactable = running;

            bool enabled = !running;
            SetButtonInteractable(addTreeButton, enabled);
            SetButtonInteractable(globalSearchButton, enabled);
            SetButtonInteractable(globalInsertButton, enabled);
            SetButtonInteractable(globalUpdateButton, enabled);
            SetButtonInteractable(globalDeleteButton, enabled);
            SetButtonInteractable(selectedSearchButton, enabled && comparisonManager.SelectedTree != null);
            SetButtonInteractable(selectedInsertButton, enabled && comparisonManager.SelectedTree != null);
            SetButtonInteractable(selectedUpdateButton, enabled && comparisonManager.SelectedTree != null);
            SetButtonInteractable(selectedDeleteButton, enabled && comparisonManager.SelectedTree != null);
            SetButtonInteractable(clearSelectedTreeButton, enabled && comparisonManager.SelectedTree != null);

            if (intervalInput != null) intervalInput.interactable = true;
            if (newTreeModeDropdown != null) newTreeModeDropdown.interactable = enabled;
            if (globalTargetInput != null) globalTargetInput.interactable = enabled;
            if (globalNewValueInput != null) globalNewValueInput.interactable = enabled;
            if (selectedTargetInput != null) selectedTargetInput.interactable = enabled && comparisonManager.SelectedTree != null;
            if (selectedNewValueInput != null) selectedNewValueInput.interactable = enabled && comparisonManager.SelectedTree != null;
            if (selectedTreeModeDropdown != null) selectedTreeModeDropdown.interactable = enabled && comparisonManager.SelectedTree != null;
            globalInsertListInput?.SetInteractable(enabled);
        }

        /// <summary>
        /// 安全设置按钮交互状态。
        /// </summary>
        /// <param name="button">目标按钮。</param>
        /// <param name="interactable">是否可交互。</param>
        private void SetButtonInteractable(Button button, bool interactable)
        {
            if (button != null) button.interactable = interactable;
        }
    }
}
