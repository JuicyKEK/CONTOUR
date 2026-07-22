Shader "Custom/JitteryShadowInFog_Cone"
{
    Properties
    {
        [Header(Silhouette)]
        [HDR] _SilhouetteColor ("Silhouette Color", Color) = (0, 0, 0, 1)
        [HDR] _FogColor ("Fog Color", Color) = (0.1, 0.1, 0.15, 1)
        [HDR] _FogEdgeColor ("Fog Edge Glow", Color) = (0.2, 0.1, 0.3, 1)
        
        [Header(Cone Settings)]
        _ConeHeight ("Cone Height", Float) = 2.0
        _ConeBaseRadius ("Cone Base Radius", Float) = 1.0
        [Toggle] _ApexAtTop ("Apex At Top", Float) = 1
        _ConeAxisTilt ("Axis Tilt", Vector) = (0, 1, 0, 0)
        
        [Header(Jitter Effect)]
        _JitterIntensity ("Jitter Intensity", Range(0, 0.2)) = 0.05
        _JitterSpeed ("Jitter Speed", Range(1, 100)) = 30
        _JitterFrequency ("Jitter Frequency", Range(1, 20)) = 5
        [Toggle] _RandomJitter ("Random Jitter", Float) = 1
        _JitterFalloffAtApex ("Jitter Falloff At Apex", Range(0, 1)) = 0.5
        
        [Header(Rotating Fog)]
        _FogDensity ("Fog Density", Range(0, 2)) = 1
        _FogScale ("Fog Scale", Range(1, 20)) = 5
        _FogRotationSpeed ("Fog Rotation Speed", Range(0, 5)) = 1
        _FogLayerSpeed ("Fog Layer Speed", Range(0, 3)) = 0.5
        _FogTurbulence ("Fog Turbulence", Range(0, 2)) = 0.8
        
        [Header(Spiral Effect)]
        _SpiralTightness ("Spiral Tightness", Range(0, 10)) = 3
        _SpiralArms ("Spiral Arms", Range(1, 8)) = 3
        _SpiralFlowSpeed ("Spiral Flow Speed", Range(0, 5)) = 1
        
        [Header(Vertical Flow)]
        _VerticalFlowSpeed ("Vertical Flow Speed", Range(0, 5)) = 1
        _VerticalFlowIntensity ("Vertical Flow Intensity", Range(0, 2)) = 0.8
        [Toggle] _FlowTowardApex ("Flow Toward Apex", Float) = 1
        
        [Header(Fog Layers)]
        _InnerFogRadius ("Inner Fog Radius", Range(0, 1)) = 0.3
        _OuterFogRadius ("Outer Fog Radius", Range(0.5, 2)) = 1.2
        
        [Header(Fresnel)]
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 2
        _FresnelIntensity ("Fresnel Intensity", Range(0, 3)) = 1.5
        
        [Header(Distortion)]
        _SilhouetteDistortion ("Silhouette Edge Distortion", Range(0, 0.5)) = 0.1
        _DistortionSpeed ("Distortion Speed", Range(0, 10)) = 3
        
        [Header(Apex Glow)]
        [Toggle] _EnableApexGlow ("Enable Apex Glow", Float) = 1
        [HDR] _ApexGlowColor ("Apex Glow Color", Color) = (0.5, 0.2, 0.8, 1)
        _ApexGlowRadius ("Apex Glow Radius", Range(0.1, 1)) = 0.3
        _ApexGlowIntensity ("Apex Glow Intensity", Range(0, 3)) = 1.5
        _ApexPulseSpeed ("Apex Pulse Speed", Range(0, 10)) = 2
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
            Name "JitteryShadowFogCone"
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
                float2 coneCoords : TEXCOORD6; // x = normalized height, y = angle
                float coneRadius : TEXCOORD7;
            };
            
            // ============================================
            // PROPERTIES
            // ============================================
            
            CBUFFER_START(UnityPerMaterial)
                float4 _SilhouetteColor;
                float4 _FogColor;
                float4 _FogEdgeColor;
                
                // Cone
                float _ConeHeight;
                float _ConeBaseRadius;
                float _ApexAtTop;
                float4 _ConeAxisTilt;
                
                // Jitter
                float _JitterIntensity;
                float _JitterSpeed;
                float _JitterFrequency;
                float _RandomJitter;
                float _JitterFalloffAtApex;
                
                // Fog
                float _FogDensity;
                float _FogScale;
                float _FogRotationSpeed;
                float _FogLayerSpeed;
                float _FogTurbulence;
                
                // Spiral
                float _SpiralTightness;
                float _SpiralArms;
                float _SpiralFlowSpeed;
                
                // Vertical Flow
                float _VerticalFlowSpeed;
                float _VerticalFlowIntensity;
                float _FlowTowardApex;
                
                // Fog Layers
                float _InnerFogRadius;
                float _OuterFogRadius;
                
                // Fresnel
                float _FresnelPower;
                float _FresnelIntensity;
                
                // Distortion
                float _SilhouetteDistortion;
                float _DistortionSpeed;
                
                // Apex Glow
                float _EnableApexGlow;
                float4 _ApexGlowColor;
                float _ApexGlowRadius;
                float _ApexGlowIntensity;
                float _ApexPulseSpeed;
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
            // CONE COORDINATE FUNCTIONS
            // ============================================
            
            // Преобразование в конические координаты
            // Возвращает: x = нормализованная высота (0 = основание, 1 = вершина)
            //             y = угол вокруг оси
            //             z = нормализованный радиус на текущей высоте
            float3 toConeCoordinates(float3 pos)
            {
                // Нормализуем высоту
                float height;
                if (_ApexAtTop > 0.5)
                {
                    // Вершина наверху: основание внизу
                    height = (pos.y + _ConeHeight * 0.5) / _ConeHeight;
                }
                else
                {
                    // Вершина внизу: основание наверху
                    height = 1.0 - (pos.y + _ConeHeight * 0.5) / _ConeHeight;
                }
                height = saturate(height);
                
                // Угол вокруг оси
                float angle = atan2(pos.z, pos.x);
                
                // Ожидаемый радиус на этой высоте
                float expectedRadius = _ConeBaseRadius * (1.0 - height);
                
                // Фактический радиус
                float actualRadius = length(pos.xz);
                
                // Нормализованный радиус (0 = на оси, 1 = на поверхности)
                float normalizedRadius = expectedRadius > 0.001 ? actualRadius / expectedRadius : 0.0;
                
                return float3(height, angle, normalizedRadius);
            }
            
            // Получить позицию вершины конуса
            float3 getConeApex()
            {
                if (_ApexAtTop > 0.5)
                {
                    return float3(0, _ConeHeight * 0.5, 0);
                }
                else
                {
                    return float3(0, -_ConeHeight * 0.5, 0);
                }
            }
            
            // Расстояние до оси конуса
            float distanceToAxis(float3 pos)
            {
                return length(pos.xz);
            }
            
            // ============================================
            // JITTER FUNCTIONS (адаптированные для конуса)
            // ============================================
            
            float3 calculateConeJitter(float3 position, float3 normal, float coneHeight, float time)
            {
                float3 jitter = float3(0, 0, 0);
                float vertexSeed = hash31(position * 100.0);
                
                // Уменьшаем jitter к вершине конуса
                float heightFactor = 1.0 - coneHeight * _JitterFalloffAtApex;
                heightFactor = max(0.1, heightFactor);
                
                // Радиальное направление (от оси)
                float2 radialDir = length(position.xz) > 0.001 ? 
                    normalize(position.xz) : float2(1, 0);
                
                // Тангенциальное направление (вокруг оси)
                float2 tangentDir = float2(-radialDir.y, radialDir.x);
                
                if (_RandomJitter > 0.5)
                {
                    float randomTime = floor(time * _JitterSpeed);
                    float3 randomOffset = hash33(float3(vertexSeed, randomTime, vertexSeed * 2.0));
                    float shouldJitter = step(0.7, hash11(randomTime + vertexSeed));
                    jitter = randomOffset * shouldJitter * _JitterIntensity * heightFactor;
                }
                
                // Радиальный jitter
                float radialJitter = sin(time * _JitterSpeed + position.y * _JitterFrequency) 
                                   * sin(time * _JitterSpeed * 1.3 + coneHeight * 10.0);
                radialJitter = sign(radialJitter) * pow(abs(radialJitter), 0.5);
                
                // Тангенциальный jitter (вокруг оси)
                float tangentJitter = cos(time * _JitterSpeed * 0.9 + position.y * _JitterFrequency * 0.8)
                                    * sin(time * _JitterSpeed * 1.1);
                tangentJitter = sign(tangentJitter) * pow(abs(tangentJitter), 0.5);
                
                // Вертикальный jitter
                float verticalJitter = sin(time * _JitterSpeed * 0.8 + length(position.xz) * _JitterFrequency * 1.2);
                verticalJitter = sign(verticalJitter) * pow(abs(verticalJitter), 0.5);
                
                // Комбинируем в конических координатах
                jitter.xz += radialDir * radialJitter * _JitterIntensity * 0.5 * heightFactor;
                jitter.xz += tangentDir * tangentJitter * _JitterIntensity * 0.3 * heightFactor;
                jitter.y += verticalJitter * _JitterIntensity * 0.4 * heightFactor;
                
                // Периодические сильные "вздрагивания"
                float spasm = sin(time * 3.0) * step(0.95, sin(time * 7.0 + vertexSeed * 10.0));
                jitter += normal * spasm * _JitterIntensity * 2.0 * heightFactor;
                
                return jitter;
            }
            
            // ============================================
            // CONE FOG FUNCTIONS
            // ============================================
            
            // Спиральный туман, закручивающийся вокруг конуса
            float coneSpiralFog(float3 pos, float3 coneCoords, float time)
            {
                float height = coneCoords.x;
                float angle = coneCoords.y;
                float radius = coneCoords.z;
                
                // Направление потока (к вершине или от неё)
                float flowDir = _FlowTowardApex > 0.5 ? 1.0 : -1.0;
                
                // Спираль закручивается при движении вверх
                float spiralAngle = angle + height * _SpiralTightness * 6.28318;
                spiralAngle += time * _FogRotationSpeed;
                spiralAngle += flowDir * time * _SpiralFlowSpeed;
                
                // Рукава спирали
                float spiral = sin(spiralAngle * _SpiralArms) * 0.5 + 0.5;
                
                // Делаем спираль более контрастной
                spiral = smoothstep(0.3, 0.7, spiral);
                
                // Затухание к оси и к краям
                float radialFade = smoothstep(0.0, 0.3, radius) * smoothstep(1.2, 0.8, radius);
                spiral *= radialFade;
                
                // Вертикальный поток
                float verticalFlow = sin(height * 10.0 - time * _VerticalFlowSpeed * flowDir) * 0.5 + 0.5;
                verticalFlow *= smoothstep(0.0, 0.2, height) * smoothstep(1.0, 0.8, height);
                
                return spiral + verticalFlow * _VerticalFlowIntensity * 0.3;
            }
            
            // Вращающийся туман адаптированный для конуса
            float rotatingConeFog(float3 pos, float3 coneCoords, float time)
            {
                float height = coneCoords.x;
                float angle = coneCoords.y;
                
                // Ожидаемый радиус на этой высоте
                float expectedRadius = _ConeBaseRadius * (1.0 - height);
                
                // Вращаем координаты
                float rotatedAngle = angle + time * _FogRotationSpeed;
                
                // Создаём координаты для шума с учётом формы конуса
                float3 noisePos = float3(
                    cos(rotatedAngle) * expectedRadius,
                    height * _ConeHeight,
                    sin(rotatedAngle) * expectedRadius
                );
                
                // Несколько слоёв тумана
                float fog1 = fbm(noisePos * _FogScale + time * _FogLayerSpeed, 4);
                float fog2 = fbm(noisePos * _FogScale * 0.5 - time * _FogLayerSpeed * 0.7 + 100.0, 3);
                float fog3 = fbm(noisePos * _FogScale * 2.0 + time * _FogLayerSpeed * 1.3 + 200.0, 2);
                
                // Спиральный компонент
                float spiralFog = coneSpiralFog(pos, coneCoords, time);
                
                // Комбинируем
                float fog = fog1 * 0.4 + fog2 * 0.25 + fog3 * 0.15 + spiralFog * 0.4;
                
                // Турбулентность
                float turb = fbm(noisePos * 3.0 + float3(time, -time * 0.5, time * 0.3), 3);
                fog += turb * _FogTurbulence * 0.2;
                
                // Затухание к вершине (туман концентрируется у основания)
                fog *= smoothstep(1.0, 0.3, height) * 0.7 + 0.3;
                
                return saturate(fog);
            }
            
            // Вертикальные потоки вдоль конуса
            float verticalConeStreams(float3 pos, float3 coneCoords, float time)
            {
                float height = coneCoords.x;
                float angle = coneCoords.y;
                
                float flowDir = _FlowTowardApex > 0.5 ? 1.0 : -1.0;
                
                float streams = 0.0;
                
                // Несколько потоков, распределённых по углу
                for (int i = 0; i < 4; i++)
                {
                    float streamAngle = float(i) * 1.5708; // PI/2
                    float angleDiff = abs(sin((angle - streamAngle) * 2.0));
                    
                    // Ширина потока
                    float stream = smoothstep(0.3, 0.0, angleDiff);
                    
                    // Движение вдоль высоты
                    float flow = sin(height * 8.0 - time * _VerticalFlowSpeed * flowDir + float(i)) * 0.5 + 0.5;
                    stream *= flow;
                    
                    // Шум
                    float noise = fbm(float3(angle + float(i), height * 5.0 - time, 0), 2);
                    stream *= (0.7 + noise * 0.5);
                    
                    streams += stream;
                }
                
                return saturate(streams * 0.4) * _VerticalFlowIntensity;
            }
            
            // Свечение вершины
            float apexGlow(float3 pos, float time)
            {
                if (_EnableApexGlow < 0.5) return 0.0;
                
                float3 apex = getConeApex();
                float dist = length(pos - apex);
                
                // Основное свечение
                float glow = 1.0 - smoothstep(0.0, _ApexGlowRadius, dist);
                
                // Пульсация
                float pulse = sin(time * _ApexPulseSpeed) * 0.3 + 0.7;
                glow *= pulse;
                
                // Лучи от вершины
                float angle = atan2(pos.z, pos.x);
                float rays = sin(angle * 6.0 + time * 2.0) * 0.5 + 0.5;
                rays *= smoothstep(_ApexGlowRadius, 0.0, dist);
                
                glow += rays * 0.3;
                
                return glow * _ApexGlowIntensity;
            }
            
            // ============================================
            // VERTEX SHADER
            // ============================================
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float time = _Time.y;
                
                // Вычисляем конические координаты
                float3 coneCoords = toConeCoordinates(input.positionOS.xyz);
                
                // Рассчитываем jitter с учётом конуса
                float3 jitter = calculateConeJitter(input.positionOS.xyz, input.normalOS, coneCoords.x, time);
                float3 jitteredPos = input.positionOS.xyz + jitter;
                
                // Дышащий эффект (сильнее у основания)
                float breatheStrength = 1.0 - coneCoords.x * 0.5;
                float breathe = sin(time * 2.0 + coneCoords.x * 3.0) * 0.02 * breatheStrength;
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
                output.coneCoords = coneCoords.xy;
                output.coneRadius = coneCoords.z;
                
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
                
                // Восстанавливаем конические координаты
                float3 coneCoords = float3(input.coneCoords.x, input.coneCoords.y, input.coneRadius);
                
                // ===== FRESNEL =====
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _FresnelPower) * _FresnelIntensity;
                
                // ===== ВРАЩАЮЩИЙСЯ КОНИЧЕСКИЙ ТУМАН =====
                float fogPattern = rotatingConeFog(input.positionOS, coneCoords, time);
                float verticalStreams = verticalConeStreams(input.positionOS, coneCoords, time);
                
                float combinedFog = fogPattern + verticalStreams;
                combinedFog = saturate(combinedFog * _FogDensity);
                
                // ===== ИСКАЖЕНИЕ КРАЁВ =====
                float edgeDistortion = fbm(input.positionWS * 10.0 + time * _DistortionSpeed, 3);
                float distortedFresnel = fresnel + edgeDistortion * _SilhouetteDistortion;
                distortedFresnel = saturate(distortedFresnel);
                
                // ===== ЗОНЫ =====
                float coreMask = 1.0 - smoothstep(0.0, _InnerFogRadius, distortedFresnel);
                float fogZone = smoothstep(_InnerFogRadius, _OuterFogRadius, distortedFresnel);
                fogZone *= combinedFog;
                
                float edgeGlow = smoothstep(0.5, 1.0, distortedFresnel);
                edgeGlow *= (1.0 + sin(time * 3.0) * 0.2);
                
                // ===== СВЕЧЕНИЕ ВЕРШИНЫ =====
                float apex = apexGlow(input.positionOS, time);
                
                // ===== МЕРЦАНИЕ =====
                float flicker = 1.0;
                if (input.jitterAmount > 0.01)
                {
                    flicker = 0.8 + hash11(time * 50.0) * 0.2;
                }
                
                // ===== ЦВЕТА =====
                half3 silhouetteCol = _SilhouetteColor.rgb * flicker;
                half3 fogCol = _FogColor.rgb * combinedFog;
                half3 edgeCol = _FogEdgeColor.rgb * edgeGlow;
                half3 apexCol = _ApexGlowColor.rgb * apex;
                
                // ===== ФИНАЛЬНОЕ СМЕШИВАНИЕ =====
                half3 finalColor = silhouetteCol;
                
                // Добавляем туман
                finalColor = lerp(finalColor, fogCol, fogZone * 0.7);
                
                // Добавляем свечение краёв
                finalColor += edgeCol * fresnel;
                
                // Добавляем свечение вершины
                finalColor += apexCol;
                
                // Спиральные акценты
                float spiralAccent = coneSpiralFog(input.positionOS, coneCoords, time);
                finalColor += _FogEdgeColor.rgb * spiralAccent * 0.15;
                
                // Случайные вспышки
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
            
            float _ConeHeight;
            float _ApexAtTop;
            float _JitterIntensity;
            float _JitterSpeed;
            float _JitterFrequency;
            float _RandomJitter;
            float _JitterFalloffAtApex;
            
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
                
                // Вычисляем высоту для cone jitter
                float height;
                if (_ApexAtTop > 0.5)
                    height = (position.y + _ConeHeight * 0.5) / _ConeHeight;
                else
                    height = 1.0 - (position.y + _ConeHeight * 0.5) / _ConeHeight;
                height = saturate(height);
                
                float heightFactor = max(0.1, 1.0 - height * _JitterFalloffAtApex);
                
                float vertexSeed = hash31_shadow(position * 100.0);
                float3 jitter = float3(0, 0, 0);
                
                if (_RandomJitter > 0.5)
                {
                    float randomTime = floor(time * _JitterSpeed);
                    float3 randomOffset = hash33_shadow(float3(vertexSeed, randomTime, vertexSeed * 2.0));
                    float shouldJitter = step(0.7, hash11_shadow(randomTime + vertexSeed));
                    jitter = randomOffset * shouldJitter * _JitterIntensity * heightFactor;
                }
                
                float jitterX = sin(time * _JitterSpeed + position.y * _JitterFrequency);
                float jitterY = sin(time * _JitterSpeed * 0.8 + position.x * _JitterFrequency * 1.2);
                float jitterZ = cos(time * _JitterSpeed * 0.9 + position.y * _JitterFrequency * 0.8);
                
                jitter += float3(jitterX, jitterY, jitterZ) * _JitterIntensity * 0.3 * heightFactor;
                
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
    }
}