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
        
        private ReactiveProperty<bool> _isOpen;
    
        private void Awake() //Переделать на сиквенсер как его починю
        {
            _isOpen = new ReactiveProperty<bool>(_isOpenOnStart);
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