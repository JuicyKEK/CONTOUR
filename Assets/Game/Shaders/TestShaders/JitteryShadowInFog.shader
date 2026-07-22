Shader "Custom/JitteryShadowInFog"
{
    Properties
    {
        [Header(Silhouette)]
        [HDR] _SilhouetteColor ("Silhouette Color", Color) = (0, 0, 0, 1)
        [HDR] _FogColor ("Fog Color", Color) = (0.1, 0.1, 0.15, 1)
        [HDR] _FogEdgeColor ("Fog Edge Glow", Color) = (0.2, 0.1, 0.3, 1)
        
        [Header(Jitter Effect)]
        _JitterIntensity ("Jitter Intensity", Range(0, 0.2)) = 0.05
        _JitterSpeed ("Jitter Speed", Range(1, 100)) = 30
        _JitterFrequency ("Jitter Frequency", Range(1, 20)) = 5
        [Toggle] _RandomJitter ("Random Jitter", Float) = 1
        
        [Header(Rotating Fog)]
        _FogDensity ("Fog Density", Range(0, 2)) = 1
        _FogScale ("Fog Scale", Range(1, 20)) = 5
        _FogRotationSpeed ("Fog Rotation Speed", Range(0, 5)) = 1
        _FogLayerSpeed ("Fog Layer Speed", Range(0, 3)) = 0.5
        _FogTurbulence ("Fog Turbulence", Range(0, 2)) = 0.8
        
        [Header(Fog Layers)]
        _InnerFogRadius ("Inner Fog Radius", Range(0, 1)) = 0.3
        _OuterFogRadius ("Outer Fog Radius", Range(0.5, 2)) = 1.2
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 2
        _FresnelIntensity ("Fresnel Intensity", Range(0, 3)) = 1.5
        
        [Header(Distortion)]
        _SilhouetteDistortion ("Silhouette Edge Distortion", Range(0, 0.5)) = 0.1
        _DistortionSpeed ("Distortion Speed", Range(0, 10)) = 3
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
            Name "JitteryShadowFog"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            // ============================================
            // STRUCTURES
            // ============================================
            
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
                float3 positionOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float jitterAmount : TEXCOORD5;
            };
            
            // ============================================
            // PROPERTIES
            // ============================================
            
            CBUFFER_START(UnityPerMaterial)
                float4 _SilhouetteColor;
                float4 _FogColor;
                float4 _FogEdgeColor;
                
                float _JitterIntensity;
                float _JitterSpeed;
                float _JitterFrequency;
                float _RandomJitter;
                
                float _FogDensity;
                float _FogScale;
                float _FogRotationSpeed;
                float _FogLayerSpeed;
                float _FogTurbulence;
                
                float _InnerFogRadius;
                float _OuterFogRadius;
                
                float _FresnelPower;
                float _FresnelIntensity;
                
                float _SilhouetteDistortion;
                float _DistortionSpeed;
            CBUFFER_END
            
            // ============================================
            // HASH & NOISE FUNCTIONS
            // ============================================
            
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }
            
            float hash21(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            
            float3 hash33(float3 p)
            {
                p = float3(
                    dot(p, float3(127.1, 311.7, 74.7)),
                    dot(p, float3(269.5, 183.3, 246.1)),
                    dot(p, float3(113.5, 271.9, 124.6))
                );
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }
            
            // Simplex-like noise
            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(
                        lerp(hash31(i + float3(0,0,0)), hash31(i + float3(1,0,0)), f.x),
                        lerp(hash31(i + float3(0,1,0)), hash31(i + float3(1,1,0)), f.x),
                        f.y
                    ),
                    lerp(
                        lerp(hash31(i + float3(0,0,1)), hash31(i + float3(1,0,1)), f.x),
                        lerp(hash31(i + float3(0,1,1)), hash31(i + float3(1,1,1)), f.x),
                        f.y
                    ),
                    f.z
                );
            }
            
            // Fractal Brownian Motion
            float fbm(float3 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise3D(p * frequency);
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }
                
                return value;
            }
            
            // ============================================
            // JITTER FUNCTIONS
            // ============================================
            
            // Быстрое дёргание - создаёт резкие скачки
            float3 calculateJitter(float3 position, float3 normal, float time)
            {
                float3 jitter = float3(0, 0, 0);
                
                // Уникальный seed для каждой вершины
                float vertexSeed = hash31(position * 100.0);
                
                // Быстрый случайный jitter
                if (_RandomJitter > 0.5)
                {
                    // Случайные резкие скачки
                    float randomTime = floor(time * _JitterSpeed);
                    float3 randomOffset = hash33(float3(vertexSeed, randomTime, vertexSeed * 2.0));
                    
                    // Случайно решаем, будет ли скачок в этот момент
                    float shouldJitter = step(0.7, hash11(randomTime + vertexSeed));
                    
                    jitter = randomOffset * shouldJitter * _JitterIntensity;
                }
                
                // Синусоидальное дёргание с разными частотами
                float jitterX = sin(time * _JitterSpeed + position.y * _JitterFrequency) 
                              * sin(time * _JitterSpeed * 1.3 + position.z * _JitterFrequency * 0.7);
                float jitterY = sin(time * _JitterSpeed * 0.8 + position.x * _JitterFrequency * 1.2)
                              * cos(time * _JitterSpeed * 1.1 + position.z * _JitterFrequency);
                float jitterZ = cos(time * _JitterSpeed * 0.9 + position.y * _JitterFrequency * 0.8)
                              * sin(time * _JitterSpeed * 1.2 + position.x * _JitterFrequency * 1.1);
                
                // Нелинейность для более резких движений
                jitterX = sign(jitterX) * pow(abs(jitterX), 0.5);
                jitterY = sign(jitterY) * pow(abs(jitterY), 0.5);
                jitterZ = sign(jitterZ) * pow(abs(jitterZ), 0.5);
                
                jitter += float3(jitterX, jitterY, jitterZ) * _JitterIntensity * 0.5;
                
                // Периодические сильные "вздрагивания"
                float spasm = sin(time * 3.0) * step(0.95, sin(time * 7.0 + vertexSeed * 10.0));
                jitter += normal * spasm * _JitterIntensity * 2.0;
                
                return jitter;
            }
            
            // ============================================
            // FOG FUNCTIONS
            // ============================================
            
            // Вращающийся туманный паттерн
            float rotatingFogPattern(float3 pos, float time)
            {
                // Центр вращения
                float3 center = float3(0, 0, 0);
                float3 relPos = pos - center;
                
                // Полярные координаты для горизонтального вращения
                float radius = length(relPos.xz);
                float angle = atan2(relPos.z, relPos.x);
                
                // Вращаем координаты
                float rotatedAngle = angle + time * _FogRotationSpeed;
                
                // Создаём вращающиеся координаты для шума
                float3 rotatedPos = float3(
                    cos(rotatedAngle) * radius,
                    relPos.y,
                    sin(rotatedAngle) * radius
                );
                
                // Несколько слоёв тумана с разными скоростями
                float fog1 = fbm(rotatedPos * _FogScale + time * _FogLayerSpeed, 4);
                float fog2 = fbm(rotatedPos * _FogScale * 0.5 - time * _FogLayerSpeed * 0.7, 3);
                float fog3 = fbm(rotatedPos * _FogScale * 2.0 + time * _FogLayerSpeed * 1.3, 2);
                
                // Спиральный паттерн
                float spiral = sin(rotatedAngle * 3.0 + radius * 5.0 - time * 2.0) * 0.5 + 0.5;
                spiral *= smoothstep(0.0, 0.5, radius) * smoothstep(1.5, 0.5, radius);
                
                // Комбинируем слои
                float fog = fog1 * 0.5 + fog2 * 0.3 + fog3 * 0.2;
                fog += spiral * 0.3;
                fog += fbm(rotatedPos * 3.0 + float3(time, -time * 0.5, time * 0.3), 3) * _FogTurbulence * 0.3;
                
                return saturate(fog);
            }
            
            // Вертикальные потоки тумана
            float verticalFogStreams(float3 pos, float time)
            {
                float streams = 0.0;
                
                // Несколько вертикальных потоков
                for (int i = 0; i < 3; i++)
                {
                    float offset = float(i) * 2.094; // 2π/3
                    float2 streamPos = float2(
                        cos(time * 0.5 + offset) * 0.3,
                        sin(time * 0.5 + offset) * 0.3
                    );
                    
                    float dist = length(pos.xz - streamPos);
                    float stream = smoothstep(0.3, 0.0, dist);
                    
                    // Вертикальное движение
                    float verticalNoise = sin(pos.y * 5.0 - time * 3.0 + offset) * 0.5 + 0.5;
                    stream *= verticalNoise;
                    
                    streams += stream;
                }
                
                return saturate(streams * 0.5);
            }
            
            // ============================================
            // VERTEX SHADER
            // ============================================
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float time = _Time.y;
                
                // Рассчитываем jitter
                float3 jitter = calculateJitter(input.positionOS.xyz, input.normalOS, time);
                float3 jitteredPos = input.positionOS.xyz + jitter;
                
                // Дополнительное искажение по нормали (дышащий эффект)
                float breathe = sin(time * 2.0 + input.positionOS.y * 3.0) * 0.02;
                jitteredPos += input.normalOS * breathe;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(jitteredPos);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.positionWS = posInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                output.jitterAmount = length(jitter);
                
                return output;
            }
            
            // ============================================
            // FRAGMENT SHADER
            // ============================================
            
            half4 frag(Varyings input) : SV_Target
            {
                float time = _Time.y;
                
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // ===== FRESNEL (определяет края) =====
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;
                
                // ===== ВРАЩАЮЩИЙСЯ ТУМАН =====
                float fogPattern = rotatingFogPattern(input.positionOS, time);
                float verticalFog = verticalFogStreams(input.positionOS, time);
                
                // Комбинируем паттерны тумана
                float combinedFog = fogPattern + verticalFog * 0.5;
                combinedFog = saturate(combinedFog * _FogDensity);
                
                // ===== ИСКАЖЕНИЕ КРАЁВ СИЛУЭТА =====
                float edgeDistortion = fbm(input.positionWS * 10.0 + time * _DistortionSpeed, 3);
                float distortedFresnel = fresnel + edgeDistortion * _SilhouetteDistortion;
                distortedFresnel = saturate(distortedFresnel);
                
                // ===== ОПРЕДЕЛЯЕМ ЗОНЫ =====
                // Ядро (чёрный силуэт)
                float coreMask = 1.0 - smoothstep(0.0, _InnerFogRadius, distortedFresnel);
                
                // Зона тумана
                float fogZone = smoothstep(_InnerFogRadius, _OuterFogRadius, distortedFresnel);
                fogZone *= combinedFog;
                
                // Внешние края (свечение)
                float edgeGlow = smoothstep(0.5, 1.0, distortedFresnel);
                edgeGlow *= (1.0 + sin(time * 3.0) * 0.2); // Пульсация
                
                // ===== МЕРЦАНИЕ СИЛУЭТА =====
                float flicker = 1.0;
                if (input.jitterAmount > 0.01)
                {
                    // Когда происходит jitter, силуэт немного мерцает
                    flicker = 0.8 + hash11(time * 50.0) * 0.2;
                }
                
                // ===== ЦВЕТА =====
                half3 silhouetteCol = _SilhouetteColor.rgb * flicker;
                half3 fogCol = _FogColor.rgb * combinedFog;
                half3 edgeCol = _FogEdgeColor.rgb * edgeGlow;
                
                // ===== ФИНАЛЬНОЕ СМЕШИВАНИЕ =====
                half3 finalColor = silhouetteCol;
                
                // Добавляем туман
                finalColor = lerp(finalColor, fogCol, fogZone * 0.7);
                
                // Добавляем свечение краёв
                finalColor += edgeCol * fresnel;
                
                // Случайные вспышки при сильном jitter
                float flashChance = step(0.03, input.jitterAmount) * hash11(floor(time * 30.0));
                finalColor += _FogEdgeColor.rgb * flashChance * 0.5;
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        // ============================================
        // SHADOW CASTER PASS
        // ============================================
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
            
            float _JitterIntensity;
            float _JitterSpeed;
            float _JitterFrequency;
            float _RandomJitter;
            
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
            
            float hash31_shadow(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            
            float3 hash33_shadow(float3 p)
            {
                p = float3(
                    dot(p, float3(127.1, 311.7, 74.7)),
                    dot(p, float3(269.5, 183.3, 246.1)),
                    dot(p, float3(113.5, 271.9, 124.6))
                );
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }
            
            float hash11_shadow(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }
            
            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                
                float time = _Time.y;
                float3 position = input.positionOS.xyz;
                float3 normal = input.normalOS;
                
                // Применяем jitter к тени тоже
                float vertexSeed = hash31_shadow(position * 100.0);
                float3 jitter = float3(0, 0, 0);
                
                if (_RandomJitter > 0.5)
                {
                    float randomTime = floor(time * _JitterSpeed);
                    float3 randomOffset = hash33_shadow(float3(vertexSeed, randomTime, vertexSeed * 2.0));
                    float shouldJitter = step(0.7, hash11_shadow(randomTime + vertexSeed));
                    jitter = randomOffset * shouldJitter * _JitterIntensity;
                }
                
                float jitterX = sin(time * _JitterSpeed + position.y * _JitterFrequency);
                float jitterY = sin(time * _JitterSpeed * 0.8 + position.x * _JitterFrequency * 1.2);
                float jitterZ = cos(time * _JitterSpeed * 0.9 + position.y * _JitterFrequency * 0.8);
                
                jitter += float3(jitterX, jitterY, jitterZ) * _JitterIntensity * 0.3;
                
                float3 jitteredPos = position + jitter;
                
                float3 positionWS = TransformObjectToWorld(jitteredPos);
                float3 normalWS = TransformObjectToWorldNormal(normal);
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
        
        // ============================================
        // DEPTH PASS (для эффектов пост-обработки)
        // ============================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            
            ZWrite On
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
            
            Varyings DepthVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }
            
            half4 DepthFrag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}