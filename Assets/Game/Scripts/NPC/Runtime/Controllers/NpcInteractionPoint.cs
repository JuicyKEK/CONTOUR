using Game.Scripts.Inventory;
using Game.Scripts.NPC.Runtime.Interfaces;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Controllers
{
    /// <summary>
    /// Универсальная точка входа для взаимодействия игрока с ботом (IInteraction,
    /// как и у дверей - см. InteractionDoor). Сама точка НЕ содержит игровой
    /// логики - она лишь пересылает Interact() в подключённую стратегию
    /// <see cref="INpcInteractionBehaviour"/>. Если у бота нет никакой реакции
    /// на взаимодействие (например обычный агрессивный монстр без диалогов) -
    /// просто не добавляйте этот компонент вообще или оставьте m_Behaviour пустым.
    ///
    /// Разные боты получают разное поведение простой заменой m_Behaviour на
    /// нужный компонент (NpcDocumentCheckBehaviour, диалоговое поведение и т.д.),
    /// без единого нового наследника NpcController.
    /// </summary>
    public class NpcInteractionPoint : MonoBehaviour, IInteraction
    {
        [SerializeField] private NpcController m_Owner;

        [Tooltip("Компонент, реализующий INpcInteractionBehaviour (проверка документов, диалог, обмен и т.д.). Может быть пустым, если у бота нет реакции на взаимодействие.")]
        [SerializeField] private MonoBehaviour m_Behaviour;

        public void Interact()
        {
            if (m_Behaviour.TryGetComponent(out INpcInteractionBehaviour interaction))
            {
                interaction.OnPlayerInteracted(m_Owner);
            }
            
            // if (m_Behaviour is INpcInteractionBehaviour behaviour)
            // {
            //     Debug.Log("Interact 2");
            //     behaviour.OnPlayerInteracted(m_Owner);
            // }
        }
    }
}

