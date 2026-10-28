Shader "Custom/InfectedConcrete"
{
    // Заражённый бетон: на бетоне вылезли чёрные вены, по ним бегут сгустки чёрной, похожей на кровь жидкости;
    // поверхность вздувается под венами и под сгустками (сдвиг вершин - нужна достаточно частая сетка, ~3-5 см).
    // Под венами - чёрная плесень (текстура), местами - пятна кожи. Текстуры накладываются с трёх сторон
    // по координатам объекта (развёртка не нужна).
    // _InfectionAmount (0..1) - сила заражения: вены тоньше и пропадают, плесень сходит. Его может вести
    // MoldInfectionVisual от зоны заражения (то же имя свойства).
    // Sway - для щупалец: свободные концы извиваются (цвет вершины R - насколько точка свободна, G - фаза щупальца);
    // у стен выключено.
    Properties
    {
        [Header(Concrete)]
        _BaseMap ("Concrete Albedo", 2D) = "white" {}
        _BaseColor ("Concrete Tint", Color) = (0.95, 0.94, 0.92, 1)
        [Normal] _NormalMap ("Concrete Normal", 2D) = "bump" {}
        _NormalStrength ("Concrete Normal Strength", Range(0, 2)) = 1
        _TileMeters ("Texture Repeat, m", Range(0.1, 4)) = 1.2
        _TriplanarSharpness ("Triplanar Sharpness", Range(1, 16)) = 6
        _Smoothness ("Concrete Smoothness", Range(0, 1)) = 0.08

        [Header(Skin Patches)]
        _SkinMap ("Skin Albedo", 2D) = "white" {}
        [Normal] _SkinNormal ("Skin Normal", 2D) = "bump" {}
        _SkinTint ("Skin Tint", Color) = (1, 1, 1, 1)
        _SkinAmount ("Skin Amount", Range(0, 1)) = 0.12
        _SkinScale ("Skin Patch Size (patches per m)", Range(0.5, 10)) = 2.5
        _SkinRepeat ("Skin Texture Repeat, m", Range(0.1, 4)) = 0.8
        _SkinSmoothness ("Skin Smoothness", Range(0, 1)) = 0.6

        [Header(Black Mold under the Veins)]
        _MoldMap ("Black Mold (RGBA)", 2D) = "black" {}
        _MoldColor ("Mold Tint", Color) = (1, 1, 1, 1)
        _MoldRepeat ("Mold Texture Repeat, m", Range(0.1, 4)) = 0.7
        _MoldSpread ("Mold Spread around Veins", Range(0, 1)) = 0.7

        [Header(Veins)]
        _InfectionAmount ("Infection Amount 0-1", Range(0, 1)) = 1
        _VeinScale ("Vein Cells per m", Range(0.5, 12)) = 3
        _VeinWidth ("Vein Width", Range(0.01, 0.3)) = 0.07
        _VeinWarp ("Vein Warp", Range(0, 2)) = 1.1
        _VeinBranches ("Thin Branches", Range(0, 1)) = 0.8
        _VeinSeed ("Seed", Float) = 0
        _VeinColor ("Vein Color", Color) = (0.015, 0.012, 0.012, 1)
        _VeinSmoothness ("Vein Smoothness", Range(0, 1)) = 0.6
        _VeinHeight ("Vein Bulge, m", Range(0, 0.05)) = 0.012

        [Header(Black Blood in the Veins)]
        _FlowColor ("Flow Color", Color) = (0.05, 0.004, 0.006, 1)
        _FlowSpeed ("Flow Speed", Range(0, 3)) = 0.6
        _FlowFrequency ("Clots per Cell", Range(0.5, 12)) = 3
        _FlowHeight ("Clot Bulge, m", Range(0, 0.06)) = 0.018
        _FlowWiden ("Vein Widening under a Clot", Range(0, 2)) = 0.8
        _PulseSpeed ("Breathing Speed", Range(0, 5)) = 1.3
        _PulseStrength ("Breathing Strength", Range(0, 1)) = 0.25
        _DetailBump ("Per-pixel Vein Relief", Range(0, 2)) = 0.6

        [Header(Tentacle Sway)]
        [Toggle(_TENTACLE_SWAY)] _TentacleSway ("Sway (vertex color R = free end, G = phase)", Float) = 0
        _SwayAmount ("Sway, m", Range(0, 0.3)) = 0.08
        _SwaySpeed ("Sway Speed", Range(0, 4)) = 0.9
        _SwayCurl ("Curl along the Tentacle", Range(0, 12)) = 4
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
            half4 _SkinTint;
            half _SkinAmount;
            float _SkinScale;
            float _SkinRepeat;
            half _SkinSmoothness;
            half4 _MoldColor;
            float _MoldRepeat;
            half _MoldSpread;
            half _InfectionAmount;
            float _VeinScale;
            float _VeinWidth;
            float _VeinWarp;
            half _VeinBranches;
            float _VeinSeed;
            half4 _VeinColor;
            half _VeinSmoothness;
            float _VeinHeight;
            half4 _FlowColor;
            float _FlowSpeed;
            float _FlowFrequency;
            float _FlowHeight;
            float _FlowWiden;
            float _PulseSpeed;
            half _PulseStrength;
            half _DetailBump;
            float _SwayAmount;
            float _SwaySpeed;
            float _SwayCurl;
        CBUFFER_END

        #include "Assets/Game/Shaders/InfectedConcrete/InfectedConcreteCommon.hlsl"
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local_vertex _TENTACLE_SWAY

            // Глянец мокрой крови и кожи - вторым слоем освещения URP.
            #define _CLEARCOAT 1

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
            TEXTURE2D(_SkinMap);    SAMPLER(sampler_SkinMap);
            TEXTURE2D(_SkinNormal); SAMPLER(sampler_SkinNormal);
            TEXTURE2D(_MoldMap);    SAMPLER(sampler_MoldMap);

            struct Attributes
            {
                float4 positionOS       : POSITION;
                float3 normalOS         : NORMAL;
                float4 color            : COLOR;
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
                float3 positionOS   : TEXCOORD6;   // до сдвига - узор вен считается по ней
                float3 normalOS     : TEXCOORD7;   // до сдвига - веса трипланара
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 displacedOS, displacedNormalOS;
                IC_DisplaceAll(input.positionOS.xyz, input.normalOS, input.color, displacedOS, displacedNormalOS);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(displacedOS);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(displacedNormalOS);
                output.positionOS = input.positionOS.xyz;
                output.normalOS = input.normalOS;

                half3 vertexLight = VertexLighting(positionInputs.positionWS, output.normalWS);
                output.fogFactorAndVertexLight = half4(ComputeFogFactor(positionInputs.positionCS.z), vertexLight);

                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
            #ifdef DYNAMICLIGHTMAP_ON
                output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
            #endif
                OUTPUT_SH4(positionInputs.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(positionInputs.positionWS), output.vertexSH, output.probeOcclusion);
                return output;
            }

            float3 TriplanarWeights(float3 n)
            {
                float3 w = pow(abs(n), _TriplanarSharpness);
                return w / max(w.x + w.y + w.z, 1e-5);
            }

            #define TRIPLANAR_SAMPLE(tex, smp, p, w) (SAMPLE_TEXTURE2D(tex, smp, (p).zy) * (w).x + SAMPLE_TEXTURE2D(tex, smp, (p).xz) * (w).y + SAMPLE_TEXTURE2D(tex, smp, (p).xy) * (w).z)

            // Трипланарная карта нормалей в координатах объекта ("whiteout" с учётом стороны оси).
            float3 TriplanarNormal(TEXTURE2D_PARAM(tex, smp), float3 p, float3 n, float3 w, float strength)
            {
                float3 axisSign = n < 0 ? -1 : 1;
                float2 uvX = p.zy; uvX.x *= axisSign.x;
                float2 uvY = p.xz; uvY.x *= axisSign.y;
                float2 uvZ = p.xy; uvZ.x *= -axisSign.z;
                float3 tX = UnpackNormalScale(SAMPLE_TEXTURE2D(tex, smp, uvX), strength);
                float3 tY = UnpackNormalScale(SAMPLE_TEXTURE2D(tex, smp, uvY), strength);
                float3 tZ = UnpackNormalScale(SAMPLE_TEXTURE2D(tex, smp, uvZ), strength);
                tX.x *= axisSign.x;
                tY.x *= axisSign.y;
                tZ.x *= -axisSign.z;
                tX = float3(tX.xy + n.zy, abs(tX.z) * n.x);
                tY = float3(tY.xy + n.xz, abs(tY.z) * n.y);
                tZ = float3(tZ.xy + n.xy, abs(tZ.z) * n.z);
                return normalize(tX.zyx * w.x + tY.xzy * w.y + tZ.xyz * w.z);
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 nOS = normalize(input.normalOS);
                float3 w = TriplanarWeights(nOS);
                IC_VeinSample veins = IC_Veins(input.positionOS);

                // Бетон
                float3 pc = input.positionOS / _TileMeters;
                half3 albedo = TRIPLANAR_SAMPLE(_BaseMap, sampler_BaseMap, pc, w).rgb * _BaseColor.rgb;
                float3 concreteN = TriplanarNormal(TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), pc, nOS, w, _NormalStrength);
                half smoothness = _Smoothness;
                half clearCoat = 0;

                // Кожа - редкими пятнами, не на самих венах
                float skinNoise = IC_Noise3(input.positionOS * _SkinScale + 5.7) * 0.7 + IC_Noise3(input.positionOS * _SkinScale * 2.9 + 1.3) * 0.3;
                half skin = smoothstep(1.0 - _SkinAmount, 1.0 - _SkinAmount + 0.08, skinNoise) * (1.0 - veins.core) * step(0.001, _SkinAmount);
                float3 ps = input.positionOS / _SkinRepeat;
                half3 skinAlbedo = TRIPLANAR_SAMPLE(_SkinMap, sampler_SkinMap, ps, w).rgb * _SkinTint.rgb;
                float3 skinN = TriplanarNormal(TEXTURE2D_ARGS(_SkinNormal, sampler_SkinNormal), ps, nOS, w, 1.0);
                albedo = lerp(albedo, skinAlbedo, skin);
                concreteN = normalize(lerp(concreteN, skinN, skin));
                smoothness = lerp(smoothness, _SkinSmoothness, skin);
                clearCoat = max(clearCoat, skin * 0.7);

                // Чёрная плесень вокруг вен и потемневший бетон
                half4 mold = TRIPLANAR_SAMPLE(_MoldMap, sampler_MoldMap, input.positionOS / _MoldRepeat, w);
                half moldMask = saturate(veins.halo * (0.4 + _MoldSpread) * (0.45 + 0.9 * IC_Noise3(input.positionOS * 3.1 + 9.2))) * mold.a;
                albedo *= lerp(1.0, half3(0.62, 0.5, 0.47), saturate(veins.halo * 0.5));
                albedo = lerp(albedo, mold.rgb * _MoldColor.rgb, moldMask);
                smoothness = lerp(smoothness, 0.22, moldMask);

                // Вены и сгустки крови в них
                albedo = lerp(albedo, _VeinColor.rgb, veins.core);
                smoothness = lerp(smoothness, _VeinSmoothness, veins.core);
                clearCoat = max(clearCoat, veins.core * 0.6);
                albedo = lerp(albedo, _FlowColor.rgb, veins.flow);
                smoothness = lerp(smoothness, 0.95, veins.flow);
                clearCoat = max(clearCoat, veins.flow);

                // Нормаль: сдвинутая поверхность + текстура бетона/кожи + мелкий рельеф вен в пикселе
                float3 displacedN = normalize(input.normalWS);
                float3 detailOS = concreteN - nOS;
                float3 normalWS = normalize(displacedN + TransformObjectToWorldNormal(detailOS, false) * (1.0 - veins.core));
                normalWS = IC_BumpFromHeight(input.positionWS, normalWS, veins.height, _DetailBump);

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
                surface.smoothness = smoothness;
                surface.clearCoatMask = clearCoat;
                surface.clearCoatSmoothness = 0.92;

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = 1;
                return color;
            }
            ENDHLSL
        }

        // Тени и глубина - с тем же сдвигом вершин, иначе вздутия не отбрасывают тень и мерцают.
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma shader_feature_local_vertex _TENTACLE_SWAY
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 ShadowVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 displacedOS, displacedNormalOS;
                IC_DisplaceAll(input.positionOS.xyz, input.normalOS, input.color, displacedOS, displacedNormalOS);
                float3 positionWS = TransformObjectToWorld(displacedOS);
                float3 normalWS = TransformObjectToWorldNormal(displacedNormalOS);
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
            #pragma target 3.5
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma shader_feature_local_vertex _TENTACLE_SWAY
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4 DepthVert(Attributes input) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 displacedOS, displacedNormalOS;
                IC_DisplaceAll(input.positionOS.xyz, input.normalOS, input.color, displacedOS, displacedNormalOS);
                return TransformObjectToHClip(displacedOS);
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
            #pragma target 3.5
            #pragma vertex NormalsVert
            #pragma fragment NormalsFrag
            #pragma shader_feature_local_vertex _TENTACLE_SWAY
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
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
                float3 displacedOS, displacedNormalOS;
                IC_DisplaceAll(input.positionOS.xyz, input.normalOS, input.color, displacedOS, displacedNormalOS);
                output.positionCS = TransformObjectToHClip(displacedOS);
                output.normalWS = TransformObjectToWorldNormal(displacedNormalOS);
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
