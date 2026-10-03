Shader "Ascension/Whitebox"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct input { float4 vertex: POSITION; float3 normal: NORMAL; };
            struct output { float4 vertex: SV_POSITION; float shade: TEXCOORD0; UNITY_FOG_COORDS(1) };
            output vert(input v) { output o; o.vertex=UnityObjectToClipPos(v.vertex); o.shade=.45+.55*max(0,dot(UnityObjectToWorldNormal(v.normal),normalize(float3(-.3,.8,-.4)))); UNITY_TRANSFER_FOG(o,o.vertex); return o; }
            fixed4 frag(output i): SV_Target { fixed4 c=fixed4(_Color.rgb*i.shade,_Color.a); UNITY_APPLY_FOG(i.fogCoord,c); return c; }
            ENDCG
        }
    }
}
