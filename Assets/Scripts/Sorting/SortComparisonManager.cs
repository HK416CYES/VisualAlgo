using System.Collections.Generic;
using UnityEngine;
using VisualAlgo.Managers;
using VisualAlgo.Managers.UI;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 管理多个排序组的共享数据、组选择和全局控制。
    /// </summary>
    public sealed class SortComparisonManager : MonoBehaviour
    {
        /// <summary>
        /// 用于新建排序组的预制体。
        /// </summary>
        [Header("排序组预制体")][SerializeField] private SortGroupController groupPrefab;

        /// <summary>
        /// 场景中所有排序组实例的父节点。
        /// </summary>
        [Header("排序组父节点")][SerializeField] private Transform groupRoot;

        /// <summary>
        /// 进入场景时默认存在的排序组数量。
        /// </summary>
        [Header("初始排序组数量")][SerializeField, Min(1)] private int initialGroupCount = 1;

        /// <summary>
        /// 每个排序组默认拥有的排序块数量。
        /// </summary>
        [Header("初始块数量")][SerializeField, Min(2)] private int initialBarCount = 12;

        /// <summary>
        /// 自动排列排序组时，相邻排序组在 Y 轴上的间距。
        /// </summary>
        [Header("组垂直间距")][SerializeField, Min(1f)] private float groupVerticalSpacing = 13f;

        /// <summary>
        /// 共享高度值允许的最小值。
        /// </summary>
        [Header("共享高度范围")][SerializeField, Min(0.1f)] private float minHeight = 0.8f;

        /// <summary>
        /// 共享高度值允许的最大值。
        /// </summary>
        [SerializeField, Min(0.2f)] private float maxHeight = 12f;

        /// <summary>
        /// 全局控制所有组时使用的单步操作间隔。
        /// </summary>
        [Header("全局操作间隔")][SerializeField, Min(0.02f)] private float globalInterval = 0.25f;

        /// <summary>
        /// 所有排序组共享的排序块宽度。
        /// </summary>
        [Header("共享块布局")][SerializeField, Min(0.05f)] private float sharedBarWidth = 0.6f;

        /// <summary>
        /// 所有排序组共享的排序块水平间距。
        /// </summary>
        [SerializeField, Min(0f)] private float sharedBarGap = 0.15f;

        /// <summary>
        /// 所有排序组共同使用的原始高度数据。
        /// </summary>
        private readonly List<float> sharedValues = new();

        /// <summary>
        /// 当前场景内由管理器维护的排序组列表。
        /// </summary>
        private readonly List<SortGroupController> groups = new();

        /// <summary>
        /// 当前被玩家选中的排序组。
        /// </summary>
        private SortGroupController selectedGroup;

        /// <summary>
        /// 当前管理器是否正在销毁或切换场景。
        /// </summary>
        private bool isDestroying;

        /// <summary>
        /// 获取所有排序组的只读列表。
        /// </summary>
        public IReadOnlyList<SortGroupController> Groups => groups;

        /// <summary>
        /// 获取当前选中的排序组。
        /// </summary>
        public SortGroupController SelectedGroup => selectedGroup;

        /// <summary>
        /// 获取当前全局操作间隔。
        /// </summary>
        public float GlobalInterval => globalInterval;

        /// <summary>
        /// 获取共享高度值允许的最小值。
        /// </summary>
        public float MinHeight => minHeight;

        /// <summary>
        /// 获取共享高度值允许的最大值。
        /// </summary>
        public float MaxHeight => maxHeight;

        /// <summary>
        /// 获取当前共享排序块宽度。
        /// </summary>
        public float SharedBarWidth => sharedBarWidth;

        /// <summary>
        /// 获取当前共享排序块间距。
        /// </summary>
        public float SharedBarGap => sharedBarGap;

        /// <summary>
        /// 获取当前是否存在正在运行且未暂停的排序组。
        /// </summary>
        public bool HasAnyActiveSimulation => groups.Exists(group => group != null && group.IsRunning && !group.IsPaused);

        /// <summary>
        /// 初始化共享数据和排序组实例。
        /// </summary>
        private void Awake()
        {
            RebuildGroupCache();
        }

        /// <summary>
        /// 在所有排序组及其子控制器完成 Awake 后再做共享数据初始化，避免场景切换时重复创建排序块。
        /// </summary>
        private void Start()
        {
            if (sharedValues.Count == 0)
                ResetSharedValues(initialBarCount);

            EnsureGroupCount(Mathf.Max(1, initialGroupCount));
            SelectGroup(groups.Count > 0 ? groups[0] : null);
        }

        /// <summary>
        /// 场景卸载时阻止后续选择逻辑访问已销毁对象。
        /// </summary>
        private void OnDestroy()
        {
            isDestroying = true;
            groups.Clear();
            selectedGroup = null;
        }

        /// <summary>
        /// 设置全局排序操作间隔。
        /// </summary>
        /// <param name="interval">玩家输入的操作间隔秒数。</param>
        public void SetGlobalInterval(float interval)
        {
            globalInterval = Mathf.Max(0.02f, interval);
            foreach (SortGroupController group in groups)
                group.SetOperationInterval(globalInterval);
        }

        /// <summary>
        /// 确保场景里存在指定数量的排序组。
        /// </summary>
        /// <param name="count">目标排序组数量。</param>
        public void EnsureGroupCount(int count)
        {
            count = Mathf.Max(1, count);

            while (groups.Count < count)
                AddGroup(groups.Count);

            while (groups.Count > count)
                RemoveLastGroup();

            LayoutGroups();
            SyncAllGroups();
            ReindexGroups();
            SelectGroup(selectedGroup != null ? selectedGroup : groups[0]);
        }

        /// <summary>
        /// 设置所有排序组的排序块宽度和块间距。
        /// </summary>
        /// <param name="barWidth">新的排序块宽度。</param>
        /// <param name="barGap">新的排序块水平间距。</param>
        public void SetSharedBarLayout(float barWidth, float barGap)
        {
            sharedBarWidth = Mathf.Max(0.05f, barWidth);
            sharedBarGap = Mathf.Max(0f, barGap);

            foreach (SortGroupController group in groups)
            {
                if (group == null || group.BarsController == null) continue;
                group.BarsController.SetBarLayout(sharedBarWidth, sharedBarGap);
                group.RefreshBorder();
            }
        }

        /// <summary>
        /// 根据指定数量重建所有组共享的初始高度值。
        /// </summary>
        /// <param name="barCount">每组排序块数量。</param>
        public void ResetSharedValues(int barCount)
        {
            barCount = Mathf.Clamp(barCount, 2, 256);
            sharedValues.Clear();

            for (int i = 0; i < barCount; i++)
            {
                float t = barCount == 1 ? 0f : (float)i / (barCount - 1);
                sharedValues.Add(Mathf.Lerp(minHeight, maxHeight, t));
            }

            SyncAllGroups();
        }

        /// <summary>
        /// 为所有组同步生成随机高度。
        /// </summary>
        public void RandomizeSharedValues()
        {
            RandomizeSharedValues(minHeight, maxHeight);
        }

        /// <summary>
        /// 按指定随机区间为所有组同步生成随机高度。
        /// </summary>
        /// <param name="requestedMinHeight">玩家输入的随机最小高度。</param>
        /// <param name="requestedMaxHeight">玩家输入的随机最大高度。</param>
        public void RandomizeSharedValues(float requestedMinHeight, float requestedMaxHeight)
        {
            float randomMin = Mathf.Clamp(Mathf.Min(requestedMinHeight, requestedMaxHeight), minHeight, maxHeight);
            float randomMax = Mathf.Clamp(Mathf.Max(requestedMinHeight, requestedMaxHeight), minHeight, maxHeight);
            if (Mathf.Approximately(randomMin, randomMax))
                randomMax = Mathf.Min(maxHeight, randomMin + 0.1f);

            for (int i = 0; i < sharedValues.Count; i++)
                sharedValues[i] = Random.Range(randomMin, randomMax);

            SyncAllGroups();
        }

        /// <summary>
        /// 通过预制体新增一个排序组，并自动选中新组。
        /// </summary>
        public void AddGroupAndSelect()
        {
            if (isDestroying) return;

            AddGroup(groups.Count);
            SortGroupController addedGroup = groups.Count > 0 ? groups[^1] : null;
            if (addedGroup != null)
                PlaceAddedGroup(addedGroup);

            if (addedGroup != null)
                addedGroup.RefreshBorder();

            SelectGroup(addedGroup);
            FocusCameraOnGroup(addedGroup);
        }

        /// <summary>
        /// 删除当前选中的排序组，并在删除后重新编号。
        /// </summary>
        public void RemoveSelectedGroup()
        {
            if (isDestroying) return;
            if (selectedGroup == null || groups.Count <= 1) return;

            int selectedIndex = groups.IndexOf(selectedGroup);
            SortGroupController removedGroup = selectedGroup;
            groups.Remove(removedGroup);
            Destroy(removedGroup.gameObject);

            if (groups.Count == 0)
            {
                selectedGroup = null;
                NotifyGroupStateChanged(null);
                return;
            }

            ReindexGroups();
            int fallbackIndex = Mathf.Clamp(selectedIndex, 0, groups.Count - 1);
            SelectGroup(groups[fallbackIndex], true);
        }

        /// <summary>
        /// 修改当前选中组内选中块的高度，并同步到其他组相同索引的块。
        /// </summary>
        /// <param name="height">目标高度。</param>
        /// <returns>是否成功修改。</returns>
        public bool TrySetSelectedHeight(float height)
        {
            if (selectedGroup == null || selectedGroup.SelectedBarIndex < 0) return false;

            int index = selectedGroup.SelectedBarIndex;
            sharedValues[index] = Mathf.Clamp(height, minHeight, maxHeight);
            SyncAllGroups();
            return true;
        }

        /// <summary>
        /// 设置当前选中的排序组，并在切换组时清理其他组残留的手动选中颜色。
        /// </summary>
        /// <param name="group">要选中的排序组。</param>
        /// <param name="clearSelectedBarInGroup">是否同时清理目标组内已选中的排序块。</param>
        public void SelectGroup(SortGroupController group, bool clearSelectedBarInGroup = false)
        {
            if (isDestroying) return;

            if (group == null)
            {
                DeselectGroup();
                return;
            }

            ClearBarSelectionOutside(group);
            if (selectedGroup != null && selectedGroup != group)
                selectedGroup.SetSelected(false);

            selectedGroup = group;
            if (clearSelectedBarInGroup)
                selectedGroup.ClearSelectedBar();

            selectedGroup.SetSelected(true);
            NotifyGroupStateChanged(group);
        }

        /// <summary>
        /// 取消当前组选中状态，并清理全部组的边框高亮和手动选中块。
        /// </summary>
        public void DeselectGroup()
        {
            if (isDestroying) return;

            selectedGroup = null;

            foreach (SortGroupController group in groups)
            {
                if (group == null) continue;
                group.ClearSelectedBar();
                group.SetSelected(false);
            }

            NotifyGroupStateChanged(null);
        }

        /// <summary>
        /// 通知 UI 当前排序组状态发生了变化。
        /// </summary>
        /// <param name="group">状态发生变化的排序组。</param>
        public void NotifyGroupStateChanged(SortGroupController group)
        {
            if (SortingSimulationUIController.Instance != null)
                SortingSimulationUIController.Instance.RefreshSelectedGroupUI();
        }

        /// <summary>
        /// 启动当前选中组的排序模拟。
        /// </summary>
        public void StartSelected()
        {
            selectedGroup?.StartSimulation(globalInterval);
            NotifyGroupStateChanged(selectedGroup);
        }

        /// <summary>
        /// 停止当前选中组的排序模拟。
        /// </summary>
        public void StopSelected()
        {
            selectedGroup?.StopSimulation();
            NotifyGroupStateChanged(selectedGroup);
        }

        /// <summary>
        /// 切换当前选中组的开始/停止状态。
        /// </summary>
        public void ToggleSelectedStartStop()
        {
            if (selectedGroup == null) return;
            selectedGroup.ToggleStartStop(globalInterval);
            NotifyGroupStateChanged(selectedGroup);
        }

        /// <summary>
        /// 切换当前选中组的暂停/恢复状态。
        /// </summary>
        public void ToggleSelectedPauseResume()
        {
            selectedGroup?.TogglePauseResume();
            NotifyGroupStateChanged(selectedGroup);
        }

        /// <summary>
        /// 启动全部排序组的排序模拟。
        /// </summary>
        public void StartAll()
        {
            foreach (SortGroupController group in groups)
                group.StartSimulation(globalInterval);

            NotifyGroupStateChanged(selectedGroup);
        }

        /// <summary>
        /// 停止全部排序组的排序模拟。
        /// </summary>
        public void StopAll()
        {
            foreach (SortGroupController group in groups)
                group.StopSimulation();

            NotifyGroupStateChanged(selectedGroup);
        }

        /// <summary>
        /// 设置全部正在运行排序组的暂停状态。
        /// </summary>
        /// <param name="paused">是否暂停。</param>
        public void SetAllPaused(bool paused)
        {
            foreach (SortGroupController group in groups)
                group.SetPaused(paused);

            NotifyGroupStateChanged(selectedGroup);
        }

        /// <summary>
        /// 根据当前运行状态切换全部排序组的开始/停止。
        /// </summary>
        public void ToggleAllStartStop()
        {
            bool anyRunning = groups.Exists(group => group.IsRunning);
            if (anyRunning) StopAll();
            else
            {
                PanelController.Instance.SetExpanded(false);
                StartAll();
            }
        }

        /// <summary>
        /// 根据当前暂停状态切换全部排序组的暂停/恢复。
        /// </summary>
        public void ToggleAllPauseResume()
        {
            bool anyRunningNotPaused = groups.Exists(group => group.IsRunning && !group.IsPaused);
            SetAllPaused(anyRunningNotPaused);
        }

        /// <summary>
        /// 清理除指定组之外所有组的手动选中块状态。
        /// </summary>
        /// <param name="keptGroup">需要保留选中状态的排序组。</param>
        private void ClearBarSelectionOutside(SortGroupController keptGroup)
        {
            foreach (SortGroupController group in groups)
            {
                if (group == null || group == keptGroup) continue;
                group.ClearSelectedBar();
            }
        }

        /// <summary>
        /// 从组父节点重新收集场景中已有的排序组。
        /// </summary>
        private void RebuildGroupCache()
        {
            groups.Clear();
            if (groupRoot == null) return;

            for (int i = 0; i < groupRoot.childCount; i++)
            {
                SortGroupController group = groupRoot.GetChild(i).GetComponent<SortGroupController>();
                if (group != null)
                    groups.Add(group);
            }
        }

        /// <summary>
        /// 将主相机平滑移动到指定排序组附近。
        /// </summary>
        /// <param name="group">需要聚焦的排序组。</param>
        private void FocusCameraOnGroup(SortGroupController group)
        {
            if (group == null || Camera.main == null) return;

            CameraManager cameraManager = Camera.main.GetComponent<CameraManager>();
            if (cameraManager != null)
                cameraManager.FocusOn(group.transform.position + new Vector3(0, group.borderHeight / 2f, 0));
        }

        /// <summary>
        /// 通过排序组预制体新增一个组。
        /// </summary>
        /// <param name="index">新增组的索引。</param>
        private void AddGroup(int index)
        {
            if (groupPrefab == null || groupRoot == null)
            {
                Debug.LogError($"{nameof(SortComparisonManager)} requires groupPrefab and groupRoot references.");
                return;
            }

            SortGroupController group = Instantiate(groupPrefab, groupRoot, false);
            group.Initialize(this, index, sharedValues);
            group.SetOperationInterval(globalInterval);
            if (group.BarsController != null)
                group.BarsController.SetBarLayout(sharedBarWidth, sharedBarGap);
            groups.Add(group);
        }

        /// <summary>
        /// 移除列表末尾的排序组。
        /// </summary>
        private void RemoveLastGroup()
        {
            SortGroupController group = groups[^1];
            groups.RemoveAt(groups.Count - 1);
            if (selectedGroup == group)
                selectedGroup = groups.Count > 0 ? groups[0] : null;

            Destroy(group.gameObject);
        }

        /// <summary>
        /// 将新增组放到当前最下方排序组的下方。
        /// </summary>
        /// <param name="addedGroup">刚刚新增的排序组。</param>
        private void PlaceAddedGroup(SortGroupController addedGroup)
        {
            if (addedGroup == null) return;
            if (groups.Count <= 1)
            {
                addedGroup.transform.localPosition = Vector3.zero;
                return;
            }

            float lowestY = float.MaxValue;
            foreach (SortGroupController group in groups)
            {
                if (group == null || group == addedGroup) continue;
                lowestY = Mathf.Min(lowestY, group.transform.localPosition.y);
            }

            if (float.IsPositiveInfinity(lowestY) || Mathf.Approximately(lowestY, float.MaxValue))
                lowestY = 0f;

            addedGroup.transform.localPosition = new Vector3(0f, lowestY - groupVerticalSpacing, 0f);
        }

        /// <summary>
        /// 将共享高度值同步到所有排序组。
        /// </summary>
        private void SyncAllGroups()
        {
            foreach (SortGroupController group in groups)
                group.ApplySharedValues(sharedValues);
        }

        /// <summary>
        /// 按世界中轴线从上到下排列所有排序组。
        /// </summary>
        private void LayoutGroups()
        {
            for (int i = 0; i < groups.Count; i++)
                groups[i].transform.localPosition = new Vector3(0f, -i * groupVerticalSpacing, 0f);
        }

        /// <summary>
        /// 重新设置所有排序组的编号和边框。
        /// </summary>
        private void ReindexGroups()
        {
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].SetGroupIndex(i);
                groups[i].RefreshBorder();
                groups[i].SetSelected(groups[i] == selectedGroup);
            }
        }

        /// <summary>
        /// 根据排序组当前高度位置从上到下重新排序并编号。
        /// </summary>
        private void ReindexGroupsByVerticalPosition()
        {
            groups.Sort((left, right) => right.transform.position.y.CompareTo(left.transform.position.y));
            for (int i = 0; i < groups.Count; i++)
            {
                groups[i].transform.SetSiblingIndex(i);
                groups[i].SetGroupIndex(i);
                groups[i].RefreshBorder();
                groups[i].SetSelected(groups[i] == selectedGroup);
            }
        }
    }
}
