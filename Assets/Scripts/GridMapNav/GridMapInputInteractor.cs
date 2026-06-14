using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace VisualAlgo.GridMapNav
{
    /// <summary>
    /// 负责处理玩家对网格图的左键选择与编辑输入。
    /// </summary>
    public sealed class GridMapInputInteractor : MonoBehaviour
    {
        /// <summary>
        /// 当前这一轮按住左键时，上一次已经应用过编辑的方格坐标。
        /// </summary>
        private GridCoordinate? lastEditedCoordinate;

        /// <summary>
        /// 当前这一轮按住左键时，上一次已经应用过编辑的网格图。
        /// </summary>
        private GridMapController lastEditedMap;

        /// <summary>
        /// 拖动单张网格图时，鼠标与网格图中心点的偏移量。
        /// </summary>
        private Vector3 dragOffset;

        /// <summary>
        /// 当前正在被拖动的网格图。
        /// </summary>
        private GridMapController draggingMap;

        /// <summary>
        /// 网格图总管理器。
        /// </summary>
        [SerializeField] private GridMapComparisonManager comparisonManager;

        /// <summary>
        /// 是否忽略位于 UI 上方的鼠标点击。
        /// </summary>
        [SerializeField] private bool ignorePointerOverUi = true;

        /// <summary>
        /// Unity 主相机缓存。
        /// </summary>
        private Camera mainCamera;

        /// <summary>
        /// 在脚本启动时缓存相机引用。
        /// </summary>
        private void Awake()
        {
            if (comparisonManager == null)
            {
                comparisonManager = FindAnyObjectByType<GridMapComparisonManager>();
            }

            mainCamera = Camera.main;
        }

        /// <summary>
        /// 在每帧检查鼠标左键点击并分发选择或编辑行为。
        /// </summary>
        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || mainCamera == null || comparisonManager == null)
            {
                return;
            }

            if (ignorePointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    draggingMap = null;
                }

                return;
            }

            Vector3 screenPoint = mouse.position.ReadValue();
            Vector3 worldPoint = mainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, -mainCamera.transform.position.z));

            if (mouse.leftButton.wasPressedThisFrame)
            {
                lastEditedCoordinate = null;
                lastEditedMap = null;
                HandlePointerPressed(worldPoint);
            }

            if (mouse.leftButton.isPressed)
            {
                if (comparisonManager.IsEditModeEnabled)
                {
                    HandlePointerHeldForEdit(worldPoint);
                }
                else
                {
                    HandlePointerDragged(worldPoint);
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                draggingMap = null;
                lastEditedCoordinate = null;
                lastEditedMap = null;
            }
        }

        /// <summary>
        /// 处理鼠标左键按下时的选择、编辑与拖动起始逻辑。
        /// </summary>
        /// <param name="worldPoint">当前鼠标所在世界坐标。</param>
        private void HandlePointerPressed(Vector3 worldPoint)
        {
            GridMapController hitMap = FindTopmostMap(worldPoint);
            if (hitMap == null)
            {
                comparisonManager.DeselectMap();
                draggingMap = null;
                return;
            }

            comparisonManager.SelectMap(hitMap);

            if (comparisonManager.IsEditModeEnabled)
            {
                if (hitMap.TryGetCellCoordinate(worldPoint, out GridCoordinate coordinate))
                {
                    comparisonManager.ApplyEdit(hitMap, coordinate);
                    lastEditedMap = hitMap;
                    lastEditedCoordinate = coordinate;
                }

                draggingMap = null;
                return;
            }

            draggingMap = hitMap;
            dragOffset = hitMap.transform.position - new Vector3(worldPoint.x, worldPoint.y, hitMap.transform.position.z);
        }

        /// <summary>
        /// 处理鼠标左键拖动单张网格图的逻辑。
        /// </summary>
        /// <param name="worldPoint">当前鼠标所在世界坐标。</param>
        private void HandlePointerDragged(Vector3 worldPoint)
        {
            if (draggingMap == null || comparisonManager.IsEditModeEnabled)
            {
                return;
            }

            Vector3 targetPosition = new(worldPoint.x + dragOffset.x, worldPoint.y + dragOffset.y, draggingMap.transform.position.z);
            draggingMap.transform.position = targetPosition;
        }

        /// <summary>
        /// 在编辑模式下，处理按住左键连续涂格的逻辑。
        /// </summary>
        /// <param name="worldPoint">当前鼠标所在世界坐标。</param>
        private void HandlePointerHeldForEdit(Vector3 worldPoint)
        {
            GridMapController hitMap = FindTopmostMap(worldPoint);
            if (hitMap == null)
            {
                return;
            }

            if (!hitMap.TryGetCellCoordinate(worldPoint, out GridCoordinate coordinate))
            {
                return;
            }

            if (lastEditedMap == hitMap && lastEditedCoordinate.HasValue && lastEditedCoordinate.Value == coordinate)
            {
                return;
            }

            comparisonManager.ApplyEdit(hitMap, coordinate);
            lastEditedMap = hitMap;
            lastEditedCoordinate = coordinate;
        }

        /// <summary>
        /// 查找当前世界坐标命中的网格图实例。
        /// </summary>
        /// <param name="worldPoint">世界坐标。</param>
        /// <returns>命中的网格图；若未命中则返回空。</returns>
        private GridMapController FindTopmostMap(Vector3 worldPoint)
        {
            IReadOnlyList<GridMapController> maps = comparisonManager.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                GridMapController map = maps[i];
                if (map != null && map.ContainsWorldPoint(worldPoint))
                {
                    return map;
                }
            }

            return null;
        }
    }
}
