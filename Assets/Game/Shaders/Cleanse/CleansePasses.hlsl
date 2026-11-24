#ifndef CLEANSE_PASSES_INCLUDED
#define CLEANSE_PASSES_INCLUDED

// Проходы шейдеров очищения. Перед подключением проход задаёт один из define:
// CLEANSE_PASS_FORWARD, CLEANSE_PASS_SHADOW, CLEANSE_PASS_DEPTH, CLEANSE_PASS_DEPTHNORMALS.
// Сдвиг вершин и вырезание одинаковые во всех проходах - иначе дыры отбрасывают тень и мерцают в глубине.

#if defined(CLEANSE_PASS_FORWARD)

struct Attributes
{
    float4 positionOS       : POSITION;
    float3 normalOS         : NORMAL;
    float4 tangentOS        : TANGENT;
    float2 uv               : TEXCOORD0;
    float2 staticLightmapUV : TEXCOORD1;
    float2 dynamicLightmapUV: TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
    float3 positionWS   : TEXCOORD1;
    float3 normalWS     : TEXCOORD2;
    float4 tangentWS    : TEXCOORD3;
    half4 fogFactorAndVertexLight : TEXCOORD4;
    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 5);
#ifdef DYNAMICLIGHTMAP_ON
    float2 dynamicLightmapUV : TEXCOORD6;
#endif
#ifdef USE_APV_PROBE_OCCLUSION
    float4 probeOcclusion : TEXCOORD7;
#endif
    float4 positionOSMelt : TEXCOORD8;   // xyz - точка объекта до сдвига, w - сколько расплавилось
    float3 normalOS       : TEXCOORD9;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    float melt;
    Cleanse_Vertex(positionOS, normalOS, melt);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(normalOS, input.tangentOS);
    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    output.positionOSMelt = float4(input.positionOS.xyz, melt);
    output.normalOS = input.normalOS;

    half3 vertexLight = VertexLighting(positionInputs.positionWS, output.normalWS);
    output.fogFactorAndVertexLight = half4(ComputeFogFactor(positionInputs.positionCS.z), vertexLight);

    OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH4(positionInputs.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(positionInputs.positionWS), output.vertexSH, output.probeOcclusion);
    return output;
}

half4 frag(Varyings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float3 positionOS = input.positionOSMelt.xyz;
    float melt = input.positionOSMelt.w;

#if defined(_CLEANSE_TRIPLANAR)
    float3 nOS = normalize(input.normalOS);
    float3 w = pow(abs(nOS), 4.0);
    w /= max(w.x + w.y + w.z, 1e-5);
    float3 tp = positionOS / max(_TileMeters, 1e-3);
    half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, tp.zy) * w.x
                     + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, tp.xz) * w.y
                     + SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, tp.xy) * w.z;
#else
    half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
#endif

    half3 albedo = baseSample.rgb * _BaseColor.rgb;
    half smoothness = _Smoothness;
    half metallic = _Metallic;
    half3 emission = 0;
    float clipValue;
    Cleanse_Surface(positionOS, melt, albedo, smoothness, metallic, emission, clipValue);
    clip(clipValue);

    float3 normalWS = normalize(input.normalWS);
#if !defined(_CLEANSE_TRIPLANAR)
    if (dot(input.tangentWS.xyz, input.tangentWS.xyz) > 1e-4)
    {
        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
        float3 bitangent = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
        normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS)));
    }
#endif

    // изнанка видна через прогоревшие дыры - она темнее
    if (!IS_FRONT_VFACE(facing, true, false))
    {
        normalWS = -normalWS;
        albedo *= 0.4;
    }

    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.positionCS = input.positionCS;
    inputData.normalWS = normalWS;
    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamicLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#elif !defined(LIGHTMAP_ON) && (defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2))
    inputData.bakedGI = SAMPLE_GI(input.vertexSH, GetAbsolutePositionWS(inputData.positionWS), inputData.normalWS,
                                  inputData.viewDirectionWS, input.positionCS.xy, input.probeOcclusion, inputData.shadowMask);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
