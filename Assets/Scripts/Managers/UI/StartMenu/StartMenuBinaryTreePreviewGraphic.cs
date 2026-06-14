using UnityEngine;
using UnityEngine.UI;

namespace VisualAlgo.Managers.UI.StartMenu
{
    /// <summary>
    /// 绘制开始菜单二叉树按钮上方的红黑树调整预览。
    /// </summary>
    public sealed class StartMenuBinaryTreePreviewGraphic : MaskableGraphic
    {
        /// <summary>
        /// 紫色节点颜色。
        /// </summary>
        [SerializeField] private Color purpleNodeColor = new(0.53f, 0.34f, 0.78f, 1f);

        /// <summary>
        /// 黑色节点颜色。
        /// </summary>
        [SerializeField] private Color blackNodeColor = new(0.08f, 0.08f, 0.08f, 1f);

        /// <summary>
        /// 边线颜色。
        /// </summary>
        [SerializeField] private Color edgeColor = new(0.08f, 0.08f, 0.08f, 1f);

        /// <summary>
        /// 节点半径。
        /// </summary>
        [SerializeField, Min(1f)] private float nodeRadius = 11f;

        /// <summary>
        /// 紫色节点外圈轮廓的厚度。
        /// </summary>
        [SerializeField, Min(0.1f)] private float purpleOutlineThickness = 2.2f;

        /// <summary>
        /// 边线粗细。
        /// </summary>
        [SerializeField, Min(1f)] private float edgeThickness = 4f;

        /// <summary>
        /// 绘制区域边距。
        /// </summary>
        [SerializeField, Min(0f)] private float padding = 10f;

        /// <summary>
        /// 边线进入完成态时的提前速度倍率。
        /// </summary>
        [SerializeField, Min(0.1f)] private float edgeEnterSpeed = 1.55f;

        /// <summary>
        /// 边线离开起始态时的提前速度倍率。
        /// </summary>
        [SerializeField, Min(0.1f)] private float edgeExitSpeed = 1.55f;

        /// <summary>
        /// 当前从未调整态过渡到调整态的进度。
        /// </summary>
        [SerializeField, Range(0f, 1f)] private float progress;

