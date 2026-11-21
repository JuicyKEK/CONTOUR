// Подсветка подбираемых предметов по маскам (PickupHighlightMask): размытие маски свечения (проходы 0, 1)
// и сложение с картинкой камеры (проход 2): обводка цветом обводки предмета (из маски обводки), мягкое свечение
// вокруг и лёгкая подкраска самого предмета цветом свечения. Параметры ставит PickupHighlightFeature.
Shader "Hidden/CONTOUR/PickupHighlightComposite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        // Размер пикселя картинки камеры в UV (1/ширина, 1/высота).
        float4 _HighlightTexel;
        // Шаг размытия, пиксели картинки камеры.
        float _BlurStep;
        // Толщина обводки, пиксели.
        float _OutlineWidth;
        float _OutlineIntensity;
        float _GlowIntensity;
        float _Fill;

        TEXTURE2D_X(_PickupGlowTex);
        // Маска с цветом обводки (того же размера, что и маска свечения в _BlitTexture).
        TEXTURE2D_X(_PickupOutlineTex);

        half4 SampleSource(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, uv, 0);
        }

        half4 SampleOutline(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X_LOD(_PickupOutlineTex, sampler_LinearClamp, uv, 0);
        }

        // Гауссово размытие по одной оси: 9 выборок с шагом _BlurStep.
        half4 Blur(float2 uv, float2 direction)
        {
            const half weights[5] = { 0.227027h, 0.1945946h, 0.1216216h, 0.054054h, 0.016216h };
            float2 stepUV = direction * _HighlightTexel.xy * _BlurStep;
            half4 color = SampleSource(uv) * weights[0];

            UNITY_UNROLL
            for (int i = 1; i < 5; i++)
            {
                color += SampleSource(uv + stepUV * i) * weights[i];
                color += SampleSource(uv - stepUV * i) * weights[i];
            }

            return color;
        }
        ENDHLSL

        Pass
        {
            Name "BlurHorizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return Blur(input.texcoord, float2(1.0, 0.0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "BlurVertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                return Blur(input.texcoord, float2(0.0, 1.0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Composite"
            // Прибавляется к картинке камеры.
            Blend One One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 mask = SampleSource(uv);

                // Обводка: самый закрашенный сосед в маске обводки на расстоянии толщины обводки и её половины
                // (16 направлений); цвет - его цвет обводки.
                half4 nearest = SampleOutline(uv);
                float2 radius = _HighlightTexel.xy * _OutlineWidth;

                UNITY_UNROLL
                for (int k = 0; k < 16; k++)
                {
                    float angle = k * (PI / 8.0);
                    float2 offset = float2(cos(angle), sin(angle)) * radius;
                    half4 outer = SampleOutline(uv + offset);
                    half4 inner = SampleOutline(uv + offset * 0.5);
                    nearest = outer.a > nearest.a ? outer : nearest;
                    nearest = inner.a > nearest.a ? inner : nearest;
                }

                half outline = saturate(nearest.a - mask.a);

                // Свечение: размытая маска снаружи предмета. В размытой маске цвет умножен на покрытие.
                half4 glow = SAMPLE_TEXTURE2D_X_LOD(_PickupGlowTex, sampler_LinearClamp, uv, 0);
                half3 glowColor = glow.a > 0.0001h ? glow.rgb / glow.a : half3(0.0h, 0.0h, 0.0h);
                half glowAmount = glow.a * (1.0h - mask.a);

                half3 color = nearest.rgb * outline * _OutlineIntensity
                    + glowColor * glowAmount * _GlowIntensity
                    + mask.rgb * _Fill;

                return half4(color, 0.0h);
            }
            ENDHLSL
        }
    }
}
