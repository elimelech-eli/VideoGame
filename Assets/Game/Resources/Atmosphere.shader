Shader "Nexus/Atmospheric Shaft"
{
    Properties { _Color ("Tint", Color) = (0.2,0.5,1,0.02) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha One
        ZWrite Off Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            fixed4 _Color;
            v2f vert(appdata_base v) { v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float fade=smoothstep(0,.22,i.uv.y)*(1-smoothstep(.8,1,i.uv.y));
                float noise=.82+.18*sin(i.uv.x*95+i.uv.y*21+_Time.y*.2);
                return fixed4(_Color.rgb,_Color.a*fade*noise);
            }
            ENDCG
        }
    }
}
