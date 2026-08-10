using Game.Scripts.Audio.Interfaces;
using Game.Scripts.InteractionObjects.Interfaces;
using R3;
using UnityEngine;

namespace Game.Scripts.InteractionObjects.Controllers
{
    public class InteractionDoorSound : MonoBehaviour
    {
        [SerializeField] private GameObject m_SoundPlayerObject;
        [SerializeField] private AudioSource m_AudioSource;
        [SerializeField] private AudioClip m_SoundClipsOpen;
        [SerializeField] private AudioClip m_SoundClipsClose;
        [SerializeField] private AudioClip m_SoundClipsTryOpen;
        
        [Header("Диапазоны рандомизации")] 
        [SerializeField] private bool m_IsRandomPitchSound = false;
        [SerializeField] private float m_MinPitch = 0.9f;
        [SerializeField] private float m_MaxPitch = 1.1f;
        [SerializeField] private float m_MinVolume = 0.8f;
        [SerializeField] private float m_MaxVolume = 1.0f;
        
        private readonly CompositeDisposable _disposables = new();
        private IInteractionDoor m_SoundPlay;

        private void Start()
        {
            if (m_SoundPlay == null)
            {
                if (!m_SoundPlayerObject)
                {
                    Debug.LogError("IInteractionDoor object is null");
                    return;
                }

                if (m_SoundPlayerObject.TryGetComponent(out IInteractionDoor soundPlay))
                {
                    m_SoundPlay = soundPlay;
                    m_SoundPlay.IsOpen
                        .Skip(1)
                        .Subscribe(PlaySoundOpener)
                        .AddTo(_disposables);
                    
                    m_SoundPlay.IsTryingOpenLockedDoor
                        .Subscribe(_ => PlaySoundTryOpen())
                        .AddTo(_disposables);
                }
            }
        }

        private void PlaySoundOpener(bool isOpen)
        {
            RandomizePitchSound();

            m_AudioSource.resource = isOpen ? m_SoundClipsOpen : m_SoundClipsClose;

            m_AudioSource.Play();
        }
        
        private void PlaySoundTryOpen()
        {
            RandomizePitchSound();

            m_AudioSource.resource = m_SoundClipsTryOpen;

            m_AudioSource.Play();
        }

        private void RandomizePitchSound()
        {
            if (m_IsRandomPitchSound)
            {
                m_AudioSource.pitch = Random.Range(m_MinPitch, m_MaxPitch);
                m_AudioSource.volume = Random.Range(m_MinVolume, m_MaxVolume);
            }
        }

        private void OnDestroy()
        {
            _disposables.Dispose();
        }
    }
}