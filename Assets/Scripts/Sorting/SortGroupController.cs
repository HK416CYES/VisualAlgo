using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 管理一个独立排序组。组内数据来自全局共享值，但排序算法、运行、暂停和停止状态彼此独立。
    /// </summary>
    public sealed class SortGroupController : MonoBehaviour
    {
        /// <summary>
        /// 组内排序块控制器，负责维护本组的块数据和动画。
        /// </summary>
        [Header("组内排序块控制器")][SerializeField] private SortBarsController barsController;

        /// <summary>
        /// 绘制排序组外框的线渲染器。
        /// </summary>
        [Header("组外边框")][SerializeField] private LineRenderer borderRenderer;

        /// <summary>
        /// 覆盖整个排序组区域的碰撞体，用于空白区域点击和拖拽检测。
        /// </summary>
        [Header("用于点击选择组的边框碰撞体")][SerializeField] private BoxCollider selectionCollider;

        /// <summary>
        /// 显示排序组编号和当前算法名称的 3D 文本。
        /// </summary>
        [Header("组编号文本")][SerializeField] private TextMeshPro groupLabel;

        /// <summary>
        /// 边框和排序块内容之间的留白。
        /// </summary>
        [Header("边框与块之间的内边距")][SerializeField, Min(0.1f)] private float borderPadding = 0.7f;

        /// <summary>
        /// 边框的最小高度。
        /// </summary>
        [Header("边框高度")][SerializeField, Min(1f)] public float borderHeight = 14f;

        /// <summary>
        /// 排序组未被选中时的边框颜色。
        /// </summary>
        [Header("边框颜色")][SerializeField] private Color normalBorderColor = Color.black;

        /// <summary>
        /// 排序组被选中时的边框高亮颜色。
        /// </summary>
        [SerializeField] private Color selectedBorderColor = new(0.15f, 0.55f, 1f, 1f);

        /// <summary>
        /// 排序组未被选中时的边框宽度。
        /// </summary>
        [Header("边框宽度")][SerializeField, Min(0.01f)] private float normalBorderWidth = 0.04f;

        /// <summary>
        /// 排序组被选中时的边框宽度。
        /// </summary>
        [SerializeField, Min(0.01f)] private float selectedBorderWidth = 0.09f;

        /// <summary>
        /// 当前可供选择的排序算法列表。
        /// </summary>
        private readonly IReadOnlyList<ISortAlgorithm> algorithms = SortAlgorithmFactory.CreateAll();

        /// <summary>
        /// 拥有此排序组的多组管理器。
        /// </summary>
        private SortComparisonManager owner;

        /// <summary>
        /// 当前正在执行的排序协程。
        /// </summary>
        private Coroutine runningRoutine;

        /// <summary>
        /// 当前排序组在管理器中的零基索引。
        /// </summary>
        private int groupIndex;

        /// <summary>
        /// 当前选中的算法索引。
        /// </summary>
        private int algorithmIndex;

        /// <summary>
        /// 当前组是否正在执行排序。
        /// </summary>
        private bool isRunning;

        /// <summary>
        /// 当前组是否处于暂停状态。
        /// </summary>
        private bool isPaused;

        /// <summary>
        /// 是否已请求停止当前排序流程。
        /// </summary>
        private bool stopRequested;

        /// <summary>
        /// 当前组每一步排序操作之间的等待秒数。
        /// </summary>
        private float currentInterval = 0.25f;

        /// <summary>
        /// 当前排序组是否被玩家选中。
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// 获取当前排序组的零基索引。
        /// </summary>
        public int GroupIndex => groupIndex;

        /// <summary>
        /// 获取当前排序组展示给玩家的一基编号。
        /// </summary>
        public int GroupNumber => groupIndex + 1;

        /// <summary>
        /// 获取当前组是否正在执行排序。
        /// </summary>
        public bool IsRunning => isRunning;

        /// <summary>
        /// 获取当前组是否处于暂停状态。
        /// </summary>
        public bool IsPaused => isPaused;

        /// <summary>
        /// 获取组内排序块控制器。
        /// </summary>
        public SortBarsController BarsController => barsController;

        /// <summary>
        /// 获取当前组选中的排序算法。
        /// </summary>
        public ISortAlgorithm SelectedAlgorithm => algorithms[Mathf.Clamp(algorithmIndex, 0, algorithms.Count - 1)];

        /// <summary>
        /// 获取当前算法索引。
        /// </summary>
        public int SelectedAlgorithmIndex => Mathf.Clamp(algorithmIndex, 0, algorithms.Count - 1);

        /// <summary>
        /// 获取当前选中块的零基索引，没有选中时返回 -1。
        /// </summary>
        public int SelectedBarIndex => barsController != null ? barsController.SelectedIndex : -1;

        /// <summary>
        /// 初始化组内引用和事件订阅。
        /// </summary>
        private void Awake()
        {
            if (barsController != null)
            {
                barsController.SetSelectionEnabled(false);
                barsController.BarSelected += HandleBarSelected;
            }
        }

        /// <summary>
        /// 释放组内事件订阅。
        /// </summary>
        private void OnDestroy()
        {
            if (barsController != null)
                barsController.BarSelected -= HandleBarSelected;
        }

        /// <summary>
        /// 由管理器注入拥有者、编号和共享高度数据。
        /// </summary>
        /// <param name="manager">拥有此组的管理器。</param>
        /// <param name="index">组索引。</param>
        /// <param name="sharedValues">所有组共享的高度值。</param>
        public void Initialize(SortComparisonManager manager, int index, IReadOnlyList<float> sharedValues)
        {
            owner = manager;
            groupIndex = index;
            name = $"Sort Group {GroupNumber}";
            ApplySharedValues(sharedValues);
            RefreshLabel();
        }

        /// <summary>
        /// 设置当前排序组编号。
        /// </summary>
        /// <param name="index">新的组索引。</param>
        public void SetGroupIndex(int index)
        {
            groupIndex = index;
            name = $"Sort Group {GroupNumber}";
            RefreshLabel();
        }

        /// <summary>
        /// 应用共享高度值，并停止当前组正在进行的排序。
        /// </summary>
        /// <param name="sharedValues">所有组共享的高度值。</param>
        public void ApplySharedValues(IReadOnlyList<float> sharedValues)
        {
            StopSimulation();
            if (barsController != null)
                barsController.SetValues(sharedValues);

            RefreshBorder();
        }

        /// <summary>
        /// 设置当前组使用的排序算法。
        /// </summary>
        /// <param name="index">算法列表索引。</param>
        public void SetAlgorithmIndex(int index)
        {
            if (isRunning) return;
            algorithmIndex = Mathf.Clamp(index, 0, algorithms.Count - 1);
            RefreshLabel();
        }

        /// <summary>
        /// 按指定间隔启动当前组的排序模拟。
        /// </summary>
        /// <param name="interval">单步操作间隔秒数。</param>
        public void StartSimulation(float interval)
        {
            if (isRunning || barsController == null) return;

            currentInterval = Mathf.Max(0.02f, interval);
            stopRequested = false;
            isPaused = false;
            runningRoutine = StartCoroutine(RunSimulation());
        }

        /// <summary>
        /// 在开始和停止之间切换当前排序组状态。
        /// </summary>
        /// <param name="interval">启动时使用的单步操作间隔秒数。</param>
        public void ToggleStartStop(float interval)
        {
            if (isRunning) StopSimulation();
            else StartSimulation(interval);
        }

        /// <summary>
        /// 在暂停和恢复之间切换当前排序组状态。
        /// </summary>
        public void TogglePauseResume()
        {
            if (!isRunning) return;
            isPaused = !isPaused;
            RefreshLabel();
        }

        /// <summary>
        /// 设置当前排序组的暂停状态。
        /// </summary>
        /// <param name="paused">是否暂停。</param>
        public void SetPaused(bool paused)
        {
            if (!isRunning) return;
            isPaused = paused;
            RefreshLabel();
        }

        /// <summary>
        /// 设置当前组的单步操作间隔，运行过程中也会即时生效。
        /// </summary>
        /// <param name="interval">新的单步操作间隔秒数。</param>
        public void SetOperationInterval(float interval)
        {
            currentInterval = Mathf.Max(0.02f, interval);
        }

        /// <summary>
        /// 设置当前排序组是否被选中，并同步刷新边框颜色。
        /// </summary>
        /// <param name="selected">是否选中。</param>
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            RefreshBorderVisual();
        }

        /// <summary>
        /// 停止当前排序组，并还原运行时视觉状态。
        /// </summary>
        public void StopSimulation()
        {
            if (!isRunning && runningRoutine == null) return;

            stopRequested = true;
            if (runningRoutine != null)
                StopCoroutine(runningRoutine);

            runningRoutine = null;
            isRunning = false;
            isPaused = false;
            stopRequested = false;
            if (barsController != null)
                barsController.ResetVisuals();

            RefreshLabel();
        }

        /// <summary>
        /// 选中当前组内指定排序块。
        /// </summary>
        /// <param name="bar">被鼠标点击的排序块视图。</param>
        /// <returns>是否成功选中。</returns>
        public bool TrySelectBar(SortBarView bar)
        {
            if (barsController == null || bar == null) return false;
            return barsController.SelectBar(bar);
        }

        /// <summary>
        /// 清理当前组内被玩家手动选中的排序块。
        /// </summary>
        public void ClearSelectedBar()
        {
            if (barsController == null) return;
            barsController.ClearSelection();
        }

        /// <summary>
        /// 修改当前选中块的高度。
        /// </summary>
        /// <param name="height">目标高度。</param>
        /// <returns>是否成功修改。</returns>
        public bool TrySetSelectedHeight(float height)
        {
            return barsController != null && barsController.TrySetSelectedHeight(height);
        }

        /// <summary>
        /// 构建面板上显示的当前组状态信息。
        /// </summary>
        /// <returns>面板状态文本。</returns>
        public string BuildInfoText()
        {
            string runState = isRunning ? (isPaused ? "暂停" : "运行中") : "停止";
            int selectedBar = SelectedBarIndex >= 0 ? SelectedBarIndex + 1 : 0;
            return $"组 {GroupNumber} | {SelectedAlgorithm.DisplayName} | {runState} | 选中块: {selectedBar}";
        }

        /// <summary>
        /// 根据排序块数量、尺寸和高度刷新边框线与点击碰撞体。
        /// </summary>
        public void RefreshBorder()
        {
            if (borderRenderer == null || selectionCollider == null || barsController == null) return;

            int count = Mathf.Max(1, barsController.Count);
            float contentWidth = count * barsController.BarWidth + Mathf.Max(0, count - 1) * barsController.Gap;
            float width = contentWidth + borderPadding * 2f;
            float halfWidth = width * 0.5f;
            float bottom = -borderPadding;
            float top = Mathf.Max(borderHeight, barsController.MaxHeight + borderPadding);

            borderRenderer.positionCount = 5;
            borderRenderer.useWorldSpace = false;
            borderRenderer.loop = false;
            borderRenderer.SetPosition(0, new Vector3(-halfWidth, bottom, 0f));
            borderRenderer.SetPosition(1, new Vector3(-halfWidth, top, 0f));
            borderRenderer.SetPosition(2, new Vector3(halfWidth, top, 0f));
            borderRenderer.SetPosition(3, new Vector3(halfWidth, bottom, 0f));
            borderRenderer.SetPosition(4, new Vector3(-halfWidth, bottom, 0f));
            RefreshBorderVisual();

            selectionCollider.center = new Vector3(0f, (top + bottom) * 0.5f, 0.25f);
            selectionCollider.size = new Vector3(width, top - bottom, 0.5f);
        }

        /// <summary>
        /// 按当前算法逐步执行排序模拟。
        /// </summary>
        private IEnumerator RunSimulation()
        {
            isRunning = true;
            RefreshLabel();
            barsController.ResetVisuals();

            List<SortOperation> operations = new(SelectedAlgorithm.CreateOperations(barsController.Values));
            foreach (SortOperation operation in operations)
            {
                if (stopRequested) break;

                if (operation.Type == SortOperationType.Swap ||
                    operation.Type == SortOperationType.Assign ||
                    operation.Type == SortOperationType.Insert)
                {
                    yield return barsController.ApplyOperationAnimated(
                        operation,
                        currentInterval,
                        () => isPaused,
                        () => stopRequested);
                }
                else
                {
                    barsController.ApplyOperation(operation);
                    if (operation.Type == SortOperationType.Compare)
                        yield return WaitForCurrentInterval();
                }
            }

            if (!stopRequested)
                barsController.MarkSorted();
            else
                barsController.ResetVisuals();

            isRunning = false;
            isPaused = false;
            stopRequested = false;
            runningRoutine = null;
            RefreshLabel();
            owner?.NotifyGroupStateChanged(this);
        }

        /// <summary>
        /// 等待当前操作间隔，等待期间支持暂停和停止。
        /// </summary>
        private IEnumerator WaitForCurrentInterval()
        {
            float elapsed = 0f;
            while (elapsed < currentInterval && !stopRequested)
            {
                if (!isPaused)
                    elapsed += Time.deltaTime;

                yield return null;
            }
        }

        /// <summary>
        /// 处理组内排序块被选中时的回调。
        /// </summary>
        /// <param name="controller">触发事件的排序块控制器。</param>
        /// <param name="index">被选中块的索引。</param>
        private void HandleBarSelected(SortBarsController controller, int index)
        {
            owner?.SelectGroup(this);
        }

        /// <summary>
        /// 刷新组编号和算法名称文本。
        /// </summary>
        private void RefreshLabel()
        {
            if (groupLabel == null) return;
            groupLabel.text = $"#{GroupNumber} {SelectedAlgorithm.DisplayName}";
        }

        /// <summary>
        /// 根据当前选中状态刷新边框颜色和线宽。
        /// </summary>
        private void RefreshBorderVisual()
        {
            if (borderRenderer == null) return;

            Color borderColor = isSelected ? selectedBorderColor : normalBorderColor;
            float borderWidth = isSelected ? selectedBorderWidth : normalBorderWidth;
            borderRenderer.startColor = borderColor;
            borderRenderer.endColor = borderColor;
            borderRenderer.startWidth = borderWidth;
            borderRenderer.endWidth = borderWidth;
        }
    }
}
