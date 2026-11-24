#ifndef CLEANSE_COMMON_INCLUDED
#define CLEANSE_COMMON_INCLUDED

// Эффекты очищения. Шейдер выбирает свой эффект через define перед подключением:
// CLEANSE_BURN (горит, как бумага), CLEANSE_MELT (плавится от кислоты), CLEANSE_CORRODE (кислота разъедает дырами),
// CLEANSE_ASH (рассыпается пеплом), CLEANSE_BURNMELT (плавится и горит).
// Всё управляется _CleanseProgress (0 - целый объект, 1 - объекта нет) и точками-источниками _Source0..3:
// xyz - точка внутри габаритов объекта (0..1 по каждой оси), w - задержка старта этой точки.
// Габариты (_BoundsMin/_BoundsMax, координаты объекта) задаёт скрипт по мешу.

// ---------- шум ----------

float Cl_Hash31(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}

float3 Cl_Hash33(float3 p)
{
    p = frac(p * float3(0.1031, 0.1030, 0.0973));
    p += dot(p, p.yxz + 33.33);
    return frac((p.xxy + p.yxx) * p.zyx);
}

float Cl_Noise(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float3 u = f * f * (3.0 - 2.0 * f);
    float a = lerp(Cl_Hash31(i), Cl_Hash31(i + float3(1, 0, 0)), u.x);
    float b = lerp(Cl_Hash31(i + float3(0, 1, 0)), Cl_Hash31(i + float3(1, 1, 0)), u.x);
    float c = lerp(Cl_Hash31(i + float3(0, 0, 1)), Cl_Hash31(i + float3(1, 0, 1)), u.x);
    float d = lerp(Cl_Hash31(i + float3(0, 1, 1)), Cl_Hash31(i + float3(1, 1, 1)), u.x);
    return lerp(lerp(a, b, u.y), lerp(c, d, u.y), u.z);
}

float Cl_Fbm(float3 p)
{
    float sum = 0.0;
    float amp = 0.5;
    [unroll] for (int k = 0; k < 4; k++)
    {
        sum += amp * Cl_Noise(p);
        p = p * 2.03 + 17.1;
        amp *= 0.5;
    }
    return sum / 0.9375;
}

// x - расстояние до ближайшей точки ячейки, y - до второй, z - случайное число ячейки; center - её точка
float3 Cl_Voronoi(float3 p, out float3 center)
{
    float3 i = floor(p);
    float3 f = frac(p);
    float d1 = 8.0;
    float d2 = 8.0;
    float id = 0.0;
    center = 0;
    [unroll] for (int z = -1; z <= 1; z++)
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float3 g = float3(x, y, z);
        float3 o = Cl_Hash33(i + g);
        float3 r = g + o - f;
        float d = dot(r, r);
        if (d < d1)
        {
            d2 = d1;
            d1 = d;
            id = Cl_Hash31(i + g + 7.7);
            center = i + g + o;
        }
        else if (d < d2)
        {
            d2 = d;
        }
    }
    return float3(sqrt(d1), sqrt(d2), id);
}

// ---------- габариты и источники ----------

float3 Cl_BoundsSize()
{
    return max(_BoundsMax.xyz - _BoundsMin.xyz, 1e-3);
}

float Cl_Size()
{
    return length(Cl_BoundsSize());
}

float3 Cl_SourceOS(float4 s)
{
    return lerp(_BoundsMin.xyz, _BoundsMax.xyz, s.xyz);
}

// Когда фронт дойдёт до точки: 0 у источника в момент его старта, около 1 через весь объект.
float Cl_Arrival(float3 p)
{
    float size = Cl_Size();
    float a = distance(p, Cl_SourceOS(_Source0)) / size + _Source0.w;
    a = min(a, distance(p, Cl_SourceOS(_Source1)) / size + _Source1.w);
    a = min(a, distance(p, Cl_SourceOS(_Source2)) / size + _Source2.w);
    a = min(a, distance(p, Cl_SourceOS(_Source3)) / size + _Source3.w);
    a += (Cl_Fbm(p / size * _NoiseScale) - 0.5) * _FrontNoise;
    return a;
}

// Когда фронт доходит до самой дальней точки меша (без шума). Считает скрипт по вершинам и источникам;
// без него - худший случай.
float Cl_ArrivalMax()
{
    return _ArrivalMax > 0.01 ? _ArrivalMax : 1.5;
}

// Положение фронта. pre - полосы перед фронтом (с самого начала видны у источников наполовину),
// post - полосы за фронтом (к концу должны пройти весь объект). Последние 10% добирают остатки шума.
float Cl_Front(float pre, float post)
{
    float front = lerp(-0.25 * _FrontNoise - 0.5 * pre, Cl_ArrivalMax() + 0.25 * _FrontNoise + post + 0.02, _CleanseProgress);
    return front + smoothstep(0.9, 1.0, _CleanseProgress) * (0.5 * _FrontNoise + post + 0.3);
}

