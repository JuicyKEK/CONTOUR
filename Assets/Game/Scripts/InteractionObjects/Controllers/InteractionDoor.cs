using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.Inventory;
using JuicyDI;
using R3;
using UnityEngine;

public class InteractionDoor : MonoBehaviour, IInteractionDoor, IInteraction
{
    public ReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
    public ReadOnlyReactiveProperty<bool> IsLocked => _isLocked;
    public Subject<Unit> IsTryingOpenLockedDoor => _isTryingOpenLockedDoor;

    [SerializeField] private bool _isLockedOnStart;
    [SerializeField] private bool _isOpenOnStart;
    
    private ReactiveProperty<bool> _isOpen;
    private ReactiveProperty<bool> _isLocked;
    private Subject<Unit> _isTryingOpenLockedDoor = new Subject<Unit>();
    
    private void Awake() //Переделать на сиквенсер как его починю
    {
        _isOpen = new ReactiveProperty<bool>(_isOpenOnStart);
        _isLocked = new ReactiveProperty<bool>(_isLockedOnStart);
    }

    public void Interact()
    {
        if (!_isLocked.Value)
        {
            _isOpen.Value = !_isOpen.Value;
        }
        else
        {
            _isTryingOpenLockedDoor.OnNext(Unit.Default);
        }
    }
    
    public void Lock()
    {
        _isLocked.Value = !_isLocked.Value;
    }
}
