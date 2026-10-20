Shader "ConvexHullDemo3D/TeachingLine" {
 Properties { _Color("Color",Color)=(1,1,1,1) [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Depth test",Float)=4 }
 SubShader { Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+100" "RenderType"="Transparent" }
 Pass { Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off ZTest [_ZTest]
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct A { float4 positionOS:POSITION;float4 color:COLOR;};
 struct V { float4 positionCS:SV_POSITION;float4 color:COLOR;};
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;
 CBUFFER_END
 V vert(A i){V o;o.positionCS=TransformObjectToHClip(i.positionOS.xyz);o.color=i.color*_Color;return o;}
 half4 frag(V i):SV_Target{return i.color;}
 ENDHLSL
 } }
}
