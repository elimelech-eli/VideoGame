Shader "NexusReview/GalaxyProjection"
{
 Properties { _MainTex("Galaxy",2D)="black"{} _Tint("Tint",Color)=(0.7,0.95,1,1) _Intensity("Intensity",Float)=1.5 }
 SubShader {
 Tags { "Queue"="Transparent" "RenderType"="Transparent" }
 Blend One One
 ZWrite Off
 Cull Off
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex; float4 _Tint; float _Intensity;
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;};
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;return o;}
 fixed4 frag(v2f i):SV_Target {
 float3 c=tex2D(_MainTex,i.uv).rgb;
 float edge=saturate((.5-length(i.uv-.5))*16);
 return float4(max(c-.015,0)*_Tint.rgb*_Intensity*edge,0);
 }
 ENDCG
 }
 }
}
