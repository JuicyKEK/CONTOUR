using System;
using DG.Tweening;
using Game.Scripts.Audio.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.View
{
    public class LockDoorView : MonoBehaviour, ISoundPlay
    {
        private readonly CompositeDisposable _disposables = new();

        public Subject<Unit> IsPlaySound => m_IsPlaySound;
        
        [SerializeField] private float moveDistance = 0.0003f;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private InteractionDoor _interactionDoor;

        private Subject<Unit> m_IsPlaySound;
        private Vector3 _closedLocalPos;
        private Vector3 _openLocalPos;
        private Collider _collider;
        private Tween _currentTween;

        private void Awake()
        {
            _openLocalPos = transform.localPosition;
            _closedLocalPos = _openLocalPos + Vector3.right * moveDistance;
        }

        private void Start()
        {
            _collider = GetComponent<BoxCollider>();

            // Положение на старте - сразу, без анимации: дверь могла загрузиться запертой из сохранения.
            SetLatchStateInstant(_interactionDoor.IsLocked.CurrentValue);

            _interactionDoor.IsLocked.Skip(1).Subscribe(OnLockedChanged).AddTo(_disposables);
        }

        private void OnLockedChanged(bool isClosed)
        {
            // Состояние пришло из сохранения - защёлка должна просто стоять в нужном положении.
            if (_interactionDoor.IsRestoringState)
            {
                SetLatchStateInstant(isClosed);
                return;
            }

            SetLatchState(isClosed);
        }

        private void SetLatchStateInstant(bool isClosed)
        {
            _currentTween?.Kill();
            transform.localPosition = isClosed ? _closedLocalPos : _openLocalPos;
        }

        private void SetLatchState(bool isClosed)
        {
            _collider.enabled = false;
            _currentTween?.Kill();
            
            Vector3 target = isClosed
                ? _closedLocalPos
                : _openLocalPos;

            _currentTween = transform
                .DOLocalMove(target, duration)
                .OnComplete(() => m_IsPlaySound?.OnNext(Unit.Default))
                .SetEase(Ease.OutQuad);
            
            _collider.enabled = true;
        }
        
        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}