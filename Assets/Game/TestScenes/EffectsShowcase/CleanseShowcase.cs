using DG.Tweening;
using UnityEngine;

namespace Game.TestScenes.EffectsShowcase
{
    /// <summary>
    /// Сцена эффектов: показ шейдеров очищения (Custom/Cleanse/...). По кругу: объект целый, за Duration секунд
    /// очищается, пауза, снова целый - с новыми точками-источниками на поверхности меша. Габариты меша передаются
    /// в материал, по ним шейдер считает источники и плавление. Только для показа, к очищаемым объектам не подключён.
    /// </summary>
    public class CleanseShowcase : MonoBehaviour
    {
        private static readonly int ProgressId = Shader.PropertyToID("_CleanseProgress");
        private static readonly int BoundsMinId = Shader.PropertyToID("_BoundsMin");
        private static readonly int BoundsMaxId = Shader.PropertyToID("_BoundsMax");
        private static readonly int ArrivalMaxId = Shader.PropertyToID("_ArrivalMax");
        private static readonly int[] SourceIds =
        {
            Shader.PropertyToID("_Source0"),
            Shader.PropertyToID("_Source1"),
            Shader.PropertyToID("_Source2"),
            Shader.PropertyToID("_Source3")
        };

        [Tooltip("Рендерер с материалом очищения.")]
        [SerializeField] private MeshRenderer m_Renderer;

        [Tooltip("Сколько секунд идёт очищение.")]
        [SerializeField] private float m_Duration = 6f;

        [Tooltip("Пауза до и после очищения, секунды.")]
        [SerializeField] private float m_Hold = 1.5f;

        [Tooltip("Сколько точек-источников (огонь, язвы, пепел расходятся от них).")]
        [SerializeField, Range(1, 4)] private int m_SourceCount = 3;

        [Tooltip("На сколько позже стартует каждая следующая точка (доля всего очищения).")]
        [SerializeField, Range(0f, 0.5f)] private float m_SourceDelay = 0.12f;

        private Material m_Material;
        private Bounds m_Bounds;
        private Vector3[] m_Vertices;
        private Sequence m_Sequence;

        private void Start()
        {
            Mesh mesh = m_Renderer.GetComponent<MeshFilter>().sharedMesh;
            m_Material = m_Renderer.material;
            m_Bounds = mesh.bounds;
            m_Vertices = mesh.isReadable ? mesh.vertices : new Vector3[0];
            m_Material.SetVector(BoundsMinId, m_Bounds.min);
            m_Material.SetVector(BoundsMaxId, m_Bounds.max);

            m_Sequence = DOTween.Sequence()
                .AppendCallback(Restart)
                .AppendInterval(m_Hold)
                .Append(DOVirtual.Float(0f, 1f, m_Duration, SetProgress).SetEase(Ease.Linear))
                .AppendInterval(m_Hold)
                .SetLoops(-1)
                .SetLink(gameObject);
        }

        private void OnDestroy()
        {
            m_Sequence?.Kill();
            if (m_Material != null)
            {
                Destroy(m_Material);
            }
        }

        private void Restart()
        {
            SetProgress(0f);
            var sources = new Vector4[SourceIds.Length];
            for (int i = 0; i < SourceIds.Length; i++)
            {
                // лишние точки уводятся далеко по времени - до них фронт не дойдёт
                sources[i] = i < m_SourceCount ? RandomSurfacePoint(i * m_SourceDelay) : new Vector4(0.5f, 0.5f, 0.5f, 10f);
                m_Material.SetVector(SourceIds[i], sources[i]);
            }

            m_Material.SetFloat(ArrivalMaxId, ArrivalMax(sources));
        }

        // Когда фронт дойдёт до самой дальней вершины: по этому шейдер растягивает очищение на всё время.
        private float ArrivalMax(Vector4[] sources)
        {
            float size = m_Bounds.size.magnitude;
            Vector3[] points = m_Vertices.Length > 0 ? m_Vertices : new[] { m_Bounds.min, m_Bounds.max, m_Bounds.center };
            float max = 0f;
            foreach (Vector3 point in points)
            {
                float nearest = float.MaxValue;
                foreach (Vector4 source in sources)
                {
                    if (source.w >= 1f)
                    {
                        continue;
                    }

                    Vector3 position = m_Bounds.min + Vector3.Scale(source, m_Bounds.size);
                    nearest = Mathf.Min(nearest, Vector3.Distance(point, position) / size + source.w);
                }

                max = Mathf.Max(max, nearest);
            }

            return max;
        }

        private Vector4 RandomSurfacePoint(float delay)
        {
            Vector3 point = m_Vertices.Length > 0
                ? m_Vertices[Random.Range(0, m_Vertices.Length)]
                : m_Bounds.center + Vector3.Scale(Random.onUnitSphere, m_Bounds.extents);
            Vector3 size = m_Bounds.size;
            return new Vector4(
                size.x > 1e-4f ? (point.x - m_Bounds.min.x) / size.x : 0.5f,
                size.y > 1e-4f ? (point.y - m_Bounds.min.y) / size.y : 0.5f,
                size.z > 1e-4f ? (point.z - m_Bounds.min.z) / size.z : 0.5f,
                delay);
        }

        private void SetProgress(float value)
        {
            m_Material.SetFloat(ProgressId, value);
        }
    }
}
