using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 负责排序组的鼠标点击、排序块选中和空白区域拖拽交互。
    /// </summary>
    public sealed class SortGroupPointerInteractor : MonoBehaviour
    {
        /// <summary>
        /// 被此交互器控制的多排序组管理器。
        /// </summary>
        [Header("排序组管理器")][SerializeField] private SortComparisonManager comparisonManager;

        /// <summary>
        /// 鼠标射线检测可命中的物理层。
        /// </summary>
        [Header("射线检测层")][SerializeField] private LayerMask raycastMask = Physics.DefaultRaycastLayers;

        /// <summary>
        /// 鼠标射线检测的最大距离。
        /// </summary>
        [Header("最大检测距离")][SerializeField, Min(1f)] private float maxRayDistance = 1000f;

        /// <summary>
        /// 当前正在被左键拖拽的排序组。
        /// </summary>
        private SortGroupController draggedGroup;

        /// <summary>
        /// 拖拽开始时，鼠标命中点到排序组原点之间的偏移。
        /// </summary>
        private Vector3 dragOffset;

        /// <summary>
        /// 当前拖拽使用的世界空间平面。
        /// </summary>
        private Plane dragPlane;

        /// <summary>
        /// 当前是否正在拖拽排序组。
        /// </summary>
        private bool isDraggingGroup;

        /// <summary>
        /// 初始化交互器所需引用。
        /// </summary>
        private void Awake()
        {
            if (comparisonManager == null)
                comparisonManager = GetComponent<SortComparisonManager>();

            if (comparisonManager == null)
                comparisonManager = FindFirstObjectByType<SortComparisonManager>();
        }

        /// <summary>
        /// 每帧读取新版 Input System 的鼠标状态并驱动选择和拖拽。
        /// </summary>
        private void Update()
        {
            Mouse mouse = Mouse.current;
            Camera mainCamera = Camera.main;
            if (mouse == null || mainCamera == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
                HandlePointerPressed(mouse, mainCamera);

            if (mouse.leftButton.isPressed && isDraggingGroup)
                UpdateGroupDrag(mouse, mainCamera);

            if (mouse.leftButton.wasReleasedThisFrame)
                EndGroupDrag();
        }

        /// <summary>
        /// 处理鼠标左键按下：点击块时选块，点击组内空白时选组并开始拖拽。
        /// </summary>
        /// <param name="mouse">当前鼠标设备。</param>
        /// <param name="mainCamera">用于屏幕射线换算的主相机。</param>
        private void HandlePointerPressed(Mouse mouse, Camera mainCamera)
        {
            EndGroupDrag();
            if (comparisonManager == null) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (!TryGetSortHit(ray, out SortGroupController group, out SortBarView bar, out Vector3 hitPoint))
            {
                comparisonManager.DeselectGroup();
                return;
            }

            if (bar != null)
            {
                if (comparisonManager.HasAnyActiveSimulation)
                {
                    comparisonManager.SelectGroup(group);
                    BeginGroupDrag(group, ray, hitPoint);
                    return;
                }

                group.TrySelectBar(bar);
                comparisonManager.SelectGroup(group);
                return;
            }

            comparisonManager.SelectGroup(group, true);
            BeginGroupDrag(group, ray, hitPoint);
        }

        /// <summary>
        /// 开始拖拽被点击空白区域所属的排序组。
        /// </summary>
        /// <param name="group">要拖拽的排序组。</param>
        /// <param name="ray">鼠标当前射线。</param>
        /// <param name="fallbackHitPoint">射线命中碰撞体时的备用命中点。</param>
        private void BeginGroupDrag(SortGroupController group, Ray ray, Vector3 fallbackHitPoint)
        {
            if (group == null) return;

            draggedGroup = group;
            dragPlane = new Plane(Vector3.forward, draggedGroup.transform.position);
            Vector3 planePoint = TryGetPlanePoint(ray, out Vector3 point) ? point : fallbackHitPoint;
            dragOffset = draggedGroup.transform.position - planePoint;
            isDraggingGroup = true;
        }

        /// <summary>
        /// 根据当前鼠标位置更新被拖拽排序组的世界坐标。
        /// </summary>
        /// <param name="mouse">当前鼠标设备。</param>
        /// <param name="mainCamera">用于屏幕射线换算的主相机。</param>
        private void UpdateGroupDrag(Mouse mouse, Camera mainCamera)
        {
            if (draggedGroup == null)
            {
                EndGroupDrag();
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());
            if (!TryGetPlanePoint(ray, out Vector3 planePoint)) return;

            Vector3 targetPosition = planePoint + dragOffset;
            targetPosition.z = draggedGroup.transform.position.z;
            draggedGroup.transform.position = targetPosition;
        }

        /// <summary>
        /// 结束当前排序组拖拽。
        /// </summary>
        private void EndGroupDrag()
        {
            draggedGroup = null;
            isDraggingGroup = false;
        }

        /// <summary>
        /// 从射线检测结果中优先返回排序块命中，否则返回排序组边框区域命中。
        /// </summary>
        /// <param name="ray">鼠标屏幕位置产生的世界射线。</param>
        /// <param name="group">命中的排序组。</param>
        /// <param name="bar">命中的排序块，空白区域命中时为 null。</param>
        /// <param name="hitPoint">命中的世界坐标。</param>
        /// <returns>是否命中了排序组相关碰撞体。</returns>
        private bool TryGetSortHit(Ray ray, out SortGroupController group, out SortBarView bar, out Vector3 hitPoint)
        {
            group = null;
            bar = null;
            hitPoint = default;

            RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, raycastMask, QueryTriggerInteraction.Collide);
            if (hits.Length == 0) return false;

            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            SortGroupController firstGroup = null;
            Vector3 firstGroupPoint = default;
            for (int i = 0; i < hits.Length; i++)
            {
                SortGroupController hitGroup = hits[i].collider.GetComponentInParent<SortGroupController>();
                if (hitGroup == null) continue;

                SortBarView hitBar = hits[i].collider.GetComponentInParent<SortBarView>();
                if (hitBar != null)
                {
                    group = hitGroup;
                    bar = hitBar;
                    hitPoint = hits[i].point;
                    return true;
                }

                if (firstGroup != null) continue;
                firstGroup = hitGroup;
                firstGroupPoint = hits[i].point;
            }

            if (firstGroup == null) return false;

            group = firstGroup;
            hitPoint = firstGroupPoint;
            return true;
        }

        /// <summary>
        /// 计算鼠标射线与当前拖拽平面的交点。
        /// </summary>
        /// <param name="ray">鼠标屏幕位置产生的世界射线。</param>
        /// <param name="point">射线和平面的交点。</param>
        /// <returns>是否成功计算交点。</returns>
        private bool TryGetPlanePoint(Ray ray, out Vector3 point)
        {
            point = default;
            if (!dragPlane.Raycast(ray, out float enter)) return false;

            point = ray.GetPoint(enter);
            return true;
        }
    }
}
