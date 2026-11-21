using Game.Scripts.Instructions.Controllers;
using Game.Scripts.Rendering;
using UnityEngine;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Цвета подсветки кассеты (PickupHighlight на том же объекте) по её описанию: обычная кассета - одними цветами,
    /// злая (AudioTapeDefinitionSO.IsEvilCassette) - другими. Поменяли кассету у AudioTapePickup - цвета меняются сами.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(PickupHighlight))]
    public class AudioTapeHighlightColor : MonoBehaviour, IPickupHighlightColorSource
    {
        [Header("Обычная кассета: свечение")]
        [ColorUsage(false, true)]
        [SerializeField] private Color m_NormalColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField, Min(0f)] private float m_NormalIntensity = 0.6f;

        [Header("Обычная кассета: обводка")]
        [ColorUsage(false, true)]
        [SerializeField] private Color m_NormalOutlineColor = new Color(1f, 0.82f, 0.35f);
        [SerializeField, Min(0f)] private float m_NormalOutlineIntensity = 0.6f;

        [Header("Злая кассета: свечение")]
        [ColorUsage(false, true)]
        [SerializeField] private Color m_EvilColor = new Color(1f, 0.08f, 0.05f);
        [SerializeField, Min(0f)] private float m_EvilIntensity = 1f;

        [Header("Злая кассета: обводка")]
        [ColorUsage(false, true)]
        [SerializeField] private Color m_EvilOutlineColor = new Color(1f, 0.08f, 0.05f);
        [SerializeField, Min(0f)] private float m_EvilOutlineIntensity = 1f;

        private AudioTapePickup m_Pickup;

        public bool TryGetHighlightColors(out Color glow, out Color outline)
        {
            if (m_Pickup == null)
            {
                m_Pickup = GetComponent<AudioTapePickup>();
            }

            bool isEvil = m_Pickup != null && m_Pickup.TapeDefinition != null && m_Pickup.TapeDefinition.IsEvilCassette;
            glow = isEvil ? PickupHighlight.Scale(m_EvilColor, m_EvilIntensity) : PickupHighlight.Scale(m_NormalColor, m_NormalIntensity);
            outline = isEvil
                ? PickupHighlight.Scale(m_EvilOutlineColor, m_EvilOutlineIntensity)
                : PickupHighlight.Scale(m_NormalOutlineColor, m_NormalOutlineIntensity);
            return true;
        }
    }
}
