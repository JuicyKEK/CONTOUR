Shader "Custom/VortexDarkness"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _DarkColor ("Dark Color", Color) = (0, 0, 0, 1)
        [HDR] _GlowColor ("Glow Color", Color) = (0.3, 0, 0.5, 1)
        [HDR] _VortexCoreColor ("Vortex Core Color", Color) = (0.5, 0, 0.8, 1)
        
        [Header(Vortex Settings)]
        _VortexCenter ("Vortex Center (Local Y)", Range(-1, 2)) = 0.5
        _VortexStrength ("Vortex Strength", Range(0, 20)) = 5
        _VortexSpeed ("Vortex Speed", Range(0, 10)) = 2
        _VortexTightness ("Vortex Tightness", Range(0.1, 5)) = 1
        _SpiralArms ("Spiral Arms", Range(1, 8)) = 3
        
        [Header(Noise Layers)]
        _NoiseScale ("Noise Scale", Range(1, 30)) = 8
        _NoiseSpeed ("Noise Flow Speed", Range(0, 3)) = 0.5
        _NoiseIntensity ("Noise Intensity", Range(0, 1)) = 0.6
        _Turbulence ("Turbulence", Range(0, 2)) = 0.8
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 2.5
        _FresnelIntensity ("Fresnel Intensity", Range(0, 3)) = 1.2
        
        [Header(Vertex Displacement)]
        _DisplacementAmount ("Displacement", Range(0, 0.3)) = 0.08
        _DisplacementSpeed ("Displacement Speed", Range(0, 5)) = 1.5
        
        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.5
        _PulseAmount ("Pulse Amount", Range(0, 0.5)) = 0.15
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
            Name "VortexDarkness"
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
                float3 positionOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _DarkColor;
                float4 _GlowColor;
                float4 _VortexCoreColor;
                float _VortexCenter;
                float _VortexStrength;
                float _VortexSpeed;
                float _VortexTightness;
                float _SpiralArms;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseIntensity;
                float _Turbulence;
                float _FresnelPower;
                float _FresnelIntensity;
                float _DisplacementAmount;
                float _DisplacementSpeed;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END
            
            // ============================================
            // NOISE FUNCTIONS
            // ============================================
            
            float hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }
            
            float hash2D(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
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
            
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash2D(i);
                float b = hash2D(i + float2(1.0, 0.0));
                float c = hash2D(i + float2(0.0, 1.0));
                float d = hash2D(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            // Фрактальный шум
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
            // VORTEX FUNCTIONS
            // ============================================
            
            // Преобразование в полярные координаты
            float2 toPolar(float2 cartesian)
            {
                float r = length(cartesian);
                float theta = atan2(cartesian.y, cartesian.x);
                return float2(r, theta);
            }
            
            // Преобразование из полярных в декартовы
            float2 toCartesian(float2 polar)
            {
                return float2(polar.x * cos(polar.y), polar.x * sin(polar.y));
            }
            
            // Главная функция вихря
            float vortexPattern(float3 pos, float time)
            {
                // Центр вихря (относительно локальных координат модели)
                float3 vortexOrigin = float3(0, _VortexCenter, 0);
                float3 relPos = pos - vortexOrigin;
                
                // Горизонтальные координаты для вращения
                float2 horizontal = relPos.xz;
                float2 polar = toPolar(horizontal);
                
                float radius = polar.x;
                float angle = polar.y;
                
                // Высота влияет на закручивание
                float heightFactor = relPos.y;
                
                // ===== СПИРАЛЬНОЕ ЗАКРУЧИВАНИЕ =====
                // Угол закручивается в зависимости от радиуса и высоты
                float twist = radius * _VortexTightness + heightFactor * 2.0;
                float rotatedAngle = angle + twist + time * _VortexSpeed;
                
                // ===== СПИРАЛЬНЫЕ РУКАВА =====
                float spiralArms = sin(rotatedAngle * _SpiralArms) * 0.5 + 0.5;
                
                // Затухание к центру и краям
                float radialFade = smoothstep(0.0, 0.3, radius) * smoothstep(2.0, 0.5, radius);
                spiralArms *= radialFade;
                
                // ===== ТУРБУЛЕНТНОСТЬ =====
                float2 twistedCoord = toCartesian(float2(radius, rotatedAngle));
                float turbulence = fbm(float3(twistedCoord * 3.0, time * 0.5), 3);
                
                // ===== ВТОРИЧНЫЕ ВИХРИ =====
                float secondaryVortex = sin(rotatedAngle * _SpiralArms * 2.0 + radius * 8.0 - time * _VortexSpeed * 1.5);
                secondaryVortex = secondaryVortex * 0.5 + 0.5;
                secondaryVortex *= smoothstep(0.1, 0.4, radius) * smoothstep(1.5, 0.8, radius);
                
                // ===== ЯДРО ВИХРЯ =====
                float core = 1.0 - smoothstep(0.0, 0.25, radius);
                core *= (sin(time * 3.0) * 0.3 + 0.7); // Пульсация ядра
                
                // Комбинируем всё
                float pattern = spiralArms * 0.6 
                              + secondaryVortex * 0.25 
                              + turbulence * _Turbulence * 0.3
                              + core * 0.4;
                
                return saturate(pattern);
            }
            
            // Вихревое смещение для вершин
            float3 vortexDisplacement(float3 pos, float3 normal, float time)
            {
                float3 vortexOrigin = float3(0, _VortexCenter, 0);
                float3 relPos = pos - vortexOrigin;
                
                float2 horizontal = relPos.xz;
                float radius = length(horizontal);
                float angle = atan2(horizontal.y, horizontal.x);
                
                // Спиральное смещение
                float twist = radius * _VortexTightness + time * _VortexSpeed;
                
                // Направление закручивания (тангенциальное)
                float2 tangent = float2(-sin(angle + twist), cos(angle + twist));
                
                // Шум для органичности
                float noise = fbm(pos * 4.0 + time * _DisplacementSpeed, 3);
                
                // Радиальное + тангенциальное + нормальное смещение
                float radialStrength = sin(twist) * 0.3;
                float tangentStrength = noise * 0.5;
                float normalStrength = noise;
                
                float3 displacement = normal * normalStrength * _DisplacementAmount;
                displacement.xz += tangent * tangentStrength * _DisplacementAmount * 0.5;
                
                return displacement;
            }
            
            // ============================================
            // VERTEX SHADER
            // ============================================
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float time = _Time.y;
                
                // Вихревое смещение вершин
                float3 displacement = vortexDisplacement(input.positionOS.xyz, input.normalOS, time);
                float3 displacedPos = input.positionOS.xyz + displacement;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(displacedPos);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.positionWS = posInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                
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
                
                // ===== ВИХРЕВОЙ ПАТТЕРН =====
                float vortex = vortexPattern(input.positionOS, time);
                
                // ===== ДОПОЛНИТЕЛЬНЫЙ ШУМ НА ПОВЕРХНОСТИ =====
                float3 noiseCoord = input.positionWS * _NoiseScale;
                noiseCoord += time * _NoiseSpeed;
                
                // Закручиваем координаты шума
                float2 polar = toPolar(noiseCoord.xz);
                polar.y += time * _VortexSpeed * 0.3;
                noiseCoord.xz = toCartesian(polar);
                
                float surfaceNoise = fbm(noiseCoord, 4) * _NoiseIntensity;
                
                // ===== FRESNEL =====
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;
                
                // ===== ПУЛЬСАЦИЯ =====
                float pulse = sin(time * _PulseSpeed) * _PulseAmount + 1.0;
                
                // ===== ЯДРО ВИХРЯ (яркое свечение в центре) =====
                float3 vortexOrigin = float3(0, _VortexCenter, 0);
                float distToCore = length(input.positionOS - vortexOrigin);
                float coreGlow = 1.0 - smoothstep(0.0, 0.5, distToCore);
                coreGlow *= pulse;
                
                // ===== КОМБИНИРОВАНИЕ ЭФФЕКТОВ =====
                float glowMask = saturate(vortex + surfaceNoise * 0.5 + fresnel);
                glowMask *= pulse;
                
                // ===== ФИНАЛЬНЫЙ ЦВЕТ =====
                half3 baseColor = lerp(_DarkColor.rgb, _GlowColor.rgb, glowMask);
                
                // Добавляем яркое ядро
                baseColor = lerp(baseColor, _VortexCoreColor.rgb, coreGlow * 0.7);
                
                // Усиливаем края
                baseColor += _GlowColor.rgb * fresnel * 0.5;
                
                return half4(baseColor, 1.0);
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