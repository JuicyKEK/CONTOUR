using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Состояние проигрывания кассеты.
    /// </summary>
    public enum AudioTapePlaybackState
    {
        /// <summary>Кассета не выбрана (или её аудио не удалось загрузить).</summary>
        Idle,
        /// <summary>Играет звук нажатия Play и перемотки, пока догружается аудио кассеты.</summary>
        Preparing,
        Playing,
        Paused,
        /// <summary>Аудио доиграло до конца.</summary>
        Finished
    }

    /// <summary>
    /// Проигрывание кассет на одном AudioSource:
    ///   1) сразу играет звук нажатия Play + перемотки;
    ///   2) параллельно асинхронно подгружает аудио кассеты (AudioClip.LoadAudioData);
    ///   3) когда аудио загружено И звук перемотки играет не меньше заданного времени
    ///      (чтобы звук нажатия кнопки успел проиграться) - запускает аудио кассеты.
    /// Кассету можно поставить на паузу и продолжить с того же места, перемотать на любую позицию
    /// (<see cref="Seek"/>), а доигравшую - запустить заново. Окончание аудио замечает <see cref="Tick"/>,
    /// который владелец вызывает каждый кадр.
    ///
    /// Чтобы загрузка не блокировала кадр, у клипов кассет в импорте должно быть выключено
    /// Preload Audio Data и включено Load In Background (или Load Type = Streaming).
    /// Аудио предыдущей кассеты выгружается при переключении на другую, если его не держит Preload.
    /// </summary>
    public sealed class AudioTapePlayback : IDisposable
    {
        // Перемотка в самый конец сразу завершила бы кассету - оставляем немного звука.
        private const float EndMargin = 0.05f;

        private readonly AudioSource m_AudioSource;
        private readonly AudioClip m_RewindClip;
        private readonly float m_MinRewindDuration;

        private CancellationTokenSource m_PlaybackCts;
        private AudioClip m_CurrentTapeClip;
        private AudioTapePlaybackState m_State = AudioTapePlaybackState.Idle;
        // Позиция кассеты, когда она не играет: на паузе, доиграла или ещё готовится (с какого места начать).
        private float m_Position;
        // Аудио кассеты стоит в AudioSource на паузе (AudioSource.Pause) - продолжать можно через UnPause.
        private bool m_IsSourcePaused;

        public AudioTapePlaybackState State => m_State;

        /// <summary>
        /// Идёт ли что-то: перемотка перед кассетой (в т.ч. пока аудио догружается) или сама кассета.
        /// </summary>
        public bool IsActive => m_State == AudioTapePlaybackState.Preparing || m_State == AudioTapePlaybackState.Playing;

        /// <summary>
        /// Аудио кассеты, которая выбрана сейчас (null - ничего не выбрано).
        /// </summary>
        public AudioClip CurrentTapeClip => m_CurrentTapeClip;

        /// <summary>
        /// Длина аудио выбранной кассеты в секундах.
        /// </summary>
        public float Length => m_CurrentTapeClip != null ? m_CurrentTapeClip.length : 0f;

        /// <summary>
        /// Позиция в аудио выбранной кассеты в секундах.
        /// </summary>
        public float Position => m_State == AudioTapePlaybackState.Playing && m_AudioSource != null ? m_AudioSource.time : m_Position;

        /// <summary>
        /// Сколько проиграно - от 0 до 1.
        /// </summary>
        public float Progress => Length > 0f ? Mathf.Clamp01(Position / Length) : 0f;

        public AudioTapePlayback(AudioSource audioSource, AudioClip rewindClip, float minRewindDuration)
        {
            m_AudioSource = audioSource;
            m_RewindClip = rewindClip;
            m_MinRewindDuration = Mathf.Max(0f, minRewindDuration);
        }

        /// <summary>
        /// Запускает кассету с начала - со звуком нажатия Play и перемотки.
        /// </summary>
        public void Play(AudioClip tapeClip)
        {
            if (m_AudioSource == null || tapeClip == null)
            {
                return;
            }

            CancelPlayback();

            if (m_CurrentTapeClip != null && m_CurrentTapeClip != tapeClip)
            {
                ReleaseTapeClip(m_CurrentTapeClip);
            }

            m_CurrentTapeClip = tapeClip;
            m_Position = 0f;
            StartPlayback(true);
        }

        /// <summary>
        /// Ставит кассету на паузу. Если аудио ещё догружается - перемотка останавливается, а кассета
        /// начнётся (после <see cref="Resume"/>) сразу с нужного места.
        /// </summary>
        public void Pause()
        {
            if (m_State == AudioTapePlaybackState.Playing)
            {
                m_Position = m_AudioSource.time;
                m_AudioSource.Pause();
                m_IsSourcePaused = true;
                m_State = AudioTapePlaybackState.Paused;
            }
            else if (m_State == AudioTapePlaybackState.Preparing)
            {
                CancelPlayback();
                m_State = AudioTapePlaybackState.Paused;
            }
        }

        /// <summary>
        /// Продолжает кассету с того места, где она стоит (после паузы или перемотки доигравшей кассеты).
        /// </summary>
        public void Resume()
        {
            if (m_State != AudioTapePlaybackState.Paused || m_CurrentTapeClip == null)
            {
                return;
            }

            if (m_IsSourcePaused && m_AudioSource.clip == m_CurrentTapeClip)
            {
                m_AudioSource.time = ClampPosition(m_Position);
                m_AudioSource.UnPause();
                m_IsSourcePaused = false;
                m_State = AudioTapePlaybackState.Playing;
                return;
            }

            // Пауза случилась во время перемотки, или кассета уже доиграла и её перемотали назад.
            StartPlayback(false);
        }

        /// <summary>
        /// Перематывает выбранную кассету: <paramref name="progress"/> - доля от длины, от 0 до 1.
        /// Доигравшая кассета после перемотки стоит на паузе в новом месте.
        /// </summary>
        public void Seek(float progress)
        {
            if (m_CurrentTapeClip == null || m_State == AudioTapePlaybackState.Idle)
            {
                return;
            }

            float position = ClampPosition(Mathf.Clamp01(progress) * Length);

            switch (m_State)
            {
                case AudioTapePlaybackState.Playing:
                    m_AudioSource.time = position;
                    break;

                case AudioTapePlaybackState.Paused:
                    m_Position = position;

                    if (m_IsSourcePaused)
                    {
                        m_AudioSource.time = position;
                    }

                    break;

                case AudioTapePlaybackState.Finished:
                    m_Position = position;
                    m_State = AudioTapePlaybackState.Paused;
                    break;

                case AudioTapePlaybackState.Preparing:
                    // Аудио ещё грузится - кассета начнётся с этого места.
                    m_Position = position;
                    break;
            }
        }

        /// <summary>
        /// Перематывает выбранную кассету на <paramref name="seconds"/> секунд вперёд (больше нуля) или назад
        /// (меньше нуля). Доигравшую кассету вперёд перематывать некуда; назад - она встаёт на паузу в новом месте.
        /// </summary>
        public void SeekBy(float seconds)
        {
            if (m_CurrentTapeClip == null || Length <= 0f)
            {
                return;
            }

            if (m_State == AudioTapePlaybackState.Finished && seconds > 0f)
            {
                return;
            }

            Seek((Position + seconds) / Length);
        }

        /// <summary>
        /// Вызывается каждый кадр: замечает, что аудио кассеты доиграло до конца.
        /// </summary>
        public void Tick()
        {
            if (m_State != AudioTapePlaybackState.Playing || m_AudioSource == null)
            {
                return;
            }

            // Пока весь звук игры на паузе (AudioListener.pause), кассета не доиграла, а стоит.
            if (!m_AudioSource.isPlaying && !AudioListener.pause)
            {
                m_Position = Length;
                m_State = AudioTapePlaybackState.Finished;
            }
        }

        /// <summary>
        /// Полная остановка: кассета больше не выбрана.
        /// </summary>
        public void Stop()
        {
            CancelPlayback();
            m_State = AudioTapePlaybackState.Idle;
            m_Position = 0f;
        }

        public void Dispose()
        {
            Stop();
        }

        private void StartPlayback(bool withRewind)
        {
            m_PlaybackCts = new CancellationTokenSource();
            m_State = AudioTapePlaybackState.Preparing;
            PlayAsync(m_CurrentTapeClip, withRewind, m_PlaybackCts.Token).Forget();
        }

        private void CancelPlayback()
        {
            if (m_PlaybackCts != null)
            {
                m_PlaybackCts.Cancel();
                m_PlaybackCts.Dispose();
                m_PlaybackCts = null;
            }

            m_IsSourcePaused = false;

            if (m_AudioSource != null)
            {
                m_AudioSource.Stop();
            }
        }

        private async UniTaskVoid PlayAsync(AudioClip tapeClip, bool withRewind, CancellationToken token)
        {
            UniTask minRewindTask = UniTask.CompletedTask;

            if (withRewind)
            {
                PlayClip(m_RewindClip);

                // Таймер запускается сразу и тикает параллельно с загрузкой. Без звука перемотки ждать нечего.
                float minRewindDuration = m_RewindClip != null ? m_MinRewindDuration : 0f;
                minRewindTask = UniTask.Delay(TimeSpan.FromSeconds(minRewindDuration), ignoreTimeScale: true,
                    cancellationToken: token);
            }

            bool isLoaded;

            try
            {
                isLoaded = await LoadAudioDataAsync(tapeClip, token);
                await minRewindTask;
            }
            catch (OperationCanceledException)
            {
                // Пауза, остановка или запущена другая кассета - состояние уже выставил тот, кто отменил.
                return;
            }

            if (!isLoaded)
            {
                Debug.LogError($"[AudioTapePlayback] Не удалось загрузить аудио кассеты '{tapeClip.name}'.", tapeClip);
                m_AudioSource.Stop();
                m_State = AudioTapePlaybackState.Idle;
                return;
            }

            PlayClip(tapeClip);
            m_AudioSource.time = ClampPosition(m_Position);
            m_State = AudioTapePlaybackState.Playing;
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

        private float ClampPosition(float position)
        {
            return Mathf.Clamp(position, 0f, Mathf.Max(0f, Length - EndMargin));
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
