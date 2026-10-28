#ifndef BLACK_MOLD_DECAL_INCLUDED
#define BLACK_MOLD_DECAL_INCLUDED

// Функция для узла Custom Function в BlackMoldDecal.shadergraph.
// Разный узор у разных декалей с одним материалом: целая часть UV (поле UV Offset у Decal Projector:
// 0, 1, 2... или 0;3, 5;1...) добавляется к сиду, дробная - координата внутри пятна. UV Scale оставлять (1, 1).

#include "Assets/Game/Shaders/BlackMold/BlackMoldCommon.hlsl"

void BlackMoldDecal_float(float2 UV, UnityTexture2D Tex, float Seed, float PatternScale, float Coverage, float Softness, float Amount, float DetailScale, float4 Tint, out float3 Color, out float Alpha)
{
    float2 cell = floor(UV + 1e-4);
    float2 uv = UV - cell;
    float seed = Seed + cell.x * 13.1 + cell.y * 7.7;
    float mask = BM_Mask(uv, seed, PatternScale, Coverage, Softness);
    float4 detail = SAMPLE_TEXTURE2D(Tex.tex, Tex.samplerstate, uv * DetailScale + BM_SeedOffset(seed) * 0.01);
    Color = detail.rgb * Tint.rgb;
    Alpha = saturate(mask * lerp(0.7, 1.0, detail.a) * Amount * Tint.a);
}

void BlackMoldDecal_half(float2 UV, UnityTexture2D Tex, float Seed, float PatternScale, float Coverage, float Softness, float Amount, float DetailScale, float4 Tint, out float3 Color, out float Alpha)
{
    BlackMoldDecal_float(UV, Tex, Seed, PatternScale, Coverage, Softness, Amount, DetailScale, Tint, Color, Alpha);
}

#endif