// ---------- плавление (вершины) ----------

// Верх оседает первым, столбики объекта плавятся с разной скоростью (подтёки), низ расплывается лужей.
// Если скрипт задал _MeltBoundsMin.w = 1, всё считается в мире: низ - пол под объектом, и объект стекает на него
// (так же для нескольких частей одного объекта и для повёрнутых объектов). Иначе - по габаритам самого объекта.
// К середине прогресса объект уже лужа: дальше она темнеет, пузырится и к концу испаряется.
float3 Cl_MeltPosition(float3 p, out float melt)
{
    bool world = _MeltBoundsMin.w > 0.5;
    float3 q = world ? TransformObjectToWorld(p) : p;
    float3 bmin = world ? _MeltBoundsMin.xyz : _BoundsMin.xyz;
    float3 bsize = max((world ? _MeltBoundsMax.xyz : _BoundsMax.xyz) - bmin, 1e-3);
    float size = length(bsize);
    float v = saturate((q.y - bmin.y) / bsize.y);
    float column = Cl_Fbm(float3(q.x, 0.0, q.z) / size * _NoiseScale * 1.5);
    float k = saturate(_CleanseProgress * (1.8 + _MeltDrips * (column - 0.5) * 2.0));
    k = k * k * (3.0 - 2.0 * k);
    melt = k;

    // степень сохраняет порядок высот внутри столбика - поверхность не выворачивается;
    // части, вросшие ниже пола, поднимаются к полу постепенно, а не прыгают на него
    float v2 = pow(v, 1.0 + 2.5 * k) * (1.0 - _MeltSag * k);
    float below = min(q.y - bmin.y, 0.0);
    float y = bmin.y + v2 * bsize.y + below * (1.0 - k);

    float2 centre = bmin.xz + bsize.xz * 0.5;
    float low = 1.0 - v2;
    float2 xz = centre + (q.xz - centre) * (1.0 + _MeltSpread * k * low * low);
    float3 r = float3(xz.x, y, xz.y);
    return world ? TransformWorldToObject(r) : r;
}

void Cl_MeltVertex(inout float3 p, inout float3 n, out float melt)
{
    float size = Cl_Size();
    float e = 0.01 * size;
    float3 up = abs(n.y) < 0.95 ? float3(0, 1, 0) : float3(1, 0, 0);
    float3 t1 = normalize(cross(n, up));
    float3 t2 = cross(n, t1);
    float m1, m2;
    float3 p0 = Cl_MeltPosition(p, melt);
    float3 p1 = Cl_MeltPosition(p + t1 * e, m1);
    float3 p2 = Cl_MeltPosition(p + t2 * e, m2);
    float3 nn = cross(p1 - p0, p2 - p0);
    nn = dot(nn, n) < 0.0 ? -nn : nn;
    n = normalize(nn + n * 1e-4);

    // кипение: поверхность медленно пузырится, пока объект плавится
    float t = _Time.y;
    float wobble = (Cl_Noise(p / size * 9.0 + float3(0.0, t * 0.8, t * 0.3)) - 0.5) * _MeltWobble * size * melt * (1.0 - melt) * 4.0;
    p = p0 + n * wobble;
}

// ---------- горение ----------

float Cl_BurnBands()
{
    return _GlowWidth + _CharWidth + _ScorchWidth;
}

// Края у прогоревших дыр загибаются, как у бумаги.
void Cl_BurnVertex(float3 original, inout float3 p, float3 n)
{
    float ahead = Cl_Arrival(original) - Cl_Front(Cl_BurnBands(), 0.0);
    float curl = 1.0 - saturate((ahead - _GlowWidth) / (_CharWidth + _ScorchWidth * 0.5));
    p += n * curl * curl * _CurlAmount * Cl_Size();
}

