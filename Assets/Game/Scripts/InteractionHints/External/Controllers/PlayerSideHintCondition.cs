using Game.Scripts.NPC.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InteractionHints.Controllers
{
    /// <summary>
    /// Подсказка видна, только когда игрок с нужной стороны (например, "закрыто с обратной стороны" - только из коридора,
    /// а не изнутри комнаты, где висит замок). Сторона - относительно оси Z объекта Reference (корень двери).
    /// </summary>
    [JDIMonoController]
    public class PlayerSideHintCondition : MonoBehaviour, IInteractionHintCondition
    {
        [Tooltip("Объект, по оси Z которого смотрится сторона (например, корень двери). Пусто - этот объект.")]
        [SerializeField] private Transform m_Reference;
        [Tooltip("Включено - подсказка, когда игрок спереди (по оси Z объекта Reference). Выключено - когда сзади.")]
        [SerializeField] private bool m_ShowInFront;

        [Inject] private IPlayerLocator m_PlayerLocator;

        public bool CanShowHint()
        {
            Transform player = m_PlayerLocator != null ? m_PlayerLocator.PlayerTransform : null;

            if (player == null)
            {
                return true;
            }

            Transform reference = m_Reference != null ? m_Reference : transform;
            float side = Vector3.Dot(player.position - reference.position, reference.forward);
            return m_ShowInFront ? side > 0f : side < 0f;
        }
    }
}
