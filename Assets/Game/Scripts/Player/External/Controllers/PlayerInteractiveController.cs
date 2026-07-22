using Game.Scripts.Inventory;
using Game.Scripts.Player.View;
using Game.Scripts.Player.Runtime.Services;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.InputController
{
    [JDIMonoController]
    [SequenceParticipant(110)]
    public class PlayerInteractiveController : MonoBehaviour, ISequence, IUpdateSequence
    {
        private const float m_RaycastDistance = 2f;
        
        [Inject] private IInputActions m_InputActions;
        
        [SerializeField] private InteractionTrigger m_InteractionTrigger;
        [SerializeField] private PlayerInteractiveView m_PlayerInteractiveView;
        
        private IInteraction m_CurrentInteractable;
        private bool m_IsRayHitObject;
        private bool m_IsCanTryInteracting;
        private bool m_IsInteractingAvailable;
        
        public void MethodInit()
        {
            
        }

        public void MethodStart()
        {
            m_InteractionTrigger.SubscribeToInteractionChanged(OnInteractionNumberChanged);
            m_InputActions.AddPressingButtonEAction(TryInteraction);
        }
        
        public void CustomUpdate()
        {
            if (m_IsCanTryInteracting)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if (Physics.Raycast(ray, out var hit, m_RaycastDistance) &&
                    hit.collider.TryGetComponent(out IInteraction interactable) )
                {
                    if (!m_IsRayHitObject)
                    {
                        m_CurrentInteractable = interactable;
                        m_IsRayHitObject = true;
                        OnAvailableInteraction();
                    }
                }
                else
                {
                    if (m_IsRayHitObject)
                    {
                        m_IsRayHitObject = false;
                        m_CurrentInteractable  = null;
                        OnAvailableInteraction();
                    }
                }
            }
            else
            {
                if (m_IsRayHitObject)
                {
                    m_IsRayHitObject = false;
                    m_CurrentInteractable  = null;
                    OnAvailableInteraction();
                }
            }
        }

        private void OnAvailableInteraction()
        {
            m_PlayerInteractiveView.ShowInteractiveImage(m_IsRayHitObject);
            
            if (m_IsRayHitObject && m_CurrentInteractable != null)
            {
                m_IsInteractingAvailable = true;
            }
            else
            {
                m_IsInteractingAvailable = false;
            }
        }

        private void TryInteraction()
        {
            if (m_IsInteractingAvailable)
            {
                m_CurrentInteractable.Interact();
            }
        }

        private void OnInteractionNumberChanged(int number)
        {
            m_IsCanTryInteracting = number > 0;
        }

    }
}