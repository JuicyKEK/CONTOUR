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
    
    public bool IsRestoringState => _isRestoringState;

    private ReactiveProperty<bool> _isOpen;
    private ReactiveProperty<bool> _isLocked;
    private ReactiveProperty<DoorOpenerKind> _lastOpener;
    private Subject<Unit> _isTryingOpenLockedDoor = new Subject<Unit>();
    private bool _isRestoringState;
    private IInteractionDoorView[] _views;

    private void Awake() //Переделать на сиквенсер как его починю
    {
        EnsureState();
    }

    /// <summary>
    /// Состояние создаётся один раз: сохранённое состояние может прийти раньше Awake (дверь на выключенном
    /// объекте), и Awake не должен затереть его стартовыми значениями.
    /// </summary>
    private void EnsureState()
    {
        _isOpen ??= new ReactiveProperty<bool>(_isOpenOnStart);
        _isLocked ??= new ReactiveProperty<bool>(_isLockedOnStart);
        _lastOpener ??= new ReactiveProperty<DoorOpenerKind>(DoorOpenerKind.Human);
    }

    public void GetState(out bool isOpen, out bool isLocked)
    {
        EnsureState();
        isOpen = _isOpen.Value;
        isLocked = _isLocked.Value;
    }

    public void RestoreState(bool isOpen, bool isLocked)
    {
        EnsureState();
        _isRestoringState = true;

        try
        {
            _isLocked.Value = isLocked;
            _isOpen.Value = isOpen;
        }
        finally
        {
            _isRestoringState = false;
        }
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
    
    /// <summary>
    /// Щеколда: задвигает или отодвигает. Задвинуть можно только закрытую дверь - пока дверь открыта
    /// или створка ещё закрывается, нажатие ничего не меняет. Отодвинуть можно всегда.
    /// </summary>
    public void Lock()
    {
        if (!_isLocked.Value && (_isOpen.Value || IsLeafMoving()))
        {
            return;
        }

        _isLocked.Value = !_isLocked.Value;
    }

    private bool IsLeafMoving()
    {
        _views ??= GetComponents<IInteractionDoorView>();

        foreach (IInteractionDoorView view in _views)
        {
            if (view.IsMoving)
            {
                return true;
            }
        }

        return false;
    }
}