void Cl_BurnSurface(float3 p, inout half3 albedo, inout half smoothness, inout half3 emission, out float clipValue)
{
    float size = Cl_Size();
    float ahead = Cl_Arrival(p) - Cl_Front(Cl_BurnBands(), 0.0);
    clipValue = ahead;

    float glow = 1.0 - saturate(ahead / _GlowWidth);
    float charred = 1.0 - saturate((ahead - _GlowWidth) / _CharWidth);
    float scorch = 1.0 - saturate((ahead - _GlowWidth - _CharWidth) / _ScorchWidth);

    // жар сначала желтит и коричневит, потом обугливает дочерна
    albedo = lerp(albedo, albedo * half3(0.62, 0.42, 0.24), scorch * 0.9);
    albedo = lerp(albedo, _CharColor.rgb * (0.7 + 0.6 * Cl_Noise(p / size * 35.0)), smoothstep(0.0, 1.0, charred));
    smoothness = lerp(smoothness, 0.04, max(charred, scorch * 0.6));

    float t = _Time.y;
    float flicker = 0.55 + 0.9 * Cl_Noise(p / size * 16.0 + float3(0.0, -t * 2.1, t * 1.3));
    float sparks = step(0.86, Cl_Noise(p / size * 70.0 + float3(t * 0.7, -t * 1.9, 0.0))) * charred * (1.0 - glow);
    half3 hot = lerp(_GlowColor.rgb, half3(1.0, 0.85, 0.55) * max(max(_GlowColor.r, _GlowColor.g), _GlowColor.b), pow(glow, 6.0));
    emission += hot * _GlowIntensity * (glow * glow * flicker + sparks * 0.35 * flicker);
}

// ---------- кислота: плавление ----------

void Cl_MeltSurface(float3 p, float melt, inout half3 albedo, inout half smoothness, inout half3 emission, out float clipValue)
{
    float size = Cl_Size();
    float t = _Time.y;

    // кислота разъедает: темнеет, теряет цвет, становится мокрой слизью; почти очищенная лужа - тёмно-зелёная
    half grey = dot(albedo, half3(0.3, 0.59, 0.11));
    half3 goo = lerp(half3(grey, grey, grey) * 0.35, _AcidColor.rgb, 0.6);
    half dark = smoothstep(0.35, 0.7, _CleanseProgress);
    goo = lerp(goo, _AcidDarkColor.rgb, dark);
    albedo = lerp(albedo, goo, saturate(melt * 1.4));
    smoothness = lerp(smoothness, 0.93, saturate(melt * 2.0));

    // подтёки сверху вниз
    float streak = Cl_Noise(float3(p.x / size * 28.0, p.y / size * 2.5 + t * 0.5, p.z / size * 28.0));
    albedo *= lerp(1.0, 0.55 + 0.6 * streak, saturate(melt * 1.5));

    // пузыри пены: ячейки, которые раздуваются и лопаются
    float3 cell;
    float3 vor = Cl_Voronoi(p / size * 16.0 + float3(0.0, -t * 0.25, 0.0), cell);
    float radius = 0.42 * saturate(sin(t * 2.6 + vor.z * 6.283) * 0.5 + 0.5);
    float bubble = (1.0 - smoothstep(radius - 0.1, radius, vor.x)) * saturate(melt * 2.2 - 0.25);
    // на почти чёрной луже пена тоже тёмная и кислота уже не светится
    half3 foam = lerp(_FoamColor.rgb, _AcidDarkColor.rgb * 2.0, dark);
    albedo = lerp(albedo, foam, bubble * 0.75);
    emission += _AcidColor.rgb * _AcidGlow * (bubble * 0.7 + melt * 0.12) * (1.0 - dark);

    // в конце лужа испаряется
    float evaporate = saturate((_CleanseProgress - 0.85) / 0.15);
    clipValue = Cl_Fbm(p / size * _NoiseScale * 2.0) + 0.03 - evaporate * 1.1;
}

// ---------- кислота: разъедание дырами ----------

void Cl_CorrodeSurface(float3 p, inout half3 albedo, inout half smoothness, inout half3 emission, out float clipValue)
{
    float size = Cl_Size();
    float t = _Time.y;
    float3 cell;
    float3 vor = Cl_Voronoi(p / size * _PitScale, cell);
    float3 cellOS = cell / _PitScale * size;

    // язвы начинаются у источников и расходятся, у каждой своя задержка; к прогрессу 1 съедено всё
    float start = (saturate(Cl_Arrival(cellOS) / (Cl_ArrivalMax() + 0.5 * _FrontNoise)) * _PitSpread + vor.z * (1.0 - _PitSpread)) * 0.68;
    float grow = saturate((_CleanseProgress - start) / 0.28);
    float rough = (Cl_Noise(p / size * _PitScale * 6.0) - 0.5) * 0.18;
    float ahead = vor.x + rough - grow * 1.45;
    clipValue = ahead;

    float foam = (1.0 - saturate(ahead / _FoamWidth)) * step(0.001, grow);
    float fizz = Cl_Noise(p / size * 90.0 + float3(0.0, t * 3.0, t * 1.7));
    float wet = (1.0 - saturate(ahead / (_FoamWidth * 4.0))) * step(0.001, grow);
    float blister = saturate(1.0 - abs(_CleanseProgress - start + 0.04) / 0.08) * (1.0 - smoothstep(0.15, 0.45, vor.x));

    // мокрый ожог вокруг язвы, вздутие перед ней, шипящая пена по краю
    albedo = lerp(albedo, albedo * half3(0.45, 0.4, 0.22), saturate(wet + blister * 0.7));
    albedo = lerp(albedo, _FoamColor.rgb * (0.75 + 0.5 * fizz), foam * smoothstep(0.35, 0.75, fizz + foam * 0.4));
    smoothness = lerp(smoothness, 0.88, saturate(wet + foam));
    emission += _AcidColor.rgb * _AcidGlow * foam * fizz;
}

