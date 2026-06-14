using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualAlgo.Managers.UI
{
    /// <summary>
    /// 挂在面板对象上后，当鼠标左键点击到面板外部时自动关闭该面板。
    /// </summary>
    public sealed class CloseOnOutsideClickPanel : MonoBehaviour
    {
        /// <summary>
        /// 触发的目标对象。点击落在该对象内时不关闭面板。为空时默认使用当前对象。
        /// </summary>
        [SerializeField] private RectTransform triggerTarget;

        /// <summary>
        /// 真正需要被关闭的目标对象。为空时默认关闭当前对象。
        /// </summary>
        [SerializeField] private GameObject closeTarget;

        /// <summary>
        /// 每帧检查鼠标点击是否落在面板外部。
        /// </summary>
        private void Update()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (!isActiveAndEnabled) return;
            if (EventSystem.current == null) return;

            ResolveReferences();
            if (triggerTarget == null) return;
            if (closeTarget != null && !closeTarget.activeInHierarchy) return;

            bool containsPoint = RectTransformUtility.RectangleContainsScreenPoint(triggerTarget, Input.mousePosition, null);
            if (!containsPoint) GetCloseTarget().SetActive(false);
        }

        /// <summary>
        /// 补全可选引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (triggerTarget == null) triggerTarget = transform as RectTransform;
            if (closeTarget == null) closeTarget = gameObject;
        }

        /// <summary>
        /// 获取最终需要被关闭的目标对象。
        /// </summary>
        /// <returns>关闭目标对象。</returns>
        private GameObject GetCloseTarget()
        {
            return closeTarget == null ? gameObject : closeTarget;
        }
    }
}
