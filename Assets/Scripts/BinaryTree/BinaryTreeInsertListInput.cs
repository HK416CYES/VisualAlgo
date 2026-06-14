using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 表示当前插入栈顶部元素的读取结果。
    /// </summary>
    public enum BinaryTreeInsertStackReadState
    {
        /// <summary>
        /// 栈为空。
        /// </summary>
        Empty,

        /// <summary>
        /// 顶部元素是合法整数。
        /// </summary>
        Valid,

        /// <summary>
        /// 顶部元素非法或为空。
        /// </summary>
        Invalid
    }

    /// <summary>
    /// 管理一组可动态增加的插入值输入框，并以顶部到尾部的顺序作为插入栈使用。
    /// </summary>
    public sealed class BinaryTreeInsertListInput : MonoBehaviour
    {
        /// <summary>
        /// 输入框容器。
        /// </summary>
        [SerializeField] private RectTransform listRoot;

        /// <summary>
        /// 添加输入框按钮。
        /// </summary>
        [SerializeField] private Button addButton;

        /// <summary>
        /// 删除最后一个输入框按钮。
        /// </summary>
        [SerializeField] private Button removeButton;

        /// <summary>
        /// 输入框模板。
        /// </summary>
        [SerializeField] private TMP_InputField inputTemplate;

        /// <summary>
        /// 当前是否已完成事件绑定。
        /// </summary>
        private bool listenersBound;

        /// <summary>
        /// 当前由该控件管理的真实输入框实例，顺序与界面从上到下保持一致。
        /// </summary>
        private readonly List<TMP_InputField> stackInputs = new();

        /// <summary>
        /// 在脚本启用时补全引用并重建输入框缓存。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            RebuildManagedInputs();
            BindListeners();
        }

        /// <summary>
        /// 获取当前列表中的全部整数值。
        /// </summary>
        /// <returns>按输入框顺序返回的整数值列表。</returns>
        public List<int> GetValues()
        {
            List<int> values = new();
            CleanupMissingInputs();
            for (int i = 0; i < stackInputs.Count; i++)
            {
                TMP_InputField input = stackInputs[i];
                if (input == null || string.IsNullOrWhiteSpace(input.text)) continue;
                if (int.TryParse(input.text.Trim(), out int value)) values.Add(value);
            }

            return values;
        }

        /// <summary>
        /// 判断当前插入栈是否仍包含输入框元素。
        /// </summary>
        /// <returns>若存在至少一个输入框则返回真。</returns>
        public bool HasAnyInput()
        {
            CleanupMissingInputs();
            return stackInputs.Count > 0;
        }

        /// <summary>
        /// 读取当前顶部元素状态，但不立即删除该输入框。
        /// </summary>
        /// <param name="value">当顶部元素合法时返回其整数值。</param>
        /// <returns>顶部元素的读取状态。</returns>
        public BinaryTreeInsertStackReadState PeekTopValue(out int value)
        {
            value = 0;
            TMP_InputField topInput = GetTopInputField();
            if (topInput == null) return BinaryTreeInsertStackReadState.Empty;

            string text = topInput.text == null ? string.Empty : topInput.text.Trim();
            return int.TryParse(text, out value) ? BinaryTreeInsertStackReadState.Valid : BinaryTreeInsertStackReadState.Invalid;
        }

        /// <summary>
        /// 移除当前顶部输入框。
        /// </summary>
        public void RemoveTopInputField()
        {
            RemoveInputField(GetTopInputField());
        }

        /// <summary>
        /// 设置整个插入列表控件是否可交互。
        /// </summary>
        /// <param name="interactable">是否可交互。</param>
        public void SetInteractable(bool interactable)
        {
            ResolveReferences();
            if (addButton != null) addButton.interactable = interactable;
            if (removeButton != null) removeButton.interactable = interactable;

            CleanupMissingInputs();
            for (int i = 0; i < stackInputs.Count; i++)
            {
                if (stackInputs[i] != null) stackInputs[i].interactable = interactable;
            }
        }

        /// <summary>
        /// 新增一个输入框。
        /// </summary>
        public void AddInputField()
        {
            ResolveReferences();
            if (listRoot == null || inputTemplate == null) return;

            TMP_InputField instance = Instantiate(inputTemplate, listRoot);
            instance.gameObject.SetActive(true);
            instance.transform.SetAsLastSibling();
            instance.name = $"插入值 Input {stackInputs.Count + 1}";
            instance.text = string.Empty;
            stackInputs.Add(instance);
            RebuildLayouts();
        }

        /// <summary>
        /// 删除最后一个输入框。
        /// </summary>
        public void RemoveLastInputField()
        {
            RemoveInputField(GetLastInputField());
        }

        /// <summary>
        /// 绑定按钮监听。
        /// </summary>
        private void BindListeners()
        {
            if (listenersBound) return;
            if (addButton != null) addButton.onClick.AddListener(AddInputField);
            if (removeButton != null) removeButton.onClick.AddListener(RemoveLastInputField);
            listenersBound = true;
        }

        /// <summary>
        /// 从场景层级重建受控输入框列表。
        /// </summary>
        private void RebuildManagedInputs()
        {
            stackInputs.Clear();
            if (listRoot == null) return;

            for (int i = 0; i < listRoot.childCount; i++)
            {
                Transform child = listRoot.GetChild(i);
                if (child == null) continue;

                TMP_InputField input = child.GetComponent<TMP_InputField>();
                if (input == null || input == inputTemplate) continue;
                stackInputs.Add(input);
            }
        }

        /// <summary>
        /// 清理已经被销毁或脱离容器的输入框引用。
        /// </summary>
        private void CleanupMissingInputs()
        {
            for (int i = stackInputs.Count - 1; i >= 0; i--)
            {
                TMP_InputField input = stackInputs[i];
                if (input == null || input.transform.parent != listRoot) stackInputs.RemoveAt(i);
            }
        }

        /// <summary>
        /// 获取当前位于顶部的输入框。
        /// </summary>
        /// <returns>顶部输入框；若栈为空则返回空。</returns>
        private TMP_InputField GetTopInputField()
        {
            CleanupMissingInputs();
            return stackInputs.Count > 0 ? stackInputs[0] : null;
        }

        /// <summary>
        /// 获取当前位于底部的输入框。
        /// </summary>
        /// <returns>底部输入框；若栈为空则返回空。</returns>
        private TMP_InputField GetLastInputField()
        {
            CleanupMissingInputs();
            return stackInputs.Count > 0 ? stackInputs[stackInputs.Count - 1] : null;
        }

        /// <summary>
        /// 补全依赖引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (listRoot == null) listRoot = transform.Find("List Root") as RectTransform;
            if (addButton == null) addButton = transform.Find("Add Button")?.GetComponent<Button>();
            if (removeButton == null) removeButton = transform.Find("Remove Button")?.GetComponent<Button>();
            if (inputTemplate == null) inputTemplate = transform.Find("List Root/Template Input")?.GetComponent<TMP_InputField>();
            if (inputTemplate != null) inputTemplate.gameObject.SetActive(false);
        }

        /// <summary>
        /// 删除指定输入框并刷新布局。
        /// </summary>
        /// <param name="input">待删除输入框。</param>
        private void RemoveInputField(TMP_InputField input)
        {
            if (input == null) return;

            stackInputs.Remove(input);
            input.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(input.gameObject);
            else DestroyImmediate(input.gameObject);
            RebuildLayouts();
        }

        /// <summary>
        /// 强制刷新当前控件及其父级布局。
        /// </summary>
        private void RebuildLayouts()
        {
            if (listRoot != null) LayoutRebuilder.ForceRebuildLayoutImmediate(listRoot);
            RectTransform current = transform as RectTransform;
            while (current != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(current);
                current = current.parent as RectTransform;
            }
        }
    }
}
