// Полноэкранный эффект "грязное стекло": тот же тёплый "мексиканский" фильтр, что в BroadcastNoise (картинка чуть тусклее
// и теплее), а поверх - едва заметные серые разводы, будто смотришь через запачканное стекло: пятна, следы протирки,
// пылинки. Разводы неподвижны (стекло перед глазами), под ними картинка чуть размыта, а в ярком свете они слегка светлеют.
// Подключается через Full Screen Pass Renderer Feature (PC_Renderer, "DirtyGlass"), материал - DirtyGlass.mat.
Shader "Game/PostProcess/DirtyGlass"
{
    Properties
    {
        [Header(Dimming)]
        _Dim ("Затемнение (0 - нет, 1 - чёрный экран)", Range(0, 1)) = 0.12
        _Desaturation ("Обесцвечивание (0 - нет, 1 - ч/б)", Range(0, 1)) = 0.2

        [Header(Warm tint)]
        _WarmStrength ("Рыжий фильтр: сила (0 - нет)", Range(0, 1)) = 0.5
        _WarmTint ("Рыжий фильтр: оттенок", Color) = (1, 0.75, 0.45, 1)

        [Header(Dirty glass)]
        _SmudgeColor ("Цвет разводов", Color) = (0.62, 0.62, 0.6, 1)
        _SmudgeOpacity ("Заметность разводов (0 - нет)", Range(0, 1)) = 0.3
        _SmudgeCoverage ("Сколько стекла запачкано (0 - чисто, 1 - всё)", Range(0, 1)) = 0.5
        _SmudgeScale ("Размер пятен (больше - мельче)", Range(0.5, 12)) = 4
        _WipeStrength ("Следы протирки (дуги)", Range(0, 1)) = 0.5
        _DustStrength ("Пылинки и точки", Range(0, 1)) = 0.5
        _SmudgeBlur ("Размытие под разводами, пикселей при 1080p", Range(0, 6)) = 2
        _SmudgeGlow ("Рассеяние света грязью, пикселей при 1080p (ореол вокруг светлого)", Range(0, 40)) = 14
        _Seed ("Узор (меняй, чтобы получить другие пятна)", Float) = 7
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
            Name "DirtyGlass"

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
                float4 _SmudgeColor;
                float _SmudgeOpacity;
                float _SmudgeCoverage;
                float _SmudgeScale;
                float _WipeStrength;
                float _DustStrength;
                float _SmudgeBlur;
                float _SmudgeGlow;
                float _Seed;
            CBUFFER_END

            // Целочисленный хеш (pcg3d): одинаковый на всех видеокартах.
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

            // Плавный шум по сетке (значения в узлах, сглаженная интерполяция).
            float ValueNoise(float2 p, uint salt)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                int2 c = int2(i);
                float a = Hash3(uint3(c + int2(0, 0) + 4096, salt)).x;
                float b = Hash3(uint3(c + int2(1, 0) + 4096, salt)).x;
                float d = Hash3(uint3(c + int2(0, 1) + 4096, salt)).x;
                float e = Hash3(uint3(c + int2(1, 1) + 4096, salt)).x;
                return lerp(lerp(a, b, f.x), lerp(d, e, f.x), f.y);
            }

            float Fbm(float2 p, uint salt)
            {
                float sum = 0.0;
                float amp = 0.5;
                for (int k = 0; k < 5; k++)
                {
                    sum += amp * ValueNoise(p, salt + (uint)k * 17u);
                    p = p * 2.03 + 3.1;
                    amp *= 0.5;
                }

                return sum;
            }

            // Маска грязи на стекле в точке экрана: пятна + дуги протирки + пылинки, 0..1.
            float SmudgeMask(float2 uv)
            {
                float aspect = _ScreenParams.x / max(_ScreenParams.y, 1.0);
                float2 p = float2(uv.x * aspect, uv.y) * _SmudgeScale;
                uint salt = (uint)abs(_Seed) * 131u + 7u;

                // Пятна: крупный шум, порог задаёт, сколько стекла запачкано.
                float blot = Fbm(p, salt);
                float threshold = lerp(0.66, 0.40, _SmudgeCoverage);
                float spots = smoothstep(threshold, threshold + 0.16, blot);
                // Внутри пятна грязь неровная - гуще и реже, с мелкой зернистостью.
                spots *= lerp(0.45, 1.0, Fbm(p * 3.7 + 11.0, salt + 5u));
                spots *= lerp(0.8, 1.0, ValueNoise(p * 40.0, salt + 6u));

                // Дуги протирки: концентрические полосы вокруг пары центров, разорванные шумом.
                float2 c1 = float2(0.3 * aspect, 0.65) * _SmudgeScale;
                float2 c2 = float2(0.8 * aspect, 0.25) * _SmudgeScale;
                float warp = Fbm(p * 1.3 + 5.0, salt + 9u) * 2.0;
                float arcs1 = sin(length(p - c1) * 9.0 + warp * 3.0) * 0.5 + 0.5;
                float arcs2 = sin(length(p - c2) * 7.0 + warp * 2.5) * 0.5 + 0.5;
                float arcMask = smoothstep(0.55, 0.85, Fbm(p * 0.8 + 21.0, salt + 13u));
                float wipes = (pow(arcs1, 6.0) + pow(arcs2, 6.0) * 0.7) * arcMask * _WipeStrength;

                // Пылинки: редкие мягкие точки разного размера.
                float2 pixel = uv * _ScreenParams.xy / max(_ScreenParams.y / 1080.0, 0.5);
                float2 cell = floor(pixel / 9.0);
                float3 h = Hash3(uint3(cell + 8192.0, salt + 21u));
                float2 local = frac(pixel / 9.0) - (0.25 + 0.5 * h.xy);
                float dotSize = lerp(0.04, 0.16, h.z);
                float dust = step(h.x, 0.05) * smoothstep(dotSize, 0.0, length(local)) * _DustStrength;

                return saturate(spots * 0.85 + wipes * 0.5 + dust * 0.6);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                half3 lumaWeights = half3(0.2126h, 0.7152h, 0.0722h);

                // Грязь на стекле: под ней картинка чуть размыта (6 точек по кругу).
                float mask = SmudgeMask(uv);
                float radius = _SmudgeBlur * max(_ScreenParams.y / 1080.0, 0.5) * mask;
                half3 blurred = color.rgb;
                if (radius > 0.05)
                {
                    float2 texel = radius / _ScreenParams.xy;
                    half3 sum = color.rgb;
                    [unroll]
                    for (int k = 0; k < 6; k++)
                    {
                        float a = k * 1.0471976;
                        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(cos(a), sin(a)) * texel).rgb;
                    }

                    blurred = sum / 7.0;
                }

                // Грязь рассеивает тот свет, что за ней: картинка под пятном становится мутнее и серее, а вокруг светлого
                // появляется ореол. В темноте рассеивать нечего - там разводов почти не видно.
                float wide = _SmudgeGlow * max(_ScreenParams.y / 1080.0, 0.5);
                half3 scatter = blurred;
                if (mask > 0.01 && wide > 0.5)
                {
                    float2 texel = wide / _ScreenParams.xy;
                    half3 sum = 0;
                    [unroll]
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * 0.7853982 + 0.39;
                        sum += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + float2(cos(a), sin(a)) * texel).rgb;
                    }

                    scatter = sum / 8.0;
                }

                half scatterLuma = dot(scatter, lumaWeights);
                half3 veil = lerp(scatter, scatterLuma.xxx, 0.6h) * _SmudgeColor.rgb * 2.0h + _SmudgeColor.rgb * 0.05h;
                half amount = mask * _SmudgeOpacity;
                color.rgb = lerp(color.rgb, blurred, saturate(mask * 1.2));
                color.rgb = lerp(color.rgb, veil, amount);

                // Потускнение и тёплый фильтр - как в BroadcastNoise.
                half luma = dot(color.rgb, lumaWeights);
                color.rgb = lerp(color.rgb, luma.xxx, _Desaturation);
                half3 warm = _WarmTint.rgb / max(dot(_WarmTint.rgb, lumaWeights), 0.001h);
                color.rgb = lerp(color.rgb, color.rgb * warm, _WarmStrength);
                color.rgb *= 1.0h - _Dim;

                return color;
            }
            ENDHLSL
        }
    }
}
