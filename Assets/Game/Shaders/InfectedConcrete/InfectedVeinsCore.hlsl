#ifndef INFECTED_VEINS_CORE_INCLUDED
#define INFECTED_VEINS_CORE_INCLUDED

// Узор чёрных вен со сгустками - общий для шейдера Custom/InfectedConcrete (стены, щупальца) и декали
// InfectedVeinsDecal.shadergraph (вены на окружающих поверхностях). Все настройки передаются параметрами,
// своих свойств материала у этого файла нет.

float3 IC_Hash33(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

float IC_Hash13(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

float IC_Noise3(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    float n000 = IC_Hash13(i);
    float n100 = IC_Hash13(i + float3(1, 0, 0));
    float n010 = IC_Hash13(i + float3(0, 1, 0));
    float n110 = IC_Hash13(i + float3(1, 1, 0));
    float n001 = IC_Hash13(i + float3(0, 0, 1));
    float n101 = IC_Hash13(i + float3(1, 0, 1));
    float n011 = IC_Hash13(i + float3(0, 1, 1));
    float n111 = IC_Hash13(i + float3(1, 1, 1));
    return lerp(lerp(lerp(n000, n100, u.x), lerp(n010, n110, u.x), u.y),
                lerp(lerp(n001, n101, u.x), lerp(n011, n111, u.x), u.y), u.z);
}

float3 IC_Warp(float3 p)
{
    return float3(IC_Noise3(p), IC_Noise3(p + 17.13), IC_Noise3(p + 41.71)) * 2.0 - 1.0;
}

// Ячейки Вороного в 3D: f1, f2 - расстояния до двух ближайших точек; граница ячеек (f2 - f1 мало) - это вена.
// pairId - одинаков по обе стороны границы (у каждой вены свой).
void IC_Voronoi(float3 p, out float f1, out float f2, out float pairId)
{
    float3 ip = floor(p);
    float3 fp = frac(p);
    f1 = 8.0;
    f2 = 8.0;
    float3 c1 = 0;
    float3 c2 = 0;
    [unroll] for (int z = -1; z <= 1; z++)
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float3 g = float3(x, y, z);
        float3 r = g + IC_Hash33(ip + g) * 0.85 + 0.075 - fp;
        float d = dot(r, r);
        if (d < f1)
        {
            f2 = f1;
            c2 = c1;
            f1 = d;
            c1 = ip + g;
        }
        else if (d < f2)
        {
            f2 = d;
            c2 = ip + g;
        }
    }

    f1 = sqrt(f1);
    f2 = sqrt(f2);
    pairId = frac(IC_Hash13(c1) + IC_Hash13(c2));
}

struct IC_VeinSample
{
    float core;    // сама вена (чёрная)
    float halo;    // окрестность вены (плесень, потемнение бетона)
    float flow;    // сгусток, бегущий по вене
    float height;  // насколько поверхность вздута, м
};

struct IC_VeinParams
{
    float amount;         // сила заражения 0..1
    float scale;          // ячеек вен на метр
    float seed;
    float warp;
    float width;
    float branches;
    float flowFrequency;
    float flowSpeed;
    float flowWiden;
    float pulseSpeed;
    float pulseStrength;
    float veinHeight;
    float flowHeight;
    float time;
};

IC_VeinSample IC_VeinsCore(float3 position, IC_VeinParams v)
{
    IC_VeinSample s;
    float amount = saturate(v.amount);
    float t = v.time;

    float3 p = position * v.scale + v.seed * 13.17;
    p += IC_Warp(p * 0.45) * v.warp;

    float f1, f2, id;
    IC_Voronoi(p, f1, f2, id);
    float e = f2 - f1;

    // Сгусток: резкий фронт, длинный хвост; бежит вдоль вены, у каждой вены своя скорость и фаза.
    float phase = frac(f1 * v.flowFrequency - t * v.flowSpeed * (0.6 + 0.8 * id) + id * 5.3);
    float clot = smoothstep(0.0, 0.07, phase) * (1.0 - smoothstep(0.07, 0.5, phase));
    float breathing = 1.0 + v.pulseStrength * sin(t * v.pulseSpeed + id * 6.2832);

    // Не замкнутые ячейки: часть вен обрывается, ветви истончаются к концам.
    float present = smoothstep(0.22, 0.42, frac(id * 7.13)) * smoothstep(0.28, 0.52, IC_Noise3(p * 0.9 + 2.1));
    float w = v.width * amount * present * (0.6 + 0.7 * IC_Noise3(p * 1.7)) * (1.0 + v.flowWiden * clot);
    float core = 1.0 - smoothstep(w * 0.55, max(w, 1e-4), e);
    float ridge = 1.0 - smoothstep(0.0, max(w * 1.7, 1e-4), e);
    ridge *= ridge;

    // Тонкие ответвления - вторая сетка мельче, местами.
    float3 p2 = p * 2.3 + 11.7;
    float g1, g2, gid;
    IC_Voronoi(p2, g1, g2, gid);
    float branchOn = smoothstep(0.3, 0.6, IC_Noise3(p * 0.8 + 3.3)) * smoothstep(0.2, 0.4, frac(gid * 5.71)) * v.branches;
    float w2 = v.width * 0.45 * amount * branchOn;
    float e2 = g2 - g1;
    float core2 = 1.0 - smoothstep(w2 * 0.55, max(w2, 1e-4), e2);
    float ridge2 = 1.0 - smoothstep(0.0, max(w2 * 1.7, 1e-4), e2);

    s.core = max(core, core2 * 0.85);
    s.halo = (1.0 - smoothstep(w, w * 3.0 + 0.06, e)) * amount * present;
    s.flow = clot * core;
    s.height = (v.veinHeight * (ridge + 0.45 * ridge2 * ridge2) + v.flowHeight * clot * ridge) * breathing * amount;
    return s;
}

#endif
