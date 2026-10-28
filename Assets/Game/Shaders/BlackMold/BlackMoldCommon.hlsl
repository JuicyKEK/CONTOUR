#ifndef BLACK_MOLD_COMMON_INCLUDED
#define BLACK_MOLD_COMMON_INCLUDED

// Общая часть чёрной плесени для заплатки (Custom/BlackMoldPatch) и декали (BlackMoldDecal.shadergraph).
// Форма пятна задаётся шумом в координатах пятна (0..1): колонии, отдельные точки спор, края всегда гаснут
// к границе - квадрат заплатки/декали не виден. Мелкий рисунок даёт текстура BlackMold (RGBA).

float BM_Hash12(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

float BM_Noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(BM_Hash12(i), BM_Hash12(i + float2(1, 0)), u.x),
                lerp(BM_Hash12(i + float2(0, 1)), BM_Hash12(i + float2(1, 1)), u.x), u.y);
}

float BM_Fbm(float2 p)
{
    float s = 0.0;
    float a = 0.5;
    [unroll] for (int k = 0; k < 4; k++)
    {
        s += a * BM_Noise(p);
        p = p * 2.03 + 7.1;
        a *= 0.5;
    }

    return s / 0.9375;
}

// Сид -> смещение шума: разные сиды - разные пятна.
float2 BM_SeedOffset(float seed)
{
    return float2(BM_Hash12(float2(seed, 1.37)), BM_Hash12(float2(seed, 8.91))) * 113.0;
}

// Плотность плесени в точке пятна: 0 - чисто, 1 - сплошная плесень.
// patternScale - сколько колоний на пятно, coverage - насколько пятно заполнено, softness - мягкость краёв колоний.
// Колонии - пятнами по шуму (в середине пятна гуще), вокруг - мелкие отдельные колонии и точки спор; к краю всё гаснет.
float BM_Mask(float2 uv, float seed, float patternScale, float coverage, float softness)
{
    float2 s = BM_SeedOffset(seed);
    float2 c = uv - 0.5;
    float edgeNoise = BM_Fbm(uv * 3.0 + s) - 0.5;
    float radial = 1.0 - smoothstep(0.12, 0.5, length(c) + edgeNoise * 0.25);
    float warp = BM_Fbm(uv * patternScale * 0.5 + s + 3.1);
    float n = BM_Fbm(uv * patternScale + s + warp * 1.4);
    float soft = max(softness, 0.005);
    float threshold = lerp(0.75, 0.3, coverage);
    float colonies = smoothstep(threshold - soft, threshold + soft, n + radial * 0.25 - 0.125);
    float n2 = BM_Noise(uv * patternScale * 3.5 + s * 2.0);
    float satellites = smoothstep(0.8 - 0.12 * coverage - soft, 0.8 - 0.12 * coverage + soft, n2) * smoothstep(0.1, 0.6, radial);
    float spores = step(0.9 - 0.06 * coverage, BM_Noise(uv * patternScale * 9.0 + s * 3.0)) * smoothstep(0.05, 0.5, radial) * 0.8;
    return saturate(max(max(colonies, satellites), spores)) * radial;
}

#endif
