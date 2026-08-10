using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(AudioSource))]
public class MusicFader : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private AnimationCurve fadeCurve =
        AnimationCurve.EaseInOut(0, 1, 1, 0);

    private Coroutine _fadeRoutine;
    private float _initialVolume;

    private void Awake()
    {
        _initialVolume = _audioSource.volume;
    }

    // ✅ Запуск музыки
    public void Play(bool loop = true)
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);
        
        _audioSource.loop = loop;
        _audioSource.volume = _initialVolume;
        _audioSource.Play();
    }

    // ✅ Запуск затухания
    public async void FadeOut()
    {
        await FadeOutAsync();
    }

    private async UniTask FadeOutAsync()
    {
        float time = 0f;
        float startVolume = _audioSource.volume;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float normalized = time / fadeDuration;
            float curveValue = fadeCurve.Evaluate(normalized);

            _audioSource.volume = startVolume * curveValue;

            await UniTask.Yield();
        }

        _audioSource.volume = 0f;
        _audioSource.Stop();
        _audioSource.volume = 1f;
    }
}