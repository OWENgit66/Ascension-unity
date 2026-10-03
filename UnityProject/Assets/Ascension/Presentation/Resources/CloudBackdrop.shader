Shader "Ascension/CloudBackdrop" {
 Properties { _MainTex("Cloud sea",2D)="white"{} }
 SubShader {Tags {"Queue"="Background" "RenderType"="Opaque"} Cull Off ZWrite Off Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct app {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct vary {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;};
 vary vert(app v){vary o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(vary i):SV_Target{return float4(tex2D(_MainTex,float2(i.uv.x,i.uv.y*.56)).rgb*.68,1);}
 ENDCG
 } }
}
