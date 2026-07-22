Shader "Custom/DarkEntity"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color", Color) = (0, 0, 0, 1)
        [HDR] _EdgeColor ("Edge Color", Color) = (0.1, 0, 0.2, 1)
        _EdgePower ("Edge Power", Range(0.1, 10)) = 3
        _EdgeIntensity ("Edge Intensity", Range(0, 2)) = 1
        
        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.2
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
            Name "DarkEntity"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
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
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _CoreColor;
                float4 _EdgeColor;
                float _EdgePower;
                float _EdgeIntensity;
                float _PulseSpeed;
                float _PulseAmount;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                
                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                
                // Fresnel для светящихся краёв
                float fresnel = 1.0 - saturate(dot(normalWS, viewDirWS));
                fresnel = pow(fresnel, _EdgePower);
                
                // Пульсация
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseAmount + 1.0;
                fresnel *= pulse * _EdgeIntensity;
                
                // Смешиваем ядро и края
                half3 finalColor = lerp(_CoreColor.rgb, _EdgeColor.rgb, fresnel);
                
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
        
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}