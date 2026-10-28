Shader "Custom/BlackMoldPatch"
{
    // Пятно чёрной плесени на тонкой плоской заплатке (префаб BlackMold_Patch). Ставится на стену, пол или потолок
    // лицом наружу, включается и выключается объектом. Форма пятна - из шума (общий код с декалью, BlackMoldCommon.hlsl);
    // у каждой заплатки свой узор сам собой (сид берётся из её положения), Seed - сдвинуть узор вручную.
    // _InfectionAmount (0..1) - сила: пятно редеет и пропадает; его может вести MoldInfectionVisual от зоны заражения.
    Properties
    {
        _MoldMap ("Black Mold (RGBA)", 2D) = "black" {}
        _MoldTint ("Mold Tint", Color) = (1, 1, 1, 1)
        _DetailScale ("Texture Repeat per Patch", Range(0.5, 8)) = 2
        _Seed ("Seed", Float) = 0
        [Toggle] _SeedFromPosition ("Different Pattern per Position", Float) = 1
        _PatternScale ("Colonies Scale", Range(1, 20)) = 6
        _Coverage ("Coverage", Range(0, 1)) = 0.5
        _EdgeSoftness ("Edge Softness", Range(0.005, 0.3)) = 0.03
        _InfectionAmount ("Infection Amount 0-1", Range(0, 1)) = 1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent-50" "IgnoreProjector" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            Offset -1, -1

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Game/Shaders/BlackMold/BlackMoldCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MoldMap_ST;
                half4 _MoldTint;
                float _DetailScale;
                float _Seed;
                float _SeedFromPosition;
                float _PatternScale;
                half _Coverage;
                half _EdgeSoftness;
                half _InfectionAmount;
                half _Smoothness;
            CBUFFER_END

            TEXTURE2D(_MoldMap); SAMPLER(sampler_MoldMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float  fogFactor  : TEXCOORD3;
                float  seed       : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(p.positionCS.z);
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                output.seed = _Seed + (_SeedFromPosition > 0.5 ? dot(floor(origin * 10.0), float3(1.731, 3.117, 5.413)) : 0.0);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float mask = BM_Mask(input.uv, input.seed, _PatternScale, _Coverage, _EdgeSoftness);
                half4 detail = SAMPLE_TEXTURE2D(_MoldMap, sampler_MoldMap, input.uv * _DetailScale + BM_SeedOffset(input.seed) * 0.01);
                half alpha = saturate(mask * lerp(0.7, 1.0, detail.a) * _InfectionAmount * _MoldTint.a);
                clip(alpha - 0.003);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalize(input.normalWS);
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
            #if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
            #endif
                inputData.fogCoord = input.fogFactor;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SampleSH(inputData.normalWS);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = detail.rgb * _MoldTint.rgb;
                surface.alpha = alpha;
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;
                surface.normalTS = half3(0, 0, 1);

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.a = alpha;
                return color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
