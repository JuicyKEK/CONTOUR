Shader "Custom/SpawnGhost"
{
    // Призрак того, что вырастет здесь при большем заражении (например, мост через провал): полупрозрачный силуэт
    // светится по краям (френель), по нему от концов к середине бегут волны, он медленно дышит и изредка мерцает.
    // Без освещения, складывается со сценой (виден в темноте), глубину не пишет.
    // Волны идут вдоль оси Z объекта: от _SpanStart и от _SpanEnd к середине.
    Properties
    {
        [HDR] _Color ("Glow Color", Color) = (1.2, 0.16, 0.1, 1)
        _Alpha ("Body Alpha", Range(0, 1)) = 0.06
        _RimPower ("Rim Power", Range(0.5, 8)) = 2.2
        _RimAlpha ("Rim Alpha", Range(0, 1)) = 0.55
        _SpanStart ("Span Start (local Z), m", Float) = 0
        _SpanEnd ("Span End (local Z), m", Float) = 1.5
        _WaveScale ("Waves per m", Range(0.2, 10)) = 2.5
        _WaveSpeed ("Wave Speed", Range(0, 5)) = 0.9
        _WaveAlpha ("Wave Alpha", Range(0, 1)) = 0.45
        _NoiseScale ("Noise per m", Range(0.5, 40)) = 9
        _Breath ("Breathing Speed", Range(0, 3)) = 0.5
        _Flicker ("Flicker", Range(0, 1)) = 0.4
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Name "Ghost"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Alpha;
                half _RimPower;
                half _RimAlpha;
                float _SpanStart;
                float _SpanEnd;
                float _WaveScale;
                float _WaveSpeed;
                half _WaveAlpha;
                float _NoiseScale;
                float _Breath;
                half _Flicker;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float Noise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(lerp(Hash(i), Hash(i + float3(1, 0, 0)), f.x),
                                 lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                            lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                                 lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y), f.z);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionOS = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float3 n = normalize(input.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half rim = pow(1.0 - saturate(abs(dot(n, v))), _RimPower);

                // Волны от обоих концов к середине: фаза - расстояние до ближнего конца.
                float fromEnd = min(input.positionOS.z - _SpanStart, _SpanEnd - input.positionOS.z);
                float wavePhase = fromEnd * _WaveScale - _Time.y * _WaveSpeed;
                half wave = smoothstep(0.75, 1.0, frac(wavePhase)) * step(0.0, fromEnd);

                half noise = Noise(input.positionOS * _NoiseScale + float3(0, 0, -_Time.y * 0.4));
                half breath = 0.7 + 0.3 * sin(_Time.y * _Breath * 6.2832);
                half flicker = 1.0 - _Flicker * step(0.9, Hash(floor(_Time.yyy * 9.0)));

                half alpha = (_Alpha + rim * _RimAlpha * (0.6 + 0.4 * noise) + wave * _WaveAlpha * noise) * breath * flicker;
                return half4(_Color.rgb, saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
