using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace VisualAlgo.Managers.UI.StartMenu
{
    /// <summary>
    /// 绘制开始菜单寻路按钮上方的 4x4 网格图和悬停路径预览。
    /// </summary>
    public sealed class StartMenuGridMapPreviewGraphic : MaskableGraphic
    {
        /// <summary>
        /// 默认可通行格子颜色。
        /// </summary>
        [SerializeField] private Color emptyCellColor = new(0.74f, 0.74f, 0.74f, 1f);

        /// <summary>
        /// 障碍格子颜色。
        /// </summary>
        [SerializeField] private Color wallCellColor = new(0.08f, 0.08f, 0.08f, 1f);

        /// <summary>
        /// 起点格子颜色。
        /// </summary>
        [SerializeField] private Color startCellColor = new(0.22f, 0.46f, 0.9f, 1f);

        /// <summary>
        /// 终点格子颜色。
        /// </summary>
        [SerializeField] private Color goalCellColor = new(0.91f, 0.24f, 0.24f, 1f);

        /// <summary>
        /// 路径主色。
        /// </summary>
        [SerializeField] private Color pathColor = new(0.18f, 0.86f, 0.82f, 1f);

        /// <summary>
        /// 路径轮廓颜色。
        /// </summary>
        [SerializeField] private Color outlineColor = Color.white;

        /// <summary>
        /// 方格之间的间隙。
        /// </summary>
        [SerializeField, Min(0f)] private float cellGap = 5f;

        /// <summary>
        /// 单个格子的圆角半径。
        /// </summary>
        [SerializeField, Min(0f)] private float cellCornerRadius = 6f;

        /// <summary>
        /// 路径主线粗细。
        /// </summary>
        [SerializeField, Min(1f)] private float pathThickness = 10f;

        /// <summary>
        /// 路径外围轮廓总粗细。
        /// </summary>
        [SerializeField, Min(1f)] private float outlineThickness = 18f;

        /// <summary>
        /// 路径节点半径。
        /// </summary>
        [FormerlySerializedAs("cornerDotRadius")]
        [SerializeField, Min(1f)] private float nodeRadius = 8f;

        /// <summary>
        /// 路径节点轮廓半径。
        /// </summary>
        [FormerlySerializedAs("cornerDotOutlineRadius")]
        [SerializeField, Min(1f)] private float nodeOutlineRadius = 12f;

        /// <summary>
        /// 节点提前显现的路径长度。
        /// </summary>
        [SerializeField, Min(0.001f)] private float nodeRevealLead = 18f;

        /// <summary>
        /// 起点节点的独立显隐进度。
        /// </summary>
        [SerializeField, Range(0f, 1f)] private float startNodeProgress;

        /// <summary>
        /// 当前路径绘制进度。
        /// </summary>
        [SerializeField, Range(0f, 1f)] private float progress;

        /// <summary>
        /// 获取或设置当前路径绘制进度。
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
        /// 获取或设置起点节点的独立显隐进度。
        /// </summary>
        public float StartNodeProgress
        {
            get => startNodeProgress;
            set
            {
                float clampedValue = Mathf.Clamp01(value);
                if (Mathf.Approximately(startNodeProgress, clampedValue)) return;
                startNodeProgress = clampedValue;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// 网格路径经过的关键节点。
        /// </summary>
        private static readonly Vector2Int[] PathNodes =
        {
            new(1, 4),
            new(2, 4),
            new(2, 1),
            new(4, 1),
            new(4, 4)
        };

        /// <summary>
        /// 障碍格子坐标集合。
        /// </summary>
        private static readonly HashSet<Vector2Int> WallCells = new()
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, 2),
            new Vector2Int(1, 3),
            new Vector2Int(3, 2),
            new Vector2Int(3, 3),
            new Vector2Int(3, 4)
        };

        /// <summary>
        /// 重新构建网格和路径图形。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float cellWidth = (rect.width - cellGap * 3f) * 0.25f;
            float cellHeight = (rect.height - cellGap * 3f) * 0.25f;

            for (int x = 1; x <= 4; x++)
            {
                for (int y = 1; y <= 4; y++)
                {
                    AddRoundedRect(vh, GetCellRect(rect, x, y, cellWidth, cellHeight), ResolveCellColor(x, y) * color, cellCornerRadius, 5);
                }
            }

            if (progress <= 0f && startNodeProgress <= 0f) return;

            Vector2[] points = BuildPathPoints(rect, cellWidth, cellHeight);
            float[] distances = BuildNodeDistances(points);
            float currentLength = distances[^1] * progress;

            AddPathShape(vh, points, distances, currentLength, startNodeProgress, outlineThickness, nodeOutlineRadius, outlineColor * color);
            AddPathShape(vh, points, distances, currentLength, startNodeProgress, pathThickness, nodeRadius, pathColor * color);
        }

        /// <summary>
        /// 根据格子坐标返回其应显示的颜色。
        /// </summary>
        /// <param name="x">格子 X 坐标。</param>
        /// <param name="y">格子 Y 坐标。</param>
        /// <returns>格子颜色。</returns>
        private Color ResolveCellColor(int x, int y)
        {
            Vector2Int coordinate = new(x, y);
            if (coordinate == new Vector2Int(1, 4)) return startCellColor;
            if (coordinate == new Vector2Int(4, 4)) return goalCellColor;
            return WallCells.Contains(coordinate) ? wallCellColor : emptyCellColor;
        }

        /// <summary>
        /// 构建路径关键节点的局部坐标。
        /// </summary>
        /// <param name="rect">整体绘制区域。</param>
        /// <param name="cellWidth">单元格宽度。</param>
        /// <param name="cellHeight">单元格高度。</param>
        /// <returns>路径关键节点数组。</returns>
        private Vector2[] BuildPathPoints(Rect rect, float cellWidth, float cellHeight)
        {
            Vector2[] points = new Vector2[PathNodes.Length];
            for (int i = 0; i < PathNodes.Length; i++) points[i] = GetCellCenter(rect, PathNodes[i], cellWidth, cellHeight);
            return points;
        }

        /// <summary>
        /// 构建每个路径关键节点的累计距离。
        /// </summary>
        /// <param name="points">路径关键节点数组。</param>
        /// <returns>累计距离数组。</returns>
        private float[] BuildNodeDistances(Vector2[] points)
        {
            float[] distances = new float[points.Length];
            for (int i = 1; i < points.Length; i++) distances[i] = distances[i - 1] + Vector2.Distance(points[i - 1], points[i]);
            return distances;
        }

        /// <summary>
        /// 绘制路径的线段与节点，轮廓和主体分别调用一次，保证整体外形连续。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="points">路径关键节点。</param>
        /// <param name="distances">节点累计距离。</param>
        /// <param name="currentLength">当前已经显示的路径长度。</param>
        /// <param name="startNodeReveal">起点节点独立显隐进度。</param>
        /// <param name="lineThickness">线段粗细。</param>
        /// <param name="nodeRadiusValue">节点半径。</param>
        /// <param name="shapeColor">绘制颜色。</param>
        private void AddPathShape(VertexHelper vh, Vector2[] points, float[] distances, float currentLength, float startNodeReveal, float lineThickness, float nodeRadiusValue, Color shapeColor)
        {
            for (int i = 0; i < points.Length; i++)
            {
                float reveal = ComputeNodeReveal(i, points.Length, distances[i], currentLength, startNodeReveal);
                if (reveal > 0f) AddCircle(vh, points[i], nodeRadiusValue * reveal, shapeColor, 24);
            }

            for (int i = 0; i < points.Length - 1; i++)
            {
                float segmentStartLength = distances[i];
                float segmentLength = distances[i + 1] - segmentStartLength;
                float drawnLength = Mathf.Clamp(currentLength - segmentStartLength, 0f, segmentLength);
                if (drawnLength <= 0f) continue;

                float t = segmentLength <= Mathf.Epsilon ? 1f : drawnLength / segmentLength;
                Vector2 partialEnd = Vector2.Lerp(points[i], points[i + 1], t);
                AddLine(vh, points[i], partialEnd, lineThickness, shapeColor);
                if (t < 0.999f) AddCircle(vh, partialEnd, lineThickness * 0.5f, shapeColor, 16);
            }
        }

        /// <summary>
        /// 计算指定节点当前应显示的比例。
        /// </summary>
        /// <param name="nodeIndex">节点索引。</param>
        /// <param name="nodeCount">节点总数。</param>
        /// <param name="nodeDistance">节点对应累计距离。</param>
        /// <param name="currentLength">当前已经显示的路径长度。</param>
        /// <param name="startNodeReveal">起点节点独立显隐进度。</param>
        /// <returns>0 到 1 的显现比例。</returns>
        private float ComputeNodeReveal(int nodeIndex, int nodeCount, float nodeDistance, float currentLength, float startNodeReveal)
        {
            if (nodeIndex == 0) return startNodeReveal;
            if (currentLength <= 0f) return 0f;

            float lead = nodeRevealLead;
            float revealStart = Mathf.Max(0f, nodeDistance - lead);
            if (currentLength <= revealStart) return 0f;
            if (currentLength >= nodeDistance) return 1f;
            float t = Mathf.InverseLerp(revealStart, nodeDistance, currentLength);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 获取指定格子的矩形区域。
        /// </summary>
        /// <param name="rect">整体绘制区域。</param>
        /// <param name="x">格子 X 坐标。</param>
        /// <param name="y">格子 Y 坐标。</param>
        /// <param name="cellWidth">单元格宽度。</param>
        /// <param name="cellHeight">单元格高度。</param>
        /// <returns>格子矩形。</returns>
        private Rect GetCellRect(Rect rect, int x, int y, float cellWidth, float cellHeight)
        {
            float left = rect.xMin + (x - 1) * (cellWidth + cellGap);
            float bottom = rect.yMin + (y - 1) * (cellHeight + cellGap);
            return new Rect(left, bottom, cellWidth, cellHeight);
        }

        /// <summary>
        /// 获取指定格子的中心点。
        /// </summary>
        /// <param name="rect">整体绘制区域。</param>
        /// <param name="coordinate">格子坐标。</param>
        /// <param name="cellWidth">单元格宽度。</param>
        /// <param name="cellHeight">单元格高度。</param>
        /// <returns>格子中心点。</returns>
        private Vector2 GetCellCenter(Rect rect, Vector2Int coordinate, float cellWidth, float cellHeight)
        {
            Rect cellRect = GetCellRect(rect, coordinate.x, coordinate.y, cellWidth, cellHeight);
            return cellRect.center;
        }

        /// <summary>
        /// 向顶点缓冲中添加一个纯色圆角矩形。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="rect">目标矩形。</param>
        /// <param name="quadColor">矩形颜色。</param>
        /// <param name="cornerRadius">圆角半径。</param>
        /// <param name="cornerSegments">每个圆角的分段数。</param>
        private void AddRoundedRect(VertexHelper vh, Rect rect, Color quadColor, float cornerRadius, int cornerSegments)
        {
            float radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(rect.width, rect.height) * 0.5f);
            if (radius <= 0.01f)
            {
                AddQuad(vh, rect, quadColor);
                return;
            }

            int centerIndex = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = quadColor;
            vertex.position = rect.center;
            vh.AddVert(vertex);

            List<Vector2> outlinePoints = BuildRoundedRectPoints(rect, radius, Mathf.Max(1, cornerSegments));
            for (int i = 0; i < outlinePoints.Count; i++)
            {
                vertex.position = outlinePoints[i];
                vh.AddVert(vertex);
            }

            for (int i = 0; i < outlinePoints.Count; i++)
            {
                int current = centerIndex + 1 + i;
                int next = centerIndex + 1 + ((i + 1) % outlinePoints.Count);
                vh.AddTriangle(centerIndex, current, next);
            }
        }

        /// <summary>
        /// 向顶点缓冲中添加一个纯色矩形。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="rect">目标矩形。</param>
        /// <param name="quadColor">矩形颜色。</param>
        private void AddQuad(VertexHelper vh, Rect rect, Color quadColor)
        {
            int startIndex = vh.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = quadColor;
            vertex.position = new Vector2(rect.xMin, rect.yMin);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMin, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMax);
            vh.AddVert(vertex);
            vertex.position = new Vector2(rect.xMax, rect.yMin);
            vh.AddVert(vertex);

            vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vh.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }

        /// <summary>
        /// 构建圆角矩形一圈的顶点。
        /// </summary>
        /// <param name="rect">目标矩形。</param>
        /// <param name="radius">圆角半径。</param>
        /// <param name="cornerSegments">每个圆角的分段数。</param>
        /// <returns>按顺时针顺序排列的轮廓点。</returns>
        private List<Vector2> BuildRoundedRectPoints(Rect rect, float radius, int cornerSegments)
        {
            List<Vector2> points = new(cornerSegments * 4 + 4);
            AppendCorner(points, new Vector2(rect.xMax - radius, rect.yMax - radius), radius, 0f, 90f, cornerSegments);
            AppendCorner(points, new Vector2(rect.xMin + radius, rect.yMax - radius), radius, 90f, 180f, cornerSegments);
            AppendCorner(points, new Vector2(rect.xMin + radius, rect.yMin + radius), radius, 180f, 270f, cornerSegments);
            AppendCorner(points, new Vector2(rect.xMax - radius, rect.yMin + radius), radius, 270f, 360f, cornerSegments);
            return points;
        }

        /// <summary>
        /// 向轮廓点集合中追加一个圆角段。
        /// </summary>
        /// <param name="points">轮廓点集合。</param>
        /// <param name="center">圆角圆心。</param>
        /// <param name="radius">圆角半径。</param>
        /// <param name="startAngle">起始角度。</param>
        /// <param name="endAngle">结束角度。</param>
        /// <param name="segments">分段数。</param>
        private void AppendCorner(List<Vector2> points, Vector2 center, float radius, float startAngle, float endAngle, int segments)
        {
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angle = Mathf.Lerp(startAngle, endAngle, t) * Mathf.Deg2Rad;
                points.Add(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        /// <summary>
        /// 向顶点缓冲中添加一条带厚度的线段矩形。
        /// </summary>
        /// <param name="vh">UI 顶点辅助器。</param>
        /// <param name="start">起点。</param>
        /// <param name="end">终点。</param>
        /// <param name="thickness">线宽。</param>
        /// <param name="lineColor">线段颜色。</param>
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
