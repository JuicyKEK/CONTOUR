#ifndef INFECTED_VEINS_DECAL_INCLUDED
#define INFECTED_VEINS_DECAL_INCLUDED

// Функция для узла Custom Function в InfectedVeinsDecal.shadergraph: чёрные вены со сгустками чёрной крови
// на любой поверхности под декалью (пол, стены, потолок, мебель).
// Узор считается по мировой точке поверхности в 3D - на стыке пола и стены вены не рвутся и не растягиваются,
// а у всех декалей с одним материалом узор общий: перекрывающиеся декали совпадают.
// Края декали гаснут по шуму. Целая часть UV (поле UV Offset у Decal Projector: 0, 1, 2...) - свой рисунок края
// у каждой декали. UV Scale оставлять (1, 1).

#include "Assets/Game/Shaders/InfectedConcrete/InfectedVeinsCore.hlsl"
#include "Assets/Game/Shaders/BlackMold/BlackMoldCommon.hlsl"

void InfectedVeinsDecal_float(float2 UV, float3 PositionWS, float Seed, float VeinScale, float VeinWidth, float VeinWarp, float Branches, float Coverage,
                              float FlowSpeed, float FlowFrequency, float Amount, float4 VeinColor, float4 FlowColor, out float3 Color, out float Alpha)
{
    float2 cell = floor(UV + 1e-4);
    float2 uv = UV - cell;
    float edgeSeed = Seed + cell.x * 13.1 + cell.y * 7.7;

    IC_VeinParams v;
    v.amount = Amount;
    v.scale = VeinScale;
    v.seed = Seed;
    v.warp = VeinWarp;
    v.width = VeinWidth;
    v.branches = Branches;
    v.flowFrequency = FlowFrequency;
    v.flowSpeed = FlowSpeed;
    v.flowWiden = 0.8;
    v.pulseSpeed = 1.3;
    v.pulseStrength = 0.25;
    v.veinHeight = 0;
    v.flowHeight = 0;
    v.time = _Time.y;
    IC_VeinSample s = IC_VeinsCore(PositionWS, v);

    // Край: к границе декали вены редеют и пропадают, граница рваная. Coverage - насколько далеко от центра они доходят.
    float2 so = BM_SeedOffset(edgeSeed);
    float edgeNoise = BM_Fbm(uv * 3.0 + so) - 0.5;
    float r = length(uv - 0.5) * 2.0;
    float reach = lerp(0.35, 1.0, saturate(Coverage));
    float fade = 1.0 - smoothstep(reach * 0.45, reach, r + edgeNoise * 0.4);

    // Вокруг вен - потемнение (как плесень под венами у бетона), по шуму неровное.
    float haloA = s.halo * 0.45 * (0.55 + 0.9 * IC_Noise3(PositionWS * 3.1 + 9.2));
    float a = saturate(s.core + haloA * (1.0 - s.core));
    float3 haloColor = float3(0.035, 0.028, 0.026);
    float3 col = (VeinColor.rgb * s.core + haloColor * haloA * (1.0 - s.core)) / max(a, 1e-4);
    Color = lerp(col, FlowColor.rgb, s.flow);
    Alpha = saturate(a * fade * VeinColor.a);
}

void InfectedVeinsDecal_half(float2 UV, float3 PositionWS, float Seed, float VeinScale, float VeinWidth, float VeinWarp, float Branches, float Coverage,
                             float FlowSpeed, float FlowFrequency, float Amount, float4 VeinColor, float4 FlowColor, out float3 Color, out float Alpha)
{
    InfectedVeinsDecal_float(UV, PositionWS, Seed, VeinScale, VeinWidth, VeinWarp, Branches, Coverage, FlowSpeed, FlowFrequency, Amount, VeinColor, FlowColor, Color, Alpha);
}

#endif
