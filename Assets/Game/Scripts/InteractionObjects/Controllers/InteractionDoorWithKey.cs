using Game.Scripts.Inventory.External.Controllers.Interfaces;
using Game.Scripts.NPC.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.Controllers
{
    /// <summary>
    /// Дверь, которую отпирает предмет из инвентаря (ключ, молоток для навесного замка). Если у игрока в руках (выбран
    /// в инвентаре) предмет с ключом Key Item Key и он с той стороны двери, где Key Side, взаимодействие отпирает дверь
    /// (IsLocked = false) и забирает предмет, если тот расходуется. Иначе - обычная дверь: запертая дёргается.
    /// Как это выглядит и звучит (замок ломается, ключ поворачивается), решает вид, подписанный на IsLocked
    /// (например BreakableLockView).
    /// </summary>
    [JDIMonoController]
    public class InteractionDoorWithKey : InteractionDoor
    {
        [Header("Ключ")]
        [Tooltip("Ключ (ObjectKey) предмета инвентаря, который отпирает дверь.")]
        [SerializeField] private string m_KeyItemKey;
        [Tooltip("Предмет забирается из инвентаря, когда им отперли дверь.")]
        [SerializeField] private bool m_ConsumeItem;
        [Tooltip("Отпереть можно только с той стороны двери, где эта точка (например, навесной замок). Пусто - с любой стороны.")]
        [SerializeField] private Transform m_KeySide;

        [Inject] private IInventoryGetObject m_Inventory;
        [Inject] private IPlayerLocator m_PlayerLocator;

        public override void Interact()
        {
            if (!IsLocked.CurrentValue || !IsKeyInHands() || !IsPlayerOnKeySide())
            {
                base.Interact();
                return;
            }

            if (m_ConsumeItem)
            {
                m_Inventory.DeleteSelectedInventoryObject();
            }

            Unlock();
        }

        private bool IsKeyInHands()
        {
            if (m_Inventory == null || string.IsNullOrEmpty(m_KeyItemKey))
            {
                return false;
            }

            var selectedObject = m_Inventory.GetInventorySelectedObject();
            return selectedObject != null && selectedObject.ObjectKey == m_KeyItemKey;
        }

        private bool IsPlayerOnKeySide()
        {
            Transform player = m_PlayerLocator != null ? m_PlayerLocator.PlayerTransform : null;

            if (m_KeySide == null || player == null)
            {
                return true;
            }

            Vector3 normal = DoorSwing.GetLeafNormal(transform);
            float keySide = Vector3.Dot(m_KeySide.position - transform.position, normal);
            float playerSide = Vector3.Dot(player.position - transform.position, normal);
            return keySide * playerSide > 0f;
        }
    }
}
