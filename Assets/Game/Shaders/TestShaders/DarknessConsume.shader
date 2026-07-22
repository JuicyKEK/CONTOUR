Shader "Custom/DarknessConsume"
{
    Properties
    {
        [Header(Original Texture)]
        _MainTex ("Main Texture", 2D) = "white" {}
        
        [Header(Darkness)]
        [HDR] _DarknessColor ("Darkness Color", Color) = (0, 0, 0, 1)
        [HDR] _EdgeGlow ("Edge Glow", Color) = (0.5, 0, 0.8, 1)
        _ConsumeAmount ("Consume Amount", Range(0, 1)) = 0
        _EdgeWidth ("Edge Width", Range(0.01, 0.3)) = 0.1
        _NoiseScale ("Noise Scale", Range(1, 20)) = 5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDirWS : TEXCOORD3;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _DarknessColor;
                float4 _EdgeGlow;
                float _ConsumeAmount;
                float _EdgeWidth;
                float _NoiseScale;
            CBUFFER_END
            
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float noise(float2 p)
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
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Оригинальный цвет
                half4 mainColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Шум для интересного паттерна поглощения
                float noiseVal = noise(input.uv * _NoiseScale + input.positionWS.xy);
                noiseVal = noiseVal * 0.5 + 0.5;
                
                // Граница поглощения
                float consumeThreshold = _ConsumeAmount * 1.5; // Расширяем диапазон
                float darknessEdge = smoothstep(consumeThreshold - _EdgeWidth, consumeThreshold, noiseVal);
                float glowEdge = smoothstep(consumeThreshold - _EdgeWidth, consumeThreshold + _EdgeWidth * 0.5, noiseVal) 
                               - smoothstep(consumeThreshold, consumeThreshold + _EdgeWidth, noiseVal);
                
                // Fresnel
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 3);
                
                // Комбинируем цвета
                half3 darkness = _DarknessColor.rgb;
                half3 glow = _EdgeGlow.rgb * (glowEdge + fresnel * (1 - darknessEdge));
                
                half3 finalColor = lerp(darkness + glow, mainColor.rgb, darknessEdge);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}