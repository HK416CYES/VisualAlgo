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
        /// 紫色节点颜色；为配合按钮主题，紫色代表红黑树中的红节点。
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
            new(0.625f, 0.18f), new(0.375f, 0.40f), new(0.75f, 0.40f),
            new(0.25f, 0.84f), new(0.50f, 0.62f), new(0.125f, 0.62f), new(0.875f, 0.18f)
        };

        /// <summary>
        /// 调整完成状态的节点相对位置。
        /// </summary>
        private static readonly Vector2[] EndNodeAnchors =
        {
            new(0.625f, 0.20f), new(0.375f, 0.20f), new(0.75f, 0.52f),
            new(0.25f, 0.52f), new(0.50f, 0.84f), new(0.125f, 0.20f), new(0.875f, 0.20f)
        };

        /// <summary>
        /// 未调整状态的节点颜色类型。
        /// </summary>
        private static readonly NodeTone[] StartNodeTones =
        {
            NodeTone.Purple, NodeTone.Black, NodeTone.Black, NodeTone.Black,
            NodeTone.Purple, NodeTone.Black, NodeTone.Purple
        };

        /// <summary>
        /// 调整完成状态的节点颜色类型。
        /// </summary>
        private static readonly NodeTone[] EndNodeTones =
        {
            NodeTone.Purple, NodeTone.Black, NodeTone.Black, NodeTone.Purple,
            NodeTone.Black, NodeTone.Black, NodeTone.Purple
        };

        /// <summary>
        /// 未调整状态的树边。
        /// </summary>
        private static readonly TreeEdge[] StartEdges =
        {
            new(3, 5), new(3, 4), new(4, 1), new(4, 2), new(2, 0), new(2, 6)
        };

        /// <summary>
        /// 调整完成状态的树边。
        /// </summary>
        private static readonly TreeEdge[] EndEdges =
        {
            new(4, 3), new(4, 2), new(3, 5), new(3, 1), new(2, 0), new(2, 6)
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
            // 左旋中旧连接淡出、新连接淡入。共有连线只绘制一次，避免闪烁。
            // 3—4 虽然父子方向反转，但仍是同一条可见线段。
            float blend = SmoothStep(progress);
            for (int i = 0; i < StartEdges.Length; i++)
                AddEdge(vh, nodePositions, StartEdges[i], tintedEdgeColor,
                    ContainsSegment(EndEdges, StartEdges[i]) ? 1f : 1f - blend);
            for (int i = 0; i < EndEdges.Length; i++)
                if (!ContainsSegment(StartEdges, EndEdges[i]))
                    AddEdge(vh, nodePositions, EndEdges[i], tintedEdgeColor, blend);
            for (int i = 0; i < nodePositions.Length; i++) AddNode(vh, nodePositions[i], i);
        }

        private static bool ContainsSegment(TreeEdge[] edges, TreeEdge target)
        {
            foreach (var edge in edges)
                if ((edge.From == target.From && edge.To == target.To) ||
                    (edge.From == target.To && edge.To == target.From)) return true;
            return false;
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
                // 只对完整行程缓动一次，避免两段缓动在中点都减速到零。
                // 二次曲线仍经过原旋转交接位置，且中点前后速度连续。
                Vector2 middle = (start + end) * 0.5f;
                if (i == 3 || i == 4) middle.y = Mathf.Lerp(bottom, top, 0.69f);
                Vector2 control = 2f * middle - 0.5f * (start + end);
                float t = SmoothStep(progress);
                positions[i] = Vector2.Lerp(Vector2.Lerp(start, control, t),
                    Vector2.Lerp(control, end, t), t);
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
            return Color.Lerp(ResolveToneColor(StartNodeTones[nodeIndex]),
                ResolveToneColor(EndNodeTones[nodeIndex]), SmoothStep(progress));
        }

        /// <summary>
        /// 计算当前节点处于紫色状态的插值权重。
        /// </summary>
        /// <param name="nodeIndex">节点索引。</param>
        /// <returns>0 表示黑色，1 表示紫色。</returns>
        private float ResolvePurpleWeight(int nodeIndex)
        {
            float start = StartNodeTones[nodeIndex] == NodeTone.Purple ? 1f : 0f;
            float end = EndNodeTones[nodeIndex] == NodeTone.Purple ? 1f : 0f;
            return Mathf.Lerp(start, end, SmoothStep(progress));
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
