Shader "ConvexHullDemo3D/Surface" {
 Properties {
  _UseVertexColors("Use face vertex colors",Float)=0
  _FaceColor("Face",Color)=(.23,.52,.86,.18)
  _EdgeColor("Triangle edges",Color)=(.12,.33,.64,.65)
  _EdgeWidth("World edge width",Float)=.018
  [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Pass {
   Tags {"LightMode"="SRPDefaultUnlit"}
   Blend SrcAlpha OneMinusSrcAlpha
   ZWrite Off
   ZTest LEqual
   Cull [_Cull]
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float3 bary:TEXCOORD0;float3 heights:TEXCOORD1;float4 color:COLOR;};
   struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;float3 bary:TEXCOORD1;float3 heights:TEXCOORD2;float4 color:COLOR;};
   CBUFFER_START(UnityPerMaterial)
   float4 _FaceColor,_EdgeColor;float _EdgeWidth,_Cull,_UseVertexColors;
   CBUFFER_END
   Varyings vert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(input.normalOS);o.bary=input.bary;o.heights=input.heights;o.color=input.color;return o;}
   half4 frag(Varyings input):SV_Target {
    float3 distances=input.bary*input.heights;
    float distance=min(distances.x,min(distances.y,distances.z));
    float aa=max(fwidth(distance),.000001);float edge=1-smoothstep(_EdgeWidth-aa,_EdgeWidth+aa,distance);
    float shade=.60+.40*abs(dot(normalize(input.normalWS),normalize(float3(.4,.8,-.6))));
    float4 face=lerp(_FaceColor,float4(input.color.rgb,input.color.a*_FaceColor.a),saturate(_UseVertexColors));
    return lerp(float4(face.rgb*shade,face.a),_EdgeColor,edge);
   }
   ENDHLSL
  }
 }
}
