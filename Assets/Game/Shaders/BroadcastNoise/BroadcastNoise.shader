// Полноэкранный эффект "конец вещания": картинка чуть тусклее и теплее (рыжий "мексиканский" фильтр, как в кино),
// поверх - редкие серо-рыжие точки, которые каждый кадр вспыхивают в новых местах, как "снег" на телевизоре после конца эфира.
// Подключается через Full Screen Pass Renderer Feature (PC_Renderer), материал - BroadcastNoise.mat.
Shader "Game/PostProcess/BroadcastNoise"
{
    Properties
    {
        [Header(Dimming)]
        _Dim ("Затемнение (0 - нет, 1 - чёрный экран)", Range(0, 1)) = 0.12
        _Desaturation ("Обесцвечивание (0 - нет, 1 - ч/б)", Range(0, 1)) = 0.2

        [Header(Warm tint)]
        _WarmStrength ("Рыжий фильтр: сила (0 - нет)", Range(0, 1)) = 0.5
        _WarmTint ("Рыжий фильтр: оттенок", Color) = (1, 0.75, 0.45, 1)

        [Header(Noise dots)]
        _NoiseDensity ("Доля ячеек с точкой", Range(0, 0.05)) = 0.006
        _NoiseStrength ("Заметность точек", Range(0, 1)) = 0.14
        _NoiseSize ("Размер точки, пикселей при 1080p", Range(1, 8)) = 2
        _NoiseStretch ("Вытянутость по горизонтали", Range(1, 4)) = 1.7
        _NoiseFps ("Смен узора в секунду", Range(1, 60)) = 30
        _NoiseClumping ("Скученность (пятна гуще и реже)", Range(0, 1)) = 0.5
        _NoiseColorGrey ("Цвет серых точек", Color) = (0.62, 0.62, 0.6, 1)
        _NoiseColorRust ("Цвет рыжих точек", Color) = (0.82, 0.45, 0.18, 1)
        _NoiseRustShare ("Доля рыжих точек", Range(0, 1)) = 0.45
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "BroadcastNoise"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Dim;
                float _Desaturation;
                float _WarmStrength;
                float4 _WarmTint;
                float _NoiseDensity;
                float _NoiseStrength;
                float _NoiseSize;
                float _NoiseStretch;
                float _NoiseFps;
                float _NoiseClumping;
                float4 _NoiseColorGrey;
                float4 _NoiseColorRust;
                float _NoiseRustShare;
            CBUFFER_END

            // Целочисленный хеш (pcg3d): одинаковый на всех видеокартах, без полос и повторов.
            float3 Hash3(uint3 v)
            {
                v = v * 1664525u + 1013904223u;
                v.x += v.y * v.z;
                v.y += v.z * v.x;
                v.z += v.x * v.y;
                v ^= v >> 16u;
                v.x += v.y * v.z;
                v.y += v.z * v.x;
                v.z += v.x * v.y;
                return float3(v) * (1.0 / 4294967296.0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Потускнение: немного убираем цвет.
                half3 lumaWeights = half3(0.2126h, 0.7152h, 0.0722h);
                half luma = dot(color.rgb, lumaWeights);
                color.rgb = lerp(color.rgb, luma.xxx, _Desaturation);

                // Рыжий фильтр: цвет уводится к оттенку, яркость оттенок не меняет (он нормирован по яркости).
                half3 warm = _WarmTint.rgb / max(dot(_WarmTint.rgb, lumaWeights), 0.001h);
                color.rgb = lerp(color.rgb, color.rgb * warm, _WarmStrength);

                // И немного убираем яркость.
                color.rgb *= 1.0h - _Dim;

                // Точки живут в ячейках сетки; размер задан для 1080p и масштабируется под разрешение.
                float scale = max(_ScreenParams.y / 1080.0, 0.5);
                float2 cellSize = float2(_NoiseSize * _NoiseStretch, _NoiseSize) * scale;
                float2 pixel = uv * _ScreenParams.xy;
                float2 cellPos = pixel / cellSize;
                uint2 cell = (uint2)floor(cellPos);

                // Узор меняется скачком _NoiseFps раз в секунду - как кадры эфирного "снега".
                uint frame = (uint)floor(_Time.y * _NoiseFps);
                float3 h = Hash3(uint3(cell, frame));

                // Крупные пятна, где точек больше или меньше: "снег" не ровный, а клочьями.
                float3 patch = Hash3(uint3(cell / 12u, frame + 7919u));
                float density = _NoiseDensity * lerp(1.0, patch.x * 2.0, _NoiseClumping);

                float isDot = step(h.x, density);
                float brightness = lerp(0.35, 1.0, h.y);

                // Мягкий вытянутый штрих внутри ячейки, без резкой квадратной границы.
                float2 local = frac(cellPos) * 2.0 - 1.0;
                float shape = smoothstep(0.0, 0.7, saturate(1.0 - dot(local, local)));

                half3 dotColor = lerp(_NoiseColorGrey.rgb, _NoiseColorRust.rgb, step(h.z, _NoiseRustShare));
                float alpha = isDot * brightness * shape * _NoiseStrength;
                color.rgb = lerp(color.rgb, dotColor, alpha);

                return color;
            }
            ENDHLSL
        }
    }
}
