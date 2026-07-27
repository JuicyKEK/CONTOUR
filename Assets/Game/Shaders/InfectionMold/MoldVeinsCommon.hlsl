#ifndef MOLD_VEINS_COMMON_INCLUDED
#define MOLD_VEINS_COMMON_INCLUDED

// Общие данные и функции для шейдера "заражённых сосудов" (MoldVeinsInfection).
// Подключается во всех пассах (Forward, ShadowCaster, DepthOnly), чтобы вздутие
// сосудов одинаково смещало вершины везде - иначе тень и depth-буфер не будут
// совпадать с видимым (вспученным) силуэтом объекта.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;

    float4 _VeinColor;
    float4 _VeinPulseColor;
    float _VeinWidth;
    float _VeinCellScale;
    float _VeinBranchWarp;

    float _NoiseScale;
    float _NoiseOctaves;
    float _PatternWarp;
    float _EdgeWidth;
    float _InfectionAmount;

    float _FlowSpeed;
    float _FlowFrequency;
    float _FlowSharpness;
    float _PulseSpeed;
    float _PulseStrength;

    float _CrawlSpeed;
    float _CrawlAmount;

    float _BulgeAmount;
    float _BulgeScale;
    float _BulgeWidth;
    float _BulgeNormalStrength;
    float _FlowBulgeAmount;

    float _AmbientContribution;
CBUFFER_END

// ---- Хэши и value-noise (как в оригинальном MoldInfection) ----
float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float2 hash2(float2 p)
{
    float2 q = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(q) * 43758.5453);
}

float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float fbm(float2 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    [unroll]
    for (int i = 0; i < 4; i++)
    {
        if (i >= octaves) break;
        value += amplitude * valueNoise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// Worley/cellular шум - расстояния до ближайшей (f1) и второй по близости (f2)
// случайной точки в сетке. Разница (f2 - f1) стремится к нулю на границах
// ячеек - это и даёт тонкую ветвящуюся сеть линий, похожую на сосуды/прожилки.
// cellId - идентификатор ближайшей ячейки, нужен, чтобы рассинхронизировать
// пульс разных "веток" сосудов между собой.
void Worley(float2 uv, out float f1, out float f2, out float2 cellId)
{
    float2 p = floor(uv);
    float2 f = frac(uv);

    f1 = 8.0;
    f2 = 8.0;
    cellId = 0.0;

    [unroll]
    for (int y = -1; y <= 1; y++)
    {
        [unroll]
        for (int x = -1; x <= 1; x++)
        {
            float2 g = float2(x, y);
            float2 o = hash2(p + g);
            float2 delta = g + o - f;
            float dist = dot(delta, delta);

            if (dist < f1)
            {
                f2 = f1;
                f1 = dist;
                cellId = p + g;
            }
            else if (dist < f2)
            {
                f2 = dist;
            }
        }
    }

    f1 = sqrt(f1);
    f2 = sqrt(f2);
}

// Крупная органичная область заражения на стене (как в оригинальном
// MoldInfection) - растёт вместе с _InfectionAmount. Сеть сосудов видна
// только внутри этой области.
float ComputeInfectionRegionMask(float2 uv)
{
    float2 crawlOffset = float2(
        valueNoise(float2(_Time.y * _CrawlSpeed, 3.1)),
        valueNoise(float2(1.7, _Time.y * _CrawlSpeed))
    ) * _CrawlAmount;

    float2 warpUV = uv * _NoiseScale;
    float2 warp = float2(
        valueNoise(warpUV * 0.5 + 7.3 + crawlOffset),
        valueNoise(warpUV * 0.5 - 4.1 + crawlOffset)
    ) * _PatternWarp;

    float noiseVal = fbm(warpUV + warp + crawlOffset, (int)_NoiseOctaves);

    float threshold = lerp(1.05, -0.05, _InfectionAmount);
    return smoothstep(threshold - _EdgeWidth, threshold + _EdgeWidth, noiseVal);
}

// Сеть сосудов по Worley-шуму с заданным масштабом ячеек/шириной линий.
// Возвращает lineMask (0..1, максимум на самой линии сосуда), f1 (расстояние
// до центра ближайшей "клетки" - по нему потом бежит импульс) и cellId.
float ComputeVeinNetwork(float2 uv, float cellScale, float width, out float f1, out float2 cellId)
{
    float2 warpedUV = uv * cellScale;
    float2 warp = float2(
        valueNoise(warpedUV * 0.7 + 11.0),
        valueNoise(warpedUV * 0.7 - 5.0)
    ) * _VeinBranchWarp;

    float f2;
    Worley(warpedUV + warp, f1, f2, cellId);

    float edge = f2 - f1;
    return 1.0 - smoothstep(0.0, max(width, 1e-4), edge);
}

// "Шарик", бегущий вдоль сосуда наружу от центра ветки (от f1 = 0). Это не
// размытое расширяющееся кольцо по всей сети, а компактное пятно с чётким
// радиусом около 2x ширины сосуда (width) - выглядит как отдельная капля
// жидкости/крови, движущаяся по трубе, а не как общее свечение.
// cellId даёт каждой ветке свою случайную фазу и скорость, чтобы шарики не
// бежали синхронно по всем сосудам разом.
float ComputeFlowPulse(float f1, float2 cellId, float width)
{
    float speedVariance = 0.6 + hash(cellId + 17.0) * 0.8;
    float phaseOffset = hash(cellId);

    // Радиус самого шарика - примерно вдвое больше ширины линии сосуда.
    float ballRadius = max(width * 2.0, 1e-3);

    // Шарик двигается вдоль радиальной координаты f1 (расстояние до центра
    // ветки) и, дойдя до предела диапазона, закольцовывается - снова
    // появляется у центра, как будто по сосуду толкается новая порция.
    float loopRange = max(1.0 / max(_FlowFrequency, 1e-3), ballRadius * 2.0);
    float ballPos = frac(_Time.y * _FlowSpeed * speedVariance / loopRange + phaseOffset) * loopRange;

    float dist = abs(f1 - ballPos);
    dist = min(dist, loopRange - dist); // закольцовка диапазона

    return pow(saturate(1.0 - dist / ballRadius), _FlowSharpness);
}

// Высота "вздутия" сосуда для смещения вершин, УЖЕ в объектных единицах
// (готова к прибавлению к позиции вдоль нормали). Складывается из двух частей:
//  1) статическое вздутие самой "трубы" сосуда (_BulgeAmount) - используется
//     отдельный, более крупный масштаб (_BulgeScale), т.к. вершинная сетка
//     обычно намного реже пикселей и тонкие капиллярные линии всё равно не
//     будет видно в геометрии, поэтому вспучиваются только крупные "стволы";
//  2) дополнительный локальный подъём (_FlowBulgeAmount) там, где именно
//     сейчас проходит бегущий по сосуду шарик - создаёт эффект "волны",
//     катящейся вместе с движением жидкости/крови.
float ComputeBulgeHeight(float2 uv)
{
    float regionMask = ComputeInfectionRegionMask(uv);

    float f1;
    float2 cellId;
    float bulgeVeinMask = ComputeVeinNetwork(uv, _BulgeScale, _BulgeWidth, f1, cellId);
    float baseMask = bulgeVeinMask * regionMask;

    float flowPulse = ComputeFlowPulse(f1, cellId, _BulgeWidth) * baseMask;

    return baseMask * _BulgeAmount + flowPulse * _FlowBulgeAmount;
}

#endif





