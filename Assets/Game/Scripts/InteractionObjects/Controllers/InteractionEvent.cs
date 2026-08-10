using Game.Scripts.Inventory;
using UnityEngine;
using UnityEngine.Events;

public class InteractionEvent : MonoBehaviour, IInteraction
{
    public UnityEvent _event;
    
    public void Interact()
    {
        _event?.Invoke();
    }
}
