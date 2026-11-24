using System.Collections.Generic;
using DG.Tweening;
using Game.Scripts.InfectionZone.External.Controllers;
using Game.Scripts.InfectionZone.Runtime.Controllers;
using R3;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Scripts.InfectionZone.External.View
{
    /// <summary>
    /// Очищенная аномалия плавится (шейдер Custom/Cleanse/Melt) и исчезает. На время плавления материалы объекта
    /// заменяются копиями материала плавления с текстурами и цветом объекта; объект стекает на пол под собой, лужа
    /// темнеет и пузырится (пузыри - эффект Cleanse_MeltBubbles), испаряется, и объект выключается. Коллайдеры
    /// выключаются сразу - сквозь лужу можно пройти. Выключенный объект возвращается к своим материалам: снова
    /// появившаяся аномалия - целая. Стоит на корне аномалии рядом с <see cref="InfectionCleanupObject"/>.
    /// Так же исчезает цель <see cref="InfectionRangeObject"/> из его списка целей, исчезающих через вид, когда
    /// заражение выходит из диапазона.
    /// </summary>
    public class InfectionCleanupMeltView : MonoBehaviour
    {
        private static readonly int s_Progress = Shader.PropertyToID("_CleanseProgress");
        private static readonly int s_BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int s_BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int s_BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int s_Smoothness = Shader.PropertyToID("_Smoothness");
        private static readonly int s_TileMeters = Shader.PropertyToID("_TileMeters");
        private static readonly int s_Triplanar = Shader.PropertyToID("_Triplanar");
        private static readonly int s_BoundsMin = Shader.PropertyToID("_BoundsMin");
        private static readonly int s_BoundsMax = Shader.PropertyToID("_BoundsMax");
        private static readonly int s_MeltBoundsMin = Shader.PropertyToID("_MeltBoundsMin");
        private static readonly int s_MeltBoundsMax = Shader.PropertyToID("_MeltBoundsMax");
        private static readonly int s_MeltSpread = Shader.PropertyToID("_MeltSpread");
        private static readonly int s_MeltSag = Shader.PropertyToID("_MeltSag");
        private const string TriplanarKeyword = "_CLEANSE_TRIPLANAR";
        private const int BubblePointCount = 400;

        [Tooltip("Аномалия: когда она очищена, объект плавится. Пусто - плавление запускают вызовом Melt() (сцена эффектов).")]
        [SerializeField] private InfectionCleanupObject m_Cleanup;

        [Tooltip("Объект диапазона заражения, у которого этот объект в списке целей, исчезающих через вид: заражение вышло из диапазона - объект плавится.")]
        [SerializeField] private InfectionRangeObject m_Range;

        [Tooltip("Материал плавления (шейдер Custom/Cleanse/Melt): цвета и сила плавления - из него, текстуры и цвет - из материалов объекта.")]
        [SerializeField] private Material m_MeltMaterial;

        [Tooltip("Пузыри на луже (Cleanse_MeltBubbles).")]
        [SerializeField] private ParticleSystem m_BubblesPrefab;

        [Tooltip("Сколько секунд объект плавится и испаряется.")]
        [SerializeField] private float m_Duration = 5f;

        [Tooltip("С какой доли плавления на луже начинают лопаться пузыри.")]
        [SerializeField, Range(0f, 1f)] private float m_BubblesFrom = 0.4f;

        [Tooltip("С какой доли новые пузыри больше не появляются (лужа испаряется).")]
        [SerializeField, Range(0f, 1f)] private float m_BubblesTo = 0.8f;

        [Tooltip("Пузырей в секунду на квадратный метр лужи.")]
        [SerializeField] private float m_BubblesPerSquareMeter = 70f;

        [Tooltip("Больше пузырей в секунду не бывает, как бы ни была велика лужа.")]
        [SerializeField] private float m_MaxBubblesPerSecond = 220f;

        [Tooltip("Насколько ниже объекта искать пол, на который он стечёт, метры.")]
        [SerializeField] private float m_FloorSearch = 4f;

        private readonly CompositeDisposable m_Disposables = new();
        private readonly List<(Renderer renderer, Material[] materials)> m_Originals = new();
        private readonly List<Material> m_MeltMaterials = new();
        private readonly List<Collider> m_DisabledColliders = new();
        private readonly List<Vector3> m_BubblePoints = new();

        private Bounds m_Bounds;
        private float m_FloorY;
        private float m_Progress;
        private float m_BubblesPerSecond;
        private float m_BubbleDebt;
        private ParticleSystem m_Bubbles;
        private Tween m_Tween;

        private void Start()
        {
            if (m_Cleanup != null)
            {
                m_Cleanup.IsCleaned.Skip(1).Subscribe(OnCleanedChanged).AddTo(m_Disposables);
            }

            if (m_Range != null)
            {
                m_Range.IsInRange.Skip(1).Subscribe(OnInRangeChanged).AddTo(m_Disposables);
            }
        }

        private void OnDisable()
        {
            // выключенная аномалия (расплавилась или её спрятали) снова целая - вдруг она появится опять
            m_Tween?.Kill();
            m_Tween = null;
            StopBubbles();
            Restore();
        }

        private void OnDestroy()
        {
            m_Disposables.Dispose();
        }

        private void OnCleanedChanged(bool isCleaned)
        {
            if (isCleaned)
            {
                Melt();
            }
        }

        private void OnInRangeChanged(bool isInRange)
        {
            if (!isInRange && gameObject.activeInHierarchy)
            {
                Melt();
            }
        }

        public void Melt()
        {
            if (m_Tween != null)
            {
                return;
            }

            if (m_MeltMaterial == null || !CollectRenderers())
            {
                gameObject.SetActive(false);
                return;
            }

            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                if (collider.enabled)
                {
                    collider.enabled = false;
                    m_DisabledColliders.Add(collider);
                }
            }

            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>())
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            m_FloorY = FindFloor();

            foreach (var original in m_Originals)
            {
                Mesh mesh = original.renderer.GetComponent<MeshFilter>().sharedMesh;
                bool triplanar = !mesh.HasVertexAttribute(VertexAttribute.TexCoord0);
                var melt = new Material[original.materials.Length];
                for (int i = 0; i < melt.Length; i++)
                {
                    melt[i] = CreateMeltMaterial(original.materials[i], mesh, triplanar);
                    m_MeltMaterials.Add(melt[i]);
                }

                original.renderer.sharedMaterials = melt;
            }

            PrepareBubbles();
            m_Progress = 0f;
            // не DOVirtual.Float: его значение приходит через OnUpdate, а OnUpdate здесь занят пузырями
            m_Tween = DOTween.To(() => m_Progress, SetProgress, 1f, m_Duration)
                .SetEase(Ease.Linear)
                .OnUpdate(EmitBubbles)
                .OnComplete(OnMelted);
        }

        private bool CollectRenderers()
        {
            m_Originals.Clear();
            bool hasBounds = false;
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (!renderer.enabled || filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                m_Originals.Add((renderer, renderer.sharedMaterials));
                if (hasBounds)
                {
                    m_Bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    m_Bounds = renderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        // Пол, на который стечёт объект: лучи вниз с середины высоты - из центра и вокруг объекта (наросты врастают в
        // стену и в пол, из-под них луч уходит этажом ниже). Берётся высота, на которую попало больше всего лучей
        // (мебель и дыры в полу - в меньшинстве); попаданий нет - низ объекта.
        private float FindFloor()
        {
            var hits = new List<float>();
            float originY = m_Bounds.center.y;
            float lowest = m_Bounds.min.y - m_FloorSearch;
            float aroundX = m_Bounds.extents.x + 0.35f;
            float aroundZ = m_Bounds.extents.z + 0.35f;
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var origin = new Vector3(m_Bounds.center.x + x * aroundX, originY, m_Bounds.center.z + z * aroundZ);
                    if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, originY - lowest, ~0, QueryTriggerInteraction.Ignore))
                    {
                        hits.Add(hit.point.y);
                    }
                }
            }

            if (hits.Count == 0)
            {
                return m_Bounds.min.y;
            }

            float floor = hits[0];
            int floorCount = 0;
            foreach (float level in hits)
            {
                int count = 0;
                foreach (float other in hits)
                {
                    if (Mathf.Abs(other - level) <= 0.15f)
                    {
                        count++;
                    }
                }

                if (count > floorCount || (count == floorCount && level > floor))
                {
                    floor = level;
                    floorCount = count;
                }
            }

            return floor;
        }

        private Material CreateMeltMaterial(Material source, Mesh mesh, bool triplanar)
        {
            var material = new Material(m_MeltMaterial);
            if (source != null)
            {
                string baseName = source.HasProperty(s_BaseMap) ? "_BaseMap" : source.HasProperty("_MainTex") ? "_MainTex" : null;
                if (baseName != null && source.GetTexture(baseName) != null)
                {
                    material.SetTexture(s_BaseMap, source.GetTexture(baseName));
                    material.SetTextureScale(s_BaseMap, source.GetTextureScale(baseName));
                    material.SetTextureOffset(s_BaseMap, source.GetTextureOffset(baseName));
                }

                if (source.HasProperty(s_BaseColor))
                {
                    material.SetColor(s_BaseColor, source.GetColor(s_BaseColor));
                }
                else if (source.HasProperty("_Color"))
                {
                    material.SetColor(s_BaseColor, source.GetColor("_Color"));
                }

                string bumpName = source.HasProperty(s_BumpMap) ? "_BumpMap" : source.HasProperty("_NormalMap") ? "_NormalMap" : null;
                if (bumpName != null && source.GetTexture(bumpName) != null)
                {
                    material.SetTexture(s_BumpMap, source.GetTexture(bumpName));
                }

                if (source.HasProperty(s_Smoothness))
                {
                    material.SetFloat(s_Smoothness, source.GetFloat(s_Smoothness));
                }

                if (source.HasProperty(s_TileMeters))
                {
                    material.SetFloat(s_TileMeters, source.GetFloat(s_TileMeters));
                }
            }

            if (triplanar)
            {
                material.EnableKeyword(TriplanarKeyword);
                material.SetFloat(s_Triplanar, 1f);
            }

            material.SetVector(s_BoundsMin, mesh.bounds.min);
            material.SetVector(s_BoundsMax, mesh.bounds.max);
            material.SetVector(s_MeltBoundsMin, new Vector4(m_Bounds.min.x, m_FloorY, m_Bounds.min.z, 1f));
            material.SetVector(s_MeltBoundsMax, m_Bounds.max);
            material.SetFloat(s_Progress, 0f);
            return material;
        }

        private void SetProgress(float value)
        {
            m_Progress = value;
            foreach (Material material in m_MeltMaterials)
            {
                material.SetFloat(s_Progress, value);
            }
        }

        // Точки пузырей - вершины объекта, сдвинутые так же, как растекается лужа: пузыри только там, где она есть.
        private void PrepareBubbles()
        {
            m_BubblePoints.Clear();
            m_BubbleDebt = 0f;
            if (m_BubblesPrefab == null)
            {
                return;
            }

            float spread = m_MeltMaterial.HasProperty(s_MeltSpread) ? m_MeltMaterial.GetFloat(s_MeltSpread) : 0.8f;
            float sag = m_MeltMaterial.HasProperty(s_MeltSag) ? m_MeltMaterial.GetFloat(s_MeltSag) : 0.96f;
            float surface = m_FloorY + (m_Bounds.max.y - m_FloorY) * (1f - sag) * 0.6f + 0.01f;
            Vector3 centre = m_Bounds.center;
            float scale = 1f + spread * 0.7f;

            foreach (var original in m_Originals)
            {
                Mesh mesh = original.renderer.GetComponent<MeshFilter>().sharedMesh;
                if (!mesh.isReadable)
                {
                    continue;
                }

                Vector3[] vertices = mesh.vertices;
                int step = Mathf.Max(1, vertices.Length * m_Originals.Count / BubblePointCount);
                for (int i = Random.Range(0, step); i < vertices.Length; i += step)
                {
                    Vector3 world = original.renderer.transform.TransformPoint(vertices[i]);
                    m_BubblePoints.Add(new Vector3(centre.x + (world.x - centre.x) * scale, surface, centre.z + (world.z - centre.z) * scale));
                }
            }

            if (m_BubblePoints.Count == 0)
            {
                m_BubblePoints.Add(new Vector3(centre.x, surface, centre.z));
            }

            float area = m_Bounds.size.x * m_Bounds.size.z * scale * scale * 0.5f;
            m_BubblesPerSecond = Mathf.Min(area * m_BubblesPerSquareMeter, m_MaxBubblesPerSecond);
        }

        private void EmitBubbles()
        {
            if (m_BubblesPrefab == null || m_Progress < m_BubblesFrom || m_Progress >= m_BubblesTo)
            {
                if (m_Progress >= m_BubblesTo)
                {
                    StopBubbles();
                }

                return;
            }

            if (m_Bubbles == null)
            {
                m_Bubbles = Instantiate(m_BubblesPrefab, m_Bounds.center, Quaternion.identity);
                ParticleSystem.EmissionModule emission = m_Bubbles.emission;
                emission.enabled = false;
                ParticleSystem.MainModule main = m_Bubbles.main;
                main.maxParticles = Mathf.Max(main.maxParticles, Mathf.CeilToInt(m_MaxBubblesPerSecond * 2f));
                m_Bubbles.Play(true);
            }

            m_BubbleDebt += m_BubblesPerSecond * Time.deltaTime;
            var emit = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            while (m_BubbleDebt >= 1f)
            {
                m_BubbleDebt -= 1f;
                Vector3 point = m_BubblePoints[Random.Range(0, m_BubblePoints.Count)];
                point.x += Random.Range(-0.04f, 0.04f);
                point.z += Random.Range(-0.04f, 0.04f);
                emit.position = point;
                m_Bubbles.Emit(emit, 1);
            }
        }

        private void StopBubbles()
        {
            if (m_Bubbles == null)
            {
                return;
            }

            // уже появившиеся пузыри долопываются сами
            m_Bubbles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(m_Bubbles.gameObject, 3f);
            m_Bubbles = null;
        }

        private void OnMelted()
        {
            m_Tween = null;
            gameObject.SetActive(false);
        }

        private void Restore()
        {
            foreach (var original in m_Originals)
            {
                if (original.renderer != null)
                {
                    original.renderer.sharedMaterials = original.materials;
                }
            }

            m_Originals.Clear();
            foreach (Material material in m_MeltMaterials)
            {
                Destroy(material);
            }

            m_MeltMaterials.Clear();
            foreach (Collider collider in m_DisabledColliders)
            {
                if (collider != null)
                {
                    collider.enabled = true;
                }
            }

            m_DisabledColliders.Clear();
        }
    }
}
