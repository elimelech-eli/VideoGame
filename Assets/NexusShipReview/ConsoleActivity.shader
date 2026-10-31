Shader "Nexus/Live Console"
{
 Properties { _MainTex("Source screen",2D)="black"{} }
 SubShader { Tags {"RenderType"="Opaque"}
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;};
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
 fixed4 frag(v2f i):SV_Target{
 float3 src=tex2D(_MainTex,i.uv).rgb;
 float2 p=i.uv*float2(40,24);float phase=_Time.y*.4+dot(floor(i.world.xz),float2(.17,.31));
 float row=step(.76,frac(p.y));float bars=step(frac(p.x*.13),.25+.22*sin(floor(p.y)*2+phase));
 float scan=pow(saturate(1-abs(frac(i.uv.y+phase*.1)-.5)*15),4);
 return float4(src*.55+float3(.05,.6,.75)*(row*bars*.3+scan*.2),1);
 }
 ENDCG
 }
 }
}
