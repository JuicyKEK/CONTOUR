using System;
using Game.Scripts.Audio.Interfaces;
using R3;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Scripts.Audio
{
    public class SoundPlayer : MonoBehaviour
    {
        [SerializeField] private GameObject m_SoundPlayerObject;
        [SerializeField] private AudioSource m_AudioSource;
        [Header("Диапазоны рандомизации")]
        [SerializeField] private bool m_IsRandomPitchSound = false;
        [SerializeField] private float m_MinPitch = 0.9f;
        [SerializeField] private float m_MaxPitch = 1.1f;
        [SerializeField] private float m_MinVolume = 0.8f;
        [SerializeField] private float m_MaxVolume = 1.0f;
        [Header("Рандомизация звуков")]
        [SerializeField] private bool m_IsSelectRandomSound = false;
        [SerializeField] private AudioClip[] m_SoundClips;
        
        private readonly CompositeDisposable _disposables = new();
        private ISoundPlay m_SoundPlay;

        private void Start()
        {
            if (m_SoundPlay == null)
            {
                if (!m_SoundPlayerObject)
                {
                    Debug.LogError("Sound player object is null");
                    return;
                }
                
                if (m_SoundPlayerObject.TryGetComponent(out ISoundPlay soundPlay))
                {
                    m_SoundPlay = soundPlay;
                    m_SoundPlay.IsPlaySound
                        .Subscribe(_ => PlaySound())
                        .AddTo(_disposables);
                }
            }
        }

        private void PlaySound()
        {
            if (m_IsRandomPitchSound)
            {
                m_AudioSource.pitch = Random.Range(m_MinPitch, m_MaxPitch);
                m_AudioSource.volume = Random.Range(m_MinVolume, m_MaxVolume);
            }

            if (m_IsSelectRandomSound)
            {
                m_AudioSource.clip = m_SoundClips[Random.Range(0, m_SoundClips.Length)];
            }
            
            m_AudioSource.Play();
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}