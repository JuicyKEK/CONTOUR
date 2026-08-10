using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.Inventory;
using JuicyDI;
using R3;
using UnityEngine;

public class InteractionDoor : MonoBehaviour, IInteractionDoor, IInteraction
{
    public ReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
    public ReadOnlyReactiveProperty<bool> IsLocked => _isLocked;
    public ReadOnlyReactiveProperty<DoorOpenerKind> LastOpener => _lastOpener;
    public Subject<Unit> IsTryingOpenLockedDoor => _isTryingOpenLockedDoor;

    [SerializeField] private bool _isLockedOnStart;
    [SerializeField] private bool _isOpenOnStart;
    
    private ReactiveProperty<bool> _isOpen;
    private ReactiveProperty<bool> _isLocked;
    private ReactiveProperty<DoorOpenerKind> _lastOpener;
    private Subject<Unit> _isTryingOpenLockedDoor = new Subject<Unit>();
    
    private void Awake() //Переделать на сиквенсер как его починю
    {
        _isOpen = new ReactiveProperty<bool>(_isOpenOnStart);
        _isLocked = new ReactiveProperty<bool>(_isLockedOnStart);
        _lastOpener = new ReactiveProperty<DoorOpenerKind>(DoorOpenerKind.Human);
    }

    public void Interact()
    {
        _lastOpener.Value = DoorOpenerKind.Human;

        if (!_isLocked.Value)
        {
            _isOpen.Value = !_isOpen.Value;
        }
        else
        {
            _isTryingOpenLockedDoor.OnNext(Unit.Default);
        }
    }

    /// <summary>
    /// Используется NPC вместо Interact(): только открывает (никогда не закрывает
    /// уже открытую дверь). Возвращает false, если дверь заперта.
    /// </summary>
    public bool TryOpenBy(DoorOpenerKind opener)
    {
        _lastOpener.Value = opener;

        if (_isLocked.Value)
        {
            _isTryingOpenLockedDoor.OnNext(Unit.Default);
            return false;
        }

        if (!_isOpen.Value)
        {
            _isOpen.Value = true;
        }

        return true;
    }
    
    public void Lock()
    {
        _isLocked.Value = !_isLocked.Value;
    }
}
