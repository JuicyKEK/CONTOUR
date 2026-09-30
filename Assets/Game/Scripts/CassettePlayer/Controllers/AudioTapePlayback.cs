using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Проигрывание кассет на одном AudioSource:
    ///   1) сразу играет звук нажатия Play + перемотки;
    ///   2) параллельно асинхронно подгружает аудио кассеты (AudioClip.LoadAudioData);
    ///   3) когда аудио загружено И звук перемотки играет не меньше заданного времени
    ///      (чтобы звук нажатия кнопки успел проиграться) - запускает аудио кассеты.
    ///
    /// Чтобы загрузка не блокировала кадр, у клипов кассет в импорте должно быть выключено
    /// Preload Audio Data и включено Load In Background (или Load Type = Streaming).
    /// Аудио предыдущей кассеты выгружается при переключении на другую, если его не держит Preload.
    /// </summary>
    public sealed class AudioTapePlayback : IDisposable
    {
        private readonly AudioSource m_AudioSource;
        private readonly AudioClip m_RewindClip;
        private readonly float m_MinRewindDuration;

        private CancellationTokenSource m_PlaybackCts;
        private AudioClip m_CurrentTapeClip;
        private bool m_IsPreparing;

        public AudioTapePlayback(AudioSource audioSource, AudioClip rewindClip, float minRewindDuration)
        {
            m_AudioSource = audioSource;
            m_RewindClip = rewindClip;
            m_MinRewindDuration = Mathf.Max(0f, minRewindDuration);
        }

        /// <summary>
        /// Идёт ли что-то: перемотка перед кассетой (в т.ч. пока аудио догружается) или сама кассета.
        /// </summary>
        public bool IsActive => m_IsPreparing || (m_AudioSource != null && m_AudioSource.isPlaying);

        public void Play(AudioClip tapeClip)
        {
            if (m_AudioSource == null || tapeClip == null)
            {
                return;
            }

            Stop();

            if (m_CurrentTapeClip != null && m_CurrentTapeClip != tapeClip)
            {
                ReleaseTapeClip(m_CurrentTapeClip);
            }

            m_CurrentTapeClip = tapeClip;
            m_PlaybackCts = new CancellationTokenSource();
            PlayAsync(tapeClip, m_PlaybackCts.Token).Forget();
        }

        public void Stop()
        {
            if (m_PlaybackCts != null)
            {
                m_PlaybackCts.Cancel();
                m_PlaybackCts.Dispose();
                m_PlaybackCts = null;
            }

            m_IsPreparing = false;

            if (m_AudioSource != null)
            {
                m_AudioSource.Stop();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        private async UniTaskVoid PlayAsync(AudioClip tapeClip, CancellationToken token)
        {
            m_IsPreparing = true;
            PlayClip(m_RewindClip);

            // Таймер запускается сразу и тикает параллельно с загрузкой. Без звука перемотки ждать нечего.
            float minRewindDuration = m_RewindClip != null ? m_MinRewindDuration : 0f;
            var minRewindTask = UniTask.Delay(TimeSpan.FromSeconds(minRewindDuration), ignoreTimeScale: true,
                cancellationToken: token);

            bool isLoaded;

            try
            {
                isLoaded = await LoadAudioDataAsync(tapeClip, token);
                await minRewindTask;
            }
            catch (OperationCanceledException)
            {
                // Остановлено или запущена другая кассета - состояние уже сбросил Stop().
                return;
            }

            m_IsPreparing = false;

            if (!isLoaded)
            {
                Debug.LogError($"[AudioTapePlayback] Не удалось загрузить аудио кассеты '{tapeClip.name}'.", tapeClip);
                m_AudioSource.Stop();
                return;
            }

            PlayClip(tapeClip);
        }

        private void PlayClip(AudioClip clip)
        {
            m_AudioSource.Stop();
            m_AudioSource.clip = clip;

            if (clip != null)
            {
                m_AudioSource.Play();
            }
        }

        private static async UniTask<bool> LoadAudioDataAsync(AudioClip clip, CancellationToken token)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded || clip.loadState == AudioDataLoadState.Failed)
            {
                if (!clip.loadInBackground && clip.loadType != AudioClipLoadType.Streaming)
                {
                    Debug.LogWarning($"[AudioTapePlayback] У клипа '{clip.name}' выключен Load In Background - " +
                                     "загрузка заблокирует кадр. Включите его в настройках импорта.", clip);
                }

                clip.LoadAudioData();
            }

            await UniTask.WaitWhile(() => clip.loadState == AudioDataLoadState.Loading, cancellationToken: token);
            return clip.loadState == AudioDataLoadState.Loaded;
        }

        private void ReleaseTapeClip(AudioClip clip)
        {
            // Клипы с Preload Audio Data Unity держит в памяти сама - их не трогаем.
            if (clip == m_RewindClip || clip.preloadAudioData)
            {
                return;
            }

            clip.UnloadAudioData();
        }
    }
}
