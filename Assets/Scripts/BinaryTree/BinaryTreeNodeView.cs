using TMPro;
using UnityEngine;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 单个二叉树节点的可视化视图。
    /// </summary>
    public sealed class BinaryTreeNodeView : MonoBehaviour
    {
        /// <summary>
        /// 节点主体精灵。
        /// </summary>
        [SerializeField] private SpriteRenderer bodyRenderer;

        /// <summary>
        /// 节点上的数值文本。
        /// </summary>
        [SerializeField] private TextMeshPro valueText;

        /// <summary>
        /// 节点选中时的高亮环。
        /// </summary>
        [SerializeField] private SpriteRenderer selectionRenderer;

        /// <summary>
        /// 在脚本启用时尝试补全视图引用。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
        }

        /// <summary>
        /// 设置节点显示值。
        /// </summary>
        /// <param name="value">新的显示值。</param>
        public void SetValue(int value)
        {
            ResolveReferences();
            if (valueText != null) valueText.text = value.ToString();
        }

        /// <summary>
        /// 设置节点显示文本。
        /// </summary>
        /// <param name="label">新的显示文本。</param>
        public void SetLabel(string label)
        {
            ResolveReferences();
            if (valueText != null) valueText.text = label;
        }

        /// <summary>
        /// 设置节点主体颜色。
        /// </summary>
        /// <param name="color">新的颜色。</param>
        public void SetColor(Color color)
        {
            ResolveReferences();
            if (bodyRenderer != null) bodyRenderer.color = color;
        }

        /// <summary>
        /// 设置节点文字颜色。
        /// </summary>
        /// <param name="color">新的文字颜色。</param>
        public void SetTextColor(Color color)
        {
            ResolveReferences();
            if (valueText != null) valueText.color = color;
        }

        /// <summary>
        /// 获取节点主体当前颜色。
        /// </summary>
        /// <returns>当前主体颜色。</returns>
        public Color GetColor()
        {
            ResolveReferences();
            return bodyRenderer != null ? bodyRenderer.color : Color.white;
        }

        /// <summary>
        /// 获取节点文字当前颜色。
        /// </summary>
        /// <returns>当前文字颜色。</returns>
        public Color GetTextColor()
        {
            ResolveReferences();
            return valueText != null ? valueText.color : Color.black;
        }

        /// <summary>
        /// 设置节点是否显示选中态。
        /// </summary>
        /// <param name="selected">是否选中。</param>
        public void SetSelected(bool selected)
        {
            ResolveReferences();
            if (selectionRenderer != null) selectionRenderer.enabled = selected;
        }

        /// <summary>
        /// 统一设置节点主体、文字与选中遮罩的透明度。
        /// </summary>
        /// <param name="alpha">目标透明度。</param>
        /// <param name="selectionAlpha">选中状态的透明度。</param>
        public void SetVisualAlpha(float alpha, float selectionAlpha)
        {
            ResolveReferences();
            if (bodyRenderer != null)
            {
                Color bodyColor = bodyRenderer.color;
                bodyColor.a = alpha;
                bodyRenderer.color = bodyColor;
            }

            if (valueText != null)
            {
                Color textColor = valueText.color;
                textColor.a = alpha;
                valueText.color = textColor;
            }

            if (selectionRenderer != null)
            {
                Color selectionColor = selectionRenderer.color;
                selectionColor.a = selectionAlpha;
                selectionRenderer.color = selectionColor;
            }
        }

        /// <summary>
        /// 设置节点的渲染层级。
        /// </summary>
        /// <param name="sortingOrder">新的排序值。</param>
        public void SetSortingOrder(int sortingOrder)
        {
            ResolveReferences();
            if (bodyRenderer != null) bodyRenderer.sortingOrder = sortingOrder;
            if (selectionRenderer != null) selectionRenderer.sortingOrder = sortingOrder - 1;
            if (valueText != null) valueText.sortingOrder = sortingOrder + 1;
        }

        /// <summary>
        /// 补全节点视图依赖引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (bodyRenderer == null) bodyRenderer = transform.Find("Body")?.GetComponent<SpriteRenderer>();
            if (valueText == null) valueText = GetComponentInChildren<TextMeshPro>(true);
            if (selectionRenderer == null) selectionRenderer = transform.Find("Selection")?.GetComponent<SpriteRenderer>();
        }
    }
}
