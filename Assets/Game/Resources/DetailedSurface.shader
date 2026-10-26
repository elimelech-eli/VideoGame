Shader "Nexus/Detailed Surface"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo", 2D) = "white" {}
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _SpecGlossMap ("Specular", 2D) = "white" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _EmissionMap ("Emission", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Tint", Color) = (0,0,0,0)
        _RoughnessMap ("Roughness", 2D) = "gray" {}
        _MetallicMap ("Metallic", 2D) = "black" {}
        _UseRoughness ("Use Roughness", Float) = 0
        _UseMetallic ("Use Metallic", Float) = 0
        _Glossiness ("Smoothness", Range(0,1)) = 0.45
        _SpecularStrength ("Specular Strength", Range(0,1)) = 0.35
        _Grime ("Surface Wear", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300
        CGPROGRAM
        #pragma surface surf StandardSpecular fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex, _BumpMap, _SpecGlossMap, _OcclusionMap, _EmissionMap, _RoughnessMap, _MetallicMap;
        fixed4 _Color, _EmissionColor;
        half _Glossiness, _SpecularStrength, _UseRoughness, _UseMetallic, _Grime;
        struct Input { float2 uv_MainTex; float3 worldPos; };
        float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
        float noise(float2 p)
        {
            float2 c=floor(p), f=frac(p); f=f*f*(3-2*f);
            return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);
        }
        void surf(Input IN, inout SurfaceOutputStandardSpecular o)
        {
            float2 uv = IN.uv_MainTex;
            fixed3 albedo = tex2D(_MainTex, uv).rgb * _Color.rgb;
            float wear=smoothstep(.25,.8,noise(IN.worldPos.xz*.55)*.7+noise(IN.worldPos.xz*2.1)*.3)*_Grime;
            albedo *= lerp(1,.48,wear);
            half metallic = tex2D(_MetallicMap, uv).r * _UseMetallic;
            o.Albedo = albedo * (1 - metallic * .8);
            o.Normal = UnpackNormal(tex2D(_BumpMap, uv));
            o.Specular = lerp(tex2D(_SpecGlossMap, uv).rgb * _SpecularStrength, albedo, metallic);
            o.Smoothness = lerp(_Glossiness, 1 - tex2D(_RoughnessMap, uv).r, _UseRoughness);
            o.Smoothness *= 1-wear*.5;
            o.Occlusion = tex2D(_OcclusionMap, uv).r;
            o.Emission = tex2D(_EmissionMap, uv).rgb * _EmissionColor.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
