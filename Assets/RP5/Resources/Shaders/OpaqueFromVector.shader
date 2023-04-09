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
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float4 world_pos : TEXCOORD01;
            };

            float4 _albedo;
            float4 _emissive;
            float _metallic;
            float _roughness;
            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world_pos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            void frag(
                v2f i,
                out float4 gbuffer0 : SV_Target0,
                out float4 gbuffer1 : SV_Target1,
                out float2 gbuffer2 : SV_Target2,
                out float4 gbuffer3 : SV_Target3,
                out float2 gbuffer4 : SV_Target4) {
                //float4 normal = tex2D(_normal_map, i.uv);
                float3 normal = normalize(i.normal); // to [0, 1]

                gbuffer0 = _albedo;
                gbuffer1 = float4(normal, 0);
                gbuffer2 = float2(1, 1);
                gbuffer3 = float4(i.world_pos);
                gbuffer4 = float2(_metallic, _roughness); // metalic roughness
            }
            ENDCG
        }
    }
}