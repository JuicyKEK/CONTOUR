using Game.Scripts.InputController;
using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Instructions.View;
using JuicyDI;
using JuicyDI.Attributes;
using System.Collections.Generic;
using AZE.AdvancedFirstPerson;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Кассетный плеер: по TAB открывает/закрывает панель (слева макет проигрывателя,
    /// справа страницы-тоглы, на каждой странице разделы с найденными/ненайденными кассетами).
    /// Пока панель открыта, движение камеры заблокировано и виден курсор мыши.
    ///
    /// Страницы спавнятся и заполняются один раз на старте. При каждом открытии панели
    /// (и при переключении страницы) обновляется только выбранная страница.
    ///
    /// Важно: повторное нажатие TAB (закрытие панели) НЕ останавливает проигрывание -
    /// кассета продолжает играть в фоне.
    ///
    /// Блок проигрывания (название кассеты, полоса прогресса, кнопки паузы/Play) появляется, когда игрок
    /// выбрал кассету, и дальше не пропадает - ни после паузы, ни когда кассета доиграла, ни после закрытия
    /// и открытия панели. Пауза - кассета стоит, вместо паузы кнопка Play, которая продолжает с того же
    /// места. Доиграла - тоже кнопка Play, но она запускает кассету с начала. Полосой прогресса кассету
    /// можно перемотать в любое место (и доигравшую тоже - тогда Play продолжит с этого места).
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(120)]
    public class CassettePlayerController : MonoBehaviour, ISequence, IUpdateSequence
    {
        [Header("Данные")]
        [Tooltip("Все страницы кассет, доступные в игре (найденные/ненайденные кассеты определяются реестром в рантайме).")]
        [SerializeField] private AudioTapePageSO[] m_Pages;

        [Header("Воспроизведение")]
        [SerializeField] private AudioSource m_AudioSource;
        [Tooltip("Звук нажатия Play и перемотки - играет перед кассетой, пока догружается её аудио.")]
        [SerializeField] private AudioClip m_RewindClip;
        [Tooltip("Сколько секунд звук перемотки играет минимум, даже если аудио кассеты уже загружено " +
                 "(чтобы звук нажатия кнопки успел проиграться).")]
        [SerializeField, Min(0f)] private float m_MinRewindDuration = 1f;

        // Вид панели и 3D-модель плеера на сцене в одном экземпляре - приходят через DI.
        [Inject] private CassettePlayerView m_View;
        [Inject] private CassettePlayerModelView m_PlayerModelView;

        // Контроллер движения игрока - на время открытой панели плеера вращение камеры блокируется.
        [Inject] private IPlayerMoveController m_PlayerMoveController;
        [Inject] private IInputActions m_InputActions;
        [Inject] private IAudioTapeFoundRegistry m_Registry;

        private readonly List<AudioTapePageSO> m_ActivePages = new();
        private readonly Dictionary<string, AudioTapeDefinitionSO> m_TapesById = new();
        private bool m_IsOpen;
        private AudioTapePlayback m_Playback;
        // Кассета, выбранная последней, - её показывает блок проигрывания (null - ещё ничего не выбирали).
        private AudioTapeDefinitionSO m_CurrentTape;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            m_Playback = new AudioTapePlayback(m_AudioSource, m_RewindClip, m_MinRewindDuration);
            CollectPagesAndTapes();
            RegistryCaseToStart();
            m_InputActions.AddPressingButtonTabAction(ToggleOpen);
            m_View.SetPlaybackActions(PausePlayback, ResumePlayback, SeekPlayback);
            m_View.BuildPages(BuildPagesData(), PlayTape, OnPageSelected);
            m_View.SetVisible(false);
            RefreshPlaybackView();
        }

        /// <summary>
        /// Отфильтровывает пустые ссылки на страницы (индексы m_ActivePages совпадают с индексами
        /// страниц во View) и собирает словарь кассет по TapeId с проверкой уникальности Id.
        /// </summary>
        private void CollectPagesAndTapes()
        {
            m_ActivePages.Clear();
            m_TapesById.Clear();

            if (m_Pages == null)
            {
                return;
            }

            foreach (var page in m_Pages)
            {
                if (page == null)
                {
                    continue;
                }

                m_ActivePages.Add(page);

                if (page.Sections == null)
                {
                    continue;
                }

                foreach (var section in page.Sections)
                {
                    if (section == null || section.Tapes == null)
                    {
                        continue;
                    }

                    foreach (var tape in section.Tapes)
                    {
                        RegisterTape(tape);
                    }
                }
            }
        }

        private void RegisterTape(AudioTapeDefinitionSO tape)
        {
            if (tape == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(tape.TapeId))
            {
                Debug.LogError($"[CassettePlayerController] У кассеты '{tape.name}' не задан TapeId.", tape);
                return;
            }

            if (m_TapesById.TryGetValue(tape.TapeId, out var registeredTape))
            {
                if (registeredTape != tape)
                {
                    Debug.LogError($"[CassettePlayerController] TapeId '{tape.TapeId}' повторяется у " +
                                   $"'{registeredTape.name}' и '{tape.name}'. Id кассет должны быть уникальными " +
                                   "(по ним работает сохранение).", tape);
                }

                return;
            }

            m_TapesById.Add(tape.TapeId, tape);
        }

        private void RegistryCaseToStart()
        {
            foreach (var tape in m_TapesById.Values)
            {
                if (tape.OnStart)
                {
                    m_Registry.MarkTapeFound(tape.TapeId);
                }
            }
        }

        /// <summary>
        /// Вызывается общей системой обновления (см. <see cref="IUpdateSequence"/>).
        /// Всегда - замечает, что кассета доиграла (в том числе при закрытой панели).
        /// Пока панель открыта - обновляет полосу прогресса и кнопку паузы/Play.
        /// </summary>
        public void CustomUpdate()
        {
            if (m_Playback == null)
            {
                return;
            }

            m_Playback.Tick();

            if (m_IsOpen)
            {
                RefreshPlaybackView();
            }
        }

        private void ToggleOpen()
        {
            if (m_IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void Open()
        {
            m_IsOpen = true;

            RefreshPage(m_View.SelectedPageIndex);
            m_View.SetVisible(true);
            RefreshPlaybackView();

            if (m_PlayerMoveController != null)
            {
                m_PlayerMoveController.SetMouseState(true);
            }

            m_PlayerModelView.SetVisible(true);
            m_View.RebuildLayout();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Close()
        {
            m_IsOpen = false;

            // Панель закрывается, но проигрывание НЕ останавливается - кассета продолжает
            // играть в фоне, пока её не остановят кнопкой паузы или она не закончится сама.
            m_View.SetVisible(false);

            if (m_PlayerMoveController != null)
            {
                m_PlayerMoveController.SetMouseState(false);
            }

            m_PlayerModelView.SetVisible(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnPageSelected(int pageIndex)
        {
            // Пока панель была закрыта, могли найтись новые кассеты - обновляем открытую страницу.
            RefreshPage(pageIndex);
            m_View.RebuildLayout();
        }

        private void RefreshPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= m_ActivePages.Count)
            {
                return;
            }

            m_View.RefreshPage(pageIndex, BuildSectionsData(m_ActivePages[pageIndex]));
        }

        private List<CassettePageRowData> BuildPagesData()
        {
            var pages = new List<CassettePageRowData>(m_ActivePages.Count);

            foreach (var page in m_ActivePages)
            {
                pages.Add(new CassettePageRowData(page.PageName, page.PageColor, BuildSectionsData(page)));
            }

            return pages;
        }

        private List<CassetteSectionRowData> BuildSectionsData(AudioTapePageSO page)
        {
            var sections = new List<CassetteSectionRowData>();

            if (page.Sections == null)
            {
                return sections;
            }

            foreach (var section in page.Sections)
            {
                if (section == null || section.Tapes == null)
                {
                    continue;
                }

                var tapeRows = new List<CassetteTapeRowData>(section.Tapes.Length);

                foreach (var tape in section.Tapes)
                {
                    if (tape == null)
                    {
                        continue;
                    }

                    bool isFound = m_Registry != null && m_Registry.IsTapeFound(tape.TapeId);
                    bool isListened = m_Registry != null && m_Registry.IsTapeListened(tape.TapeId);
                    bool isCompleted = m_Registry != null && m_Registry.IsTapeCompleted(tape.TapeId);
                    tapeRows.Add(new CassetteTapeRowData(tape.TapeId, tape.DisplayName, isFound, isListened,
                        tape.IsEvilCassette, isCompleted));
                }

                sections.Add(new CassetteSectionRowData(section.DisplayName, tapeRows));
            }

            return sections;
        }

        private void PlayTape(string tapeId)
        {
            var tape = FindTapeById(tapeId);

            if (tape == null)
            {
                return;
            }

            // Метка "НОВОЕ" снимается при первом запуске кассеты - запоминаем это в реестре (и в сохранении).
            m_Registry?.MarkTapeListened(tape.TapeId);

            if (tape.Clip == null || m_AudioSource == null)
            {
                return;
            }

            // Сначала звук нажатия/перемотки, само аудио кассеты - после асинхронной загрузки.
            m_CurrentTape = tape;
            m_Playback.Play(tape.Clip);
            RefreshPlaybackView();
        }

        private void PausePlayback()
        {
            m_Playback.Pause();
            RefreshPlaybackView();
        }

        /// <summary>
        /// Кнопка Play: кассета на паузе - продолжает с того же места; доиграла - запускает с начала.
        /// </summary>
        private void ResumePlayback()
        {
            if (m_CurrentTape == null || m_CurrentTape.Clip == null)
            {
                return;
            }

            if (m_Playback.State == AudioTapePlaybackState.Paused)
            {
                m_Playback.Resume();
            }
            else if (!m_Playback.IsActive)
            {
                m_Playback.Play(m_CurrentTape.Clip);
            }

            RefreshPlaybackView();
        }

        private void SeekPlayback(float progress)
        {
            m_Playback.Seek(progress);
            RefreshPlaybackView();
        }

        private void RefreshPlaybackView()
        {
            if (m_CurrentTape == null)
            {
                m_View.SetPlaybackVisible(false);
                return;
            }

            m_View.SetTapeTitle(m_CurrentTape.DisplayName);
            m_View.SetPlaying(m_Playback.IsActive);
            m_View.SetProgress(m_Playback.Progress);
            m_View.SetPlaybackVisible(true);
        }

        private void OnDestroy()
        {
            m_Playback?.Dispose();
        }

        private AudioTapeDefinitionSO FindTapeById(string tapeId)
        {
            if (string.IsNullOrEmpty(tapeId))
            {
                return null;
            }

            return m_TapesById.TryGetValue(tapeId, out var tape) ? tape : null;
        }
    }
}

