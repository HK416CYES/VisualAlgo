using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 管理和创建排序块，跟踪它们当前的值，处理鼠标发起的排序块选中操作，并负责将排序操作在场景里可视化出来。
    /// </summary>
    public sealed class SortBarsController : MonoBehaviour
    {
        #region 字段及属性定义

        [Header("用于在场景中实例化单个排序块的预制体引用")]
        [SerializeField] private SortBarView barPrefab;

        [Header("用于存放所有实例化排序块的父节点 Transform")]
        [SerializeField] private Transform barRoot;

        [Header("排序块的材质")]
        [Tooltip("排序块在常态下应用的材质。")][SerializeField] private Material normalMaterial;
        [Tooltip("排序块在被选中或发生交互时（被算法激活时）应用的材质")][SerializeField] private Material activeMaterial;
        [Tooltip("快速排序中轴元素使用的材质。未指定时会在运行时自动创建一个紫色材质。")][SerializeField] private Material pivotMaterial;
        [Tooltip("排序块在排序完成以后应用的材质。")][SerializeField] private Material sortedMaterial;

        [Header("首次启动时初始生成的排序块数量")][SerializeField, Min(2)] private int initialCount = 12;
        [Header("允许生成的最大排序块数量")][SerializeField, Min(2)] private int maxBarCount = 256;

        [Header("排序块的宽度")][SerializeField, Min(0.05f)] private float barWidth = 0.6f;

        [Header("排序块之间的水平间距")][SerializeField, Min(0f)] private float gap = 0.15f;

        [Header("排序块高度的数值范围")]
        [Tooltip("排序块所能表示的最小数值")][SerializeField, Min(0.1f)] private float minHeight = 0.8f;
        [Tooltip("排序块所能表示的最大数值")][SerializeField, Min(0.2f)] private float maxHeight = 12f;

        /// <summary>
        /// 内部存储并维护的各排序块实时高度数值列表。
        /// </summary>
        private readonly List<float> values = new();

        /// <summary>
        /// 内部存储的各个排序块视觉视图组件的引用列表与 values 列表顺序相对应。
        /// </summary>
        private readonly List<SortBarView> bars = new();
        private readonly HashSet<int> activeIndices = new();
        private readonly HashSet<int> sortedIndices = new();
        private Material runtimePivotMaterial;
        private int pivotIndex = -1;
        private bool swapVisualInProgress;

        /// <summary>
        /// 当前控制器是否正在销毁或随场景卸载。
        /// </summary>
        private bool isDestroying;

        /// <summary>
        /// 当前被用户鼠标选中的排序块的内部数组索引。
        /// </summary>
        private int selectedIndex = -1;

        /// <summary>
        /// 是否允许用户在这个阶段通过鼠标左键来选中场景中的排序块。
        /// </summary>
        private bool allowSelection = true;

        public event Action<SortBarsController, int> BarSelected;

        /// <summary>
        /// 获取所有排序块当前的高度/数值的只读列表。
        /// </summary>
        public IReadOnlyList<float> Values => values;

        /// <summary>
        /// 获取当前场景中存在的排序块总数。
        /// </summary>
        public int Count => bars.Count;

        /// <summary>
        /// 获取设定中排序块可产生的最小高度。
        /// </summary>
        public float MinHeight => minHeight;

        /// <summary>
        /// 获取设定中排序块可产生的最大高度。
        /// </summary>
        public float MaxHeight => maxHeight;

        /// <summary>
        /// 检查当前是否存在已选中的有效排序块。
        /// </summary>
        public bool HasSelectedBar => IsValidIndex(selectedIndex);

        /// <summary>
        /// 获取被选中的排序块用来展示给用户的序号（内部索引+1）。
        /// </summary>
        public int SelectedUserIndex => selectedIndex + 1;

        /// <summary>
        /// 获取当前被选中排序块的零基索引，没有选中时返回 -1。
        /// </summary>
        public int SelectedIndex => selectedIndex;

        /// <summary>
        /// 获取排序块宽度，供组边框根据实际布局计算外框尺寸。
        /// </summary>
        public float BarWidth => barWidth;

        /// <summary>
        /// 获取排序块间距，供组边框根据实际布局计算外框尺寸。
        /// </summary>
        public float Gap => gap;

        #endregion

        private void Awake()
        {
            NormalizeLimits();
            RebuildCacheFromExistingBars();

            if (bars.Count == 0 && barPrefab != null && barRoot != null) Generate(initialCount);
        }

        private void Update()
        {
            if (isDestroying) return;
            TrySelectBarFromMouse();
        }

        /// <summary>
        /// 场景卸载时清空缓存，避免后续逻辑访问已销毁的排序块引用。
        /// </summary>
        private void OnDestroy()
        {
            isDestroying = true;
            StopAllCoroutines();
            bars.Clear();
            values.Clear();
            ClearVisualTracking();
        }

        /// <summary>
        /// 从现有的排序块重建缓存列表，以确保在编辑器中对场景进行修改后，控制器能够正确识别和管理这些块。
        /// </summary>
        private void RebuildCacheFromExistingBars()
        {
            bars.Clear();
            values.Clear();
            if (barRoot == null) return;

            for (int i = 0; i < barRoot.childCount; i++)
            {
                SortBarView bar = barRoot.GetChild(i).GetComponent<SortBarView>();
                if (bar == null) continue;

                float height = Mathf.Clamp(bar.ReadCurrentHeight(), minHeight, maxHeight);
                bar.Initialize(bars.Count, barWidth, normalMaterial, activeMaterial, sortedMaterial, GetPivotMaterial());
                bar.SetHeight(height);
                bars.Add(bar);
                values.Add(height);
            }

            ApplyLayout();
        }

        /// <summary>
        /// 依赖注入
        /// </summary>
        public void Configure(Transform root, Material normal, Material active, Material sorted, Material pivot = null)
        {
            barRoot = root;
            normalMaterial = normal;
            activeMaterial = active;
            sortedMaterial = sorted;
            pivotMaterial = pivot;
        }

        /// <summary>
        /// 清除过往所有的块，根据设定的数量重新生成新的带坡度高度的初始排序块。
        /// </summary>
        /// <param name="count">需要重新生成多少个排序块</param>
        public void Generate(int count)
        {
            // 每次生成都重新创建数据和场景对象，以使视图呈现的内容始终与值列表相匹配
            NormalizeLimits();
            count = Mathf.Clamp(count, 2, maxBarCount);
            ClearBars();

            for (int i = 0; i < count; i++)
            {
                values.Add(Mathf.Lerp(minHeight, maxHeight, count == 1 ? 0f : (float)i / (count - 1)));
                SortBarView bar = CreateBar();
                if (bar != null) bars.Add(bar);
            }

            ApplyLayout();
            ClearVisualTracking();
            RefreshAllVisualStates();
            ClearSelection();
        }

        /// <summary>
        /// 在保持既有块不变的前提下随机打乱每个块的高度值。
        /// </summary>
        public void RandomizeHeights()
        {
            RandomizeHeights(minHeight, maxHeight);
        }

        /// <summary>
        /// 将一组共享高度值应用到当前排序组。用于多个排序组保持相同元素。
        /// </summary>
        public void SetValues(IReadOnlyList<float> sourceValues)
        {
            if (sourceValues == null || sourceValues.Count == 0) return;

            NormalizeLimits();

            // 某些场景切换路径下，组管理器可能会早于本控制器的 Awake 调用 SetValues。
            // 此时 bars 列表尚未重建，但 barRoot 下其实已经带着场景序列化出来的现成排序块。
            // 先把这些现有块收集进缓存，避免误走“清空并新建”分支导致播放模式下出现 12+12 的重复柱体。
            if (bars.Count == 0 && barRoot != null && barRoot.childCount > 0)
                RebuildCacheFromExistingBars();

            RemoveDestroyedBarReferences();

            ClearBars();
            for (int i = 0; i < sourceValues.Count; i++)
            {
                values.Add(Mathf.Clamp(sourceValues[i], minHeight, maxHeight));
                SortBarView bar = CreateBar();
                if (bar != null) bars.Add(bar);
            }

            ApplyLayout();
            ClearVisualTracking();
            RefreshAllVisualStates();
            ClearSelection();
        }

        /// <summary>
        /// 在保持既有块数量不变的前提下，按指定区间随机打乱每个块的高度值。
        /// </summary>
        public void RandomizeHeights(float requestedMinHeight, float requestedMaxHeight)
        {
            // 维持相同的排序块数量，只打乱高度数值列表，之后应用这些变化。
            NormalizeLimits();
            float randomMin = Mathf.Clamp(Mathf.Min(requestedMinHeight, requestedMaxHeight), minHeight, maxHeight);
            float randomMax = Mathf.Clamp(Mathf.Max(requestedMinHeight, requestedMaxHeight), minHeight, maxHeight);
            if (Mathf.Approximately(randomMin, randomMax))
                randomMax = Mathf.Min(maxHeight, randomMin + 0.1f);

            for (int i = 0; i < values.Count; i++)
            {
                values[i] = UnityEngine.Random.Range(randomMin, randomMax);
            }

            ApplyLayout();
            ClearVisualTracking();
            RefreshAllVisualStates();
            ClearSelection();
        }

        /// <summary>
        /// 将指定的高度值直接应用到当前被鼠标选定的排序块上。
        /// </summary>
        /// <param name="height">新的高度值</param>
        /// <returns>修改是否成功执行</returns>
        public bool TrySetSelectedHeight(float height)
        {
            if (!HasSelectedBar) return false;

            return TrySetHeightAt(selectedIndex, height);
        }

        /// <summary>
        /// 修改指定索引排序块的高度。
        /// </summary>
        /// <param name="index">要修改的排序块索引。</param>
        /// <param name="height">目标高度。</param>
        /// <returns>是否成功修改。</returns>
        public bool TrySetHeightAt(int index, float height)
        {
            if (!IsValidIndex(index)) return false;

            NormalizeLimits();
            values[index] = Mathf.Clamp(height, minHeight, maxHeight);
            bars[index].SetHeight(values[index]);
            ApplyLayout();
            RefreshAllVisualStates();
            return true;
        }

        /// <summary>
        /// 设定是否允许用户使用鼠标指针在场景中点选。
        /// </summary>
        public void SetSelectionEnabled(bool enabled)
        {
            allowSelection = enabled;
        }

        /// <summary>
        /// 设置排序块的宽度和块之间的间距，并立即刷新当前布局。
        /// </summary>
        /// <param name="newBarWidth">新的排序块宽度。</param>
        /// <param name="newGap">新的排序块间距。</param>
        public void SetBarLayout(float newBarWidth, float newGap)
        {
            if (isDestroying) return;

            barWidth = Mathf.Max(0.05f, newBarWidth);
            gap = Mathf.Max(0f, newGap);
            ApplyLayout();
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 对特定的两个索引指向的块应用一个抽象的排序步骤操作并在场景中反馈显示。
        /// </summary>
        /// <param name="operation">单步操作行为描述数据记录</param>
        public void ApplyOperation(SortOperation operation)
        {
            // 算法会描述它想要做什么操作；这个控制器在接收到该意图后会据此执行具体的可视化展现以及逻辑数组的更新
            if (!IsValidIndex(operation.FirstIndex)) return;

            if (operation.Type == SortOperationType.MarkSorted)
            {
                sortedIndices.Add(operation.FirstIndex);
                if (pivotIndex == operation.FirstIndex) pivotIndex = -1;
                activeIndices.Clear();
                AddActiveIndex(operation.FirstIndex);
                RefreshAllVisualStates();
                return;
            }

            if (operation.Type == SortOperationType.Pivot)
            {
                pivotIndex = operation.FirstIndex;
                activeIndices.Clear();
                RefreshAllVisualStates();
                return;
            }

            if (operation.Type == SortOperationType.Assign)
            {
                values[operation.FirstIndex] = Mathf.Clamp(operation.Value, minHeight, maxHeight);
                bars[operation.FirstIndex].SetHeight(values[operation.FirstIndex]);
                activeIndices.Clear();
                AddActiveIndex(operation.FirstIndex);
                RefreshAllVisualStates();
                return;
            }

            if (!IsValidIndex(operation.SecondIndex)) return;

            if (operation.Type == SortOperationType.Insert)
            {
                CommitInsert(operation.FirstIndex, operation.SecondIndex);
                ApplyLayout();
                activeIndices.Clear();
                AddActiveIndex(operation.SecondIndex);
                RefreshAllVisualStates();
                return;
            }

            if (operation.Type == SortOperationType.Swap) Swap(operation.FirstIndex, operation.SecondIndex);

            activeIndices.Clear();
            AddActiveIndex(operation.FirstIndex);
            AddActiveIndex(operation.SecondIndex);
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 应用排序操作。交换操作会在给定时长内平滑移动，其余操作保持即时更新并由调用方控制等待时间。
        /// </summary>
        public IEnumerator ApplyOperationAnimated(
            SortOperation operation,
            float duration,
            Func<bool> isPaused,
            Func<bool> shouldStop)
        {
            if (operation.Type == SortOperationType.Assign)
            {
                yield return AssignAnimated(operation.FirstIndex, operation.Value, duration, isPaused, shouldStop);
                yield break;
            }

            if (operation.Type == SortOperationType.Insert)
            {
                yield return InsertAnimated(operation.FirstIndex, operation.SecondIndex, duration, isPaused, shouldStop);
                yield break;
            }

            if (operation.Type != SortOperationType.Swap)
            {
                ApplyOperation(operation);
                yield break;
            }

            if (!IsValidIndex(operation.FirstIndex) || !IsValidIndex(operation.SecondIndex))
                yield break;

            yield return SwapAnimated(operation.FirstIndex, operation.SecondIndex, duration, isPaused, shouldStop);
        }

        /// <summary>
        /// 标记并为所有排序块设置“已完成排序”材质样式反馈。
        /// </summary>
        public void MarkSorted()
        {
            sortedIndices.Clear();
            for (int i = 0; i < bars.Count; i++)
                sortedIndices.Add(i);

            activeIndices.Clear();
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 将界面所有的渲染状态全部切回常规默认状态并重新刷新激活的高亮元素。
        /// </summary>
        public void ResetVisuals()
        {
            ClearVisualTracking();
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 利用预制体/图元创建单一一个实体块并在场景里挂载相关脚本组件。
        /// </summary>
        private SortBarView CreateBar()
        {
            if (barRoot == null || barPrefab == null)
            {
                Debug.LogError($"{nameof(SortBarsController)} on {name} requires barRoot and barPrefab references.");
                return null;
            }

            SortBarView barView = Instantiate(barPrefab, barRoot, false);
            barView.gameObject.name = "Sort Bar";
            barView.Initialize(bars.Count, barWidth, normalMaterial, activeMaterial, sortedMaterial, GetPivotMaterial());
            return barView;
        }

        /// <summary>
        /// 清除所有排序块
        /// </summary>
        private void ClearBars()
        {
            bars.Clear();
            values.Clear();
            ClearVisualTracking();
            if (barRoot == null) return;

            for (int i = barRoot.childCount - 1; i >= 0; i--)
                DestroyImmediateSafe(barRoot.GetChild(i).gameObject);
        }

        /// <summary>
        /// 根据当前的 values 列表中的数值应用布局
        /// </summary>
        private void ApplyLayout()
        {
            // 通过设定好各自的x坐标以及保持底部对齐（通过SortBarView.SetHeight中实现）来布局生成的这些方块
            float totalWidth = values.Count * barWidth + Mathf.Max(0, values.Count - 1) * gap;
            float startX = -totalWidth * 0.5f + barWidth * 0.5f;

            for (int i = 0; i < bars.Count; i++)
            {
                bars[i].Initialize(i, barWidth, normalMaterial, activeMaterial, sortedMaterial, GetPivotMaterial());
                bars[i].SetXPosition(startX + i * (barWidth + gap));
                bars[i].SetHeight(values[i]);
            }
        }

        /// <summary>
        /// 交换两个排序块的位置和它们在 values 列表中的数值，同时如果其中一个块被选中则更新 selectedIndex 以保持选中状态的正确性。
        /// </summary>
        /// <param name="firstIndex"></param>
        /// <param name="secondIndex"></param>
        private void Swap(int firstIndex, int secondIndex)
        {
            (values[firstIndex], values[secondIndex]) = (values[secondIndex], values[firstIndex]);
            (bars[firstIndex], bars[secondIndex]) = (bars[secondIndex], bars[firstIndex]);

            if (selectedIndex == firstIndex) selectedIndex = secondIndex;
            else if (selectedIndex == secondIndex) selectedIndex = firstIndex;

            if (pivotIndex == firstIndex) pivotIndex = secondIndex;
            else if (pivotIndex == secondIndex) pivotIndex = firstIndex;

            ApplyLayout();
        }

        /// <summary>
        /// 在当前操作间隔内把两个排序块平滑移动到对方的位置，动画结束后再提交数组顺序。
        /// </summary>
        private IEnumerator SwapAnimated(
            int firstIndex,
            int secondIndex,
            float duration,
            Func<bool> isPaused,
            Func<bool> shouldStop)
        {
            SortBarView firstBar = bars[firstIndex];
            SortBarView secondBar = bars[secondIndex];
            float firstStartX = firstBar.transform.localPosition.x;
            float secondStartX = secondBar.transform.localPosition.x;
            float animationDuration = Mathf.Max(0.02f, duration);

            activeIndices.Clear();
            AddActiveIndex(firstIndex, true);
            AddActiveIndex(secondIndex, true);
            swapVisualInProgress = true;
            RefreshAllVisualStates();

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                if (shouldStop != null && shouldStop())
                {
                    swapVisualInProgress = false;
                    ApplyLayout();
                    yield break;
                }

                if (isPaused == null || !isPaused())
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / animationDuration);
                    t = t * t * (3f - 2f * t);
                    firstBar.SetXPosition(Mathf.Lerp(firstStartX, secondStartX, t));
                    secondBar.SetXPosition(Mathf.Lerp(secondStartX, firstStartX, t));
                }

                yield return null;
            }

            firstBar.SetXPosition(secondStartX);
            secondBar.SetXPosition(firstStartX);

            CommitSwap(firstIndex, secondIndex);
            ApplyLayout();
            swapVisualInProgress = false;
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 在当前操作间隔内平滑改变指定位置的高度，用于归并排序的写回过程。
        /// </summary>
        private IEnumerator AssignAnimated(
            int index,
            float targetValue,
            float duration,
            Func<bool> isPaused,
            Func<bool> shouldStop)
        {
            if (!IsValidIndex(index)) yield break;

            float startValue = values[index];
            float endValue = Mathf.Clamp(targetValue, minHeight, maxHeight);
            float animationDuration = Mathf.Max(0.02f, duration);

            activeIndices.Clear();
            AddActiveIndex(index);
            RefreshAllVisualStates();

            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                if (shouldStop != null && shouldStop())
                {
                    bars[index].SetHeight(startValue);
                    ApplyLayout();
                    yield break;
                }

                if (isPaused == null || !isPaused())
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / animationDuration);
                    t = t * t * (3f - 2f * t);
                    bars[index].SetHeight(Mathf.Lerp(startValue, endValue, t));
                }

                yield return null;
            }

            values[index] = endValue;
            bars[index].SetHeight(endValue);
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 在当前操作间隔内把一个块插入到目标位置，中间块整体平移补位。用于归并排序合并阶段。
        /// </summary>
        private IEnumerator InsertAnimated(
            int fromIndex,
            int toIndex,
            float duration,
            Func<bool> isPaused,
            Func<bool> shouldStop)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex)
                yield break;

            int rangeStart = Mathf.Min(fromIndex, toIndex);
            int rangeEnd = Mathf.Max(fromIndex, toIndex);
            int rangeLength = rangeEnd - rangeStart + 1;
            float[] startPositions = new float[rangeLength];
            float[] targetPositions = new float[rangeLength];

            for (int index = rangeStart; index <= rangeEnd; index++)
                startPositions[index - rangeStart] = bars[index].transform.localPosition.x;

            for (int index = rangeStart; index <= rangeEnd; index++)
            {
                int localIndex = index - rangeStart;
                if (index == fromIndex)
                {
                    targetPositions[localIndex] = startPositions[toIndex - rangeStart];
                }
                else if (fromIndex > toIndex && index >= toIndex && index < fromIndex)
                {
                    targetPositions[localIndex] = startPositions[index + 1 - rangeStart];
                }
                else if (fromIndex < toIndex && index > fromIndex && index <= toIndex)
                {
                    targetPositions[localIndex] = startPositions[index - 1 - rangeStart];
                }
                else
                {
                    targetPositions[localIndex] = startPositions[localIndex];
                }
            }

            activeIndices.Clear();
            AddActiveIndex(fromIndex);
            AddActiveIndex(toIndex);
            RefreshAllVisualStates();

            float animationDuration = Mathf.Max(0.02f, duration);
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                if (shouldStop != null && shouldStop())
                {
                    ApplyLayout();
                    yield break;
                }

                if (isPaused == null || !isPaused())
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / animationDuration);
                    t = t * t * (3f - 2f * t);

                    for (int index = rangeStart; index <= rangeEnd; index++)
                    {
                        int localIndex = index - rangeStart;
                        bars[index].SetXPosition(Mathf.Lerp(startPositions[localIndex], targetPositions[localIndex], t));
                    }
                }

                yield return null;
            }

            for (int index = rangeStart; index <= rangeEnd; index++)
                bars[index].SetXPosition(targetPositions[index - rangeStart]);

            CommitInsert(fromIndex, toIndex);
            ApplyLayout();
            activeIndices.Clear();
            AddActiveIndex(toIndex);
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 只提交内部数组顺序，不直接改物体坐标；用于动画结束后的状态同步。
        /// </summary>
        private void CommitSwap(int firstIndex, int secondIndex)
        {
            (values[firstIndex], values[secondIndex]) = (values[secondIndex], values[firstIndex]);
            (bars[firstIndex], bars[secondIndex]) = (bars[secondIndex], bars[firstIndex]);

            if (selectedIndex == firstIndex) selectedIndex = secondIndex;
            else if (selectedIndex == secondIndex) selectedIndex = firstIndex;

            if (pivotIndex == firstIndex) pivotIndex = secondIndex;
            else if (pivotIndex == secondIndex) pivotIndex = firstIndex;
        }

        /// <summary>
        /// 提交插入后的数据和视图顺序。fromIndex 的元素移动到 toIndex，其间元素按方向平移。
        /// </summary>
        private void CommitInsert(int fromIndex, int toIndex)
        {
            if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex) || fromIndex == toIndex) return;

            SortBarView movedBar = bars[fromIndex];
            float movedValue = values[fromIndex];

            bars.RemoveAt(fromIndex);
            values.RemoveAt(fromIndex);
            bars.Insert(toIndex, movedBar);
            values.Insert(toIndex, movedValue);

            selectedIndex = RemapIndexAfterInsert(selectedIndex, fromIndex, toIndex);
            pivotIndex = RemapIndexAfterInsert(pivotIndex, fromIndex, toIndex);
        }

        private static int RemapIndexAfterInsert(int index, int fromIndex, int toIndex)
        {
            if (index < 0) return index;
            if (index == fromIndex) return toIndex;

            if (fromIndex > toIndex && index >= toIndex && index < fromIndex)
                return index + 1;

            if (fromIndex < toIndex && index > fromIndex && index <= toIndex)
                return index - 1;

            return index;
        }

        private void SetAllState(SortBarVisualState state)
        {
            foreach (SortBarView bar in bars)
            {
                bar.SetVisualState(state);
            }
        }

        /// <summary>
        /// 根据“已排序位置”和“最近一步操作位置”统一刷新颜色。已排序状态优先级最高。
        /// </summary>
        private void RefreshAllVisualStates()
        {
            if (isDestroying) return;

            RemoveDestroyedBarReferences();

            for (int i = 0; i < bars.Count; i++)
            {
                if (bars[i] == null) continue;

                if (sortedIndices.Contains(i))
                {
                    bars[i].SetVisualState(SortBarVisualState.Sorted);
                }
                else if (pivotIndex == i && (!swapVisualInProgress || !activeIndices.Contains(i)))
                {
                    bars[i].SetVisualState(SortBarVisualState.Pivot);
                }
                else if (activeIndices.Contains(i) || selectedIndex == i)
                {
                    bars[i].SetVisualState(SortBarVisualState.Active);
                }
                else
                {
                    bars[i].SetVisualState(SortBarVisualState.Normal);
                }
            }
        }

        private void ClearVisualTracking()
        {
            activeIndices.Clear();
            sortedIndices.Clear();
            pivotIndex = -1;
            swapVisualInProgress = false;
        }

        /// <summary>
        /// 移除列表里已经被 Unity 销毁的排序块引用，防止场景切换时访问 MissingReference 对象。
        /// </summary>
        private void RemoveDestroyedBarReferences()
        {
            for (int i = bars.Count - 1; i >= 0; i--)
            {
                if (bars[i] != null) continue;

                bars.RemoveAt(i);
                if (i < values.Count)
                    values.RemoveAt(i);

                activeIndices.Remove(i);
                sortedIndices.Remove(i);
                if (selectedIndex == i) selectedIndex = -1;
                if (pivotIndex == i) pivotIndex = -1;
            }
        }

        /// <summary>
        /// 保证运行时阈值合法，并让已有场景序列化值获得新的默认高度上限。
        /// </summary>
        private void NormalizeLimits()
        {
            maxBarCount = Mathf.Max(2, maxBarCount);
            maxHeight = Mathf.Max(12f, maxHeight, minHeight + 0.1f);
        }

        /// <summary>
        /// 比较高亮不覆盖快排轴；交换动画可以临时覆盖，以保留原有交换样式。
        /// </summary>
        private void AddActiveIndex(int index, bool allowPivot = false)
        {
            if (!IsValidIndex(index)) return;
            if (!allowPivot && index == pivotIndex) return;

            activeIndices.Add(index);
        }

        /// <summary>
        /// 获取快排轴材质；没有手动指定时创建一个运行时材质，避免要求用户额外配置资源。
        /// </summary>
        private Material GetPivotMaterial()
        {
            if (pivotMaterial != null) return pivotMaterial;
            if (runtimePivotMaterial != null) return runtimePivotMaterial;

            Shader shader = normalMaterial != null ? normalMaterial.shader : Shader.Find("Standard");
            runtimePivotMaterial = new Material(shader)
            {
                name = "Runtime Sort Pivot Material"
            };
            SetMaterialColor(runtimePivotMaterial, new Color(0.78f, 0.18f, 1f, 1f));

            return runtimePivotMaterial;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null) return;

            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        }

        /// <summary>
        /// 检查给定的索引是否有效
        /// </summary>
        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < bars.Count;
        }

        /// <summary>
        /// 尝试通过鼠标点击选择排序块
        /// </summary>
        private void TrySelectBarFromMouse()
        {
            // 通过Unity的New Input System获取鼠标点击情况以选取排序块，同时如果点击UI区域则被过滤掉
            if (!allowSelection) return;

            Mouse mouse = Mouse.current;
            Camera mainCamera = Camera.main;
            if (mouse == null || mainCamera == null || !mouse.leftButton.wasPressedThisFrame) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;

            SortBarView selectedBar = hit.collider.GetComponentInParent<SortBarView>();
            if (selectedBar == null) return;

            SelectBar(selectedBar);
        }

        /// <summary>
        /// 选中指定的排序块
        /// </summary>
        public bool SelectBar(SortBarView selectedBar)
        {
            int index = bars.IndexOf(selectedBar);
            if (!IsValidIndex(index)) return false;

            selectedIndex = index;
            RefreshAllVisualStates();
            BarSelected?.Invoke(this, selectedIndex);
            return true;
        }

        /// <summary>
        /// 清除玩家当前选中的排序块
        /// </summary>
        public void ClearSelection()
        {
            if (isDestroying)
            {
                selectedIndex = -1;
                return;
            }

            selectedIndex = -1;
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 更新选中块的颜色
        /// </summary>
        private void RefreshSelectionVisual()
        {
            RefreshAllVisualStates();
        }

        /// <summary>
        /// 确保排序块的根节点存在，如果没有则创建一个。
        /// </summary>
        public bool ContainsBar(SortBarView bar)
        {
            return bar != null && bars.Contains(bar);
        }

        /// <summary>
        /// 安全销毁目标
        /// </summary>
        /// <param name="target">要销毁的目标</param>
        private static void DestroyImmediateSafe(UnityEngine.Object target)
        {
            if (target == null) return;

            if (Application.isPlaying) Destroy(target);
            else DestroyImmediate(target);
        }
    }
}
