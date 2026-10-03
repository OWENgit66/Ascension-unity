Shader "Ascension/Aura" {
 Properties {_Color("Color",Color)=(.2,.9,1,.2)}
 SubShader {Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 float4 _Color;
 float4 vert(float4 v:POSITION):SV_POSITION{return UnityObjectToClipPos(v);}
 fixed4 frag():SV_Target{return _Color;}
 ENDCG
 } }
}
