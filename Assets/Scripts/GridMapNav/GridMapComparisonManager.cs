using System.Collections.Generic;
using UnityEngine;
using VisualAlgo.Managers;
using VisualAlgo.Managers.UI;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 管理多个网格图实例、共享网格数据以及全局运行控制。
    /// </summary>
    public sealed class GridMapComparisonManager : MonoBehaviour
    {
        /// <summary>
        /// 共享网格数据组件。
        /// </summary>
        [Header("核心依赖")][SerializeField] private SharedGridData sharedGridData;

        /// <summary>
        /// 网格图实例工厂。
        /// </summary>
        [SerializeField] private GridMapFactory gridMapFactory;

        /// <summary>
        /// 网格图预制体。
        /// </summary>
        [SerializeField] private GridMapController gridMapPrefab;

        /// <summary>
        /// 方格预制体。
        /// </summary>
        [SerializeField] private GridCellView cellPrefab;

        /// <summary>
        /// 所有网格图实例的父节点。
        /// </summary>
        [SerializeField] private Transform mapsRoot;

        /// <summary>
        /// 网格图之间的额外垂直间距。
        /// </summary>
        [Header("布局设置")][SerializeField, Min(0.5f)] private float mapSpacing = 1.6f;

        /// <summary>
        /// 默认网格宽度。
        /// </summary>
        [Header("默认配置")][SerializeField, Min(2)] private int defaultWidth = 10;

        /// <summary>
        /// 默认网格高度。
        /// </summary>
        [SerializeField, Min(2)] private int defaultHeight = 8;

        /// <summary>
        /// 默认操作间隔时间。
        /// </summary>
        [SerializeField, Min(0.01f)] private float stepInterval = 0.15f;

        /// <summary>
        /// 当前默认编辑模式。
        /// </summary>
        [SerializeField] private GridEditMode editMode = GridEditMode.PaintWall;

        /// <summary>
        /// 当前是否启用了编辑模式。
        /// </summary>
        [SerializeField] private bool editModeEnabled = false;

        /// <summary>
        /// 当前外围边框粗细。
        /// </summary>
        [SerializeField, Min(0.01f)] private float borderThickness = 0.16f;

        /// <summary>
        /// 当前场景中的全部网格图实例。
        /// </summary>
        private readonly List<GridMapController> maps = new();

        /// <summary>
        /// 当前被选中的网格图。
        /// </summary>
        private GridMapController selectedMap;

        /// <summary>
        /// 当选中网格图发生变化时触发。
        /// </summary>
        public event System.Action<GridMapController> OnSelectedMapChanged;

        /// <summary>
        /// 获取当前所有网格图实例。
        /// </summary>
        public IReadOnlyList<GridMapController> Maps => maps;

        /// <summary>
        /// 获取当前选中的网格图。
        /// </summary>
        public GridMapController SelectedMap => selectedMap;

        /// <summary>
        /// 获取当前操作间隔时间。
        /// </summary>
        public float StepInterval => stepInterval;

        /// <summary>
        /// 获取当前编辑模式。
        /// </summary>
        public GridEditMode EditMode => editMode;

        /// <summary>
        /// 获取当前是否启用了编辑模式。
        /// </summary>
        public bool IsEditModeEnabled => editModeEnabled;

        /// <summary>
        /// 获取当前外围边框粗细。
        /// </summary>
        public float BorderThickness => borderThickness;

        /// <summary>
        /// 获取当前是否至少有一个网格图正在运行。
        /// </summary>
        public bool AnyMapRunning
        {
            get
            {
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i] != null && maps[i].IsRunning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// 在脚本启动时完成共享数据与默认网格图初始化。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            sharedGridData.Initialize(defaultWidth, defaultHeight);
            gridMapFactory.Configure(gridMapPrefab, mapsRoot);
            CollectExistingMaps();
            EnsureAtLeastOneMap();
            ApplyBorderThicknessToAllMaps();
        }

        /// <summary>
        /// 切换全局编辑模式。再次点击当前模式时会退出编辑状态。
        /// </summary>
        /// <param name="mode">新的编辑模式。</param>
        public void ToggleEditMode(GridEditMode mode)
        {
            if (editModeEnabled && editMode == mode)
            {
                editModeEnabled = false;
                return;
            }
            editMode = mode;
            editModeEnabled = true;
        }

        /// <summary>
        /// 设置编辑模式启用状态。
        /// </summary>
        /// <param name="enabled">是否启用</param>
        public void SetEditModeEnabled(bool enabled)
        {
            editModeEnabled = enabled;
        }

        /// <summary>
        /// 设置全局寻路步进间隔。
        /// </summary>
        /// <param name="interval">新的时间间隔。</param>
        public void SetStepInterval(float interval)
        {
            stepInterval = Mathf.Max(0.01f, interval);
        }

        /// <summary>
        /// 设置全局边框粗细并同步到所有网格图。
        /// </summary>
        /// <param name="thickness">新的粗细值。</param>
        public void SetBorderThickness(float thickness)
        {
            borderThickness = Mathf.Max(0.01f, thickness);
            ApplyBorderThicknessToAllMaps();
        }

        /// <summary>
        /// 根据输入的宽高重新初始化共享网格。
        /// </summary>
        /// <param name="width">新宽度。</param>
        /// <param name="height">新高度。</param>
        public void RebuildSharedGrid(int width, int height)
        {
            StopAllMaps();
            sharedGridData.Initialize(width, height);
            LayoutMaps();
        }

        /// <summary>
        /// 新建一个与现有网格数据完全同步的网格图实例。
        /// </summary>
        /// <returns>新建出的网格图控制器。</returns>
        public GridMapController AddMap()
        {
            ResolveReferences();

            GridMapController map = gridMapFactory.CreateMap($"Grid Map {maps.Count + 1}");
            if (map == null)
            {
                return null;
            }

            int mapIndex = maps.Count + 1;
            map.Configure(sharedGridData, cellPrefab, mapIndex);
            maps.Add(map);
            map.SetBorderThickness(borderThickness);
            PositionNewMap(map);
            SelectMap(map);
            FocusCameraOnMap(map);
            return map;
        }

        /// <summary>
        /// 删除当前选中的网格图，并自动选中相邻图。
        /// </summary>
        /// <returns>若成功删除则返回真。</returns>
        public bool RemoveSelectedMap()
        {
            return RemoveMap(selectedMap);
        }

        /// <summary>
        /// 选中指定网格图。
        /// </summary>
        /// <param name="map">目标网格图。</param>
        public void SelectMap(GridMapController map)
        {
            selectedMap = map;

            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    maps[i].SetSelected(maps[i] == map);
                }
            }

            OnSelectedMapChanged?.Invoke(selectedMap);
        }

        /// <summary>
        /// 清空当前选中状态。
        /// </summary>
        public void DeselectMap()
        {
            SelectMap(null);
        }

        /// <summary>
        /// 将一次点击编辑应用到指定网格图对应的共享方格上。
        /// </summary>
        /// <param name="map">被点击的网格图。</param>
        /// <param name="coordinate">被点击的方格坐标。</param>
        public void ApplyEdit(GridMapController map, GridCoordinate coordinate)
        {
            if (map == null)
            {
                return;
            }

            if (!editModeEnabled)
            {
                SelectMap(map);
                return;
            }

            SelectMap(map);
            StopAllMaps();

            GridCellType currentType = sharedGridData.GetCellType(coordinate.X, coordinate.Y);
            GridCellType targetType = ResolveTargetCellType(currentType);

            sharedGridData.SetCellType(coordinate, targetType);
        }

        /// <summary>
        /// 启动所有网格图的寻路可视化。
        /// </summary>
        public void StartAllMaps()
        {
            if (!sharedGridData.HasStart || !sharedGridData.HasGoal) return;


            GridMapNavUIController.Instance.ToggleUIEnableOnStartOrStop(false);
            PanelController.Instance.SetExpanded(false);
            for (int i = 0; i < maps.Count; i++)
                if (maps[i] != null) maps[i].StartSimulation(() => stepInterval);
        }

        /// <summary>
        /// 停止所有网格图的寻路可视化。
        /// </summary>
        public void StopAllMaps()
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    maps[i].StopSimulation();
                }
            }
        }

        /// <summary>
        /// 重置所有网格图的寻路可视化状态。
        /// </summary>
        public void ResetAllMaps()
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    maps[i].StopSimulation();
                }
            }
        }

        /// <summary>
        /// 重新排列所有网格图，使第一张位于世界中心，其余依次向下排列。
        /// </summary>
        public void LayoutMaps()
        {
            for (int i = 0; i < maps.Count; i++)
            {
                GridMapController map = maps[i];
                if (map == null)
                {
                    continue;
                }

                map.SetMapIndex(i + 1);

                float height = map.OuterHeight;
                if (i == 0)
                {
                    map.transform.localPosition = Vector3.zero;
                }
                else
                {
                    GridMapController previousMap = maps[i - 1];
                    float previousHalfHeight = previousMap.OuterHeight * 0.5f;
                    float currentHalfHeight = height * 0.5f;
                    float previousY = previousMap.transform.localPosition.y;
                    float currentY = previousY - previousHalfHeight - mapSpacing - currentHalfHeight;
                    map.transform.localPosition = new Vector3(0f, currentY, 0f);
                }
            }
        }

        /// <summary>
        /// 删除指定网格图实例。
        /// </summary>
        /// <param name="map">目标网格图。</param>
        /// <returns>若成功删除则返回真。</returns>
        private bool RemoveMap(GridMapController map)
        {
            if (map == null)
            {
                return false;
            }

            int removedIndex = maps.IndexOf(map);
            if (removedIndex < 0)
            {
                return false;
            }

            map.StopSimulation();
            maps.RemoveAt(removedIndex);

            if (Application.isPlaying)
            {
                Destroy(map.gameObject);
            }
            else
            {
                DestroyImmediate(map.gameObject);
            }

            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    maps[i].SetMapIndex(i + 1);
                }
            }

            GridMapController nextSelection = removedIndex < maps.Count
                ? maps[removedIndex]
                : maps.Count > 0 ? maps[maps.Count - 1] : null;
            SelectMap(nextSelection);
            return true;
        }

        /// <summary>
        /// 保证场景中至少存在一个网格图实例。
        /// </summary>
        private void EnsureAtLeastOneMap()
        {
            if (maps.Count == 0)
            {
                AddMap();
            }
        }

        /// <summary>
        /// 收集场景中已经预放置的网格图实例，并将其纳入运行时管理。
        /// </summary>
        private void CollectExistingMaps()
        {
            maps.Clear();

            if (mapsRoot == null)
            {
                return;
            }

            GridMapController[] existingMaps = mapsRoot.GetComponentsInChildren<GridMapController>(true);
            for (int i = 0; i < existingMaps.Length; i++)
            {
                GridMapController map = existingMaps[i];
                if (map == null)
                {
                    continue;
                }

                map.Configure(sharedGridData, cellPrefab, i + 1);
                map.SetBorderThickness(borderThickness);
                maps.Add(map);
            }

            LayoutMaps();
        }

        /// <summary>
        /// 将当前边框粗细同步到所有网格图。
        /// </summary>
        private void ApplyBorderThicknessToAllMaps()
        {
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                {
                    maps[i].SetBorderThickness(borderThickness);
                }
            }
        }

        /// <summary>
        /// 仅为新建网格图计算默认追加位置，而不重排现有网格图。
        /// </summary>
        /// <param name="map">新建网格图。</param>
        private void PositionNewMap(GridMapController map)
        {
            if (map == null)
            {
                return;
            }

            if (maps.Count <= 1)
            {
                map.transform.localPosition = Vector3.zero;
                return;
            }

            GridMapController previousMap = maps[maps.Count - 2];
            if (previousMap == null)
            {
                map.transform.localPosition = Vector3.zero;
                return;
            }

            float previousHalfHeight = previousMap.OuterHeight * 0.5f;
            float currentHalfHeight = map.OuterHeight * 0.5f;
            float currentY = previousMap.transform.localPosition.y - previousHalfHeight - mapSpacing - currentHalfHeight;
            map.transform.localPosition = new Vector3(previousMap.transform.localPosition.x, currentY, 0f);
        }

        /// <summary>
        /// 将主相机平滑聚焦到指定网格图的中心位置。
        /// </summary>
        /// <param name="map">目标网格图。</param>
        private void FocusCameraOnMap(GridMapController map)
        {
            if (map == null || Camera.main == null)
            {
                return;
            }

            CameraManager cameraManager = Camera.main.GetComponent<CameraManager>();
            if (cameraManager != null)
            {
                cameraManager.FocusOn(map.transform.position);
            }
        }

        /// <summary>
        /// 解析并补全管理器依赖引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (sharedGridData == null)
            {
                sharedGridData = GetComponentInChildren<SharedGridData>();
            }

            if (gridMapFactory == null)
            {
                gridMapFactory = GetComponentInChildren<GridMapFactory>();
            }

            if (mapsRoot == null)
            {
                Transform found = transform.Find("Maps Root");
                if (found != null)
                {
                    mapsRoot = found;
                }
            }
        }

        /// <summary>
        /// 根据当前编辑模式与方格现有类型，计算点击后应切换到的目标类型。
        /// </summary>
        /// <param name="currentType">当前方格类型。</param>
        /// <returns>点击后应写入的方格类型。</returns>
        private GridCellType ResolveTargetCellType(GridCellType currentType)
        {
            return editMode switch
            {
                GridEditMode.PaintWall => GridCellType.Wall,
                GridEditMode.Erase => GridCellType.Empty,
                GridEditMode.SetStart => currentType == GridCellType.Start ? GridCellType.Empty : GridCellType.Start,
                GridEditMode.SetGoal => currentType == GridCellType.Goal ? GridCellType.Empty : GridCellType.Goal,
                _ => currentType
            };
        }
    }
}
