// Маска подсветки подбираемых предметов: предмет закрашивается своими цветами - свечения (_PickupHighlightColor,
// в первую цель) и обводки (_PickupOutlineColor, во вторую); их ставит PickupHighlightFeature перед каждым предметом. Глубина сцены проверяется, поэтому то, что закрыто
// стеной, в маску не попадает.
Shader "Hidden/CONTOUR/PickupHighlightMask"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            Name "Mask"
            ZWrite Off
            ZTest LEqual
            Cull Back
            // Те же вершины, что у самого предмета, - чуть ближе к камере, чтобы не мерцать на равной глубине.
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _PickupHighlightColor;
            float4 _PickupOutlineColor;

            // Две маски сразу: цвет свечения и цвет обводки.
            struct FragOutput
            {
                half4 glow : SV_Target0;
                half4 outline : SV_Target1;
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            FragOutput Frag(Varyings input)
            {
                FragOutput output;
                output.glow = half4(_PickupHighlightColor.rgb, 1.0h);
                output.outline = half4(_PickupOutlineColor.rgb, 1.0h);
                return output;
            }
            ENDHLSL
        }
    }
}
