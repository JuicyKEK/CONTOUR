Shader "Custom/WetFlesh"
{
    // Шейдер мокрой, только что отделённой плоти (плоть на маршах 6-го этажа, перепонка над головой).
    // Текстура и карта нормалей накладываются по мировым координатам с трёх сторон (трипланар) - без развёртки
    // и без швов; поверх плоти - тонкая мокрая плёнка (второй глянцевый слой), блики медленно ползут;
    // по стенкам стекают тёмно-красные подтёки, по ним изредка бегут капли.
    // Для моделей со своей развёрткой (голова) - текстура по развёртке (_UvAlbedo), направление стекания - любое.
    Properties
    {
        [Header(Flesh)]
        _BaseMap ("Albedo (tileable)", 2D) = "white" {}
        [Toggle(_UV_ALBEDO)] _UvAlbedo ("Albedo by Model UV (not triplanar)", Float) = 0
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        [Normal] _NormalMap ("Normal Map (tileable)", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 3)) = 1.2
        _TileMeters ("Texture Repeat, m", Range(0.2, 5)) = 1.6
        _TriplanarSharpness ("Triplanar Sharpness", Range(1, 16)) = 6

        [Header(Wet Film)]
        _Smoothness ("Flesh Smoothness", Range(0, 1)) = 0.55
        _WetSmoothness ("Wet Film Smoothness", Range(0, 1)) = 0.93
        _WetAmount ("Wet Film Amount", Range(0, 1)) = 0.85
        _CrawlSpeed ("Highlight Crawl Speed", Range(0, 0.2)) = 0.02
        _CrawlScale ("Highlight Crawl Scale", Range(0.5, 6)) = 2.3

        [Header(Drips)]
        _DripColor ("Drip Color", Color) = (0.11, 0.007, 0.009, 1)
        _DripDown ("Drip Flow Direction (world)", Vector) = (0, -1, 0, 0)
        _DripColumns ("Drip Columns per m", Range(0.5, 12)) = 3
        _DripChance ("Drip Chance per Column", Range(0, 1)) = 0.65
        _DripWidth ("Drip Width (of a column)", Range(0.02, 0.5)) = 0.18
        _DripSpeed ("Drop Speed, m/s", Range(0, 2)) = 0.35
        _DripRepeat ("Drop Repeat, m", Range(0.5, 10)) = 3.0
        _DripStain ("Stain Strength", Range(0, 1)) = 0.85
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _NormalStrength;
            float _TileMeters;
            half _TriplanarSharpness;
            half _Smoothness;
            half _WetSmoothness;
            half _WetAmount;
            float _CrawlSpeed;
            float _CrawlScale;
            half4 _DripColor;
            float4 _DripDown;
            float _DripColumns;
            half _DripChance;
            half _DripWidth;
            float _DripSpeed;
            float _DripRepeat;
            half _DripStain;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            // Второй глянцевый слой (мокрая плёнка) - средствами освещения URP.
            #define _CLEARCOAT 1
            #pragma shader_feature_local _UV_ALBEDO

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);    SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);  SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float3 normalOS         : NORMAL;
                float2 uv               : TEXCOORD0;
                float2 staticLightmapUV : TEXCOORD1;
                float2 dynamicLightmapUV: TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                half4 fogFactorAndVertexLight : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 3);
            #ifdef DYNAMICLIGHTMAP_ON
                float2 dynamicLightmapUV : TEXCOORD4;
            #endif
            #ifdef USE_APV_PROBE_OCCLUSION
                float4 probeOcclusion : TEXCOORD5;
            #endif
                float2 uv           : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);

                half3 vertexLight = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
                output.fogFactorAndVertexLight = half4(ComputeFogFactor(positionInputs.positionCS.z), vertexLight);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
            #ifdef DYNAMICLIGHTMAP_ON
                output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
            #endif
                OUTPUT_SH4(positionInputs.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(positionInputs.positionWS), output.vertexSH, output.probeOcclusion);
                return output;
            }

            float Hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float Noise11(float x)
            {
                float i = floor(x);
                float f = frac(x);
                float u = f * f * (3.0 - 2.0 * f);
                return lerp(Hash11(i), Hash11(i + 1.0), u);
            }

            // Трипланарные веса по нормали.
            float3 TriplanarWeights(float3 n)
            {
                float3 w = pow(abs(n), _TriplanarSharpness);
                return w / max(w.x + w.y + w.z, 1e-5);
            }

            half3 TriplanarAlbedo(float3 p, float3 w)
            {
                half3 x = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.zy).rgb;
                half3 y = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xz).rgb;
                half3 z = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, p.xy).rgb;
                return x * w.x + y * w.y + z * w.z;
            }

            // Трипланарная карта нормалей (смешивание "whiteout" с учётом стороны оси).
            float3 TriplanarNormal(float3 p, float3 n, float3 w, float strength)
            {
                float3 axisSign = n < 0 ? -1 : 1;
                float2 uvX = p.zy; uvX.x *= axisSign.x;
                float2 uvY = p.xz; uvY.x *= axisSign.y;
                float2 uvZ = p.xy; uvZ.x *= -axisSign.z;
                float3 tX = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvX), strength);
                float3 tY = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvY), strength);
                float3 tZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvZ), strength);
                tX.x *= axisSign.x;
                tY.x *= axisSign.y;
                tZ.x *= -axisSign.z;
                tX = float3(tX.xy + n.zy, abs(tX.z) * n.x);
                tY = float3(tY.xy + n.xz, abs(tY.z) * n.y);
                tZ = float3(tZ.xy + n.xy, abs(tZ.z) * n.z);
                return normalize(tX.zyx * w.x + tY.xzy * w.y + tZ.xyz * w.z);
            }

            // Подтёки: по стенкам - полосы вдоль направления стекания в случайных "столбцах", по ним бегут капли.
            half DripMask(float3 positionWS, float3 n)
            {
                float3 down = dot(_DripDown.xyz, _DripDown.xyz) > 1e-6 ? normalize(_DripDown.xyz) : float3(0, -1, 0);
                half wall = smoothstep(0.25, 0.65, 1.0 - abs(dot(n, down)));
                if (wall <= 0.0) return 0;
                // Поперёк стекания - одна из двух осей, лучше лежащая на поверхности; вдоль - высота против стекания.
                float3 axisA = normalize(cross(down, abs(down.z) < 0.9 ? float3(0, 0, 1) : float3(1, 0, 0)));
                float3 axisB = cross(down, axisA);
                float u = (abs(dot(n, axisA)) > abs(dot(n, axisB)) ? dot(positionWS, axisB) : dot(positionWS, axisA)) * _DripColumns;
                float height = -dot(positionWS, down);
                float column = floor(u);
                float within = frac(u);
                if (Hash11(column * 9.13 + 3.7) > _DripChance) return 0;
                float centre = 0.25 + 0.5 * Hash11(column * 1.71);
                float width = _DripWidth * (0.6 + 0.8 * Hash11(column * 3.17));

                // Капля: голова внизу, хвост вверх; бежит вниз со своей скоростью.
                float speed = _DripSpeed * (0.5 + Hash11(column * 5.31));
                float phase = frac((height + _Time.y * speed) / _DripRepeat + Hash11(column * 7.77));
                half drop = smoothstep(0.0, 0.015, phase) * (1.0 - smoothstep(0.015, 0.35, phase));
                // Засохший/текущий след: пятнами по высоте.
                half stain = smoothstep(0.3, 0.6, Noise11(height * 0.9 + column * 13.1)) * _DripStain;

                float halfWidth = width * (1.0 + drop * 0.8) * 0.5;
                half across = 1.0 - smoothstep(halfWidth * 0.5, halfWidth, abs(within - centre));
                return saturate(max(stain, drop)) * across * wall;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 geomNormal = normalize(input.normalWS);
                float3 p = input.positionWS / _TileMeters;
                float3 w = TriplanarWeights(geomNormal);

            #if defined(_UV_ALBEDO)
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb * _BaseColor.rgb;
            #else
                half3 albedo = TriplanarAlbedo(p, w) * _BaseColor.rgb;
            #endif
                float3 normalWS = TriplanarNormal(p, geomNormal, w, _NormalStrength);
                // Второй, медленно ползущий слой нормалей - блики "живут".
                float3 drift = float3(0.37, -1.0, 0.21) * _Time.y * _CrawlSpeed;
                float3 crawl = TriplanarNormal(p * _CrawlScale + drift, geomNormal, w, _NormalStrength * 0.6);
                normalWS = normalize(normalWS + crawl - geomNormal);

                half drip = DripMask(input.positionWS, geomNormal);
                albedo = lerp(albedo, _DripColor.rgb, drip * 0.9);
                normalWS = normalize(lerp(normalWS, geomNormal, drip * 0.8));   // жидкость гладкая

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #else
                inputData.shadowCoord = float4(0, 0, 0, 0);
            #endif
                inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
            #if defined(DYNAMICLIGHTMAP_ON)
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
            #elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
                inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS,
                                              inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion, inputData.shadowMask);
            #else
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
            #endif

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.alpha = 1;
                surface.metallic = 0;
                surface.specular = 0;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                // Гладкость: плоть - пятнами, подтёки - как жидкость; сверху - мокрая плёнка.
                half spots = Noise11(dot(input.positionWS, float3(2.3, 1.7, 3.1)));
                surface.smoothness = lerp(_Smoothness * (0.75 + 0.5 * spots), _WetSmoothness, drip);
                surface.clearCoatMask = saturate(_WetAmount * (0.7 + 0.3 * spots) + drip);
                surface.clearCoatSmoothness = _WetSmoothness;

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        // Тени и глубина - свои минимальные пассы (как в остальных шейдерах проекта).
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirectionWS = normalize(_LightPosition - positionWS);
            #else
                float3 lightDirectionWS = _LightDirection;
            #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                return ApplyShadowClamping(positionCS);
            }

            half4 ShadowFrag() : SV_Target
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
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                return TransformObjectToHClip(input.positionOS.xyz);
            }

            half DepthFrag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings NormalsVert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 NormalsFrag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
                float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
                return half4(PackFloat2To888(remapped), 0.0);
            #else
                return half4(normalWS, 0.0);
            #endif
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