// ---------- пепел ----------

// Осыпающиеся части проседают вниз.
void Cl_AshVertex(float3 original, inout float3 p)
{
    float ahead = Cl_Arrival(original) - Cl_Front(_AshWidth, _CrumbleWidth * 1.6);
    float behind = saturate(-ahead / _CrumbleWidth);
    p.y -= behind * behind * _FallDistance * Cl_Size();
}

void Cl_AshSurface(float3 p, inout half3 albedo, inout half smoothness, inout half3 emission, out float clipValue)
{
    float size = Cl_Size();
    float ahead = Cl_Arrival(p) - Cl_Front(_AshWidth, _CrumbleWidth * 1.6);
    float ash = 1.0 - saturate(ahead / _AshWidth);
    albedo = lerp(albedo, _AshColor.rgb * (0.8 + 0.4 * Cl_Noise(p / size * 45.0)), smoothstep(0.0, 1.0, ash));
    smoothness = lerp(smoothness, 0.0, ash);

    // сеть трещин, которые проступают в пепле
    float3 cell;
    float3 vor = Cl_Voronoi(p / size * _CrackScale, cell);
    float edge = vor.y - vor.x;
    float crack = (1.0 - smoothstep(0.015, 0.07, edge)) * saturate(ash * 2.0 - 0.4);
    albedo = lerp(albedo, half3(0.04, 0.035, 0.03), crack);
    float fresh = saturate(1.0 - abs(ahead + _AshWidth * 0.3) / (_AshWidth * 0.7));
    emission += _GlowColor.rgb * _GlowIntensity * 0.4 * crack * fresh;

    // за фронтом каждый кусок крошится от краёв со своей задержкой
    float behind = -ahead / _CrumbleWidth - vor.z * 0.5;
    clipValue = behind > 0.0 ? edge - behind * 1.6 : 1.0;
}

// ---------- общий вход ----------

// При прогрессе 0 эффекта нет совсем - объект выглядит как обычно.
void Cleanse_Vertex(inout float3 p, inout float3 n, out float melt)
{
    melt = 0.0;
    if (_CleanseProgress <= 0.0)
    {
        return;
    }
    float3 original = p;
#if defined(CLEANSE_MELT) || defined(CLEANSE_BURNMELT)
    Cl_MeltVertex(p, n, melt);
#endif
#if defined(CLEANSE_BURN) || defined(CLEANSE_BURNMELT)
    Cl_BurnVertex(original, p, n);
#endif
#if defined(CLEANSE_ASH)
    Cl_AshVertex(original, p);
#endif
}

// p - точка объекта до сдвига вершин (узоры не плывут вместе с плавлением)
void Cleanse_Surface(float3 p, float melt, inout half3 albedo, inout half smoothness, inout half metallic, inout half3 emission, out float clipValue)
{
    clipValue = 1.0;
    if (_CleanseProgress <= 0.0)
    {
        return;
    }
#if defined(CLEANSE_MELT) || defined(CLEANSE_BURNMELT)
    float meltClip;
    Cl_MeltSurface(p, melt, albedo, smoothness, emission, meltClip);
    metallic *= 1.0 - melt;
    clipValue = min(clipValue, meltClip);
#endif
#if defined(CLEANSE_BURN) || defined(CLEANSE_BURNMELT)
    float burnClip;
    Cl_BurnSurface(p, albedo, smoothness, emission, burnClip);
    clipValue = min(clipValue, burnClip);
#endif
#if defined(CLEANSE_CORRODE)
    Cl_CorrodeSurface(p, albedo, smoothness, emission, clipValue);
#endif
#if defined(CLEANSE_ASH)
    Cl_AshSurface(p, albedo, smoothness, emission, clipValue);
    metallic = 0.0;
#endif
}

float Cleanse_Clip(float3 p, float melt)
{
    half3 albedo = 0.5;
    half smoothness = 0.5;
    half metallic = 0.0;
    half3 emission = 0.0;
    float clipValue;
    Cleanse_Surface(p, melt, albedo, smoothness, metallic, emission, clipValue);
    return clipValue;
}

#endif
