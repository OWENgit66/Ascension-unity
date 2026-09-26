Shader "Ascension/PortfolioSurface" {
 Properties { _Color("Tint",Color)=(1,1,1,1) _MainTex("Texture",2D)="white"{} _Floor("Floor stone",Float)=0 }
 SubShader { Tags {"RenderType"="Opaque"} Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex; float4 _Color; float _Floor;
 struct app {float4 vertex:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;};
 struct vary {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float shade:TEXCOORD2;};
 vary vert(app v){vary o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.shade=.66+.34*max(0,dot(UnityObjectToWorldNormal(v.normal),normalize(float3(-.3,.8,-.4))));return o;}
 fixed4 frag(vary i):SV_Target {
 float3 c=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
 if(_Floor>.5){float2 p=i.world.xz;float2 tile=abs(frac(p/3)-.5);float seam=smoothstep(.484,.498,max(tile.x,tile.y));float grain=sin(floor(p.x/3)*7+floor(p.y/3)*13)*.006;float r=length(p);float rim=1-smoothstep(.025,.06,abs(r-14.65));c=c*(1-seam*.20)+grain+rim*float3(.12,.10,.055);}
 return float4(c*i.shade,1);}
 ENDCG
 } }
}
