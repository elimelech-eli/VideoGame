Shader "Hidden/Nexus/ShipViewport"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 position:SV_POSITION; float3 world:TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o;o.position=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 sky=float2(i.world.x+i.world.z,i.world.y)*12;
                float2 cell=floor(sky);
                float random=frac(sin(dot(cell,float2(12.9898,78.233)))*43758.5453);
                float star=step(.975,random)*pow(saturate(1-length(frac(sky)-.5)*5),3);
                return fixed4(float3(.007,.017,.035)+star*float3(.5,.7,.9),1);
            }
            ENDCG
        }
    }
}
