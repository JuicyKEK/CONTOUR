#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Scripts.Editor
{
    /// <summary>
    /// Утилита редактора для быстрого создания на сцене двух эффектов частиц
    /// заражения грибком:
    ///  - "Mold Spore Burst" - разлетающиеся в стороны и оседающие споры/пыльца
    ///    (например, когда игрок задевает заражённую поверхность);
    ///  - "Mold Pollen Fog" - медленно стелющийся, клубящийся туман из пыльцы,
    ///    висящий возле заражённых зон.
    ///
    /// Это стартовая точка: после создания настройте в инспекторе цвет, масштаб,
    /// текстуру спрайта и Shape под конкретную сцену/масштаб уровня.
    /// Находится в Tools > Mold VFX.
    /// </summary>
    public static class MoldParticleSystemsBuilder
    {
        private const string MenuRoot = "Tools/Mold VFX/";

        [MenuItem(MenuRoot + "Create Spore Burst Particle System")]
        private static void CreateSporeBurst()
        {
            var go = new GameObject("MoldSporeBurst");
            Undo.RegisterCreatedObjectUndo(go, "Create Mold Spore Burst");

            var ps = go.AddComponent<ParticleSystem>();
            ConfigureSporeBurst(ps);

            Selection.activeGameObject = go;
        }

        [MenuItem(MenuRoot + "Create Pollen Fog Particle System")]
        private static void CreatePollenFog()
        {
            var go = new GameObject("MoldPollenFog");
            Undo.RegisterCreatedObjectUndo(go, "Create Mold Pollen Fog");

            var ps = go.AddComponent<ParticleSystem>();
            ConfigurePollenFog(ps);

            Selection.activeGameObject = go;
        }

        // Встроенный в Unity мягкий круглый спрайт частицы - удобная отправная
        // точка, чтобы сразу видеть результат. Замените sharedMaterial на свой
        // материал со спрайтом спор/пыльцы, когда он появится в проекте.
        private static Material GetDefaultParticleMaterial()
        {
            return AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        }

        private static void ConfigureSporeBurst(ParticleSystem ps)
        {
            // ---- Main: короткий "взрыв" отдельных спор, оседающих под гравитацией ----
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.05f, 0.08f, 0.03f, 1f),
                new Color(0.15f, 0.2f, 0.08f, 1f));
            main.gravityModifier = 0.4f;
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // ---- Emission: одним залпом (Burst), без непрерывного потока ----
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, 30, 60, 1, 0f)
            });

            // ---- Shape: вылет из небольшой сферы (точки заражения) во все стороны ----
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            shape.radiusThickness = 1f;

            // ---- Небольшой радиальный "разлёт" + затухание скорости, чтобы споры не летели вечно ----
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.3f);

            var limitVelocity = ps.limitVelocityOverLifetime;
            limitVelocity.enabled = true;
            limitVelocity.dampen = 0.6f;
            limitVelocity.limit = new ParticleSystem.MinMaxCurve(1.5f);

            // ---- Лёгкий шум для органичности траектории (не идеально баллистической) ----
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.5f);
            noise.frequency = 0.8f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.2f);
            noise.damping = true;

            // ---- Уменьшение размера к концу жизни - споры "оседают" и мельчают визуально ----
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0.4f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // ---- Быстрое появление, плавное угасание альфы ----
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.15f),
                    new GradientAlphaKey(0.6f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            // ---- Столкновение с полом/стенами - споры реалистично оседают на поверхности ----
            var collision = ps.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = new ParticleSystem.MinMaxCurve(0.5f);
            collision.bounce = new ParticleSystem.MinMaxCurve(0.1f);
            collision.lifetimeLoss = new ParticleSystem.MinMaxCurve(0.3f);

            var particleRenderer = ps.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = GetDefaultParticleMaterial();
            particleRenderer.sortMode = ParticleSystemSortMode.Distance;
        }

        private static void ConfigurePollenFog(ParticleSystem ps)
        {
            // ---- Main: крупные, медленные, почти невесомые частицы тумана ----
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.35f, 0.4f, 0.25f, 0.05f),
                new Color(0.5f, 0.55f, 0.3f, 0.12f));
            main.gravityModifier = -0.02f; // едва заметно "плывёт" вверх, как настоящая взвесь пыльцы
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // ---- Emission: постоянный редкий поток, а не залп ----
            var emission = ps.emission;
            emission.rateOverTime = 4f;

            // ---- Shape: объём (Box) над/вокруг заражённой зоны, откуда рождается туман ----
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3f, 1.5f, 3f);

            // ---- Медленный дрейф вверх ----
            var velocityOverLifetime = ps.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);

            // ---- Крупный, низкочастотный шум - создаёт "клубящееся" органичное движение тумана ----
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.15f);
            noise.frequency = 0.15f;
            noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.05f);
            noise.quality = ParticleSystemNoiseQuality.High;
            noise.damping = true;

            // ---- Частицы медленно разрастаются, как расплывающееся облако ----
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            var sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.6f);
            sizeCurve.AddKey(0.5f, 1f);
            sizeCurve.AddKey(1f, 1.2f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // ---- Плавное появление/исчезание - без резких границ, как настоящая дымка ----
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            var particleRenderer = ps.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.sharedMaterial = GetDefaultParticleMaterial();
            particleRenderer.sortMode = ParticleSystemSortMode.Distance;
        }
    }
}
#endif


