using UnityEngine;
using DG.Tweening;

public class ButtonPressView : MonoBehaviour
{
    [SerializeField] private float _pressDepth = 0.02f;
    [SerializeField] private float _pressDuration = 0.07f;
    [SerializeField] private float _releaseDuration = 0.1f;

    private Vector3 _initialLocalPos;
    private Tween _currentTween;

    private void Awake()
    {
        _initialLocalPos = transform.localPosition;
    }

    public void PlayPressAnimation()
    {
        _currentTween?.Kill();

        Vector3 pressedPos = _initialLocalPos + Vector3.down * _pressDepth;

        Sequence seq = DOTween.Sequence();

        seq.Append(transform
            .DOLocalMove(pressedPos, _pressDuration)
            .SetEase(Ease.OutQuad));

        seq.Append(transform
            .DOLocalMove(_initialLocalPos, _releaseDuration)
            .SetEase(Ease.OutBack));

        _currentTween = seq;
    }
}