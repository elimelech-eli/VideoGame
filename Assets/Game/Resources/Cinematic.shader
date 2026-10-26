Shader "Hidden/Nexus/Cinematic"
{
    Properties { _MainTex ("Source", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float3 tone(float3 c) { return saturate((c * (2.51*c+.03))/(c*(2.43*c+.59)+.14)); }
            fixed4 frag(v2f_img i) : SV_Target
            {
                float3 c = tex2D(_MainTex,i.uv).rgb;
                float3 glow = 0;
                for (int x=-1;x<=1;x++) for (int y=-1;y<=1;y++)
                    glow += max(0,tex2D(_MainTex,i.uv+float2(x,y)*_MainTex_TexelSize.xy*3).rgb-1);
                c = tone((c + glow * .035) * 1.12);
                float2 p = i.uv * 2 - 1;
                c *= 1 - dot(p,p) * .09;
                return fixed4(c,1);
            }
            ENDCG
        }
    }
}
