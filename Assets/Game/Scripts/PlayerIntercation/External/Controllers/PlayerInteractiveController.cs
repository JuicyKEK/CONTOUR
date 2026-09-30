using Game.Scripts.Inventory;
using Game.Scripts.Player.View;
using Game.Scripts.Story;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InputController
{
    /// <summary>
    /// Взаимодействие игрока с объектами: каждый кадр пускает короткий луч из камеры и, если он попал
    /// в коллайдер с <see cref="IInteraction"/>, показывает значок взаимодействия и разрешает
    /// взаимодействие по кнопке E.
    ///
    /// Луч на 2 м стоит единицы микросекунд, поэтому его пускают постоянно, без предварительного
    /// триггера вокруг игрока: триггер не дешевле (физика обрабатывает все коллайдеры, пересекающие
    /// сферу), а OnTriggerExit не приходит для объектов, выключенных внутри сферы (подобранные кассеты,
    /// очищенные аномалии) - из-за этого счётчик объектов "залипал" и луч всё равно пускался всегда.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(110)]
    public class PlayerInteractiveController : MonoBehaviour, ISequence, IUpdateSequence, IPlayerControlHandle
    {
        [Inject] private IInputActions m_InputActions;
        [Inject] private PlayerInteractiveView m_PlayerInteractiveView;

        [SerializeField] private Camera m_Camera;
        [SerializeField] private float m_RaycastDistance = 1.3f;

        private IInteraction m_CurrentInteractable;
        private bool m_IsControlEnabled = true;

        public void MethodInit()
        {

        }

        public void MethodStart()
        {
            m_InputActions.AddPressingButtonEAction(TryInteraction);
        }

        public void SetControlEnabled(bool isEnabled)
        {
            m_IsControlEnabled = isEnabled;

            if (!m_IsControlEnabled)
            {
                m_CurrentInteractable = null;
                m_PlayerInteractiveView.ShowInteractiveImage(false);
            }
        }

        public void CustomUpdate()
        {
            if (!m_IsControlEnabled)
            {
                return;
            }

            Ray ray = m_Camera.ScreenPointToRay(Input.mousePosition);
            IInteraction interactable = null;

            if (Physics.Raycast(ray, out var hit, m_RaycastDistance))
            {
                hit.collider.TryGetComponent(out interactable);
            }

            SetCurrentInteractable(interactable);
        }

        private void SetCurrentInteractable(IInteraction interactable)
        {
            if (interactable == m_CurrentInteractable)
            {
                return;
            }

            m_CurrentInteractable = interactable;
            m_PlayerInteractiveView.ShowInteractiveImage(m_CurrentInteractable != null);
        }

        private void TryInteraction()
        {
            if (m_IsControlEnabled && m_CurrentInteractable != null)
            {
                m_CurrentInteractable.Interact();
            }
        }
    }
}