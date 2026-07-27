Shader "Custom/MoldInfection"
{
    // Шейдер чёрного пульсирующего грибка для стен, заражённых по сюжету.
    // Базовая текстура стены остаётся видна там, где заражения ещё нет,
    // а по мере роста _InfectionAmount (0..1) по поверхности органично
    // расползаются тёмные пятна плесени с мягким влажным краем и лёгкой пульсацией.
    Properties
    {
        [Header(Base Wall)]
        _MainTex ("Wall Albedo", 2D) = "white" {}
        _BumpMap ("Wall Normal Map", 2D) = "bump" {}

        [Header(Mold Look)]
        [HDR] _MoldColor ("Mold Color", Color) = (0.02, 0.02, 0.02, 1)
        [HDR] _MoldEdgeColor ("Mold Edge Glow", Color) = (0.15, 0.05, 0.1, 1)
        _EdgeWidth ("Edge Width", Range(0.01, 0.5)) = 0.12
        _Roughness ("Mold Roughness", Range(0, 1)) = 0.9

        [Header(Pattern)]
        _NoiseScale ("Noise Scale", Range(1, 40)) = 12
        _NoiseOctaves ("Noise Octaves (int 1-4)", Range(1, 4)) = 3
        _PatternWarp ("Pattern Warp", Range(0, 2)) = 0.6

        [Header(Infection Amount)]
        // Управляется извне (например MaterialPropertyBlock из скрипта зоны заражения).
        _InfectionAmount ("Infection Amount 0-1", Range(0, 1)) = 0

        [Header(Pulse Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.2
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.25

        [Header(Crawl Animation)]
        // Медленное "ползучее" движение самого паттерна грибка во времени (не путать с пульсацией яркости).
        _CrawlSpeed ("Crawl Speed", Range(0, 2)) = 0.15
        _CrawlAmount ("Crawl Amount", Range(0, 2)) = 0.35

        [Header(Ambient)]
        // Вклад окружающего освещения (Environment Ambient Color / Light Probes).
        // 0 = полностью убрать зависимость яркости объекта от Lighting > Environment > Ambient Color.
        _AmbientContribution ("Ambient Contribution", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "MoldForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

                struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 tangentWS  : TEXCOORD3;
                float3 bitangentWS: TEXCOORD4;
                float fogCoord    : TEXCOORD5;
                float4 shadowCoord: TEXCOORD6;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MoldColor;
                float4 _MoldEdgeColor;
                float _EdgeWidth;
                float _Roughness;
                float _NoiseScale;
                float _NoiseOctaves;
                float _PatternWarp;
                float _InfectionAmount;
                float _PulseSpeed;
                float _PulseStrength;
                float _CrawlSpeed;
                float _CrawlAmount;
                float _AmbientContribution;
            CBUFFER_END

            // ---- Процедурный value-noise (как в DarknessConsume) ----
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Фрактальный шум (fBm) - несколько октав для органичного "грибкового" паттерна.
            float fbm(float2 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    if (i >= octaves) break;
                    value += amplitude * valueNoise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }

                return value;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                output.fogCoord = ComputeFogFactor(posInputs.positionCS.z);
                output.shadowCoord = GetShadowCoord(posInputs);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // ---- Базовая стена ----
                half4 wallAlbedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv));
                float3x3 tangentToWorld = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                float3 normalWS = normalize(mul(normalTS, tangentToWorld));

                // ---- Паттерн грибка ----
                // Небольшой warp (искажение координат вторым шумом) даёт более "живой", неровный узор,
                // а не просто круглые пятна - похоже на реальные колонии плесени.
                // Дополнительно warp медленно смещается во времени (crawl) - паттерн как будто
                // ползёт и извивается, а не просто мигает на месте.
                float2 crawlOffset = float2(
                    valueNoise(float2(_Time.y * _CrawlSpeed, 3.1)),
                    valueNoise(float2(1.7, _Time.y * _CrawlSpeed))
                ) * _CrawlAmount;

                float2 warpUV = input.uv * _NoiseScale;
                float2 warp = float2(
                    valueNoise(warpUV * 0.5 + 7.3 + crawlOffset),
                    valueNoise(warpUV * 0.5 - 4.1 + crawlOffset)
                ) * _PatternWarp;

                float noiseVal = fbm(warpUV + warp + crawlOffset, (int)_NoiseOctaves);

                // Порог появления пятен растёт вместе со степенью заражения зоны.
                // При _InfectionAmount = 0 порог максимален - пятен почти нет (либо совсем нет).
                // При _InfectionAmount = 1 порог минимален - грибок покрывает почти всю поверхность.
                float threshold = lerp(1.05, -0.05, _InfectionAmount);

                float moldMask = smoothstep(threshold - _EdgeWidth, threshold + _EdgeWidth, noiseVal);
                float edgeMask = smoothstep(threshold - _EdgeWidth, threshold, noiseVal)
                                - smoothstep(threshold, threshold + _EdgeWidth * 0.6, noiseVal);

                // ---- Пульсация ----
                // Грибок слегка "дышит" - яркость и вклад его цвета плавно колеблются во времени.
                // На чистую стену пульсация не влияет.
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseStrength * moldMask;

                // ---- Освещение ----
                // GetMainLight() отвечает только за один Directional Light сцены. Если он выключен
                // или отсутствует, свет от точечных/спот-источников считается отдельно как
                // "additional lights", а без каких-либо источников должно оставаться хотя бы
                // ambient/SH-освещение - иначе поверхность становится полностью чёрной.
                Light mainLight = GetMainLight(input.shadowCoord);
                float mainNdotL = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * (mainLight.shadowAttenuation * mainNdotL);

                // Ambient/SH-освещение окружения (skybox/light probes) - даёт базовую подсветку
                // даже когда в сцене вообще нет активных источников света. Управляется
                // _AmbientContribution: поставьте 0, чтобы объект вообще не зависел от
                // Lighting > Environment > Environment Lighting > Ambient Color.
                lighting += SampleSH(normalWS) * _AmbientContribution;

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                // LIGHT_LOOP_BEGIN/END сами переключаются между обычным per-object циклом
                // и обходом кластеров в Forward+ (USE_CLUSTER_LIGHT_LOOP). Важно: макрос
                // жёстко обращается к переменной с именем "inputData", поэтому она должна
                // называться именно так и содержать positionWS/normalizedScreenSpaceUV.
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                    float additionalNdotL = saturate(dot(normalWS, additionalLight.direction));
                    lighting += additionalLight.color * additionalLight.distanceAttenuation
                        * additionalLight.shadowAttenuation * additionalNdotL;
                LIGHT_LOOP_END
                #endif

                half3 moldColor = _MoldColor.rgb * pulse;
                half3 edgeGlow = _MoldEdgeColor.rgb * edgeMask * pulse;

                half3 finalMold = (moldColor + edgeGlow) * lighting;
                half3 finalWall = wallAlbedo.rgb * lighting;

                half3 finalColor = lerp(finalWall, finalMold, moldMask);

                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Тени и depth-only пассы пишем сами (минимальные), а не через UsePass из Lit -
        // заимствование пассов из другого шейдера приводит к конфликту keyword-пространств
        // ("State comes from an incompatible keyword space"), т.к. набор multi_compile
        // ключевых слов и порядок их объявления у Lit не совпадает с нашим forward-пассом.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
