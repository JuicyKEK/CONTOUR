using Game.Scripts.InputController;
using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Instructions.View;
using JuicyDI;
using JuicyDI.Attributes;
using System.Collections.Generic;
using AZE.AdvancedFirstPerson;
using UnityEngine;
using UnityEngine.UI;

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
    /// кассета продолжает играть в фоне. Остановить её можно либо кнопкой паузы по центру
    /// панели (видна только пока что-то играет), либо она останавливается сама, когда
    /// аудиофайл заканчивается.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(120)]
    public class CassettePlayerController : MonoBehaviour, ISequence, IUpdateSequence
    {
        [Header("Вид")]
        [SerializeField] private CassettePlayerView m_View;
        [SerializeField] private RectTransform m_PanelRectTransform;

        [Header("Данные")]
        [Tooltip("Все страницы кассет, доступные в игре (найденные/ненайденные кассеты определяются реестром в рантайме).")]
        [SerializeField] private AudioTapePageSO[] m_Pages;

        [Header("Воспроизведение")]
        [SerializeField] private AudioSource m_AudioSource;

        [Header("Игрок")]
        [Tooltip("Контроллер камеры игрока - на время открытой панели плеера его вращение блокируется.")]
        [SerializeField] private GameObject m_CassettePlayerModel;
        [Inject] private IPlayerMoveController m_PlayerMoveController;

        [Inject] private IInputActions m_InputActions;
        [Inject] private IAudioTapeFoundRegistry m_Registry;

        private readonly List<AudioTapePageSO> m_ActivePages = new();
        private readonly Dictionary<string, AudioTapeDefinitionSO> m_TapesById = new();
        private bool m_IsOpen;
        private bool m_WasPlayingLastFrame;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            CollectPagesAndTapes();
            RegistryCaseToStart();
            m_InputActions.AddPressingButtonTabAction(ToggleOpen);
            m_View.SetPauseButtonAction(StopPlayback);
            m_View.BuildPages(BuildPagesData(), PlayTape, OnPageSelected);
            m_View.SetVisible(false);
            m_View.SetPauseButtonVisible(false);
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
        /// Пока панель открыта - следит за естественным окончанием аудиофайла и прячет
        /// кнопку паузы, когда проигрывание закончилось само по себе.
        /// </summary>
        public void CustomUpdate()
        {
            if (!m_IsOpen || m_AudioSource == null)
            {
                return;
            }

            bool isPlayingNow = m_AudioSource.isPlaying;

            if (m_WasPlayingLastFrame && !isPlayingNow)
            {
                m_View.SetPauseButtonVisible(false);
            }

            m_WasPlayingLastFrame = isPlayingNow;
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
            m_View.SetPauseButtonVisible(m_AudioSource != null && m_AudioSource.isPlaying);
            m_WasPlayingLastFrame = m_AudioSource != null && m_AudioSource.isPlaying;

            if (m_PlayerMoveController != null)
            {
                m_PlayerMoveController.SetMouseState(true);
            }

            CassettePlayerModel();
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_PanelRectTransform);
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

            CassettePlayerModel();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void CassettePlayerModel() //Можно все переделать под реакт т.к. view
        {
            m_CassettePlayerModel.SetActive(m_IsOpen);
        }

        private void OnPageSelected(int pageIndex)
        {
            // Пока панель была закрыта, могли найтись новые кассеты - обновляем открытую страницу.
            RefreshPage(pageIndex);
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_PanelRectTransform);
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
                    tapeRows.Add(new CassetteTapeRowData(tape.TapeId, tape.DisplayName, isFound, tape.IsEvilCassette));
                }

                sections.Add(new CassetteSectionRowData(section.DisplayName, tapeRows));
            }

            return sections;
        }

        private void PlayTape(string tapeId)
        {
            var tape = FindTapeById(tapeId);

            if (tape == null || tape.Clip == null || m_AudioSource == null)
            {
                return;
            }

            m_AudioSource.Stop();
            m_AudioSource.clip = tape.Clip;
            m_AudioSource.Play();

            m_View.SetPauseButtonVisible(true);
            m_WasPlayingLastFrame = true;
        }

        private void StopPlayback()
        {
            if (m_AudioSource != null)
            {
                m_AudioSource.Stop();
            }

            m_View.SetPauseButtonVisible(false);
            m_WasPlayingLastFrame = false;
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

