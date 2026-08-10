using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerMusicController : MonoBehaviour
{
    [SerializeField] private AudioSource m_AudioSource;
    [SerializeField] private FirstPersonController m_Player;
    [Header("Fade Settings")]
    [SerializeField] private float m_FadeDuration = 2f;
    [SerializeField] private AnimationCurve m_FadeCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    private Coroutine m_FadeRoutine;
    private float m_InitialVolume;

    private void Awake()
    {
        m_InitialVolume = m_AudioSource.volume;
    }

    private void Reset()
    {
        // Убедимся что коллайдер триггер
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FirstPersonController>() == null)
            return;

        StartFade(0f); // Плавное выключение
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<FirstPersonController>() == null)
            return;

        if (!m_AudioSource.isPlaying)
        {
            m_AudioSource.Play();
        }

        StartFade(m_InitialVolume); // Плавное включение
    }
    
    private void StartFade(float targetVolume)
    {
        if (m_FadeRoutine != null)
            StopCoroutine(m_FadeRoutine);

        m_FadeRoutine = StartCoroutine(FadeRoutine(targetVolume));
    }

    private IEnumerator FadeRoutine(float targetVolume)
    {
        float startVolume = m_AudioSource.volume;
        float time = 0f;

        while (time < m_FadeDuration)
        {
            time += Time.deltaTime;

            float normalized = time / m_FadeDuration;
            float curveValue = m_FadeCurve.Evaluate(normalized);

            m_AudioSource.volume = Mathf.Lerp(startVolume, targetVolume, curveValue);

            yield return null;
        }

        m_AudioSource.volume = targetVolume;

        if (Mathf.Approximately(targetVolume, 0f))
            m_AudioSource.Stop();
    }
}