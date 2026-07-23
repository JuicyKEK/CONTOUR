using System;
using DG.Tweening;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.View
{
    public class LockDoorView : MonoBehaviour
    {
        private readonly CompositeDisposable _disposables = new();
        
        [SerializeField] private float moveDistance = 0.0003f;
        [SerializeField] private float duration = 0.1f;
        [SerializeField] private InteractionDoor _interactionDoor;

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
            _interactionDoor.IsLocked.Subscribe(SetLatchState).AddTo(_disposables);
            _collider = GetComponent<BoxCollider>();
            SetLatchState(_interactionDoor.IsLocked.CurrentValue);
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
                .SetEase(Ease.OutQuad);
            
            _collider.enabled = true;
        }
        
        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}