using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VisualAlgo.BinaryTree
{
    /// <summary>
    /// 支持添加与删除最后一个元素的插入列表控件。
    /// </summary>
    public sealed class BinaryTreeInsertListInputV2 : MonoBehaviour
    {
        /// <summary>
        /// 输入框父节点。
        /// </summary>
        [SerializeField] private RectTransform listRoot;

        /// <summary>
        /// 添加元素按钮。
        /// </summary>
        [SerializeField] private Button addButton;

        /// <summary>
        /// 删除最后一个元素按钮。
        /// </summary>
        [SerializeField] private Button removeButton;

        /// <summary>
        /// 输入框模板。
        /// </summary>
        [SerializeField] private TMP_InputField inputTemplate;

        /// <summary>
        /// 当前是否已完成监听绑定。
        /// </summary>
        private bool listenersBound;

        /// <summary>
        /// 在脚本启用时补全引用并生成默认输入框。
        /// </summary>
        private void Awake()
        {
            ResolveReferences();
            BindListeners();
            EnsureAtLeastOneInput();
        }

        /// <summary>
        /// 读取当前全部输入值。
        /// </summary>
        /// <returns>按输入框顺序返回的值列表。</returns>
        public List<int> GetValues()
        {
            List<int> values = new();
            ResolveReferences();
            if (listRoot == null) return values;

            TMP_InputField[] inputs = listRoot.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < inputs.Length; i++)
            {
                TMP_InputField input = inputs[i];
                if (input == null || !input.gameObject.activeSelf || string.IsNullOrWhiteSpace(input.text)) continue;
                if (int.TryParse(input.text.Trim(), out int value)) values.Add(value);
            }

            return values;
        }

        /// <summary>
        /// 设置控件整体是否可交互。
        /// </summary>
        /// <param name="interactable">是否可交互。</param>
        public void SetInteractable(bool interactable)
        {
            ResolveReferences();
            if (addButton != null) addButton.interactable = interactable;
            if (removeButton != null) removeButton.interactable = interactable;
            if (listRoot == null) return;

            TMP_InputField[] inputs = listRoot.GetComponentsInChildren<TMP_InputField>(true);
            for (int i = 0; i < inputs.Length; i++)
            {
                if (inputs[i] != null) inputs[i].interactable = interactable;
            }
        }

        /// <summary>
        /// 添加一个新的输入框。
        /// </summary>
        public void AddInputField()
        {
            ResolveReferences();
            if (listRoot == null || inputTemplate == null) return;

            TMP_InputField instance = Instantiate(inputTemplate, listRoot);
            instance.name = $"插入值 Input {listRoot.childCount}";
            instance.gameObject.SetActive(true);
            instance.text = string.Empty;
            RebuildLayouts();
        }

        /// <summary>
        /// 删除最后一个输入框。
        /// </summary>
        public void RemoveLastInputField()
        {
            ResolveReferences();
            if (listRoot == null) return;

            TMP_InputField[] inputs = listRoot.GetComponentsInChildren<TMP_InputField>(true);
            if (inputs.Length <= 1) return;
            TMP_InputField lastInput = inputs[inputs.Length - 1];
            if (lastInput == null) return;

            if (Application.isPlaying) Destroy(lastInput.gameObject);
            else DestroyImmediate(lastInput.gameObject);
            RebuildLayouts();
        }

        /// <summary>
        /// 绑定按钮事件。
        /// </summary>
        private void BindListeners()
        {
            if (listenersBound) return;
            if (addButton != null) addButton.onClick.AddListener(AddInputField);
            if (removeButton != null) removeButton.onClick.AddListener(RemoveLastInputField);
            listenersBound = true;
        }

        /// <summary>
        /// 保证至少存在一个输入框。
        /// </summary>
        private void EnsureAtLeastOneInput()
        {
            ResolveReferences();
            if (listRoot == null || inputTemplate == null) return;
            TMP_InputField[] inputs = listRoot.GetComponentsInChildren<TMP_InputField>(true);
            if (inputs.Length == 0) AddInputField();
        }

        /// <summary>
        /// 补全组件引用。
        /// </summary>
        private void ResolveReferences()
        {
            if (listRoot == null) listRoot = transform.Find("List Root") as RectTransform;
            if (addButton == null) addButton = transform.Find("Add Button")?.GetComponent<Button>();
            if (removeButton == null) removeButton = transform.Find("Remove Button")?.GetComponent<Button>();
            if (inputTemplate == null) inputTemplate = transform.Find("List Root/Template Input")?.GetComponent<TMP_InputField>();
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
