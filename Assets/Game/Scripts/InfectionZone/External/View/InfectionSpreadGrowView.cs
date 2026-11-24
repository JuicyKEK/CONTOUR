using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.InfectionZone.External.Controllers;
using R3;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.InfectionZone.External.View
{
    /// <summary>
    /// Гнездо заражения разрастается - обратная сторона <see cref="InfectionCleanupMeltView"/>. Игрок заразил гнездо
    /// (<see cref="InfectionSpreadObject.IsSpread"/>) - оно не исчезает, а за несколько секунд:
    /// - само вздувается рывками, вены на нём бьются чаще, по нему вздуваются и лопаются волдыри (шейдер, Blisters)
    ///   и пузыри (частицы), гнездо рывками выдыхает облака спор;
    /// - из стены выползают новые щупальца и грибок - рост в шейдере (свойство _Growth у Custom/InfectedConcrete и
    ///   Custom/WetFlesh с включённым Growth): куски вылезают по очереди, с перехлёстом, дёргаются, свежие - мокрые и красные;
    /// - по стене и полу расползаются плесень, вены и гниль: у декалей растёт порог шума (_Coverage, _InfectionAmount),
    ///   сами декали раздвигаются;
    /// - старые щупальца вокруг в это время извиваются сильнее и быстрее.
    /// Разросшееся гнездо так и живёт: волдыри продолжают надуваться и лопаться, вокруг висят споры (они в наросте).
    /// Гнездо уже разросшееся на старте или разрослось из сохранения - сразу конечный вид, без роста.
    /// Стоит на корне гнезда (InfectionSpreadSpot).
    /// </summary>
    public class InfectionSpreadGrowView : MonoBehaviour
    {
        private static readonly int s_Growth = Shader.PropertyToID("_Growth");
        private static readonly int s_PulseSpeed = Shader.PropertyToID("_PulseSpeed");
        private static readonly int s_PulseStrength = Shader.PropertyToID("_PulseStrength");
        private static readonly int s_BlisterAmount = Shader.PropertyToID("_BlisterAmount");
        private static readonly int s_SwayAmount = Shader.PropertyToID("_SwayAmount");
        private static readonly int s_SwaySpeed = Shader.PropertyToID("_SwaySpeed");
        private static readonly int s_Coverage = Shader.PropertyToID("_Coverage");
        private static readonly int s_InfectionAmount = Shader.PropertyToID("_InfectionAmount");

        [Tooltip("Гнездо: когда его заразили, оно разрастается.")]
        [SerializeField] private InfectionSpreadObject m_Spread;

        [Header("Гнездо")]
        [Tooltip("Само гнездо: вздувается до Nest Scale.")]
        [SerializeField] private Transform m_Nest;

        [Tooltip("Во сколько раз гнездо больше после роста, по его осям (Z - от стены).")]
        [SerializeField] private Vector3 m_NestScale = new(2f, 2f, 3f);

        [Tooltip("Рендереры гнезда (Custom/InfectedConcrete): пока оно растёт, вены бьются чаще и сильнее.")]
        [SerializeField] private Renderer[] m_NestRenderers;

        [Tooltip("Во сколько раз чаще бьются вены гнезда на пике роста.")]
        [SerializeField, Min(1f)] private float m_PulseSpeedBoost = 5f;

        [Tooltip("Сила пульсации вен гнезда на пике роста.")]
        [SerializeField, Range(0f, 1f)] private float m_PulseStrengthPeak = 1f;

        [Header("Новый нарост")]
        [Tooltip("Новый нарост (щупальца, грибок, плесень, вены, споры): выключен, пока гнездо не разрослось.")]
        [SerializeField] private GameObject m_Growth;

        [Tooltip("Рендереры нароста с ростом в шейдере (Growth включён в материале) - им задаётся _Growth.")]
        [SerializeField] private Renderer[] m_GrowthRenderers;

        [Tooltip("Декали нароста (плесень, вены, гниль): расползаются по стене и полу.")]
        [SerializeField] private DecalProjector[] m_GrowthDecals;

        [Tooltip("С какой доли своего размера декаль начинает расползаться.")]
        [SerializeField, Range(0.05f, 1f)] private float m_DecalStartSize = 0.3f;

        [Header("Старые щупальца")]
        [Tooltip("Щупальца, которые уже были (Sway в материале): пока гнездо растёт, извиваются сильнее и быстрее.")]
        [SerializeField] private Renderer[] m_WritheRenderers;

        [Tooltip("Во сколько раз сильнее извиваются старые щупальца на пике роста.")]
        [SerializeField, Min(1f)] private float m_WritheBoost = 3.5f;

        [Header("Волдыри, пузыри, споры")]
        [Tooltip("Доля волдырей на гнезде на пике роста (у материала гнезда включены Blisters).")]
        [SerializeField, Range(0f, 1f)] private float m_BlisterPeak = 1f;

        [Tooltip("Доля волдырей у разросшегося гнезда: они так и продолжают надуваться и лопаться.")]
        [SerializeField, Range(0f, 1f)] private float m_BlisterAfter = 0.35f;

        [Tooltip("Пузыри на поверхности гнезда, пока оно растёт: надуваются и лопаются.")]
        [SerializeField] private ParticleSystem m_Bubbles;

        [Tooltip("Пузырей в секунду на пике роста.")]
        [SerializeField, Min(0f)] private float m_BubblesPerSecond = 45f;

        [Tooltip("Споры, которые гнездо рывками выдыхает, пока растёт.")]
        [SerializeField] private ParticleSystem m_Spores;

        [Tooltip("Спор за один выдох.")]
        [SerializeField, Min(0)] private int m_SporesPerBurst = 150;

        [Tooltip("Облако (дымка) спор при каждом выдохе.")]
        [SerializeField] private ParticleSystem m_SporeHaze;

        [Tooltip("Клубов дымки за один выдох.")]
        [SerializeField, Min(0)] private int m_HazePerBurst = 6;

        [Tooltip("Сколько раз гнездо выдыхает споры за рост (равномерно по ходу роста).")]
        [SerializeField, Min(0)] private int m_SporeBursts = 4;

        [Header("Время")]
        [Tooltip("Сколько секунд гнездо разрастается.")]
        [SerializeField, Min(0.1f)] private float m_Duration = 8f;

        private struct GrowingDecal
        {
            public DecalProjector Decal;
            public Material Shared;
            public Material Instance;
            public Vector3 Size;
            public float Coverage;
            public float Amount;
        }

        private readonly CompositeDisposable m_Disposables = new();
        private readonly List<GrowingDecal> m_Decals = new();
        private readonly List<Vector3> m_BubblePoints = new();
        private readonly List<Vector3> m_BubbleNormals = new();
        private MaterialPropertyBlock m_Block;
        private Vector3 m_NestBaseScale;
        private float m_PulseSpeed;
        private float m_PulseStrength;
        private float m_SwayAmount;
        private float m_SwaySpeed;
        private float m_Progress;
        private float m_BubbleDebt;
        private int m_BurstsDone;
        private Tween m_Tween;

        private void Start()
        {
            if (m_Spread == null)
            {
                Debug.LogWarning($"[{nameof(InfectionSpreadGrowView)}] {name}: не назначено гнездо (InfectionSpreadObject).", this);
                return;
            }

            m_Block = new MaterialPropertyBlock();
            RememberOriginals();

            if (m_Spread.IsSpread.CurrentValue)
            {
                ShowGrown();
            }
            else if (m_Growth != null)
            {
                m_Growth.SetActive(false);
            }

            m_Spread.IsSpread
                .Skip(1)
                .Where(isSpread => isSpread)
                .Subscribe(_ => OnSpread())
                .AddTo(m_Disposables);
        }

        private void OnDestroy()
        {
            m_Tween?.Kill();
            m_Disposables.Dispose();
            RestoreDecals();
        }

        private void OnSpread()
        {
            if (m_Spread.IsRestoringState)
            {
                ShowGrown();
                return;
            }

            Grow();
        }

        private void RememberOriginals()
        {
            if (m_Nest != null)
            {
                m_NestBaseScale = m_Nest.localScale;
                RememberBubblePoints();
            }

            Material nestMaterial = FirstMaterial(m_NestRenderers);
            m_PulseSpeed = GetFloat(nestMaterial, s_PulseSpeed, 1.3f);
            m_PulseStrength = GetFloat(nestMaterial, s_PulseStrength, 0.25f);

            Material writheMaterial = FirstMaterial(m_WritheRenderers);
            m_SwayAmount = GetFloat(writheMaterial, s_SwayAmount, 0.08f);
            m_SwaySpeed = GetFloat(writheMaterial, s_SwaySpeed, 0.9f);
        }

        // Пузыри - на лицевой стороне гнезда (нормаль от стены, к минус Z гнезда): вершины меша гнезда в его координатах.
        private void RememberBubblePoints()
        {
            m_BubblePoints.Clear();
            m_BubbleNormals.Clear();

            if (!m_Nest.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
            {
                return;
            }

            Vector3[] vertices = filter.sharedMesh.vertices;
            Vector3[] normals = filter.sharedMesh.normals;

            for (int i = 0; i < vertices.Length && i < normals.Length; i++)
            {
                if (normals[i].z < -0.2f)
                {
                    m_BubblePoints.Add(vertices[i]);
                    m_BubbleNormals.Add(normals[i]);
                }
            }
        }

        private void Grow()
        {
            if (m_Tween != null)
            {
                return;
            }

            PrepareDecals();
            SetProgress(0f);

            if (m_Growth != null)
            {
                m_Growth.SetActive(true);
            }

            PlayEmitter(m_Bubbles);
            PlayEmitter(m_Spores);
            PlayEmitter(m_SporeHaze);
            m_BubbleDebt = 0f;
            m_BurstsDone = 0;

            m_Progress = 0f;
            m_Tween = DOTween.To(() => m_Progress, SetProgress, 1f, m_Duration)
                .SetEase(Ease.Linear)
                .OnUpdate(EmitBubbles)
                .OnComplete(OnGrown);
        }

        private void OnGrown()
        {
            m_Tween = null;
            ShowGrown();
        }

        /// <summary>
        /// Конечный вид: нарост вырос, гнездо большое и в волдырях, декали - со своими материалами и размерами, вены и
        /// старые щупальца - как в материалах.
        /// </summary>
        private void ShowGrown()
        {
            m_Tween?.Kill();
            m_Tween = null;

            if (m_Growth != null)
            {
                m_Growth.SetActive(true);
            }

            if (m_Nest != null)
            {
                m_Nest.localScale = Vector3.Scale(m_NestBaseScale, m_NestScale);
            }

            // В материалах нароста _Growth = 1 - вырос.
            ClearBlocks(m_GrowthRenderers);
            ClearBlocks(m_NestRenderers);
            ClearBlocks(m_WritheRenderers);
            SetFloat(m_NestRenderers, s_BlisterAmount, m_BlisterAfter);
            RestoreDecals();
        }

        private void SetProgress(float progress)
        {
            m_Progress = progress;

            // Рост в шейдере: щупальца и грибок вылезают по своей очереди внутри общего хода.
            SetFloat(m_GrowthRenderers, s_Growth, progress);

            // Пик судорог - в середине роста, к концу всё успокаивается.
            float peak = Mathf.Sin(progress * Mathf.PI);

            if (m_Nest != null)
            {
                float swell = DOVirtual.EasedValue(0f, 1f, Mathf.Clamp01(progress / 0.7f), Ease.OutBack, 2.2f);
                float throb = 1f + 0.08f * Mathf.Sin(progress * 47f) * Mathf.Sin(progress * 11f + 0.6f) * (1f - progress);
                m_Nest.localScale = Vector3.Scale(m_NestBaseScale, Vector3.LerpUnclamped(Vector3.one, m_NestScale, swell)) * throb;
            }

            foreach (var nestRenderer in m_NestRenderers)
            {
                if (nestRenderer == null)
                {
                    continue;
                }

                nestRenderer.GetPropertyBlock(m_Block);
                m_Block.SetFloat(s_PulseSpeed, m_PulseSpeed * Mathf.Lerp(1f, m_PulseSpeedBoost, peak));
                m_Block.SetFloat(s_PulseStrength, Mathf.Lerp(m_PulseStrength, m_PulseStrengthPeak, peak));
                m_Block.SetFloat(s_BlisterAmount, Mathf.Max(m_BlisterPeak * peak, m_BlisterAfter * progress));
                nestRenderer.SetPropertyBlock(m_Block);
            }

            foreach (var writheRenderer in m_WritheRenderers)
            {
                if (writheRenderer == null)
                {
                    continue;
                }

                writheRenderer.GetPropertyBlock(m_Block);
                m_Block.SetFloat(s_SwayAmount, m_SwayAmount * Mathf.Lerp(1f, m_WritheBoost, peak));
                m_Block.SetFloat(s_SwaySpeed, m_SwaySpeed * Mathf.Lerp(1f, m_WritheBoost * 0.7f, peak));
                writheRenderer.SetPropertyBlock(m_Block);
            }

            // Выдохи спор - равномерно по ходу роста.
            while (m_BurstsDone < m_SporeBursts && progress >= (m_BurstsDone + 1f) / (m_SporeBursts + 1f))
            {
                m_BurstsDone++;
                EmitSpores();
            }

            // Плесень, вены и гниль расползаются быстро вначале и медленно дотягиваются.
            float spread = DOVirtual.EasedValue(0f, 1f, Mathf.Clamp01(progress / 0.9f), Ease.OutCubic);
            float decalSize = Mathf.Lerp(m_DecalStartSize, 1f, spread);

            foreach (var item in m_Decals)
            {
                if (item.Decal == null)
                {
                    continue;
                }

                item.Decal.size = new Vector3(item.Size.x * decalSize, item.Size.y * decalSize, item.Size.z);
                item.Instance.SetFloat(s_Coverage, item.Coverage * spread);
                item.Instance.SetFloat(s_InfectionAmount, item.Amount * spread);
            }
        }

        private void EmitSpores()
        {
            if (m_Spores != null && m_SporesPerBurst > 0)
            {
                m_Spores.Emit(m_SporesPerBurst);
            }

            if (m_SporeHaze != null && m_HazePerBurst > 0)
            {
                m_SporeHaze.Emit(m_HazePerBurst);
            }
        }

        // Пузыри - чаще к середине роста, по лицевой стороне гнезда (которая сама сейчас раздувается).
        private void EmitBubbles()
        {
            if (m_Bubbles == null || m_Nest == null || m_BubblePoints.Count == 0)
            {
                return;
            }

            m_BubbleDebt += m_BubblesPerSecond * Mathf.Sin(m_Progress * Mathf.PI) * Time.deltaTime;
            Matrix4x4 nestToWorld = m_Nest.localToWorldMatrix;
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };

            while (m_BubbleDebt >= 1f)
            {
                m_BubbleDebt -= 1f;
                int index = Random.Range(0, m_BubblePoints.Count);
                Vector3 normal = nestToWorld.MultiplyVector(m_BubbleNormals[index]).normalized;
                emit.position = nestToWorld.MultiplyPoint3x4(m_BubblePoints[index]) + normal * 0.01f;
                m_Bubbles.Emit(emit, 1);
            }
        }

        private static void PlayEmitter(ParticleSystem emitter)
        {
            if (emitter != null && !emitter.isPlaying)
            {
                emitter.Play(true);
            }
        }

        /// <summary>
        /// На время роста у каждой декали свой материал: порог шума у всех разный. После роста декали возвращаются к общим
        /// материалам - в них уже конечные значения.
        /// </summary>
        private void PrepareDecals()
        {
            RestoreDecals();

            if (m_GrowthDecals == null)
            {
                return;
            }

            foreach (var decal in m_GrowthDecals)
            {
                if (decal == null || decal.material == null)
                {
                    continue;
                }

                Material shared = decal.material;
                var instance = new Material(shared);
                m_Decals.Add(new GrowingDecal
                {
                    Decal = decal,
                    Shared = shared,
                    Instance = instance,
                    Size = decal.size,
                    Coverage = GetFloat(shared, s_Coverage, 1f),
                    Amount = GetFloat(shared, s_InfectionAmount, 1f)
                });
                decal.material = instance;
            }
        }

        private void RestoreDecals()
        {
            foreach (var item in m_Decals)
            {
                if (item.Decal != null)
                {
                    item.Decal.size = item.Size;
                    item.Decal.material = item.Shared;
                }

                if (item.Instance != null)
                {
                    Destroy(item.Instance);
                }
            }

            m_Decals.Clear();
        }

        private void SetFloat(Renderer[] renderers, int property, float value)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var target in renderers)
            {
                if (target == null)
                {
                    continue;
                }

                target.GetPropertyBlock(m_Block);
                m_Block.SetFloat(property, value);
                target.SetPropertyBlock(m_Block);
            }
        }

        private static void ClearBlocks(Renderer[] renderers)
        {
            if (renderers == null)
            {
                return;
            }

            foreach (var target in renderers)
            {
                if (target != null)
                {
                    target.SetPropertyBlock(null);
                }
            }
        }

        private static Material FirstMaterial(Renderer[] renderers)
        {
            if (renderers == null)
            {
                return null;
            }

            foreach (var target in renderers)
            {
                if (target != null && target.sharedMaterial != null)
                {
                    return target.sharedMaterial;
                }
            }

            return null;
        }

        private static float GetFloat(Material material, int property, float fallback)
        {
            return material != null && material.HasProperty(property) ? material.GetFloat(property) : fallback;
        }
    }
}
