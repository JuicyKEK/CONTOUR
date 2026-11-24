#ifndef CLEANSE_INPUT_INCLUDED
#define CLEANSE_INPUT_INCLUDED

// Общие свойства всех шейдеров очищения. Каждый шейдер показывает в инспекторе только свои,
// остальные остаются нулями и не используются.
CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    half4 _BaseColor;
    half _BumpScale;
    half _Smoothness;
    half _Metallic;
    float _TileMeters;

    float _CleanseProgress;
    float4 _Source0;
    float4 _Source1;
    float4 _Source2;
    float4 _Source3;
    float4 _BoundsMin;
    float4 _BoundsMax;
    float _NoiseScale;
    float _FrontNoise;
    float _ArrivalMax;

    half4 _GlowColor;
    half _GlowIntensity;
    float _GlowWidth;
    half4 _CharColor;
    float _CharWidth;
    float _ScorchWidth;
    float _CurlAmount;

    half4 _AcidColor;
    half4 _AcidDarkColor;
    float4 _MeltBoundsMin;
    float4 _MeltBoundsMax;
    half4 _FoamColor;
    half _AcidGlow;
    float _MeltSag;
    float _MeltSpread;
    float _MeltDrips;
    float _MeltWobble;

    float _PitScale;
    float _PitSpread;
    float _FoamWidth;

    half4 _AshColor;
    float _AshWidth;
    float _CrackScale;
    float _CrumbleWidth;
    float _FallDistance;
CBUFFER_END

TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

#endif
