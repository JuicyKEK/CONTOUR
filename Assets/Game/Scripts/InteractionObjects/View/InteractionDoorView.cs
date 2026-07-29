using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Game.Scripts.InteractionObjects.Interfaces;
using R3;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.InteractionObjects.Controllers
{
    public class InteractionDoorView : MonoBehaviour, IInteractionDoorView
    {
        private readonly CompositeDisposable _disposables = new();

        [SerializeField] private UnityEvent _onOpen;
        [Header("Open Settings")]
        [SerializeField] private Vector3 _openAngle = new Vector3(0f, 90f, 0f);
        [SerializeField] private float _duration = 0.5f;
        [SerializeField] private AnimationCurve _openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Header("Locked Shake Settings")]
        [SerializeField] private float _lockedShakeAngle = 5f;
        [SerializeField] private float _lockedShakeDuration = 0.15f;
        [SerializeField] private int _lockedShakecount = 2;
        [SerializeField] private int _lockedShakeDirection = -1;

        private Tween _currentTween;
        private Quaternion _closedRotation;
        private Quaternion _openRotation;

        private CancellationTokenSource _cts;
        private IInteractionDoor _door;
        private Collider _collider;

        private void Awake()
        {
            _closedRotation = transform.localRotation;
            _openRotation = Quaternion.Euler(_openAngle.x, _openAngle.y, _openAngle.z);
        }

        private void Start()
        {
            _door = GetComponent<IInteractionDoor>();

            if (_door != null)
            {
                _collider = GetComponent<Collider>();
                _door.IsOpen.Subscribe(ChangeState)
                    .AddTo(_disposables);
                _door.IsTryingOpenLockedDoor.Subscribe(_ => TryOpenDoor())
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
            _collider.enabled = false;
            _currentTween?.Kill();

            Quaternion targetRotation = isOpen
                ? _openRotation
                : _closedRotation;
            Debug.Log(this.name + ": SetDoorStateAsync called");
            _currentTween = transform
                .DOLocalRotateQuaternion(targetRotation, _duration)
                .OnComplete(() => _onOpen?.Invoke())
                .SetEase(_openCurve);

            try
            {
                await _currentTween.AsyncWaitForCompletion();
            }
            catch (OperationCanceledException)
            {

            }
            
            _collider.enabled = true;
        }

        private void TryOpenDoor()
        {
            _currentTween?.Kill();

            Quaternion shakeRotation =
                _closedRotation * Quaternion.Euler(0, _lockedShakeAngle * _lockedShakeDirection, 0);

            Sequence seq = DOTween.Sequence();

            for (int i = 0; i < _lockedShakecount; i++)
            {
                seq.Append(
                    transform.DOLocalRotateQuaternion(shakeRotation, _lockedShakeDuration)
                        .SetEase(Ease.OutQuad)
                );

                seq.Append(
                    transform.DOLocalRotateQuaternion(_closedRotation, _lockedShakeDuration)
                        .SetEase(Ease.InQuad)
                );
            }


            _currentTween = seq;
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}