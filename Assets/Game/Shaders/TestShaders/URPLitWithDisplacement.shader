Shader "Custom/URPLitWithDisplacement"
{
    Properties
    {
        [Header(Base Material)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        
        [Space(10)]
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.0
        
        [Space(10)]
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        
        [Space(30)]
        [Header(DEFORMATION EFFECTS)]
        
        // ===== WAVE =====
        [Space(10)]
        [Toggle(EFFECT_WAVE)] _EnableWave("Enable Wave", Float) = 0
        [ShowIf(EFFECT_WAVE)] _WaveAmplitude("Wave Amplitude", Float) = 0.5
        [ShowIf(EFFECT_WAVE)] _WaveFrequency("Wave Frequency", Float) = 5.0
        [ShowIf(EFFECT_WAVE)] _WaveSpeed("Wave Speed", Float) = 2.0
        [ShowIf(EFFECT_WAVE)] _WaveDirection("Wave Direction", Vector) = (1, 0, 0, 0)
        
        // ===== TWIST =====
        [Space(10)]
        [Toggle(EFFECT_TWIST)] _EnableTwist("Enable Twist", Float) = 0
        [ShowIf(EFFECT_TWIST)] _TwistAmount("Twist Amount", Float) = 1.0
        [ShowIf(EFFECT_TWIST)] _TwistSpeed("Twist Speed", Float) = 0.5
        [ShowIf(EFFECT_TWIST)] _TwistCenter("Twist Center Height", Float) = 0.0
        
        // ===== PULSE =====
        [Space(10)]
        [Toggle(EFFECT_PULSE)] _EnablePulse("Enable Pulse", Float) = 0
        [ShowIf(EFFECT_PULSE)] _PulseAmount("Pulse Amount", Float) = 0.3
        [ShowIf(EFFECT_PULSE)] _PulseSpeed("Pulse Speed", Float) = 1.0
        
        // ===== NOISE =====
        [Space(10)]
        [Toggle(EFFECT_NOISE)] _EnableNoise("Enable Noise", Float) = 0
        [ShowIf(EFFECT_NOISE)] _NoiseAmount("Noise Amount", Float) = 0.2
        [ShowIf(EFFECT_NOISE)] _NoiseScale("Noise Scale", Float) = 1.0
        [ShowIf(EFFECT_NOISE)] _NoiseSpeed("Noise Speed", Float) = 0.1
        
        // ===== WIND =====
        [Space(10)]
        [Toggle(EFFECT_WIND)] _EnableWind("Enable Wind", Float) = 0
        [ShowIf(EFFECT_WIND)] _WindDirection("Wind Direction", Vector) = (1, 0, 0, 0)
        [ShowIf(EFFECT_WIND)] _WindSpeed("Wind Speed", Float) = 1.0
        [ShowIf(EFFECT_WIND)] _WindStrength("Wind Strength", Float) = 0.5
        [ShowIf(EFFECT_WIND)] _WindTurbulence("Wind Turbulence", Float) = 1.0
        
        // ===== BEND =====
        [Space(10)]
        [Toggle(EFFECT_BEND)] _EnableBend("Enable Bend", Float) = 0
        [ShowIf(EFFECT_BEND)] _BendAmount("Bend Amount", Float) = 0.5
        [ShowIf(EFFECT_BEND)] _BendDirection("Bend Direction", Vector) = (1, 0, 0, 0)
        
        // ===== MELT =====
        [Space(10)]
        [Toggle(EFFECT_MELT)] _EnableMelt("Enable Melt", Float) = 0
        [ShowIf(EFFECT_MELT)] _MeltHeight("Melt Start Height", Float) = 0.0
        [ShowIf(EFFECT_MELT)] _MeltRange("Melt Range", Float) = 1.0
        [ShowIf(EFFECT_MELT)] _MeltSpeed("Melt Speed", Float) = 0.5
        
        // ===== RIPPLE =====
        [Space(10)]
        [Toggle(EFFECT_RIPPLE)] _EnableRipple("Enable Ripple", Float) = 0
        [ShowIf(EFFECT_RIPPLE)] _RippleCenter("Ripple Center", Vector) = (0, 0, 0, 0)
        [ShowIf(EFFECT_RIPPLE)] _RippleAmount("Ripple Amount", Float) = 0.5
        [ShowIf(EFFECT_RIPPLE)] _RippleSpeed("Ripple Speed", Float) = 3.0
        [ShowIf(EFFECT_RIPPLE)] _RippleFrequency("Ripple Frequency", Float) = 5.0
        
        [Space(20)]
        [Header(Lighting)]
        _AmbientBoost("Ambient Boost", Range(0, 2)) = 0.3
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300
        
        // ============================================
        // FORWARD LIT PASS
        // ============================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // Effect toggles
            #pragma shader_feature_local EFFECT_WAVE
            #pragma shader_feature_local EFFECT_TWIST
            #pragma shader_feature_local EFFECT_PULSE
            #pragma shader_feature_local EFFECT_NOISE
            #pragma shader_feature_local EFFECT_WIND
            #pragma shader_feature_local EFFECT_BEND
            #pragma shader_feature_local EFFECT_MELT
            #pragma shader_feature_local EFFECT_RIPPLE

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                float2 lightmapUV   : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv                       : TEXCOORD0;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);
                
                float3 positionWS               : TEXCOORD2;
                float3 normalWS                 : TEXCOORD3;
                
                #ifdef _NORMALMAP
                    float4 tangentWS            : TEXCOORD4;
                #endif

                float3 viewDirWS                : TEXCOORD5;
                half4 fogFactorAndVertexLight   : TEXCOORD6;
                
                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    float4 shadowCoord          : TEXCOORD7;
                #endif

                float4 positionCS               : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BaseMap);            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);            SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _BumpScale;
                half _Metallic;
                half _Smoothness;
                half _OcclusionStrength;
                half _AmbientBoost;
                
                // Wave
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float4 _WaveDirection;
                
                // Twist
                float _TwistAmount;
                float _TwistSpeed;
                float _TwistCenter;
                
                // Pulse
                float _PulseAmount;
                float _PulseSpeed;
                
                // Noise
                float _NoiseAmount;
                float _NoiseScale;
                float _NoiseSpeed;
                
                // Wind
                float4 _WindDirection;
                float _WindSpeed;
                float _WindStrength;
                float _WindTurbulence;
                
                // Bend
                float _BendAmount;
                float4 _BendDirection;
                
                // Melt
                float _MeltHeight;
                float _MeltRange;
                float _MeltSpeed;
                
                // Ripple
                float4 _RippleCenter;
                float _RippleAmount;
                float _RippleSpeed;
                float _RippleFrequency;
            CBUFFER_END

            // ============================================
            // DEFORMATION FUNCTIONS
            // ============================================
            
            // Simple noise function
            float SimpleNoise(float3 pos)
            {
                return frac(sin(dot(pos, float3(12.9898, 78.233, 45.164))) * 43758.5453);
            }

            // Apply all active deformations
            float3 ApplyDeformations(float3 positionOS, float3 normalOS)
            {
                float3 deformedPos = positionOS;
                
                #ifdef EFFECT_WAVE
                {
                    // Wave along custom direction
                    float waveInput = dot(deformedPos, _WaveDirection.xyz) * _WaveFrequency + _Time.y * _WaveSpeed;
                    float wave = sin(waveInput) * _WaveAmplitude;
                    deformedPos += normalOS * wave;
                }
                #endif
                
                #ifdef EFFECT_TWIST
                {
                    // Twist around Y axis
                    float heightFromCenter = deformedPos.y - _TwistCenter;
                    float angle = heightFromCenter * _TwistAmount + _Time.y * _TwistSpeed;
                    float c = cos(angle);
                    float s = sin(angle);
                    
                    float3 twisted = deformedPos;
                    twisted.x = deformedPos.x * c - deformedPos.z * s;
                    twisted.z = deformedPos.x * s + deformedPos.z * c;
                    deformedPos = twisted;
                }
                #endif
                
                #ifdef EFFECT_PULSE
                {
                    // Pulsing expansion/contraction
                    float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount;
                    deformedPos += normalOS * pulse;
                }
                #endif
                
                #ifdef EFFECT_NOISE
                {
                    // Noise displacement
                    float3 noisePos = deformedPos * _NoiseScale + _Time.y * _NoiseSpeed;
                    float noise = SimpleNoise(noisePos);
                    deformedPos += normalOS * (noise - 0.5) * _NoiseAmount;
                }
                #endif
                
                #ifdef EFFECT_WIND
                {
                    // Wind effect (stronger at top)
                    float heightFactor = saturate(deformedPos.y + 0.5); // 0 at bottom, 1 at top
                    
                    // Turbulent wind pattern
                    float windWave = sin(deformedPos.x * _WindTurbulence + _Time.y * _WindSpeed) * 
                                   cos(deformedPos.z * _WindTurbulence * 0.7 + _Time.y * _WindSpeed * 0.8);
                    
                    float3 windOffset = _WindDirection.xyz * windWave * _WindStrength * heightFactor;
                    deformedPos += windOffset;
                }
                #endif
                
                #ifdef EFFECT_BEND
                {
                    // Bend in direction
                    float bendFactor = deformedPos.y * _BendAmount;
                    deformedPos += _BendDirection.xyz * bendFactor;
                }
                #endif
                
                #ifdef EFFECT_MELT
                {
                    // Melting/gravity effect
                    float falloff = saturate((deformedPos.y - _MeltHeight) / _MeltRange);
                    float meltAmount = _Time.y * _MeltSpeed * (1.0 - falloff);
                    deformedPos.y -= meltAmount;
                }
                #endif
                
                #ifdef EFFECT_RIPPLE
                {
                    // Ripple from center point
                    float2 posXZ = deformedPos.xz;
                    float2 centerXZ = _RippleCenter.xz;
                    float dist = distance(posXZ, centerXZ);
                    
                    float ripple = sin(dist * _RippleFrequency - _Time.y * _RippleSpeed) * 
                                 exp(-dist * 0.5) * _RippleAmount;
                    
                    deformedPos += normalOS * ripple;
                }
                #endif
                
                return deformedPos;
            }

            // ============================================
            // VERTEX SHADER
            // ============================================
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // === APPLY DEFORMATIONS ===
                float3 positionOS = input.positionOS.xyz;
                float3 normalOS = input.normalOS;
                
                positionOS = ApplyDeformations(positionOS, normalOS);
                
                // === TRANSFORMS ===
                VertexPositionInputs vertexInput = GetVertexPositionInputs(positionOS);
                VertexNormalInputs normalInput = GetVertexNormalInputs(normalOS, input.tangentOS);

                half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
                half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = viewDirWS;
                
                #ifdef _NORMALMAP
                    real sign = input.tangentOS.w * GetOddNegativeScale();
                    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
                    output.tangentWS = tangentWS;
                #endif

                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

                output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
                output.positionWS = vertexInput.positionWS;

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    output.shadowCoord = GetShadowCoord(vertexInput);
                #endif

                output.positionCS = vertexInput.positionCS;

                return output;
            }

            // ============================================
            // FRAGMENT SHADER
            // ============================================
            half4 LitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 albedoAlpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                albedoAlpha *= _BaseColor;

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).g;
                occlusion = LerpWhiteTo(occlusion, _OcclusionStrength);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedoAlpha.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.smoothness = _Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.occlusion = occlusion;
                surfaceData.alpha = 1.0;
                surfaceData.emission = 0;
                surfaceData.specular = 0;
                surfaceData.clearCoatMask = 0;
                surfaceData.clearCoatSmoothness = 0;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.viewDirectionWS = SafeNormalize(input.viewDirWS);

                #ifdef _NORMALMAP
                    float sgn = input.tangentWS.w;
                    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
                #else
                    inputData.normalWS = input.normalWS;
                #endif

                inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);

                #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
                    inputData.shadowCoord = input.shadowCoord;
                #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
                    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                #else
                    inputData.shadowCoord = float4(0, 0, 0, 0);
                #endif

                inputData.fogCoord = input.fogFactorAndVertexLight.x;
                inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
                inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
                
                #ifndef LIGHTMAP_ON
                    inputData.bakedGI += half3(_AmbientBoost, _AmbientBoost, _AmbientBoost);
                #endif
                
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                half4 color = UniversalFragmentPBR(inputData, surfaceData);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                color.rgb += surfaceData.albedo * 0.3; // Добавляем ambient

                return color;
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
            Cull Back

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // Same effect toggles
            #pragma shader_feature_local EFFECT_WAVE
            #pragma shader_feature_local EFFECT_TWIST
            #pragma shader_feature_local EFFECT_PULSE
            #pragma shader_feature_local EFFECT_NOISE
            #pragma shader_feature_local EFFECT_WIND
            #pragma shader_feature_local EFFECT_BEND
            #pragma shader_feature_local EFFECT_MELT
            #pragma shader_feature_local EFFECT_RIPPLE

            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
            };

            float4 _BaseMap_ST;
            
            // All deformation parameters (same as ForwardLit)
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float4 _WaveDirection;
            float _TwistAmount;
            float _TwistSpeed;
            float _TwistCenter;
            float _PulseAmount;
            float _PulseSpeed;
            float _NoiseAmount;
            float _NoiseScale;
            float _NoiseSpeed;
            float4 _WindDirection;
            float _WindSpeed;
            float _WindStrength;
            float _WindTurbulence;
            float _BendAmount;
            float4 _BendDirection;
            float _MeltHeight;
            float _MeltRange;
            float _MeltSpeed;
            float4 _RippleCenter;
            float _RippleAmount;
            float _RippleSpeed;
            float _RippleFrequency;

            // === SAME DEFORMATION FUNCTIONS ===
            float SimpleNoise(float3 pos)
            {
                return frac(sin(dot(pos, float3(12.9898, 78.233, 45.164))) * 43758.5453);
            }

            float3 ApplyDeformations(float3 positionOS, float3 normalOS)
            {
                float3 deformedPos = positionOS;
                
                #ifdef EFFECT_WAVE
                {
                    float waveInput = dot(deformedPos, _WaveDirection.xyz) * _WaveFrequency + _Time.y * _WaveSpeed;
                    float wave = sin(waveInput) * _WaveAmplitude;
                    deformedPos += normalOS * wave;
                }
                #endif
                
                #ifdef EFFECT_TWIST
                {
                    float heightFromCenter = deformedPos.y - _TwistCenter;
                    float angle = heightFromCenter * _TwistAmount + _Time.y * _TwistSpeed;
                    float c = cos(angle);
                    float s = sin(angle);
                    float3 twisted = deformedPos;
                    twisted.x = deformedPos.x * c - deformedPos.z * s;
                    twisted.z = deformedPos.x * s + deformedPos.z * c;
                    deformedPos = twisted;
                }
                #endif
                
                #ifdef EFFECT_PULSE
                {
                    float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount;
                    deformedPos += normalOS * pulse;
                }
                #endif
                
                #ifdef EFFECT_NOISE
                {
                    float3 noisePos = deformedPos * _NoiseScale + _Time.y * _NoiseSpeed;
                    float noise = SimpleNoise(noisePos);
                    deformedPos += normalOS * (noise - 0.5) * _NoiseAmount;
                }
                #endif
                
                #ifdef EFFECT_WIND
                {
                    float heightFactor = saturate(deformedPos.y + 0.5);
                    float windWave = sin(deformedPos.x * _WindTurbulence + _Time.y * _WindSpeed) * 
                                   cos(deformedPos.z * _WindTurbulence * 0.7 + _Time.y * _WindSpeed * 0.8);
                    float3 windOffset = _WindDirection.xyz * windWave * _WindStrength * heightFactor;
                    deformedPos += windOffset;
                }
                #endif
                
                #ifdef EFFECT_BEND
                {
                    float bendFactor = deformedPos.y * _BendAmount;
                    deformedPos += _BendDirection.xyz * bendFactor;
                }
                #endif
                
                #ifdef EFFECT_MELT
                {
                    float falloff = saturate((deformedPos.y - _MeltHeight) / _MeltRange);
                    float meltAmount = _Time.y * _MeltSpeed * (1.0 - falloff);
                    deformedPos.y -= meltAmount;
                }
                #endif
                
                #ifdef EFFECT_RIPPLE
                {
                    float2 posXZ = deformedPos.xz;
                    float2 centerXZ = _RippleCenter.xz;
                    float dist = distance(posXZ, centerXZ);
                    float ripple = sin(dist * _RippleFrequency - _Time.y * _RippleSpeed) * 
                                 exp(-dist * 0.5) * _RippleAmount;
                    deformedPos += normalOS * ripple;
                }
                #endif
                
                return deformedPos;
            }

            float4 GetShadowPositionHClip(Attributes input)
            {
                // === APPLY SAME DEFORMATIONS ===
                float3 positionOS = ApplyDeformations(input.positionOS.xyz, input.normalOS);

                float3 positionWS = TransformObjectToWorld(positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    Light mainLight = GetMainLight();
                    float3 lightDirectionWS = mainLight.direction;
                #endif

                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = GetShadowPositionHClip(input);
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }

        // ============================================
        // DEPTH ONLY PASS
        // ============================================
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma shader_feature_local EFFECT_WAVE
            #pragma shader_feature_local EFFECT_TWIST
            #pragma shader_feature_local EFFECT_PULSE
            #pragma shader_feature_local EFFECT_NOISE
            #pragma shader_feature_local EFFECT_WIND
            #pragma shader_feature_local EFFECT_BEND
            #pragma shader_feature_local EFFECT_MELT
            #pragma shader_feature_local EFFECT_RIPPLE

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 position     : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _BaseMap_ST;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float4 _WaveDirection;
            float _TwistAmount;
            float _TwistSpeed;
            float _TwistCenter;
            float _PulseAmount;
            float _PulseSpeed;
            float _NoiseAmount;
            float _NoiseScale;
            float _NoiseSpeed;
            float4 _WindDirection;
            float _WindSpeed;
            float _WindStrength;
            float _WindTurbulence;
            float _BendAmount;
            float4 _BendDirection;
            float _MeltHeight;
            float _MeltRange;
            float _MeltSpeed;
            float4 _RippleCenter;
            float _RippleAmount;
            float _RippleSpeed;
            float _RippleFrequency;

            float SimpleNoise(float3 pos)
            {
                return frac(sin(dot(pos, float3(12.9898, 78.233, 45.164))) * 43758.5453);
            }

            float3 ApplyDeformations(float3 positionOS, float3 normalOS)
            {
                float3 deformedPos = positionOS;
                
                #ifdef EFFECT_WAVE
                    float waveInput = dot(deformedPos, _WaveDirection.xyz) * _WaveFrequency + _Time.y * _WaveSpeed;
                    float wave = sin(waveInput) * _WaveAmplitude;
                    deformedPos += normalOS * wave;
                #endif
                
                #ifdef EFFECT_TWIST
                    float heightFromCenter = deformedPos.y - _TwistCenter;
                    float angle = heightFromCenter * _TwistAmount + _Time.y * _TwistSpeed;
                    float c = cos(angle);
                    float s = sin(angle);
                    float3 twisted = deformedPos;
                    twisted.x = deformedPos.x * c - deformedPos.z * s;
                    twisted.z = deformedPos.x * s + deformedPos.z * c;
                    deformedPos = twisted;
                #endif
                
                #ifdef EFFECT_PULSE
                    float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount;
                    deformedPos += normalOS * pulse;
                #endif
                
                #ifdef EFFECT_NOISE
                    float3 noisePos = deformedPos * _NoiseScale + _Time.y * _NoiseSpeed;
                    float noise = SimpleNoise(noisePos);
                    deformedPos += normalOS * (noise - 0.5) * _NoiseAmount;
                #endif
                
                #ifdef EFFECT_WIND
                    float heightFactor = saturate(deformedPos.y + 0.5);
                    float windWave = sin(deformedPos.x * _WindTurbulence + _Time.y * _WindSpeed) * 
                                   cos(deformedPos.z * _WindTurbulence * 0.7 + _Time.y * _WindSpeed * 0.8);
                    deformedPos += _WindDirection.xyz * windWave * _WindStrength * heightFactor;
                #endif
                
                #ifdef EFFECT_BEND
                    float bendFactor = deformedPos.y * _BendAmount;
                    deformedPos += _BendDirection.xyz * bendFactor;
                #endif
                
                #ifdef EFFECT_MELT
                    float falloff = saturate((deformedPos.y - _MeltHeight) / _MeltRange);
                    deformedPos.y -= _Time.y * _MeltSpeed * (1.0 - falloff);
                #endif
                
                #ifdef EFFECT_RIPPLE
                    float dist = distance(deformedPos.xz, _RippleCenter.xz);
                    float ripple = sin(dist * _RippleFrequency - _Time.y * _RippleSpeed) * 
                                 exp(-dist * 0.5) * _RippleAmount;
                    deformedPos += normalOS * ripple;
                #endif
                
                return deformedPos;
            }

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = ApplyDeformations(input.position.xyz, input.normalOS);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = TransformObjectToHClip(positionOS);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return 0;
            }
            ENDHLSL
        }

        // ============================================
        // DEPTH NORMALS PASS
        // ============================================
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma shader_feature_local EFFECT_WAVE
            #pragma shader_feature_local EFFECT_TWIST
            #pragma shader_feature_local EFFECT_PULSE
            #pragma shader_feature_local EFFECT_NOISE
            #pragma shader_feature_local EFFECT_WIND
            #pragma shader_feature_local EFFECT_BEND
            #pragma shader_feature_local EFFECT_MELT
            #pragma shader_feature_local EFFECT_RIPPLE

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            float4 _BaseMap_ST;
            float _BumpScale;
            float _WaveAmplitude;
            float _WaveFrequency;
            float _WaveSpeed;
            float4 _WaveDirection;
            float _TwistAmount;
            float _TwistSpeed;
            float _TwistCenter;
            float _PulseAmount;
            float _PulseSpeed;
            float _NoiseAmount;
            float _NoiseScale;
            float _NoiseSpeed;
            float4 _WindDirection;
            float _WindSpeed;
            float _WindStrength;
            float _WindTurbulence;
            float _BendAmount;
            float4 _BendDirection;
            float _MeltHeight;
            float _MeltRange;
            float _MeltSpeed;
            float4 _RippleCenter;
            float _RippleAmount;
            float _RippleSpeed;
            float _RippleFrequency;

            float SimpleNoise(float3 pos)
            {
                return frac(sin(dot(pos, float3(12.9898, 78.233, 45.164))) * 43758.5453);
            }

            float3 ApplyDeformations(float3 positionOS, float3 normalOS)
            {
                float3 deformedPos = positionOS;
                
                #ifdef EFFECT_WAVE
                    float waveInput = dot(deformedPos, _WaveDirection.xyz) * _WaveFrequency + _Time.y * _WaveSpeed;
                    deformedPos += normalOS * sin(waveInput) * _WaveAmplitude;
                #endif
                
                #ifdef EFFECT_TWIST
                    float angle = (deformedPos.y - _TwistCenter) * _TwistAmount + _Time.y * _TwistSpeed;
                    float c = cos(angle);
                    float s = sin(angle);
                    float3 twisted = deformedPos;
                    twisted.x = deformedPos.x * c - deformedPos.z * s;
                    twisted.z = deformedPos.x * s + deformedPos.z * c;
                    deformedPos = twisted;
                #endif
                
                #ifdef EFFECT_PULSE
                    deformedPos += normalOS * sin(_Time.y * _PulseSpeed) * _PulseAmount;
                #endif
                
                #ifdef EFFECT_NOISE
                    float noise = SimpleNoise(deformedPos * _NoiseScale + _Time.y * _NoiseSpeed);
                    deformedPos += normalOS * (noise - 0.5) * _NoiseAmount;
                #endif
                
                #ifdef EFFECT_WIND
                    float heightFactor = saturate(deformedPos.y + 0.5);
                    float windWave = sin(deformedPos.x * _WindTurbulence + _Time.y * _WindSpeed) * 
                                   cos(deformedPos.z * _WindTurbulence * 0.7 + _Time.y * _WindSpeed * 0.8);
                    deformedPos += _WindDirection.xyz * windWave * _WindStrength * heightFactor;
                #endif
                
                #ifdef EFFECT_BEND
                    deformedPos += _BendDirection.xyz * deformedPos.y * _BendAmount;
                #endif
                
                #ifdef EFFECT_MELT
                    float falloff = saturate((deformedPos.y - _MeltHeight) / _MeltRange);
                    deformedPos.y -= _Time.y * _MeltSpeed * (1.0 - falloff);
                #endif
                
                #ifdef EFFECT_RIPPLE
                    float dist = distance(deformedPos.xz, _RippleCenter.xz);
                    float ripple = sin(dist * _RippleFrequency - _Time.y * _RippleSpeed) * exp(-dist * 0.5) * _RippleAmount;
                    deformedPos += normalOS * ripple;
                #endif
                
                return deformedPos;
            }

            Varyings DepthNormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionOS = ApplyDeformations(input.positionOS.xyz, input.normalOS);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = TransformObjectToHClip(positionOS);

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;

                return output;
            }

            half4 DepthNormalsFragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 normalWS = normalize(input.normalWS);
                
                return float4(PackNormalOctRectEncode(TransformWorldToViewDir(normalWS, true)), 0.0, 0.0);
            }
            ENDHLSL
        }

        // ============================================
        // META PASS
        // ============================================
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"

            ENDHLSL
        }
    }
    
    CustomEditor "DeformationShaderGUI"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}