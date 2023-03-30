Shader "Custuom/geometry"
{
    Properties
    {
        // [optional: attribute] name("display text in Inspector", type name) = default value
        [MainColor, Gamma]_base_color("base color", 2D) = "white" {}
        _metallic_roughness("metallic roughness", 2D) = "white" {}
        [Normal]_normal_map("normal map", 2D) = "dark" {}
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

            sampler2D _base_color;
            float4 _base_color_ST;
            sampler2D _metallic_roughness;
            sampler2D _normal_map;

            

            v2f vert(appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _base_color);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world_pos = mul(v.vertex, unity_ObjectToWorld);
                return o;
            }

            void frag(
                v2f i,
                out float4 gbuffer0 : SV_Target0,
                out float4 gbuffer1 : SV_Target1,
                out float2 gbuffer2 : SV_Target2,
                out float4 gbuffer3 : SV_Target3,
                out float2 gbuffer4 : SV_Target4) {
                float4 color = tex2D(_base_color, i.uv);
                float4 mr = tex2D(_metallic_roughness, i.uv);
                //float4 normal = tex2D(_normal_map, i.uv);
                float3 normal = i.normal;

                gbuffer0 = color;
                gbuffer1 = float4(normal, 0);
                gbuffer2 = float2(1, 1);
                gbuffer3 = float4(i.world_pos);
                gbuffer4 = float2(mr.g, mr.b); // metalic roughness
            }
            ENDCG
        }
    }
}