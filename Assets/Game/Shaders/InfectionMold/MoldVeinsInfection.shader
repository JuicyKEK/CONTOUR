Shader "Custom/MoldVeinsInfection"
{
    // Шейдер заражённых грибком стен, но в виде "сосудов": по поверхности
    // растекается ветвящаяся сеть вздувшихся прожилок (как настоящие сосуды),
    // а вдоль них наружу от узлов бегут пульсирующие импульсы - имитация
    // движения заражённой жидкости/крови по грибковой сети. Сама сеть сосудов
    // ещё и физически вспучивает поверхность (вершинное смещение + коррекция
    // нормалей), а не является плоской картинкой.
    //
    // Крупная область заражения растёт вместе с _InfectionAmount (0..1) -
    // это тот же механизм спреда, что и в оригинальном MoldInfection.
    Properties
    {
        [Header(Base Wall)]
        _MainTex ("Wall Albedo", 2D) = "white" {}
        _BumpMap ("Wall Normal Map", 2D) = "bump" {}

        [Header(Vein Look)]
        [HDR] _VeinColor ("Vein Color (idle)", Color) = (0.05, 0.0, 0.02, 1)
        [HDR] _VeinPulseColor ("Vein Pulse Glow Color", Color) = (1.5, 0.12, 0.05, 1)
        _VeinWidth ("Vein Line Width", Range(0.01, 0.6)) = 0.15
        _VeinCellScale ("Vein Network Scale (fine, visual)", Range(1, 40)) = 10
        _VeinBranchWarp ("Vein Branch Warp", Range(0, 2)) = 0.5

        [Header(Infection Region)]
        // Крупная органичная область заражения на стене - управляется извне
        // (например MaterialPropertyBlock из скрипта зоны заражения).
        _InfectionAmount ("Infection Amount 0-1", Range(0, 1)) = 0
        _NoiseScale ("Region Noise Scale", Range(1, 40)) = 8
        _NoiseOctaves ("Region Noise Octaves (int 1-4)", Range(1, 4)) = 3
        _PatternWarp ("Region Pattern Warp", Range(0, 2)) = 0.6
        _EdgeWidth ("Region Edge Width", Range(0.01, 0.5)) = 0.15

        [Header(Pulse Flow)]
        // По сосудам бегут наружу от узлов сети компактные "шарики" - как
        // капли крови/жидкости, а не сплошное свечение всей сети разом.
        _FlowSpeed ("Flow Speed", Range(0, 5)) = 1.5
        // Чем выше значение, тем короче путь, который шарик проезжает по
        // сосуду до того, как закольцеваться и появиться заново у центра.
        _FlowFrequency ("Flow Loop Distance (inverse)", Range(1, 20)) = 6
        _FlowSharpness ("Flow Ball Edge Sharpness", Range(1, 8)) = 3

        [Header(Breathing)]
        // Медленное общее "дыхание" яркости заражённой области.
        _PulseSpeed ("Breathing Speed", Range(0, 5)) = 1.2
        _PulseStrength ("Breathing Strength", Range(0, 1)) = 0.25

        [Header(Crawl Animation)]
        // Медленное "ползучее" смещение узора области заражения во времени.
        _CrawlSpeed ("Crawl Speed", Range(0, 2)) = 0.15
        _CrawlAmount ("Crawl Amount", Range(0, 2)) = 0.35

        [Header(Vessel Bulge Vertex Displacement)]
        // Настоящее вздутие сосудов геометрией (не только картинка!). Для
        // гладкого силуэта нужна мешам с достаточным числом вершин -
        // на редкой сетке вздутия будут более угловатыми.
        _BulgeAmount ("Bulge Height (object units)", Range(0, 0.2)) = 0.03
        _BulgeScale ("Bulge Vein Scale (coarse)", Range(1, 20)) = 5
        _BulgeWidth ("Bulge Vein Width", Range(0.05, 0.8)) = 0.35
        _BulgeNormalStrength ("Bulge Normal Strength", Range(0, 5)) = 1.5
        // Доп. подъём вершины именно там, где сейчас проходит бегущий по
        // сосуду импульс - "волна", катящаяся вместе с движением жидкости.
        _FlowBulgeAmount ("Flow Pulse Extra Bulge (object units)", Range(0, 0.1)) = 0.02

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
            Name "VeinsForward"
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "MoldVeinsCommon.hlsl"

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
                float2 displaceUV : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 normalWS   : TEXCOORD3;
                float3 tangentWS  : TEXCOORD4;
                float3 bitangentWS: TEXCOORD5;
                float fogCoord    : TEXCOORD6;
                float4 shadowCoord: TEXCOORD7;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            Varyings vert(Attributes input)
            {
                Varyings output;

                // ---- Вздутие сосудов (реальное вершинное смещение) ----
                // Берём "сырые" UV меша (без TRANSFORM_TEX/tiling), чтобы узор
                // вздутия идеально совпадал с видимым узором сосудов в frag(),
                // независимо от настроек Tiling/Offset текстуры стены.
                float2 displaceUV = input.uv;

                const float kEps = 0.002;
                float h  = ComputeBulgeHeight(displaceUV);
                float hu = ComputeBulgeHeight(displaceUV + float2(kEps, 0.0));
                float hv = ComputeBulgeHeight(displaceUV + float2(0.0, kEps));
                float dHdu = (hu - h) / kEps;
                float dHdv = (hv - h) / kEps;

                float3 normalOS = normalize(input.normalOS);
                float3 tangentOS = normalize(input.tangentOS.xyz);
                float3 bitangentOS = cross(normalOS, tangentOS) * input.tangentOS.w;

                // Смещаем вершину вдоль нормали на уже готовую высоту вздутия
                // (включает и статическую "трубу" сосуда, и локальный подъём
                // в месте прохождения бегущего импульса).
                float3 displacedPositionOS = input.positionOS.xyz + normalOS * h;

                // Наклоняем геометрическую нормаль в сторону склона вздутия -
                // как bump-mapping, но применённый к самой нормали. Это делает
                // вздутия визуально куда объёмнее даже на не очень плотной сетке.
                float3 bumpedNormalOS = normalize(normalOS - (dHdu * tangentOS + dHdv * bitangentOS) * _BulgeNormalStrength);

                VertexPositionInputs posInputs = GetVertexPositionInputs(displacedPositionOS);
                VertexNormalInputs normInputs = GetVertexNormalInputs(bumpedNormalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.displaceUV = displaceUV;
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

                float2 uv = input.displaceUV;

                // ---- Крупная область заражения ----
                float regionMask = ComputeInfectionRegionMask(uv);

                // ---- Сеть сосудов внутри заражённой области ----
                float f1;
                float2 cellId;
                float veinMask = ComputeVeinNetwork(uv, _VeinCellScale, _VeinWidth, f1, cellId) * regionMask;

                // ---- Шарик, бегущий по сосудам наружу от узлов сети ----
                float flow = ComputeFlowPulse(f1, cellId, _VeinWidth) * veinMask;

                // ---- Общее "дыхание" яркости заражённой области ----
                float breathing = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseStrength * regionMask;

                // ---- Освещение ----
                Light mainLight = GetMainLight(input.shadowCoord);
                float mainNdotL = saturate(dot(normalWS, mainLight.direction));
                half3 lighting = mainLight.color * (mainLight.shadowAttenuation * mainNdotL);

                // Ambient/SH (skybox/light probes) - см. _AmbientContribution.
                lighting += SampleSH(normalWS) * _AmbientContribution;

                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                // LIGHT_LOOP_BEGIN/END сами переключаются между обычным
                // per-object циклом и обходом кластеров в Forward+.
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

                half3 veinIdleColor = _VeinColor.rgb * breathing;
                half3 veinGlow = _VeinPulseColor.rgb * flow;

                half3 finalVein = (veinIdleColor + veinGlow) * lighting;
                half3 finalWall = wallAlbedo.rgb * lighting;

                half3 finalColor = lerp(finalWall, finalVein, veinMask);

                finalColor = MixFog(finalColor, input.fogCoord);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        // Тени и depth-only пассы пишем сами (минимальные, без UsePass из Lit -
        // см. пояснение в MoldInfection.shader про конфликт keyword-пространств).
        // Обязательно применяем то же самое вершинное вздутие сосудов, что и в
        // forward-пассе - иначе тень/silhouette будут не совпадать с видимой
        // (вспученной) геометрией объекта.
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "MoldVeinsCommon.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 normalOS = normalize(input.normalOS);
                float h = ComputeBulgeHeight(input.uv);
                float3 displacedPositionOS = input.positionOS.xyz + normalOS * h;

                float3 positionWS = TransformObjectToWorld(displacedPositionOS);
                float3 normalWS = TransformObjectToWorldNormal(normalOS);

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

            #include "MoldVeinsCommon.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 normalOS = normalize(input.normalOS);
                float h = ComputeBulgeHeight(input.uv);
                float3 displacedPositionOS = input.positionOS.xyz + normalOS * h;

                output.positionCS = TransformObjectToHClip(displacedPositionOS);
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








