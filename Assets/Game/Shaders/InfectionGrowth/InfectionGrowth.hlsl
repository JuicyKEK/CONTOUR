#ifndef INFECTION_GROWTH_INCLUDED
#define INFECTION_GROWTH_INCLUDED

// Рост нароста (ключевое слово _GROWTH): общая часть шейдеров Custom/InfectedConcrete (щупальца) и Custom/WetFlesh (грибок).
// Меш собран из кусков (щупальце, гриб). У каждой вершины в UV3: xyz - точка, из которой растёт её кусок (координаты
// объекта), w - очередь куска (0 - растёт первым, 1 - последним). Общий ход роста _Growth (0..1) двигает вид
// (InfectionSpreadGrowView). Кусок начинает расти, когда до него дошла очередь, и вырастает за долю _GrowthSpan общего
// хода: выползает из своей точки с перехлёстом и, пока растёт, судорожно дёргается (сильнее к свободному концу).
// fresh - насколько кусок ещё свежий (1 - только вылез, 0 - вырос): пиксельная часть делает его мокрым и красным.
// Нужны свойства материала _Growth, _GrowthSpan, _GrowthWrithe (в UnityPerMaterial каждого шейдера).

float InfectionGrowthOf(float4 growthData)
{
    float span = max(_GrowthSpan, 1e-3);
    return saturate((_Growth - growthData.w * (1.0 - span)) / span);
}

float3 InfectionGrow(float3 positionOS, float4 growthData, float freeEnd, float phase, out float fresh)
{
#if defined(_GROWTH)
    float g = InfectionGrowthOf(growthData);

    // Выползает с перехлёстом (ease out back): чуть больше нужного - и оседает.
    float x = g - 1.0;
    float scale = g <= 0.0 ? 0.0 : 1.0 + 2.2 * x * x * x + 1.2 * x * x;
    float3 grown = growthData.xyz + (positionOS - growthData.xyz) * scale;

    // Судороги, пока растёт: частые рывки, у корня слабее, у кончика сильнее.
    float active = 4.0 * g * (1.0 - g);
    float t = _Time.y * 11.0 + phase * 6.2832;
    float3 twitch = float3(sin(t), sin(t * 1.37 + 1.7), cos(t * 0.71 + 0.4)) * sin(t * 0.29 + phase * 3.1);
    grown += twitch * (_GrowthWrithe * active * (0.25 + 0.75 * saturate(freeEnd)));

    fresh = g > 0.0 ? 1.0 - g * g : 0.0;
    return grown;
#else
    fresh = 0.0;
    return positionOS;
#endif
}

// Свежий нарост мокрый и сырой: краснее и глянцевее (fresh из InfectionGrow).
void InfectionGrowthFreshSurface(half fresh, inout half3 albedo, inout half smoothness)
{
#if defined(_GROWTH)
    albedo = lerp(albedo, albedo * half3(1.6, 0.45, 0.42) + half3(0.05, 0.0, 0.0), fresh * 0.7);
    smoothness = lerp(smoothness, 0.95, fresh * 0.8);
#endif
}

#endif
