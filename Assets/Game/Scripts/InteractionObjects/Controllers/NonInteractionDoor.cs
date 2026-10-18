using Game.Scripts.InteractionObjects.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.Controllers
{
    public class NonInteractionDoor : MonoBehaviour, IInteractionSimpleDoor
    {
        public ReadOnlyReactiveProperty<bool> IsOpen => _isOpen;
        
        [SerializeField] private bool _isLocked;
        [SerializeField] private bool _isOpenOnStart;
        
        public bool IsRestoringState => _isRestoringState;

        private ReactiveProperty<bool> _isOpen;
        private bool _isRestoringState;

        private void Awake() //Переделать на сиквенсер как его починю
        {
            EnsureState();
        }

        /// <summary>
        /// Состояние создаётся один раз: сохранённое состояние может прийти раньше Awake (дверь на выключенном
        /// объекте), и Awake не должен затереть его стартовым значением.
        /// </summary>
        private void EnsureState()
        {
            _isOpen ??= new ReactiveProperty<bool>(_isOpenOnStart);
        }

        public void GetState(out bool isOpen, out bool isLocked)
        {
            EnsureState();
            isOpen = _isOpen.Value;
            isLocked = _isLocked;
        }

        public void RestoreState(bool isOpen, bool isLocked)
        {
            EnsureState();
            _isRestoringState = true;

            try
            {
                _isLocked = isLocked;
                _isOpen.Value = isOpen;
            }
            finally
            {
                _isRestoringState = false;
            }
        }
        
        public void DoorOpener()
        {
            if (!_isLocked)
            {
                _isOpen.Value = !_isOpen.Value;
            }
        }
        
        public void DoorLocked()
        {
            _isLocked = !_isLocked;
        }

    }
}