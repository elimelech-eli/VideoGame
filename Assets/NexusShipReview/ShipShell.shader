Shader "Nexus/Ship Shell"
{
 Properties { _Color("Hull",Color)=(.13,.18,.22,1) _Opacity("Opacity",Range(0,1))=1 }
 SubShader {
 Tags {"RenderType"="TransparentCutout" "Queue"="AlphaTest"}
 Cull Off
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 fixed4 _Color;float _Opacity;
 struct Input {float4 screenPos;float3 worldPos;};
 void surf(Input i,inout SurfaceOutputStandard o){
 float2 pixel=floor(i.screenPos.xy/i.screenPos.w*_ScreenParams.xy);
 float noise=frac(52.9829189*frac(dot(pixel,float2(.06711056,.00583715))));
 clip(_Opacity>=.999?1:_Opacity-noise-.001);
 float seam=step(.975,frac(i.worldPos.z*.45));
 float rib=step(.96,frac(abs(i.worldPos.x)*.65));
 float plate=frac(sin(floor(i.worldPos.x*.65)*21+floor(i.worldPos.z*.45)*17)*145.7);
 o.Albedo=_Color.rgb*(.85+plate*.3)*(1-seam*.45)*(1-rib*.25);o.Metallic=.6;o.Smoothness=.28;o.Alpha=1;
 }
 ENDCG
 }
 FallBack "Diffuse"
}
