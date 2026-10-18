using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Scripts.InteractionObjects.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.View
{
    public class NonInteractionDoorView : MonoBehaviour
    {
        private readonly CompositeDisposable _disposables = new();
    
        [SerializeField] private Vector3 _openAngle = new Vector3(0f, 90f, 0f);
        [SerializeField] private float _duration = 0.5f;

        private Tween _currentTween;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;

        private CancellationTokenSource _cts;
        private IInteractionSimpleDoor _door;

        private void Awake()
        {
            _closedRotation = transform.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(_openAngle.x, _openAngle.y, _openAngle.z);
        }
        
        private void Start()
        {
            _door = GetComponent<IInteractionSimpleDoor>();
            
            if (_door != null)
            {
                // Положение на старте - сразу, без анимации: дверь могла загрузиться открытой из сохранения.
                SetDoorStateInstant(_door.IsOpen.CurrentValue);

                _door.IsOpen.Skip(1).Subscribe(ChangeState)
                    .AddTo(_disposables);
            }
        }

        public async void ChangeState(bool isOpen)
        {
            // Состояние пришло из сохранения - дверь должна просто стоять открытой/закрытой.
            if (_door != null && _door.IsRestoringState)
            {
                SetDoorStateInstant(isOpen);
                return;
            }

            await SetDoorStateAsync(isOpen);
        }

        private void SetDoorStateInstant(bool isOpen)
        {
            _cts?.Cancel();
            _currentTween?.Kill();
            transform.localRotation = isOpen ? _openRotation : _closedRotation;
        }

        private async UniTask SetDoorStateAsync(bool isOpen)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _currentTween?.Kill();

            Quaternion targetRotation = isOpen
                ? _openRotation
                : _closedRotation;

            _currentTween = transform
                .DOLocalRotateQuaternion(targetRotation, _duration)
                .SetEase(Ease.OutCubic);

            try
            {
                await _currentTween.AsyncWaitForCompletion();
            }
            catch (OperationCanceledException)
            {
                
            }
        }

      
        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}