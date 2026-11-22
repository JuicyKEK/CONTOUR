using Game.Scripts.InteractionObjects.Interfaces;
using UnityEngine;

namespace Game.Scripts.InteractionHints.Controllers
{
    /// <summary>
    /// Подсказка видна, только пока дверь (IInteractionDoor на этом же объекте) заперта: отперли или сломали замок -
    /// подсказки больше нет.
    /// </summary>
    public class LockedDoorHintCondition : MonoBehaviour, IInteractionHintCondition
    {
        private IInteractionDoor m_Door;

        public bool CanShowHint()
        {
            m_Door ??= GetComponent<IInteractionDoor>();
            return m_Door != null && m_Door.IsLocked.CurrentValue;
        }
    }
}