        /// <summary>
        /// 获取或设置当前预览进度。
        /// </summary>
        public float Progress
        {
            get => progress;
            set
            {
                float clampedValue = Mathf.Clamp01(value);
                if (Mathf.Approximately(progress, clampedValue)) return;
                progress = clampedValue;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// 节点颜色类型。
        /// </summary>
        private enum NodeTone
        {
            Black,
            Purple
        }

        /// <summary>
        /// 表示一条树边。
        /// </summary>
        private readonly struct TreeEdge
        {
            /// <summary>
            /// 起点节点索引。
            /// </summary>
            public readonly int From;

            /// <summary>
            /// 终点节点索引。
            /// </summary>
            public readonly int To;

            /// <summary>
            /// 构造一条树边。
            /// </summary>
            /// <param name="from">起点节点索引。</param>
            /// <param name="to">终点节点索引。</param>
            public TreeEdge(int from, int to)
            {
                From = from;
                To = to;
            }
        }

        /// <summary>
        /// 未调整状态的节点相对位置。
        /// </summary>
        private static readonly Vector2[] StartNodeAnchors =
        {
            new(0.54f, 0.84f),
            new(0.38f, 0.62f),
            new(0.76f, 0.62f),
            new(0.24f, 0.40f),
            new(0.52f, 0.40f),
            new(0.12f, 0.18f),
            new(0.88f, 0.40f)
        };

        /// <summary>
        /// 调整完成状态的节点相对位置。
        /// </summary>
        private static readonly Vector2[] EndNodeAnchors =
        {
            new(0.70f, 0.62f),
            new(0.50f, 0.84f),
            new(0.82f, 0.40f),
            new(0.30f, 0.62f),
            new(0.42f, 0.40f),
            new(0.18f, 0.40f),
            new(0.58f, 0.40f)
        };

        /// <summary>
        /// 未调整状态的节点颜色类型。
        /// </summary>
        private static readonly NodeTone[] StartNodeTones =
        {
            NodeTone.Black,
            NodeTone.Purple,
            NodeTone.Black,
            NodeTone.Black,
            NodeTone.Black,
            NodeTone.Purple,
            NodeTone.Purple
        };

        /// <summary>
        /// 调整完成状态的节点颜色类型。
        /// </summary>
        private static readonly NodeTone[] EndNodeTones =
        {
            NodeTone.Black,
            NodeTone.Purple,
            NodeTone.Black,
            NodeTone.Black,
            NodeTone.Purple,
            NodeTone.Black,
            NodeTone.Black
        };

        /// <summary>
        /// 未调整状态的树边。
        /// </summary>
        private static readonly TreeEdge[] StartEdges =
        {
            new(0, 1),
            new(0, 2),
            new(1, 3),
            new(1, 4),
            new(3, 5),
            new(2, 6)
        };

        /// <summary>
        /// 调整完成状态的树边。
        /// </summary>
        private static readonly TreeEdge[] EndEdges =
        {
            new(1, 3),
            new(1, 0),
            new(3, 5),
            new(3, 4),
            new(0, 6),
            new(0, 2)
        };

        /// <summary>
        /// 根据当前进度重建二叉树预览图形。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            Vector2[] nodePositions = BuildInterpolatedPositions(rect);
            Color tintedEdgeColor = edgeColor * color;
            float startEdgeAlpha = 1f - SmoothStep(Mathf.Clamp01(progress * edgeExitSpeed));
            float endEdgeAlpha = SmoothStep(Mathf.Clamp01(progress * edgeEnterSpeed));

            for (int i = 0; i < StartEdges.Length; i++) AddEdge(vh, nodePositions, StartEdges[i], tintedEdgeColor, startEdgeAlpha);
            for (int i = 0; i < EndEdges.Length; i++) AddEdge(vh, nodePositions, EndEdges[i], tintedEdgeColor, endEdgeAlpha);
            for (int i = 0; i < nodePositions.Length; i++) AddNode(vh, nodePositions[i], i);
        }

        /// <summary>
        /// 计算所有节点在当前进度下的插值位置。
        /// </summary>
        /// <param name="rect">绘制区域。</param>
        /// <returns>当前节点位置数组。</returns>
        private Vector2[] BuildInterpolatedPositions(Rect rect)
        {
            Vector2[] positions = new Vector2[StartNodeAnchors.Length];
            float left = rect.xMin + padding;
            float right = rect.xMax - padding;
            float bottom = rect.yMin + padding;
            float top = rect.yMax - padding;
            for (int i = 0; i < positions.Length; i++)
            {
                Vector2 start = ResolvePoint(rect, StartNodeAnchors[i], left, right, bottom, top);
                Vector2 end = ResolvePoint(rect, EndNodeAnchors[i], left, right, bottom, top);
                positions[i] = Vector2.Lerp(start, end, SmoothStep(progress));
            }

            return positions;
        }

        /// <summary>
        /// 将归一化坐标转换为实际绘制坐标。
        /// </summary>
        /// <param name="rect">绘制区域。</param>
        /// <param name="anchor">归一化坐标。</param>
        /// <param name="left">左边界。</param>
        /// <param name="right">右边界。</param>
        /// <param name="bottom">下边界。</param>
        /// <param name="top">上边界。</param>
        /// <returns>实际绘制坐标。</returns>
        private static Vector2 ResolvePoint(Rect rect, Vector2 anchor, float left, float right, float bottom, float top)
        {
            float x = Mathf.Lerp(left, right, anchor.x);
            float y = Mathf.Lerp(bottom, top, anchor.y);
            return new Vector2(x, y);
        }

        /// <summary>
        /// 根据当前进度计算节点颜色。
        /// </summary>
        /// <param name="nodeIndex">节点索引。</param>
        /// <returns>当前节点颜色。</returns>
        private Color ResolveNodeColor(int nodeIndex)
        {
            Color startColor = ResolveToneColor(StartNodeTones[nodeIndex]);
            Color endColor = ResolveToneColor(EndNodeTones[nodeIndex]);
            return Color.Lerp(startColor, endColor, SmoothStep(progress));
        }

        /// <summary>
        /// 计算当前节点处于紫色状态的插值权重。
        /// </summary>
        /// <param name="nodeIndex">节点索引。</param>
        /// <returns>0 表示黑色，1 表示紫色。</returns>
        private float ResolvePurpleWeight(int nodeIndex)
        {
            float startWeight = StartNodeTones[nodeIndex] == NodeTone.Purple ? 1f : 0f;
            float endWeight = EndNodeTones[nodeIndex] == NodeTone.Purple ? 1f : 0f;
            return Mathf.Lerp(startWeight, endWeight, SmoothStep(progress));
        }

        /// <summary>
        /// 根据颜色类型返回实际颜色。
        /// </summary>
        /// <param name="tone">颜色类型。</param>
        /// <returns>节点颜色。</returns>
        private Color ResolveToneColor(NodeTone tone)
        {
            return tone == NodeTone.Purple ? purpleNodeColor : blackNodeColor;
        }

        /// <summary>
        /// 绘制一个节点。紫色节点会额外绘制与边线同色的外圈轮廓。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="center">圆心。</param>
        /// <param name="nodeIndex">节点索引。</param>
        private void AddNode(VertexHelper vh, Vector2 center, int nodeIndex)
        {
            float purpleWeight = ResolvePurpleWeight(nodeIndex);
            if (purpleWeight > 0.001f)
            {
                Color outlineNodeColor = edgeColor * color;
                outlineNodeColor.a *= purpleWeight;
                AddCircle(vh, center, nodeRadius, outlineNodeColor, 24);
            }

            float innerRadius = purpleWeight > 0.001f ? Mathf.Max(1f, nodeRadius - purpleOutlineThickness * purpleWeight) : nodeRadius;
            AddCircle(vh, center, innerRadius, ResolveNodeColor(nodeIndex) * color, 24);
        }

        /// <summary>
        /// 绘制一条树边。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="nodePositions">节点位置数组。</param>
        /// <param name="edge">树边定义。</param>
        /// <param name="baseColor">基础颜色。</param>
        /// <param name="alpha">当前透明度。</param>
        private void AddEdge(VertexHelper vh, Vector2[] nodePositions, TreeEdge edge, Color baseColor, float alpha)
        {
            if (alpha <= 0.001f) return;
            Color lineColor = baseColor;
            lineColor.a *= alpha;
            AddLine(vh, nodePositions[edge.From], nodePositions[edge.To], edgeThickness, lineColor);
        }

        /// <summary>
        /// 为当前进度应用平滑缓动。
        /// </summary>
        /// <param name="value">原始进度。</param>
        /// <returns>平滑后的进度。</returns>
        private static float SmoothStep(float value)
        {
            float t = Mathf.Clamp01(value);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 向顶点缓冲中添加一条带厚度的线段。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="start">起点。</param>
        /// <param name="end">终点。</param>
        /// <param name="thickness">线宽。</param>
        /// <param name="lineColor">线条颜色。</param>
        private void AddLine(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color lineColor)
        {
            Vector2 direction = end - start;
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;

            Vector2 normal = new(-direction.y, direction.x);
            normal.Normalize();
            Vector2 offset = normal * (thickness * 0.5f);

            int startIndex = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = lineColor;

            vertex.position = start - offset;
            vh.AddVert(vertex);
            vertex.position = start + offset;
            vh.AddVert(vertex);
            vertex.position = end + offset;
            vh.AddVert(vertex);
            vertex.position = end - offset;
            vh.AddVert(vertex);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }

        /// <summary>
        /// 向顶点缓冲中添加一个纯色圆形。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="center">圆心。</param>
        /// <param name="radius">半径。</param>
        /// <param name="circleColor">颜色。</param>
        /// <param name="segments">圆周分段数。</param>
        private void AddCircle(VertexHelper vh, Vector2 center, float radius, Color circleColor, int segments)
        {
            if (radius <= 0f) return;
            int centerIndex = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = circleColor;
            vertex.position = center;
            vh.AddVert(vertex);

            for (int i = 0; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                vertex.position = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                vh.AddVert(vertex);
            }

            for (int i = 1; i <= segments; i++) vh.AddTriangle(centerIndex, centerIndex + i, centerIndex + i + 1);
        }
    }
}