#endif

    SurfaceData surface = (SurfaceData)0;
    surface.albedo = albedo;
    surface.alpha = 1;
    surface.metallic = metallic;
    surface.specular = 0;
    surface.normalTS = half3(0, 0, 1);
    surface.occlusion = 1;
    surface.smoothness = smoothness;
    surface.emission = emission;

    half4 color = UniversalFragmentPBR(inputData, surface);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = 1;
    return color;
}

#endif

#if defined(CLEANSE_PASS_SHADOW)

float3 _LightDirection;
float3 _LightPosition;

struct ShadowAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct ShadowVaryings
{
    float4 positionCS     : SV_POSITION;
    float4 positionOSMelt : TEXCOORD0;
};

ShadowVaryings ShadowVert(ShadowAttributes input)
{
    ShadowVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    float melt;
    Cleanse_Vertex(positionOS, normalOS, melt);
    float3 positionWS = TransformObjectToWorld(positionOS);
    float3 normalWS = TransformObjectToWorldNormal(normalOS);
#if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    output.positionCS = ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS)));
    output.positionOSMelt = float4(input.positionOS.xyz, melt);
    return output;
}

half4 ShadowFrag(ShadowVaryings input) : SV_Target
{
    clip(Cleanse_Clip(input.positionOSMelt.xyz, input.positionOSMelt.w));
    return 0;
}

#endif

#if defined(CLEANSE_PASS_DEPTH)

struct DepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthVaryings
{
    float4 positionCS     : SV_POSITION;
    float4 positionOSMelt : TEXCOORD0;
};

DepthVaryings DepthVert(DepthAttributes input)
{
    DepthVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    float melt;
    Cleanse_Vertex(positionOS, normalOS, melt);
    output.positionCS = TransformObjectToHClip(positionOS);
    output.positionOSMelt = float4(input.positionOS.xyz, melt);
    return output;
}

half DepthFrag(DepthVaryings input) : SV_Target
{
    clip(Cleanse_Clip(input.positionOSMelt.xyz, input.positionOSMelt.w));
    return input.positionCS.z;
}

#endif

#if defined(CLEANSE_PASS_DEPTHNORMALS)

struct NormalsAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct NormalsVaryings
{
    float4 positionCS     : SV_POSITION;
    float3 normalWS       : TEXCOORD0;
    float4 positionOSMelt : TEXCOORD1;
};

NormalsVaryings NormalsVert(NormalsAttributes input)
{
    NormalsVaryings output;
    UNITY_SETUP_INSTANCE_ID(input);
    float3 positionOS = input.positionOS.xyz;
    float3 normalOS = input.normalOS;
    float melt;
    Cleanse_Vertex(positionOS, normalOS, melt);
    output.positionCS = TransformObjectToHClip(positionOS);
    output.normalWS = TransformObjectToWorldNormal(normalOS);
    output.positionOSMelt = float4(input.positionOS.xyz, melt);
    return output;
}

half4 NormalsFrag(NormalsVaryings input, FRONT_FACE_TYPE facing : FRONT_FACE_SEMANTIC) : SV_Target
{
    clip(Cleanse_Clip(input.positionOSMelt.xyz, input.positionOSMelt.w));
    float3 normalWS = normalize(input.normalWS);
    normalWS = IS_FRONT_VFACE(facing, true, false) ? normalWS : -normalWS;
#if defined(_GBUFFER_NORMALS_OCT)
    float2 octNormalWS = PackNormalOctQuadEncode(normalWS);
    float2 remapped = saturate(octNormalWS * 0.5 + 0.5);
    return half4(PackFloat2To888(remapped), 0.0);
#else
    return half4(normalWS, 0.0);
#endif
}

#endif

#endif
