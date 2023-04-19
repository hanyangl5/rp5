Shader "Custuom/OpaqueFromVector"
{
    Properties
    {
        // [optional: attribute] name("display text in Inspector", type name) = default value
        _metallic ("metallic", Range (0.0, 1.0)) = 0.5
        _roughness ("rougness", Range (0.0, 1.0)) = 0.5
        _albedo ("albedo", Color) = (1.0, 1.0, 1.0)
        _emissive ("emissive", Color) = (0.0, 0.0, 0.0)

    }
    SubShader
    {
        Tags { "LightMode"="geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex VSMain
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
            
            float4 _albedo;
            float4 _emissive;
            float _metallic;
            float _roughness;

            float4x4 view_projection_prev;
            float4x4 view_projection;
            float2 jitter_offset_prev;
            float2 jitter_offset;
            
            VsOutput VSMain(VsInput v) {
                VsOutput o;
                float4 postion_ws = mul(unity_ObjectToWorld, v.pos);
                o.position_clip = mul(view_projection, postion_ws);
                o.position_clip_prev = mul(view_projection_prev, postion_ws);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.position_ws = postion_ws;
                return o;
            }

            void PSMain(
                PsInput i,
                out float4 gbuffer0 : SV_Target0,
                out float4 gbuffer1 : SV_Target1,
                out float2 gbuffer2 : SV_Target2,
                out float4 gbuffer3 : SV_Target3,
                out float2 gbuffer4 : SV_Target4) {
                //float4 normal = tex2D(_normal_map, i.uv);
                float3 normal = normalize(i.normal); // to [0, 1]

                gbuffer0 = _albedo;
                gbuffer1 = float4(normal, 0);
                // mv should be in [-1, 1]
                gbuffer2 = ((i.position_clip / i.position_clip.w - jitter_offset)  - (i.position_clip_prev / i.position_clip_prev.w - jitter_offset_prev)) * 0.5;
                gbuffer3 = float4(i.position_ws);
                gbuffer4 = float2(_metallic, _roughness); // metalic roughness
            }
            ENDCG
        }
    }
}