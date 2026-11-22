using System;
using Game.Scripts.InputController;
using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.Inventory.External.Controllers.Interfaces;
using Game.Scripts.NPC.Runtime.Interfaces;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.Controllers
{
    /// <summary>
    /// Замок, который ломается предметом из инвентаря (обветшалый навесной замок - молотком). Вешается на объект двери
    /// рядом с InteractionDoor. Если у игрока в руках (выбран в инвентаре) нужный предмет и он с той стороны двери, где
    /// висит замок, взаимодействие ломает замок: дверь отпирается, модель замка падает на пол. Иначе - обычное
    /// взаимодействие (запертая дверь дёргается, показывается подсказка, если есть).
    /// Перехватывает взаимодействие через IInteractionInterceptor (PlayerInteractiveController спрашивает его перед Interact).
    /// </summary>
    [JDIMonoController]
    public class BreakableLock : MonoBehaviour, IInteractionInterceptor
    {
        [Tooltip("Ключ предмета инвентаря, которым ломается замок.")]
        [SerializeField] private string m_RequiredItemKey = "Hammer";
        [Tooltip("Предмет расходуется (пропадает из инвентаря). Молоток не расходуется.")]
        [SerializeField] private bool m_ConsumeItem;
        [Tooltip("Модель замка: при поломке падает на пол. Сломать можно только с той стороны двери, где она висит.")]
        [SerializeField] private Transform m_LockModel;
        [Tooltip("Скорость, с которой сломанный замок отлетает от двери, м/с.")]
        [SerializeField, Min(0f)] private float m_DropPush = 0.6f;

        [Inject] private IInventoryGetObject m_Inventory;
        [Inject] private IPlayerLocator m_PlayerLocator;

        private bool m_IsBroken;

        public bool IsBroken => m_IsBroken;

        public bool TryIntercept(Action proceed)
        {
            if (m_IsBroken || m_Inventory == null)
            {
                return false;
            }

            var selectedObject = m_Inventory.GetInventorySelectedObject();

            if (selectedObject == null || selectedObject.ObjectKey != m_RequiredItemKey || !IsPlayerOnLockSide())
            {
                return false;
            }

            if (m_ConsumeItem)
            {
                m_Inventory.DeleteSelectedInventoryObject();
            }

            Break();
            return true;
        }

        private void Break()
        {
            m_IsBroken = true;

            var door = GetComponent<IInteractionDoor>();

            if (door != null && door.IsLocked.CurrentValue)
            {
                door.Lock();
            }

            DropLockModel();
        }

        private bool IsPlayerOnLockSide()
        {
            Transform player = m_PlayerLocator != null ? m_PlayerLocator.PlayerTransform : null;

            if (m_LockModel == null || player == null)
            {
                return true;
            }

            Vector3 normal = GetLeafNormal();
            float lockSide = Vector3.Dot(m_LockModel.position - transform.position, normal);
            float playerSide = Vector3.Dot(player.position - transform.position, normal);
            return lockSide * playerSide > 0f;
        }

        /// <summary>
        /// Нормаль створки: самая тонкая ось её BoxCollider (нет коллайдера - ось Z объекта).
        /// </summary>
        private Vector3 GetLeafNormal()
        {
            if (!TryGetComponent(out BoxCollider box))
            {
                return transform.forward;
            }

            Vector3 size = Vector3.Scale(box.size, transform.lossyScale);
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

            if (size.x <= size.y && size.x <= size.z)
            {
                return transform.right;
            }

            return size.y <= size.z ? transform.up : transform.forward;
        }

        private void DropLockModel()
        {
            if (m_LockModel == null)
            {
                return;
            }

            Vector3 normal = GetLeafNormal();
            float side = Mathf.Sign(Vector3.Dot(m_LockModel.position - transform.position, normal));

            // Замок больше не висит на створке: не двигается вместе с дверью и падает сам.
            m_LockModel.SetParent(null, true);

            if (!m_LockModel.TryGetComponent(out Collider _))
            {
                m_LockModel.gameObject.AddComponent<BoxCollider>();
            }

            if (!m_LockModel.TryGetComponent(out Rigidbody body))
            {
                body = m_LockModel.gameObject.AddComponent<Rigidbody>();
            }

            body.mass = 0.5f;
            body.linearVelocity = normal * (side * m_DropPush);
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 4f;
        }
    }
}
