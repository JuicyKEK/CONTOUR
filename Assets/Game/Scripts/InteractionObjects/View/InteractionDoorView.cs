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
        [SerializeField] private UnityEvent _onStartOpen;
        [SerializeField] private UnityEvent _onClose;
        [Header("Open Settings")]
        [SerializeField] private Vector3 _openAngle = new Vector3(0f, 90f, 0f);
        [SerializeField] private float _duration = 0.5f;
        [SerializeField] private AnimationCurve _openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [Header("Monster Open Settings")]
        [Tooltip("Когда дверь открывает монстр (IInteractionDoor.LastOpener == Monster) - используется отдельная, обычно более резкая анимация вместо обычной Open Settings.")]
        [SerializeField] private bool _useMonsterOpenAnimation = true;
        [SerializeField] private Vector3 _monsterOpenAngle = new Vector3(0f, 120f, 0f);
        [SerializeField] private float _monsterDuration = 0.2f;
        [SerializeField] private AnimationCurve _monsterOpenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private UnityEvent _onOpenByMonster;
        [Header("Locked Shake Settings")]
        [SerializeField] private float _lockedShakeAngle = 5f;
        [SerializeField] private float _lockedShakeDuration = 0.15f;
        [SerializeField] private int _lockedShakecount = 2;
        [SerializeField] private int _lockedShakeDirection = -1;

        private Tween _currentTween;
        private Quaternion _closedRotation;

        private CancellationTokenSource _cts;
        private IInteractionDoor _door;
        private Collider _collider;

        private void Awake()
        {
            _closedRotation = transform.localRotation;
        }

        private void Start()
        {
            _door = GetComponent<IInteractionDoor>();

            if (_door != null)
            {
                _collider = GetComponent<Collider>();
                _door.IsOpen.Skip(1)
                    .Subscribe(ChangeState)
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

            bool isMonster = isOpen
                && _useMonsterOpenAnimation
                && _door.LastOpener.CurrentValue == DoorOpenerKind.Monster;

            Quaternion targetRotation = isOpen
                ? Quaternion.Euler(isMonster ? _monsterOpenAngle : _openAngle)
                : _closedRotation;
            float duration = isMonster ? _monsterDuration : _duration;
            AnimationCurve curve = isMonster ? _monsterOpenCurve : _openCurve;

            _currentTween = transform
                .DOLocalRotateQuaternion(targetRotation, duration)
                .OnStart(() => _onStartOpen?.Invoke())
                .OnComplete(() =>
                {
                    if (isOpen)
                    {
                        if (isMonster)
                        {
                            _onOpenByMonster?.Invoke();
                        }
                        else
                        {
                            _onOpen?.Invoke();
                        }
                    }
                    else
                    {
                        _onClose?.Invoke();
                    }
                })
                .SetEase(curve);

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