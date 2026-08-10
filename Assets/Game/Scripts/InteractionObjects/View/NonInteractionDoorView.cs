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
                _door.IsOpen.Subscribe(ChangeState)
                    .AddTo(_disposables);
            }
        }

        public async void ChangeState(bool isOpen)
        {
            await SetDoorStateAsync(isOpen);
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