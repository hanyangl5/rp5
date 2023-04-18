Shader "Custuom/Opaque"
{
    Properties
    {
        // [optional: attribute] name("display text in Inspector", type name) = default value
        [MainColor, Gamma]_albedo_tex("albedo tex", 2D) = "dark" {}
        [Emissive, Gamma]_emissive_tex("emissive tex", 2D) = "dark" {}
        _metallic_tex("metallic tex", 2D) = "dark" {}
        _roughness_tex("roughness tex", 2D) = "dark" {}
        [Normal]_normal_map("normal map", 2D) = "dark" {}
        _ao_map_tex("ao map", 2D) = "dark" {}
    
    }
    SubShader
    {
        Tags { "LightMode"="geometry" }

        Pass
        {
            CGPROGRAM
            #pragma pos VSMain
            #pragma fragment PSMain
            
            #include "UnityCG.cginc"

            struct VsInput {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            struct PsInput {
                float2 uv : TEXCOORD0;
                float4 position_clip : SV_POSITION;
                float4 position_clip_prev : TEXCOORD01;
                float3 normal : NORMAL;
                float4 position_ws : TEXCOORD02;
            };
            typedef PsInput VsOutput; 

            sampler2D _albedo_tex;
            float4 _albedo_tex_ST;
            sampler2D _metallic_tex;
            sampler2D _roughness_tex;
            sampler2D _normal_map;

            VsOutput VSMain(VsInput v) {
                VsOutput o;
                o.position_clip = UnityObjectToClipPos(v.pos);
                o.position_clip_prev = UnityObjectToClipPos(v.pos); // previous mvp
                o.uv = TRANSFORM_TEX(v.uv, _albedo_tex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.position_ws = mul(unity_ObjectToWorld, v.pos);
                return o;
            }

            void PSMain(
                PsInput i,
                out float4 gbuffer0 : SV_Target0,
                out float4 gbuffer1 : SV_Target1,
                out float2 gbuffer2 : SV_Target2,
                out float4 gbuffer3 : SV_Target3,
                out float2 gbuffer4 : SV_Target4) {
                float4 color = tex2D(_albedo_tex, i.uv);
                float m = tex2D(_metallic_tex, i.uv).r;
                float r = tex2D(_roughness_tex, i.uv).r;
                //float4 normal = tex2D(_normal_map, i.uv);
                float3 normal = normalize(i.normal);
                color.rgb = pow(color.rgb,float3(2.2, 2.2, 2.2));
                gbuffer0 = float4(color.rgb, 0.0);
                gbuffer1 = float4(normal, 0.0);
                gbuffer2 = float2(1, 1);
                gbuffer3 = float4(i.position_ws);
                gbuffer4 = float2(m, r); // metalic roughness
            }
            ENDCG
        }
    }
}