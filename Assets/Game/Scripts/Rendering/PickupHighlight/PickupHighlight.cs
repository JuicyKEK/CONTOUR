using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Rendering
{
    /// <summary>
    /// Подсветка подбираемого предмета: обводка и мягкое свечение его цветом. Рисует её эффект рендера
    /// PickupHighlightFeature (в PC_Renderer / Mobile_Renderer), общие толщина и яркость - там же.
    /// Пока объект включён - подсвечен; подобрали (объект выключен) - подсветки нет. Сквозь стены не видна.
    /// Обводятся все MeshRenderer и SkinnedMeshRenderer предмета и его детей (частицы - нет).
    /// Работает и в редакторе: цвет видно сразу, без запуска игры.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class PickupHighlight : MonoBehaviour
    {
        /// <summary>
        /// Подсвеченные сейчас предметы (включённые компоненты).
        /// </summary>
        public static readonly List<PickupHighlight> Active = new();

        /// <summary>
        /// Показывать подсветку в игре (в окне Scene она видна всегда). Выключает PickupHighlightController, пока
        /// у игрока отключено управление.
        /// </summary>
        public static bool IsShown = true;

        [Header("Свечение")]
        [Tooltip("Цвет мягкого свечения вокруг предмета и лёгкой подкраски самого предмета. Если на объекте есть " +
                 "компонент, который сам выбирает цвета (например, AudioTapeHighlightColor у кассет), цвета берутся из него.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color m_Color = new Color(1f, 0.85f, 0.35f);
        [Tooltip("Яркость свечения, множитель к цвету: 0 - свечения нет, больше 1 - ярче.")]
        [SerializeField, Min(0f)] private float m_Intensity = 1f;

        [Header("Обводка")]
        [Tooltip("Цвет контура вокруг предмета - отдельно от свечения.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color m_OutlineColor = new Color(1f, 0.85f, 0.35f);
        [Tooltip("Яркость обводки, множитель к цвету: 0 - обводки нет, больше 1 - ярче.")]
        [SerializeField, Min(0f)] private float m_OutlineIntensity = 1f;

        private readonly List<Renderer> m_Renderers = new();
        private IPickupHighlightColorSource m_ColorSource;

        public Color Color
        {
            get => m_Color;
            set => m_Color = value;
        }

        public float Intensity
        {
            get => m_Intensity;
            set => m_Intensity = Mathf.Max(0f, value);
        }

        public Color OutlineColor
        {
            get => m_OutlineColor;
            set => m_OutlineColor = value;
        }

        public float OutlineIntensity
        {
            get => m_OutlineIntensity;
            set => m_OutlineIntensity = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Цвета, которыми рисуется подсветка: свечение и обводка (цвет × яркость, или от компонента-источника на объекте).
        /// </summary>
        public void GetFinalColors(out Color glow, out Color outline)
        {
            // Источник могли добавить после включения подсветки.
            m_ColorSource ??= GetComponent<IPickupHighlightColorSource>();

            if (m_ColorSource != null && m_ColorSource.TryGetHighlightColors(out glow, out outline))
            {
                return;
            }

            glow = Scale(m_Color, m_Intensity);
            outline = Scale(m_OutlineColor, m_OutlineIntensity);
        }

        public static Color Scale(Color color, float intensity)
        {
            return new Color(color.r * intensity, color.g * intensity, color.b * intensity, 1f);
        }

        public IReadOnlyList<Renderer> Renderers => m_Renderers;

        private void OnEnable()
        {
            m_ColorSource = GetComponent<IPickupHighlightColorSource>();
            CollectRenderers();

            if (!Active.Contains(this))
            {
                Active.Add(this);
            }
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void OnTransformChildrenChanged()
        {
            CollectRenderers();
        }

        /// <summary>
        /// Пересобрать список обводимых рендереров (у предмета поменялись дочерние объекты).
        /// </summary>
        public void CollectRenderers()
        {
            m_Renderers.Clear();

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    m_Renderers.Add(renderer);
                }
            }
        }
    }
}
