using System;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using Game.Scripts.InteractionObjects.Interfaces;
using R3;
using UnityEngine.Events;

public class DefaultDoorView : MonoBehaviour, IInteractionDoorView
{
    private readonly CompositeDisposable _disposables = new();
    
    [SerializeField] private Vector3 _openAngle = new Vector3(0f, 90f, 0f);
    [SerializeField] private float _duration = 0.5f;
    [Header("Monster Open Settings")]
    [Tooltip("Когда дверь открывает монстр (IInteractionDoor.LastOpener == Monster) - используется отдельная, обычно более резкая анимация вместо обычной Open Settings.")]
    [SerializeField] private bool _useMonsterOpenAnimation = true;
    [SerializeField] private Vector3 _monsterOpenAngle = new Vector3(0f, 120f, 0f);
    [SerializeField] private float _monsterDuration = 0.2f;
    [SerializeField] private UnityEvent _onOpenByMonster;
    [SerializeField] private float _lockedShakeAngle = 5f;
    [SerializeField] private float _lockedShakeDuration = 0.15f;
    [SerializeField] private int _lockedShakecount = 2;
    [SerializeField] private int _lockedShakeDirection = -1;
    [SerializeField] private UnityEvent _onOpen;
    
    private Tween _currentTween;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Quaternion _monsterRotation;

    private CancellationTokenSource _cts;
    private IInteractionDoor _door;
    private Collider _collider;

    private void Awake()
    {
        _closedRotation = transform.localRotation;
        _openRotation = _closedRotation * Quaternion.Euler(_openAngle.x, _openAngle.y, _openAngle.z);
        _monsterRotation = _closedRotation * Quaternion.Euler(_monsterOpenAngle.x, _monsterOpenAngle.y, _monsterOpenAngle.z);
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

        bool isMonster = isOpen
            && _useMonsterOpenAnimation
            && _door.LastOpener.CurrentValue == DoorOpenerKind.Monster;

        Quaternion targetRotation = isOpen
            ? (isMonster ? _monsterRotation : _openRotation)
            : _closedRotation;
        float duration = isMonster ? _monsterDuration : _duration;

        _currentTween = transform
            .DOLocalRotateQuaternion(targetRotation, duration)
            .OnComplete(() =>
            {
                if (isMonster)
                {
                    _onOpenByMonster?.Invoke();
                }
                else
                {
                    _onOpen?.Invoke();
                }
            })
            .SetEase(Ease.OutCubic);

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
