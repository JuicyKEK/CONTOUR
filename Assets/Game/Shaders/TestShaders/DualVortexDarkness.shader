Shader "Custom/DualVortexDarkness"
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
        _VortexSeparation ("Vortex Separation", Range(0.1, 1.5)) = 0.5
        
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
            Name "DualVortexDarkness"
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
            };
            
            // ============================================
            // PROPERTIES
            // ============================================
            
            CBUFFER_START(UnityPerMaterial)
                float4 _DarkColor;
                float4 _GlowColor;
                float4 _VortexCoreColor;
                float _VortexCenter;
                float _VortexStrength;
                float _VortexSpeed;
                float _VortexTightness;
                float _SpiralArms;
                float _VortexSeparation;
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
            // NOISE FUNCTIONS (Уровень 1 - базовые)
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
            
            // ============================================
            // NOISE FUNCTIONS (Уровень 2 - составные)
            // ============================================
            
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
            // COORDINATE FUNCTIONS (Уровень 2)
            // ============================================
            
            float2 toPolar(float2 cartesian)
            {
                float r = length(cartesian);
                float theta = atan2(cartesian.y, cartesian.x);
                return float2(r, theta);
            }
            
            float2 toCartesian(float2 polar)
            {
                return float2(polar.x * cos(polar.y), polar.x * sin(polar.y));
            }
            
            // ============================================
            // VORTEX FUNCTIONS (Уровень 3)
            // ВАЖНО: vortexPatternSingle должна быть ДО dualVortexPattern
            // ============================================
            
            // Одиночный вихрь с заданным направлением
            float vortexPatternSingle(float3 pos, float3 origin, float time, float direction)
            {
                float3 relPos = pos - origin;
                
                // Горизонтальные координаты для вращения
                float2 horizontal = relPos.xz;
                float2 polar = toPolar(horizontal);
                
                float radius = polar.x;
                float angle = polar.y;
                
                // Высота влияет на закручивание
                float heightFactor = relPos.y * direction;
                
                // Спиральное закручивание
                float twist = radius * _VortexTightness + heightFactor * 1.5;
                float rotatedAngle = angle + twist * direction + time * _VortexSpeed * direction;
                
                // Спиральные рукава
                float spiralArms = sin(rotatedAngle * _SpiralArms) * 0.5 + 0.5;
                
                // Затухание к центру и краям
                float radialFade = smoothstep(0.0, 0.2, radius) * smoothstep(1.5, 0.3, radius);
                spiralArms *= radialFade;
                
                // Вторичные мелкие вихри
                float secondaryVortex = sin(rotatedAngle * _SpiralArms * 2.0 + radius * 6.0 - time * _VortexSpeed * 1.5 * direction);
                secondaryVortex = secondaryVortex * 0.5 + 0.5;
                secondaryVortex *= smoothstep(0.1, 0.3, radius) * smoothstep(1.2, 0.6, radius);
                
                // Турбулентность
                float2 twistedCoord = toCartesian(float2(radius, rotatedAngle));
                float turbulence = fbm(float3(twistedCoord * 2.0, time * 0.3 * direction), 3);
                
                // Ядро вихря (яркое свечение)
                float core = 1.0 - smoothstep(0.0, 0.2, radius);
                core *= (sin(time * 3.0 * direction) * 0.2 + 0.8);
                
                // Комбинируем
                float pattern = spiralArms * 0.5 
                              + secondaryVortex * 0.2 
                              + turbulence * _Turbulence * 0.2
                              + core * 0.3;
                
                // Затухание по высоте от центра вихря
                float heightFade = 1.0 - smoothstep(0.0, 1.0, abs(relPos.y));
                pattern *= heightFade;
                
                return saturate(pattern);
            }
            
            // Двойной встречный вихрь
            float dualVortexPattern(float3 pos, float time)
            {
                // Верхний вихрь (вращается по часовой стрелке)
                float3 upperOrigin = float3(0, _VortexCenter + _VortexSeparation, 0);
                float upperVortex = vortexPatternSingle(pos, upperOrigin, time, 1.0);
                
                // Нижний вихрь (вращается против часовой стрелки)
                float3 lowerOrigin = float3(0, _VortexCenter - _VortexSeparation, 0);
                float lowerVortex = vortexPatternSingle(pos, lowerOrigin, time, -1.0);
                
                // Зона смешивания в центре (где вихри встречаются)
                float centerY = _VortexCenter;
                float blendZone = smoothstep(centerY - 0.3, centerY + 0.3, pos.y);
                
                // Основной паттерн от двух вихрей
                float combined = lerp(lowerVortex, upperVortex, blendZone);
                
                // Дополнительная турбулентность в зоне столкновения
                float collisionZone = 1.0 - abs(pos.y - centerY) * 2.0;
                collisionZone = saturate(collisionZone);
                
                float collisionTurbulence = fbm(float3(pos.xz * 5.0, time), 3);
                combined += collisionTurbulence * collisionZone * _Turbulence * 0.3;
                
                // Яркое свечение в центре столкновения
                float collisionGlow = collisionZone * (sin(time * 4.0) * 0.2 + 0.8);
                float distFromCenter = length(float2(pos.x, pos.z));
                collisionGlow *= smoothstep(0.5, 0.0, distFromCenter);
                combined += collisionGlow * 0.2;
                
                return saturate(combined);
            }
            
            // ============================================
            // DISPLACEMENT FUNCTION (Уровень 3)
            // ============================================
            
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
                
                // Разное направление для верха и низа
                float directionMult = sign(relPos.y);
                
                // Шум для органичности
                float noise = fbm(pos * 4.0 + time * _DisplacementSpeed, 3);
                
                // Смещение
                float tangentStrength = noise * 0.5 * directionMult;
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
                
                // ===== ДВОЙНОЙ ВИХРЕВОЙ ПАТТЕРН =====
                float vortex = dualVortexPattern(input.positionOS, time);
                
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
                
                // ===== ЯДРА ВИХРЕЙ =====
                float3 upperOrigin = float3(0, _VortexCenter + _VortexSeparation, 0);
                float3 lowerOrigin = float3(0, _VortexCenter - _VortexSeparation, 0);
                
                float distToUpperCore = length(input.positionOS - upperOrigin);
                float distToLowerCore = length(input.positionOS - lowerOrigin);
                
                float upperCoreGlow = 1.0 - smoothstep(0.0, 0.4, distToUpperCore);
                float lowerCoreGlow = 1.0 - smoothstep(0.0, 0.4, distToLowerCore);
                float coreGlow = max(upperCoreGlow, lowerCoreGlow) * pulse;
                
                // ===== ЗОНА СТОЛКНОВЕНИЯ =====
                float collisionZone = 1.0 - saturate(abs(input.positionOS.y - _VortexCenter) * 3.0);
                float collisionGlow = collisionZone * (sin(time * 5.0) * 0.3 + 0.7);
                
                // ===== КОМБИНИРОВАНИЕ ЭФФЕКТОВ =====
                float glowMask = saturate(vortex + surfaceNoise * 0.5 + fresnel);
                glowMask *= pulse;
                
                // ===== ФИНАЛЬНЫЙ ЦВЕТ =====
                half3 baseColor = lerp(_DarkColor.rgb, _GlowColor.rgb, glowMask);
                
                // Добавляем яркие ядра
                baseColor = lerp(baseColor, _VortexCoreColor.rgb, coreGlow * 0.6);
                
                // Добавляем свечение в зоне столкновения
                half3 collisionColor = (_GlowColor.rgb + _VortexCoreColor.rgb) * 0.5;
                baseColor = lerp(baseColor, collisionColor, collisionGlow * 0.4);
                
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