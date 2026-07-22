using System;
using Game.Scripts.Inventory;
using UnityEngine;

namespace Game.Scripts.Player.Runtime.Services
{
    public class InteractionTrigger : MonoBehaviour
    {
        private Action<int> m_NumberInteractionChanged;
        private int m_NumberInteractionObject;

        public void SubscribeToInteractionChanged(Action<int> interactionChangedAction)
        {
            m_NumberInteractionChanged += interactionChangedAction;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<IInteraction>(out IInteraction interact))
            {
                m_NumberInteractionObject++;
                m_NumberInteractionChanged?.Invoke(m_NumberInteractionObject);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<IInteraction>(out IInteraction interact))
            {
                m_NumberInteractionObject--;
                m_NumberInteractionChanged?.Invoke(m_NumberInteractionObject);
            }
        }
    }
}