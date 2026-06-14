Shader "UI/StartMenuGridPathReveal"
{
    Properties
    {
        [PerRendererData]_MainTex("Sprite Texture", 2D) = "white" {}
        _Color("Tint", Color) = (1,1,1,1)
        _EmptyCellColor("Empty Cell Color", Color) = (0.74,0.74,0.74,1)
        _WallCellColor("Wall Cell Color", Color) = (0.08,0.08,0.08,1)
        _StartCellColor("Start Cell Color", Color) = (0.22,0.46,0.9,1)
        _GoalCellColor("Goal Cell Color", Color) = (0.91,0.24,0.24,1)
        _PathColor("Path Color", Color) = (0.18,0.86,0.82,1)
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
        _RectMin("Rect Min", Vector) = (0,0,0,0)
        _RectSize("Rect Size", Vector) = (100,100,0,0)
        _CellGap("Cell Gap", Float) = 5
        _PathThickness("Path Thickness", Float) = 10
        _OutlineThickness("Outline Thickness", Float) = 18
        _NodeRadius("Node Radius", Float) = 8
        _NodeOutlineRadius("Node Outline Radius", Float) = 12
        _NodeRevealLead("Node Reveal Lead", Float) = 14
        _RevealSoftness("Reveal Softness", Float) = 10
        _Progress("Progress", Range(0,1)) = 0

        [HideInInspector]_StencilComp("Stencil Comparison", Float) = 8
        [HideInInspector]_Stencil("Stencil ID", Float) = 0
        [HideInInspector]_StencilOp("Stencil Operation", Float) = 0
        [HideInInspector]_StencilWriteMask("Stencil Write Mask", Float) = 255
        [HideInInspector]_StencilReadMask("Stencil Read Mask", Float) = 255
        [HideInInspector]_ColorMask("Color Mask", Float) = 15
        [HideInInspector]_UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 localPos : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _EmptyCellColor;
            fixed4 _WallCellColor;
            fixed4 _StartCellColor;
            fixed4 _GoalCellColor;
            fixed4 _PathColor;
            fixed4 _OutlineColor;
            fixed4 _TintColor;
            float4 _RectMin;
            float4 _RectSize;
            float _CellGap;
            float _PathThickness;
            float _OutlineThickness;
            float _NodeRadius;
            float _NodeOutlineRadius;
            float _NodeRevealLead;
            float _RevealSoftness;
            float _Progress;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.localPos = v.vertex.xy;
                o.color = v.color * _Color;
                return o;
            }

            float2 GetCellSize()
            {
                return (_RectSize.xy - _CellGap * 3.0) * 0.25;
            }

            float2 GetCellCenter(float x, float y)
            {
                float2 cellSize = GetCellSize();
                float2 origin = _RectMin.xy;
                return origin + float2((x - 0.5) * cellSize.x + (x - 1.0) * _CellGap, (y - 0.5) * cellSize.y + (y - 1.0) * _CellGap);
            }

            fixed4 ResolveCellColor(int2 coord)
            {
                if (coord.x == 1 && coord.y == 4) return _StartCellColor;
                if (coord.x == 4 && coord.y == 4) return _GoalCellColor;
                if ((coord.x == 1 && coord.y == 1) || (coord.x == 1 && coord.y == 2) || (coord.x == 1 && coord.y == 3) ||
                    (coord.x == 3 && coord.y == 2) || (coord.x == 3 && coord.y == 3) || (coord.x == 3 && coord.y == 4)) return _WallCellColor;
                return _EmptyCellColor;
            }

            float DistanceToSegment(float2 p, float2 a, float2 b, out float t)
            {
                float2 ab = b - a;
                float denominator = max(dot(ab, ab), 1e-5);
                t = saturate(dot(p - a, ab) / denominator);
                return distance(p, lerp(a, b, t));
            }

            float CircleMask(float dist, float radius, float aa)
            {
                return 1.0 - smoothstep(radius - aa, radius + aa, dist);
            }

            float RevealMask(float currentLength, float revealLength, float softness)
            {
                return saturate((currentLength - revealLength + softness) / max(softness, 1e-5));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 p = i.localPos;
                float2 local = p - _RectMin.xy;
                float2 cellSize = GetCellSize();
                float2 stepSize = cellSize + _CellGap;

                float2 gridPos = local / max(stepSize, 1e-5);
                int2 coord = int2(floor(gridPos)) + int2(1, 1);
                float2 cellOrigin = float2(coord.x - 1, coord.y - 1) * stepSize;
                float2 inCell = local - cellOrigin;
                bool validCoord = coord.x >= 1 && coord.x <= 4 && coord.y >= 1 && coord.y <= 4;
                bool insideCell = inCell.x >= 0.0 && inCell.y >= 0.0 && inCell.x <= cellSize.x && inCell.y <= cellSize.y;
                fixed4 baseColor = (validCoord && insideCell) ? ResolveCellColor(coord) : fixed4(0, 0, 0, 0);

                float currentLength = _Progress * (cellSize.x * 1.0 + cellSize.y * 3.0 + cellSize.x * 2.0 + cellSize.y * 3.0);
                float aa = max(fwidth(p.x) + fwidth(p.y), 1.0);
                float lineRadius = _PathThickness * 0.5;
                float outlineRadius = _OutlineThickness * 0.5;
                float fillMask = 0.0;
                float strokeMask = 0.0;

                float2 n0 = GetCellCenter(1.0, 4.0);
                float2 n1 = GetCellCenter(2.0, 4.0);
                float2 n2 = GetCellCenter(2.0, 1.0);
                float2 n3 = GetCellCenter(4.0, 1.0);
                float2 n4 = GetCellCenter(4.0, 4.0);
                float d01 = distance(n0, n1);
                float d12 = distance(n1, n2);
                float d23 = distance(n2, n3);
                float d34 = distance(n3, n4);

                if (_Progress > 0.0)
                {
                    float t;
                    float dist;
                    float reveal;

                    dist = DistanceToSegment(p, n0, n1, t);
                    reveal = RevealMask(currentLength, t * d01, _RevealSoftness);
                    fillMask = max(fillMask, CircleMask(dist, lineRadius, aa) * reveal);
                    strokeMask = max(strokeMask, CircleMask(dist, outlineRadius, aa) * reveal);

                    dist = DistanceToSegment(p, n1, n2, t);
                    reveal = RevealMask(currentLength, d01 + t * d12, _RevealSoftness);
                    fillMask = max(fillMask, CircleMask(dist, lineRadius, aa) * reveal);
                    strokeMask = max(strokeMask, CircleMask(dist, outlineRadius, aa) * reveal);

                    dist = DistanceToSegment(p, n2, n3, t);
                    reveal = RevealMask(currentLength, d01 + d12 + t * d23, _RevealSoftness);
                    fillMask = max(fillMask, CircleMask(dist, lineRadius, aa) * reveal);
                    strokeMask = max(strokeMask, CircleMask(dist, outlineRadius, aa) * reveal);

                    dist = DistanceToSegment(p, n3, n4, t);
                    reveal = RevealMask(currentLength, d01 + d12 + d23 + t * d34, _RevealSoftness);
                    fillMask = max(fillMask, CircleMask(dist, lineRadius, aa) * reveal);
                    strokeMask = max(strokeMask, CircleMask(dist, outlineRadius, aa) * reveal);

                    float nodeReveal;
                    float nodeDist = distance(p, n0);
                    nodeReveal = RevealMask(currentLength, 0.6, _NodeRevealLead);
                    fillMask = max(fillMask, CircleMask(nodeDist, _NodeRadius, aa) * nodeReveal);
                    strokeMask = max(strokeMask, CircleMask(nodeDist, _NodeOutlineRadius, aa) * nodeReveal);

                    nodeDist = distance(p, n1);
                    nodeReveal = RevealMask(currentLength, max(0.0, d01 - _NodeRevealLead), _NodeRevealLead);
                    fillMask = max(fillMask, CircleMask(nodeDist, _NodeRadius, aa) * nodeReveal);
                    strokeMask = max(strokeMask, CircleMask(nodeDist, _NodeOutlineRadius, aa) * nodeReveal);

                    nodeDist = distance(p, n2);
                    nodeReveal = RevealMask(currentLength, max(0.0, d01 + d12 - _NodeRevealLead), _NodeRevealLead);
                    fillMask = max(fillMask, CircleMask(nodeDist, _NodeRadius, aa) * nodeReveal);
                    strokeMask = max(strokeMask, CircleMask(nodeDist, _NodeOutlineRadius, aa) * nodeReveal);

                    nodeDist = distance(p, n3);
                    nodeReveal = RevealMask(currentLength, max(0.0, d01 + d12 + d23 - _NodeRevealLead), _NodeRevealLead);
                    fillMask = max(fillMask, CircleMask(nodeDist, _NodeRadius, aa) * nodeReveal);
                    strokeMask = max(strokeMask, CircleMask(nodeDist, _NodeOutlineRadius, aa) * nodeReveal);

                    nodeDist = distance(p, n4);
                    nodeReveal = RevealMask(currentLength, max(0.0, d01 + d12 + d23 + d34 - _NodeRevealLead), _NodeRevealLead);
                    fillMask = max(fillMask, CircleMask(nodeDist, _NodeRadius, aa) * nodeReveal);
                    strokeMask = max(strokeMask, CircleMask(nodeDist, _NodeOutlineRadius, aa) * nodeReveal);
                }

                fixed4 result = baseColor;
                result = lerp(result, _OutlineColor, saturate(strokeMask) * _OutlineColor.a);
                result = lerp(result, _PathColor, saturate(fillMask) * _PathColor.a);
                result *= i.color * _TintColor;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
