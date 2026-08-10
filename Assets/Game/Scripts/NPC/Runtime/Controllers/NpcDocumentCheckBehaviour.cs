using Game.Scripts.NPC.Runtime.Interfaces;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Конкретная стратегия взаимодействия: проверка документов бота (см.
    /// пример "мирный сотрудник газовой службы" / "мимик" из ТЗ). Вешается на
    /// GameObject рядом с NpcInteractionPoint и указывается в его m_Behaviour.
    ///
    /// Ничего не знает про FSM бота напрямую - только просит NpcController
    /// проиграть анимацию/звук по ключу и поднимает нужный StoryEventChannelSO,
    /// чтобы сюжет мог сделать развилку (StoryEventCondition на канал).
    /// Дальнейшую реакцию (SetAggressiveOn+ForceChase у мимика и т.д.) обычно
    /// вешают через StoryEventChannelListener на этот же канал - не здесь.
    /// </summary>
    public class NpcDocumentCheckBehaviour : MonoBehaviour, INpcInteractionBehaviour
    {
        [Tooltip("Если true - документы бота 'поддельные'/испорчены (мимик).")]
        [SerializeField] private bool m_HasForgedDocuments;

        [Tooltip("Раздаётся, когда документы в порядке.")]
        [SerializeField] private StoryEventChannelSO m_OnDocumentsValidChannel; //TODO: Переделать, а то фигня получается

        [Tooltip("Раздаётся, когда документы поддельные/испорченные.")]
        [SerializeField] private StoryEventChannelSO m_OnDocumentsForgedChannel;

        public void OnPlayerInteracted(NpcController controller)
        {
            if (m_HasForgedDocuments)
            {
                controller.PlayAnimationSound("DocumentsForged");
                m_OnDocumentsForgedChannel?.Raise();
            }
            else
            {
                controller.PlayAnimationSound("DocumentsValid");
                m_OnDocumentsValidChannel?.Raise();
            }
        }
    }
}

