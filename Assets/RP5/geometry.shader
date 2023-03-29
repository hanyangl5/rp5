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
                return o;
            }

            void frag(
                v2f i,
                out float4 GT0 : SV_Target0,
                out float4 GT1 : SV_Target1,
                out float2 GT2 : SV_Target2,
                out float4 GT3 : SV_Target3,
                out float2 GT4 : SV_Target4) {
                float4 color = tex2D(_base_color, i.uv);
                float4 mr = tex2D(_metallic_roughness, i.uv);
                //float4 normal = tex2D(_normal_map, i.uv);
                float3 normal = i.normal;

                GT0 = color;
                GT1 = float4(normal, 0);
                GT2 = float2(1, 1);
                GT3 = float4(0, 0, 1, 1);
                GT4 = float2(mr.g, mr.b); // metalic roughness
            }
            ENDCG
        }
    }
}