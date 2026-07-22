Shader "Custom/URP_BlackParticles_Opaque"
{
    Properties
    {
        _BaseMap ("Particle Texture", 2D) = "white" {}
        _BaseColor ("Color (should be black)", Color) = (0,0,0,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.3
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "Queue"="AlphaTest"      // можно оставить "Geometry", но AlphaTest логичнее
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode"="UniversalForward" }

            Cull Back
            ZWrite On      // ВАЖНО: чтобы частицы закрывали модель
            ZTest LEqual
            Blend One Zero // Без прозрачного бленда – как обычная непрозрачная геометрия

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // URP core
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float  _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                // Берём альфу из текстуры, умноженную на цвет
                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 col    = texCol * _BaseColor;

                // Отсекаем "полупрозрачные" части по альфе, чтобы они не мешали depth
                clip(col.a - _Cutoff);

                // Полностью чёрный (или заданный в _BaseColor) непрозрачный цвет
                return half4(col.rgb, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}