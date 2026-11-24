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
    [Header("С разбега")]
    [Tooltip("Дверь распахивают с разбега: в неё вбегает игрок или монстр в погоне. Угол - как у обычного открытия " +
             "(у монстра - как в Monster Open Settings), а время - это.")]
    [SerializeField, Min(0.01f)] private float _rushDuration = 0.2f;
    [Tooltip("Створка уходит от того, кто вбегает: если он с той стороны, куда дверь открывается, она откроется " +
             "в обратную сторону на тот же угол. Выключите у двери, которой в обратную сторону мешает стена или мебель.")]
    [SerializeField] private bool _rushSwingAwayFromOpener = true;
    [Space]
    [SerializeField] private float _lockedShakeAngle = 5f;
    [SerializeField] private float _lockedShakeDuration = 0.15f;
    [SerializeField] private int _lockedShakecount = 2;
    [SerializeField] private int _lockedShakeDirection = -1;
    [SerializeField] private UnityEvent _onOpen;
    
    public bool IsMoving => _swingTween != null && _swingTween.IsActive() && !_swingTween.IsComplete();

    private Tween _currentTween;
    private Tween _swingTween;
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

            // Положение на старте - сразу, без анимации: дверь могла загрузиться открытой из сохранения.
            SetDoorStateInstant(_door.IsOpen.CurrentValue);

            _door.IsOpen.Skip(1).Subscribe(ChangeState)
                .AddTo(_disposables);
            _door.IsTryingOpenLockedDoor.Subscribe(_ => TryOpenDoor())
                .AddTo(_disposables);
        }
    }

    public async void ChangeState(bool isOpen)
    {
        // Дверь может позвать вид напрямую ещё до его Start (восстановление стороны открытой створки).
        _door ??= GetComponent<IInteractionDoor>();
        _collider ??= GetComponent<Collider>();

        // Состояние пришло из сохранения - дверь должна просто стоять открытой/закрытой.
        if (_door != null && _door.IsRestoringState)
        {
            SetDoorStateInstant(isOpen);
            return;
        }

        await SetDoorStateAsync(isOpen);
    }

    public bool ShouldOpenReversed(Vector3 openerPosition)
    {
        if (!_rushSwingAwayFromOpener)
        {
            return false;
        }

        _door ??= GetComponent<IInteractionDoor>();
        bool isMonster = _useMonsterOpenAnimation
            && _door != null
            && _door.LastOpener.CurrentValue == DoorOpenerKind.Monster;

        return Game.Scripts.InteractionObjects.DoorSwing.IsSwingTowards(
            transform, _closedRotation, isMonster ? _monsterRotation : _openRotation, openerPosition);
    }

    private Quaternion GetOpenRotation(Quaternion openRotation, bool isReversed)
    {
        return isReversed ? Game.Scripts.InteractionObjects.DoorSwing.Reverse(_closedRotation, openRotation) : openRotation;
    }

    private void SetDoorStateInstant(bool isOpen)
    {
        _cts?.Cancel();
        _currentTween?.Kill();
        bool isReversed = _door != null && _door.IsOpenReversed;
        transform.localRotation = isOpen ? GetOpenRotation(_openRotation, isReversed) : _closedRotation;

        if (_collider != null)
        {
            _collider.enabled = true;
        }
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

        bool isRush = isOpen && _door.IsRushOpen;
        bool isReversed = isOpen && _door.IsOpenReversed;

        Quaternion targetRotation = isOpen
            ? GetOpenRotation(isMonster ? _monsterRotation : _openRotation, isReversed)
            : _closedRotation;
        float duration = isRush ? _rushDuration : isMonster ? _monsterDuration : _duration;

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
        _swingTween = _currentTween;

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
