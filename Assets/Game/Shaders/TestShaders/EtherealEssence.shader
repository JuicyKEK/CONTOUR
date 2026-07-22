Shader "Custom/EtherealEssence"
{
    Properties
    {
        [Header(Main Colors)]
        [HDR] _PrimaryColor ("Primary Color", Color) = (0.1, 0.5, 1, 1)
        [HDR] _SecondaryColor ("Secondary Color", Color) = (1, 0.2, 0.5, 1)
        [HDR] _TertiaryColor ("Tertiary Color", Color) = (0.2, 1, 0.5, 1)
        [HDR] _CoreGlow ("Core Glow Color", Color) = (1, 1, 1, 1)
        
        [Header(Color Animation)]
        _ColorShiftSpeed ("Color Shift Speed", Range(0, 3)) = 0.5
        _ColorContrast ("Color Contrast", Range(0.5, 3)) = 1.5
        _Saturation ("Saturation", Range(0, 2)) = 1.2
        
        [Header(Fresnel and Edge)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 2.5
        _FresnelIntensity ("Fresnel Intensity", Range(0, 5)) = 2
        [HDR] _FresnelColor ("Fresnel Color", Color) = (0.5, 0.8, 1, 1)
        _RimPulseSpeed ("Rim Pulse Speed", Range(0, 10)) = 2
        
        [Header(Inner Glow)]
        _InnerGlowIntensity ("Inner Glow Intensity", Range(0, 3)) = 1
        _InnerGlowFalloff ("Inner Glow Falloff", Range(0.1, 5)) = 2
        _SubsurfaceDistortion ("Subsurface Distortion", Range(0, 1)) = 0.5
        
        [Header(Energy Streams)]
        [Toggle] _EnableStreams ("Enable Energy Streams", Float) = 1
        _StreamCount ("Stream Count", Range(1, 10)) = 5
        _StreamSpeed ("Stream Speed", Range(0, 10)) = 3
        _StreamWidth ("Stream Width", Range(0.01, 0.3)) = 0.08
        _StreamIntensity ("Stream Intensity", Range(0, 3)) = 1.5
        _StreamTurbulence ("Stream Turbulence", Range(0, 2)) = 0.5
        
        [Header(Holographic Lines)]
        [Toggle] _EnableHoloLines ("Enable Holographic Lines", Float) = 1
        _HoloLineFrequency ("Line Frequency", Range(10, 200)) = 60
        _HoloLineSpeed ("Line Speed", Range(0, 20)) = 5
        _HoloLineIntensity ("Line Intensity", Range(0, 1)) = 0.3
        _HoloLineWidth ("Line Width", Range(0.01, 0.5)) = 0.1
        
        [Header(Inner Stars and Particles)]
        [Toggle] _EnableStars ("Enable Inner Stars", Float) = 1
        _StarDensity ("Star Density", Range(10, 100)) = 40
        _StarBrightness ("Star Brightness", Range(0, 3)) = 1.5
        _StarTwinkleSpeed ("Twinkle Speed", Range(0, 20)) = 5
        _StarSize ("Star Size", Range(0.001, 0.05)) = 0.015
        
        [Header(Nebula Effect)]
        [Toggle] _EnableNebula ("Enable Nebula", Float) = 1
        _NebulaScale ("Nebula Scale", Range(1, 20)) = 5
        _NebulaSpeed ("Nebula Flow Speed", Range(0, 2)) = 0.3
        _NebulaIntensity ("Nebula Intensity", Range(0, 2)) = 0.8
        _NebulaTurbulence ("Nebula Turbulence", Range(0, 3)) = 1
        
        [Header(Iridescence)]
        [Toggle] _EnableIridescence ("Enable Iridescence", Float) = 1
        _IridescenceScale ("Iridescence Scale", Range(0.5, 5)) = 2
        _IridescenceSpeed ("Iridescence Speed", Range(0, 3)) = 0.5
        _IridescenceIntensity ("Iridescence Intensity", Range(0, 2)) = 1
        
        [Header(Distortion)]
        _VertexDisplacement ("Vertex Displacement", Range(0, 0.2)) = 0.03
        _DisplacementSpeed ("Displacement Speed", Range(0, 5)) = 1.5
        _DisplacementScale ("Displacement Scale", Range(1, 10)) = 3
        
        [Header(Pulse)]
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.3
        
        [Header(Transparency)]
        _Opacity ("Base Opacity", Range(0, 1)) = 0.9
        _DepthFade ("Depth Fade", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "EtherealEssence"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            // ============================================
            // STRUCTURES
            // ============================================
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
                float2 uv : TEXCOORD4;
                float4 screenPos : TEXCOORD5;
                float3 tangentWS : TEXCOORD6;
                float3 bitangentWS : TEXCOORD7;
            };
            
            // ============================================
            // PROPERTIES
            // ============================================
            
            CBUFFER_START(UnityPerMaterial)
                // Colors
                float4 _PrimaryColor;
                float4 _SecondaryColor;
                float4 _TertiaryColor;
                float4 _CoreGlow;
                float _ColorShiftSpeed;
                float _ColorContrast;
                float _Saturation;
                
                // Fresnel
                float _FresnelPower;
                float _FresnelIntensity;
                float4 _FresnelColor;
                float _RimPulseSpeed;
                
                // Inner Glow
                float _InnerGlowIntensity;
                float _InnerGlowFalloff;
                float _SubsurfaceDistortion;
                
                // Streams
                float _EnableStreams;
                float _StreamCount;
                float _StreamSpeed;
                float _StreamWidth;
                float _StreamIntensity;
                float _StreamTurbulence;
                
                // Holo Lines
                float _EnableHoloLines;
                float _HoloLineFrequency;
                float _HoloLineSpeed;
                float _HoloLineIntensity;
                float _HoloLineWidth;
                
                // Stars
                float _EnableStars;
                float _StarDensity;
                float _StarBrightness;
                float _StarTwinkleSpeed;
                float _StarSize;
                
                // Nebula
                float _EnableNebula;
                float _NebulaScale;
                float _NebulaSpeed;
                float _NebulaIntensity;
                float _NebulaTurbulence;
                
                // Iridescence
                float _EnableIridescence;
                float _IridescenceScale;
                float _IridescenceSpeed;
                float _IridescenceIntensity;
                
                // Distortion
                float _VertexDisplacement;
                float _DisplacementSpeed;
                float _DisplacementScale;
                
                // Pulse
                float _PulseSpeed;
                float _PulseIntensity;
                
                // Transparency
                float _Opacity;
                float _DepthFade;
            CBUFFER_END
            
            // ============================================
            // NOISE FUNCTIONS
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
                return frac(sin(p) * 43758.5453123);
            }
            
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
            // COLOR FUNCTIONS
            // ============================================
            
            // Конвертация HSV в RGB
            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }
            
            // Радужный градиент
            float3 rainbow(float t)
            {
                return hsv2rgb(float3(t, 0.8, 1.0));
            }
            
            // Космический градиент
            float3 cosmicGradient(float t, float time)
            {
                float3 col1 = _PrimaryColor.rgb;
                float3 col2 = _SecondaryColor.rgb;
                float3 col3 = _TertiaryColor.rgb;
                
                t = frac(t + time * _ColorShiftSpeed);
                
                float3 color;
                if (t < 0.33)
                    color = lerp(col1, col2, t * 3.0);
                else if (t < 0.66)
                    color = lerp(col2, col3, (t - 0.33) * 3.0);
                else
                    color = lerp(col3, col1, (t - 0.66) * 3.0);
                
                return color;
            }
            
            // ============================================
            // EFFECT FUNCTIONS
            // ============================================
            
            // Энергетические потоки
            float energyStreams(float3 pos, float3 normal, float time)
            {
                if (_EnableStreams < 0.5) return 0.0;
                
                float streams = 0.0;
                
                // Вертикальные спиральные потоки
                float angle = atan2(pos.z, pos.x);
                float radius = length(pos.xz);
                
                for (int i = 0; i < int(_StreamCount); i++)
                {
                    float offset = float(i) / _StreamCount * 6.28318;
                    
                    // Спиральный поток
                    float spiralAngle = angle + pos.y * 2.0 + time * _StreamSpeed + offset;
                    float stream = sin(spiralAngle * 3.0) * 0.5 + 0.5;
                    
                    // Делаем поток тонким
                    stream = smoothstep(1.0 - _StreamWidth, 1.0, stream);
                    
                    // Турбулентность
                    float turb = fbm(pos * 3.0 + time + offset, 2);
                    stream *= (1.0 + turb * _StreamTurbulence);
                    
                    // Пульсация
                    float pulse = sin(time * 3.0 + offset + pos.y * 5.0) * 0.5 + 0.5;
                    stream *= pulse;
                    
                    streams += stream;
                }
                
                return saturate(streams) * _StreamIntensity;
            }
            
            // Голографические линии
            float holographicLines(float3 pos, float time)
            {
                if (_EnableHoloLines < 0.5) return 0.0;
                
                // Горизонтальные сканирующие линии
                float scanLine = sin((pos.y + time * _HoloLineSpeed) * _HoloLineFrequency);
                scanLine = smoothstep(1.0 - _HoloLineWidth, 1.0, scanLine);
                
                // Диагональные линии
                float diagLine = sin((pos.x + pos.y + pos.z + time * _HoloLineSpeed * 0.5) * _HoloLineFrequency * 0.5);
                diagLine = smoothstep(1.0 - _HoloLineWidth, 1.0, diagLine) * 0.5;
                
                // Вертикальные линии
                float vertLine = sin((pos.x + time * _HoloLineSpeed * 0.3) * _HoloLineFrequency * 0.3);
                vertLine = smoothstep(1.0 - _HoloLineWidth, 1.0, vertLine) * 0.3;
                
                return (scanLine + diagLine + vertLine) * _HoloLineIntensity;
            }
            
            // Внутренние звёзды/частицы
            float innerStars(float3 pos, float time)
            {
                if (_EnableStars < 0.5) return 0.0;
                
                float stars = 0.0;
                
                // Создаём сетку для звёзд
                float3 gridPos = pos * _StarDensity;
                float3 gridId = floor(gridPos);
                float3 gridUV = frac(gridPos) - 0.5;
                
                // Проверяем соседние ячейки
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            float3 offset = float3(x, y, z);
                            float3 cellId = gridId + offset;
                            
                            // Случайная позиция звезды в ячейке
                            float3 starPos = hash33(cellId) - 0.5;
                            float3 toStar = gridUV - offset - starPos;
                            float dist = length(toStar);
                            
                            // Звезда
                            float star = smoothstep(_StarSize, 0.0, dist);
                            
                            // Мерцание
                            float twinkle = sin(time * _StarTwinkleSpeed + hash31(cellId) * 6.28) * 0.5 + 0.5;
                            star *= twinkle;
                            
                            stars += star;
                        }
                    }
                }
                
                return stars * _StarBrightness;
            }
            
            // Туманность внутри
            float3 nebulaEffect(float3 pos, float time)
            {
                if (_EnableNebula < 0.5) return float3(0, 0, 0);
                
                float3 nebulaPos = pos * _NebulaScale + time * _NebulaSpeed;
                
                // Несколько слоёв шума для туманности
                float nebula1 = fbm(nebulaPos, 4);
                float nebula2 = fbm(nebulaPos * 1.5 + 100.0, 3);
                float nebula3 = fbm(nebulaPos * 0.5 + 200.0, 5);
                
                // Создаём цветные слои
                float3 color1 = _PrimaryColor.rgb * nebula1;
                float3 color2 = _SecondaryColor.rgb * nebula2;
                float3 color3 = _TertiaryColor.rgb * nebula3;
                
                // Турбулентность
                float turb = fbm(nebulaPos * 2.0 + time * 0.5, 3) * _NebulaTurbulence;
                
                float3 nebula = (color1 + color2 * 0.7 + color3 * 0.5) * (1.0 + turb);
                
                return nebula * _NebulaIntensity;
            }
            
            // Иризация (радужные переливы)
            float3 iridescence(float3 viewDir, float3 normal, float time)
            {
                if (_EnableIridescence < 0.5) return float3(0, 0, 0);
                
                float NdotV = dot(normal, viewDir);
                
                // Тонкоплёночная интерференция
                float interference = NdotV * _IridescenceScale + time * _IridescenceSpeed;
                
                // Радужный цвет
                float3 iriColor = rainbow(frac(interference));
                
                // Усиливаем на краях (где угол острее)
                float edgeFactor = 1.0 - abs(NdotV);
                edgeFactor = pow(edgeFactor, 1.5);
                
                return iriColor * edgeFactor * _IridescenceIntensity;
            }
            
            // ============================================
            // VERTEX SHADER
            // ============================================
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float time = _Time.y;
                
                // Анимированное смещение вершин
                float3 pos = input.positionOS.xyz;
                float noise = fbm(pos * _DisplacementScale + time * _DisplacementSpeed, 3);
                float3 displacement = input.normalOS * noise * _VertexDisplacement;
                
                // Дышащий эффект
                float breathe = sin(time * _PulseSpeed) * _PulseIntensity * 0.1;
                displacement += input.normalOS * breathe;
                
                float3 displacedPos = pos + displacement;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(displacedPos);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                output.positionWS = posInputs.positionWS;
                output.positionOS = input.positionOS.xyz;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.tangentWS = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                
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
                
                // ===== БАЗОВЫЕ ВЫЧИСЛЕНИЯ =====
                
                // Fresnel
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                float fresnelEffect = pow(fresnel, _FresnelPower) * _FresnelIntensity;
                
                // Пульсация
                float pulse = sin(time * _PulseSpeed) * _PulseIntensity + 1.0;
                float rimPulse = sin(time * _RimPulseSpeed) * 0.3 + 0.7;
                
                // ===== ЦВЕТОВАЯ БАЗА =====
                
                // Градиент по позиции
                float gradientT = input.positionOS.y * 0.5 + 0.5;
                gradientT += fbm(input.positionOS * 2.0 + time * 0.2, 2) * 0.3;
                float3 baseColor = cosmicGradient(gradientT, time);
                
                // Контраст и насыщенность
                baseColor = pow(baseColor, _ColorContrast);
                float luminance = dot(baseColor, float3(0.299, 0.587, 0.114));
                baseColor = lerp(float3(luminance, luminance, luminance), baseColor, _Saturation);
                
                // ===== ЭФФЕКТЫ =====
                
                // Туманность
                float3 nebula = nebulaEffect(input.positionOS, time);
                
                // Энергетические потоки
                float streams = energyStreams(input.positionOS, normalWS, time);
                float3 streamColor = cosmicGradient(input.positionOS.y + time * 0.5, time) * streams;
                
                // Голографические линии
                float holoLines = holographicLines(input.positionWS, time);
                float3 holoColor = rainbow(frac(input.positionWS.y * 0.5 + time * 0.2)) * holoLines;
                
                // Внутренние звёзды
                float stars = innerStars(input.positionOS, time);
                float3 starColor = _CoreGlow.rgb * stars;
                
                // Иризация
                float3 iriColor = iridescence(viewDirWS, normalWS, time);
                
                // ===== ВНУТРЕННЕЕ СВЕЧЕНИЕ =====
                
                // Имитация подповерхностного рассеивания
                float3 lightDir = normalize(float3(0.5, 1, 0.3));
                float subsurface = saturate(dot(normalWS, -lightDir));
                subsurface = pow(subsurface, _InnerGlowFalloff);
                
                float3 innerGlow = _CoreGlow.rgb * subsurface * _InnerGlowIntensity;
                
                // Ядро свечения (в центре объекта)
                float distFromCenter = length(input.positionOS);
                float coreGlow = 1.0 - smoothstep(0.0, 0.5, distFromCenter);
                innerGlow += _CoreGlow.rgb * coreGlow * 0.5;
                
                // ===== КРАЕВОЕ СВЕЧЕНИЕ =====
                
                float3 rimGlow = _FresnelColor.rgb * fresnelEffect * rimPulse;
                
                // Добавляем радужные переливы на краях
                rimGlow += rainbow(frac(fresnel + time * 0.3)) * fresnelEffect * 0.3;
                
                // ===== ФИНАЛЬНОЕ СМЕШИВАНИЕ =====
                
                half3 finalColor = baseColor * 0.3;
                
                // Добавляем туманность
                finalColor += nebula;
                
                // Добавляем потоки энергии
                finalColor += streamColor;
                
                // Добавляем голографические линии
                finalColor += holoColor;
                
                // Добавляем звёзды
                finalColor += starColor;
                
                // Добавляем иризацию
                finalColor += iriColor;
                
                // Добавляем внутреннее свечение
                finalColor += innerGlow * pulse;
                
                // Добавляем краевое свечение
                finalColor += rimGlow;
                
                // ===== ФИНАЛЬНЫЕ КОРРЕКТИРОВКИ =====
                
                // Общая пульсация яркости
                finalColor *= pulse;
                
                // Блики
                float sparkle = pow(hash31(input.positionWS * 50.0 + time * 10.0), 20.0);
                finalColor += _CoreGlow.rgb * sparkle * 2.0;
                
                // ===== ПРОЗРАЧНОСТЬ =====
                
                // Базовая прозрачность
                float alpha = _Opacity;
                
                // Края более непрозрачные
                alpha = lerp(alpha * 0.5, 1.0, fresnelEffect * 0.5);
                
                // Более яркие области более непрозрачные
                alpha = saturate(alpha + length(finalColor) * 0.1);
                
                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
        
        // ============================================
        // ВТОРОЙ ПРОХОД - ОБРАТНАЯ СТОРОНА
        // ============================================
        Pass
        {
            Name "EtherealEssenceBack"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Front
            
            HLSLPROGRAM
            #pragma vertex vertBack
            #pragma fragment fragBack
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewDirWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _PrimaryColor;
                float4 _SecondaryColor;
                float4 _CoreGlow;
                float _FresnelPower;
                float _FresnelIntensity;
                float _Opacity;
                float _VertexDisplacement;
                float _DisplacementSpeed;
                float _DisplacementScale;
                float _PulseSpeed;
                float _PulseIntensity;
            CBUFFER_END
            
            float hash31_back(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }
            
            float noise3D_back(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                return lerp(
                    lerp(
                        lerp(hash31_back(i), hash31_back(i + float3(1,0,0)), f.x),
                        lerp(hash31_back(i + float3(0,1,0)), hash31_back(i + float3(1,1,0)), f.x),
                        f.y
                    ),
                    lerp(
                        lerp(hash31_back(i + float3(0,0,1)), hash31_back(i + float3(1,0,1)), f.x),
                        lerp(hash31_back(i + float3(0,1,1)), hash31_back(i + float3(1,1,1)), f.x),
                        f.y
                    ),
                    f.z
                );
            }
            
            float fbm_back(float3 p, int octaves)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * noise3D_back(p);
                    amplitude *= 0.5;
                    p *= 2.0;
                }
                return value;
            }
            
            Varyings vertBack(Attributes input)
            {
                Varyings output;
                
                float time = _Time.y;
                float3 pos = input.positionOS.xyz;
                float noise = fbm_back(pos * _DisplacementScale + time * _DisplacementSpeed, 3);
                float breathe = sin(time * _PulseSpeed) * _PulseIntensity * 0.1;
                float3 displacement = input.normalOS * (noise * _VertexDisplacement + breathe);
                
                float3 displacedPos = pos + displacement;
                
                output.positionCS = TransformObjectToHClip(displacedPos);
                output.normalWS = TransformObjectToWorldNormal(-input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(displacedPos));
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }
            
            half4 fragBack(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;
                
                // Внутреннее свечение
                float3 innerColor = lerp(_PrimaryColor.rgb, _SecondaryColor.rgb, input.positionOS.y * 0.5 + 0.5);
                innerColor *= 0.3;
                innerColor += _CoreGlow.rgb * fresnel * 0.5;
                
                float alpha = _Opacity * 0.3 * (1.0 + fresnel * 0.5);
                
                return half4(innerColor, alpha);
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}