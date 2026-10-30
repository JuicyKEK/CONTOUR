#ifndef INFECTED_CONCRETE_COMMON_INCLUDED
#define INFECTED_CONCRETE_COMMON_INCLUDED

// Общая часть шейдера Custom/InfectedConcrete: сеть чёрных вен на бетоне, по венам бегут сгустки чёрной крови,
// поверхность вздувается под венами и под сгустками. Одна и та же функция считается в вершинах (сдвиг поверхности,
// во всех пассах - чтобы тени и глубина совпадали) и в пикселях (цвет, мелкий рельеф).
// Узор привязан к координатам объекта: префаб можно двигать и поворачивать, вены едут вместе с ним.
// Сам узор вен - в InfectedVeinsCore.hlsl (его же использует декаль вен).

#include "Assets/Game/Shaders/InfectedConcrete/InfectedVeinsCore.hlsl"

IC_VeinSample IC_Veins(float3 positionOS)
{
    IC_VeinParams v;
    v.amount = _InfectionAmount;
    v.scale = _VeinScale;
    v.seed = _VeinSeed;
    v.warp = _VeinWarp;
    v.width = _VeinWidth;
    v.branches = _VeinBranches;
    v.flowFrequency = _FlowFrequency;
    v.flowSpeed = _FlowSpeed;
    v.flowWiden = _FlowWiden;
    v.pulseSpeed = _PulseSpeed;
    v.pulseStrength = _PulseStrength;
    v.veinHeight = _VeinHeight;
    v.flowHeight = _FlowHeight;
    v.time = _Time.y;
    return IC_VeinsCore(positionOS, v);
}

// Сдвиг вершины по нормали на высоту вен и новая нормаль (по двум соседним точкам на поверхности).
void IC_Displace(float3 positionOS, float3 normalOS, out float3 displacedOS, out float3 displacedNormalOS)
{
    float3 n = normalize(normalOS);
    float3 tangent = normalize(cross(n, abs(n.y) < 0.95 ? float3(0, 1, 0) : float3(1, 0, 0)));
    float3 bitangent = cross(n, tangent);
    const float eps = 0.006;
    float h0 = IC_Veins(positionOS).height;
    float ht = IC_Veins(positionOS + tangent * eps).height;
    float hb = IC_Veins(positionOS + bitangent * eps).height;
    displacedOS = positionOS + n * h0;
    float3 pt = positionOS + tangent * eps + n * ht;
    float3 pb = positionOS + bitangent * eps + n * hb;
    displacedNormalOS = normalize(cross(pt - displacedOS, pb - displacedOS));
}

// Шевеление щупалец (только с включённым Sway, ключевое слово _TENTACLE_SWAY).
// Цвет вершины: R - насколько точка свободна (0 - лежит на поверхности или у корня, 1 - кончик), G - фаза щупальца.
// Сдвиг растёт к кончику, фаза бежит вдоль щупальца - оно изгибается волной, а не качается палкой.
// Узор вен считается по точке до сдвига, поэтому вены едут вместе со щупальцем.
// Reach: свободные концы тянутся к _ReachTarget.xyz (мир) с силой _ReachTarget.w - не дальше _ReachAmount и не дальше
// половины расстояния до цели.
float3 IC_Sway(float3 positionOS, float4 color)
{
#if defined(_TENTACLE_SWAY)
    float w = saturate(color.r);
    float ph = color.g * 6.2832;
    float t = _Time.y * _SwaySpeed;
    float k = w * _SwayCurl;
    float3 o = float3(sin(t + ph + k),
                      0.6 * sin(t * 1.37 + ph * 1.7 + k * 1.3),
                      cos(t * 0.83 + ph * 2.3 + k * 0.8));
    float3 targetOS = mul(GetWorldToObjectMatrix(), float4(_ReachTarget.xyz, 1.0)).xyz;
    float3 toTarget = targetOS - positionOS;
    float dist = length(toTarget);
    float3 reach = toTarget / max(dist, 1e-3) * min(dist * 0.5, _ReachAmount) * saturate(_ReachTarget.w) * w * w;
    return o * (_SwayAmount * w * w) + reach;
#else
    return 0;
#endif
}

void IC_DisplaceAll(float3 positionOS, float3 normalOS, float4 color, out float3 displacedOS, out float3 displacedNormalOS)
{
    IC_Displace(positionOS, normalOS, displacedOS, displacedNormalOS);
    displacedOS += IC_Sway(positionOS, color);
}

// Рельеф по высоте в пикселе (поверхностный градиент, Mikkelsen 2010) - чёткие края вен на редкой сетке.
float3 IC_BumpFromHeight(float3 positionWS, float3 n, float h, float strength)
{
    float3 dpdx = ddx(positionWS);
    float3 dpdy = ddy(positionWS);
    float dhdx = ddx(h) * strength;
    float dhdy = ddy(h) * strength;
    float3 r1 = cross(dpdy, n);
    float3 r2 = cross(n, dpdx);
    float det = dot(dpdx, r1);
    float3 grad = sign(det) * (dhdx * r1 + dhdy * r2);
    return normalize(abs(det) * n - grad);
}

#endif
