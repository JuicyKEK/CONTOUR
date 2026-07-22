Shader "Custom/LivingDarkness"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _DarkColor ("Dark Color", Color) = (0, 0, 0, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.2, 0, 0.3, 1)
        
        [Header(Noise)]
        _NoiseScale ("Noise Scale", Range(1, 50)) = 10
        _NoiseSpeed ("Noise Speed", Range(0, 5)) = 1
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.5
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 2
        _FresnelIntensity ("Fresnel Intensity", Range(0, 3)) = 1
        
        [Header(Vertex Displacement)]
        _DisplacementAmount ("Displacement", Range(0, 0.5)) = 0.05
        _DisplacementSpeed ("Displacement Speed", Range(0, 10)) = 2
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
            Name "LivingDarkness"
            Tags { "LightMode" = "UniversalForward" }
            
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
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float2 uv : TEXCOORD3;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _DarkColor;
                float4 _GlowColor;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseIntensity;
                float _FresnelPower;
                float _FresnelIntensity;
                float _DisplacementAmount;
                float _DisplacementSpeed;
            CBUFFER_END
            
            // Простой 3D шум
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(
                        lerp(hash(i + float3(0,0,0)), hash(i + float3(1,0,0)), f.x),
                        lerp(hash(i + float3(0,1,0)), hash(i + float3(1,1,0)), f.x),
                        f.y
                    ),
                    lerp(
                        lerp(hash(i + float3(0,0,1)), hash(i + float3(1,0,1)), f.x),
                        lerp(hash(i + float3(0,1,1)), hash(i + float3(1,1,1)), f.x),
                        f.y
                    ),
                    f.z
                );
            }
            
            // Фрактальный шум
            float fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * noise3D(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Анимированное смещение вершин
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float noiseVal = fbm(worldPos * 2 + _Time.y * _DisplacementSpeed);
                float3 displacement = input.normalOS * noiseVal * _DisplacementAmount;
                
                float3 displacedPos = input.positionOS.xyz + displacement;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(displacedPos);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.positionWS = posInputs.positionWS;
                output.uv = input.uv;
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Анимированный шум на поверхности
                float3 noiseCoord = input.positionWS * _NoiseScale + _Time.y * _NoiseSpeed;
                float noiseValue = fbm(noiseCoord);
                noiseValue = noiseValue * _NoiseIntensity;
                
                // Fresnel эффект
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;
                
                // Комбинируем шум и fresnel
                float glowMask = saturate(fresnel + noiseValue * 0.5);
                
                // Финальный цвет
                half3 finalColor = lerp(_DarkColor.rgb, _GlowColor.rgb, glowMask);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        // Shadow pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            
            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            float3 _LightDirection;
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif
                
                output.positionCS = positionCS;
                return output;
            }
            
            half4 ShadowFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}