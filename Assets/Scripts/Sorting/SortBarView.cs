using System.Globalization;
using TMPro;
using UnityEngine;

namespace VisualAlgo.Sorting
{
    /// <summary>
    /// 表示排序块在可视化过程中的材质状态。
    /// </summary>
    public enum SortBarVisualState
    {
        Normal,
        Active,
        Pivot,
        Sorted
    }

    /// <summary>
    /// 控制单个排序块的网格（Mesh）、碰撞体（Collider）、材质（Material）以及高度数值标签。
    /// 包含高度、当前索引的值，并处理更新自身视觉效果相关的功能。
    /// </summary>
    public sealed class SortBarView : MonoBehaviour
    {
        [Header("当前高度文本")][SerializeField] private TextMeshPro heightLabel;
        [Header("块物体")][SerializeField] private GameObject bar;

        private MeshRenderer meshRenderer;
        private Collider barCollider;
        private Transform barTransform;
        private Material normalMaterial;
        private Material activeMaterial;
        private Material pivotMaterial;
        private Material sortedMaterial;
        private float width = 0.6f;

        /// <summary>
        /// 当前排序块代表的数值大小。
        /// </summary>
        public float Value { get; private set; }

        /// <summary>
        /// 当前排序块在数组中对应的索引。
        /// </summary>
        public int Index { get; private set; }

        private void Awake()
        {
            CacheComponents();
            EnsureHeightLabel();
        }

        /// <summary>
        /// 缓存柱体子物体上的渲染和碰撞组件。脚本所在根物体只负责整体布局，不再被拉伸。
        /// </summary>
        private void CacheComponents()
        {
            EnsureBarObject();

            if (barTransform == null && bar != null) barTransform = bar.transform;
            if (meshRenderer == null && bar != null) meshRenderer = bar.GetComponent<MeshRenderer>();

            if (barCollider == null)
            {
                if (bar != null) barCollider = bar.GetComponent<Collider>();
                if (barCollider == null && bar != null) barCollider = bar.AddComponent<BoxCollider>();
            }
        }

        /// <summary>
        /// 确保存在独立的柱体子物体。这样调整柱体高度时不会影响同级的高度文本。
        /// </summary>
        private void EnsureBarObject()
        {
            if (bar != null)
            {
                barTransform = bar.transform;
                return;
            }

            Transform namedBar = transform.Find("Bar");
            if (namedBar == null) namedBar = transform.Find("Block");
            if (namedBar == null)
            {
                MeshRenderer[] childRenderers = GetComponentsInChildren<MeshRenderer>(true);
                foreach (MeshRenderer childRenderer in childRenderers)
                {
                    if (childRenderer.transform == transform) continue;
                    if (childRenderer.GetComponent<TextMeshPro>() != null) continue;

                    namedBar = childRenderer.transform;
                    break;
                }
            }

            if (namedBar == null)
            {
                Debug.LogError($"{nameof(SortBarView)} on {name} requires a Bar child object in the prefab.");
                return;
            }

            bar = namedBar.gameObject;
            barTransform = namedBar;
        }

        /// <summary>
        /// 确保高度标签存在。这里只接受 TextMeshPro（三维文本），不使用 TextMeshProUGUI/Canvas UI。
        /// </summary>
        private void EnsureHeightLabel()
        {
            if (heightLabel == null)
            {
                Transform labelTransform = transform.Find("Height Label");
                if (labelTransform != null)
                    heightLabel = labelTransform.GetComponent<TextMeshPro>();
            }

            if (heightLabel == null) return;

            heightLabel.alignment = TextAlignmentOptions.Center;
            heightLabel.color = new Color(0.08f, 0.09f, 0.11f, 1f);
            heightLabel.textWrappingMode = TextWrappingModes.NoWrap;
            heightLabel.raycastTarget = false;
        }

        /// <summary>
        /// 初始化排序块的基础信息及视觉样式。
        /// </summary>
        /// <param name="index">当前的初始索引位置。 </param>
        /// <param name="barWidth">排序块的宽度尺寸。</param>
        /// <param name="normal">处于普通状态时的材质。</param>
        /// <param name="active">处于运行比对/交换等活跃操作时的材质。</param>
        /// <param name="sorted">排序完成状态时的材质。</param>
        /// <param name="labelFont">数值顶部显示的字体资产。</param>
        public void Initialize(
            int index,
            float barWidth,
            Material normal,
            Material active,
            Material sorted,
            Material pivot = null)
        {
            Index = index;
            width = Mathf.Max(0.05f, barWidth);
            normalMaterial = normal;
            activeMaterial = active;
            sortedMaterial = sorted;
            pivotMaterial = pivot;
            CacheComponents();
            EnsureHeightLabel();
            SetVisualState(SortBarVisualState.Normal);
        }

        /// <summary>
        /// 设置排序块的高度，以反映其保存的数值大小。
        /// </summary>
        /// <param name="height">要设置的目标高度。</param>
        public void SetHeight(float height)
        {
            // 只缩放柱体子物体，根物体和文本不参与缩放，从根源上避免文本被压扁或拉伸。
            Value = Mathf.Max(0.1f, height);
            transform.localScale = Vector3.one;

            CacheComponents();
            if (barTransform != null)
            {
                barTransform.localScale = new Vector3(width, Value, 1f);
                barTransform.localPosition = new Vector3(0f, Value * 0.5f, 0f);
                barTransform.localRotation = Quaternion.identity;
            }

            Vector3 rootPosition = transform.localPosition;
            rootPosition.y = 0f;
            transform.localPosition = rootPosition;

            RefreshHeightLabel();
        }

        /// <summary>
        /// 更新排列块的基于网格的本地 X 轴位置坐标。
        /// </summary>
        /// <param name="xPosition">目标本地 X 轴坐标。</param>
        public void SetXPosition(float xPosition)
        {
            Vector3 position = transform.localPosition;
            position.x = xPosition;
            transform.localPosition = position;
        }

        /// <summary>
        /// 设定排序块当前所处的视觉状态，以切换对应的材质外观。
        /// </summary>
        /// <param name="state">目标更新到哪种可视化状态。</param>
        public void SetVisualState(SortBarVisualState state)
        {
            CacheComponents();

            Material material = state switch
            {
                SortBarVisualState.Active => activeMaterial,
                SortBarVisualState.Pivot => pivotMaterial,
                SortBarVisualState.Sorted => sortedMaterial,
                _ => normalMaterial
            };

            if (material != null && meshRenderer != null) meshRenderer.sharedMaterial = material;
        }

        /// <summary>
        /// 从柱体子物体读取当前高度，供控制器从已有场景对象重建缓存时使用。
        /// </summary>
        public float ReadCurrentHeight()
        {
            CacheComponents();
            if (barTransform == null) return Value;
            return Mathf.Max(0.1f, barTransform.localScale.y);
        }

        /// <summary>
        /// 刷新高度标签内容和位置。标签与柱体同级，因此保持固定缩放即可稳定显示。
        /// </summary>
        private void RefreshHeightLabel()
        {
            if (heightLabel == null) return;

            heightLabel.text = Value.ToString("0.0", CultureInfo.InvariantCulture);
            Transform labelTransform = heightLabel.transform;
            labelTransform.localPosition = new Vector3(0f, -0.32f, -0.56f);
            labelTransform.localRotation = Quaternion.identity;
            labelTransform.localScale = Vector3.one;
        }
    }
}
