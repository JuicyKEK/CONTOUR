using Game.Scripts.InputController;
using Game.Scripts.Instructions.Data;
using Game.Scripts.Instructions.Interfaces;
using Game.Scripts.Instructions.View;
using JuicyDI;
using JuicyDI.Attributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Кассетный плеер: по TAB открывает/закрывает панель (слева макет проигрывателя,
    /// справа список разделов с найденными/ненайденными кассетами). Пока панель открыта,
    /// движение камеры FirstPersonController заблокировано и виден курсор мыши.
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
        [Tooltip("Все разделы кассет, доступные в игре (найденные/ненайденные определяются реестром в рантайме).")]
        [SerializeField] private AudioTapeSectionDefinitionSO[] m_Sections;

        [Header("Воспроизведение")]
        [SerializeField] private AudioSource m_AudioSource;

        [Header("Игрок")]
        [Tooltip("Контроллер камеры игрока - на время открытой панели плеера его вращение блокируется.")]
        [SerializeField] private FirstPersonController m_FirstPersonController;
        [SerializeField] private GameObject m_CassettePlayerModel;

        [Inject] private IInputActions m_InputActions;
        [Inject] private IAudioTapeFoundRegistry m_Registry;

        private readonly List<CassetteSectionRowData> m_SectionRowsBuffer = new();
        private bool m_IsOpen;
        private bool m_WasPlayingLastFrame;

        public void MethodInit()
        {
        }

        public void MethodStart()
        {
            RegistryCaseToStart();
            m_InputActions.AddPressingButtonTabAction(ToggleOpen);
            m_View.SetPauseButtonAction(StopPlayback);
            m_View.SetVisible(false);
            m_View.SetPauseButtonVisible(false);
        }

        private void RegistryCaseToStart()
        {
            for (int i = 0; i < m_Sections.Length; i++)
            {
                for (int j = 0; j < m_Sections[i].Tapes.Length; j++)
                {
                    if (m_Sections[i].Tapes[j].OnStart)
                    {
                        m_Registry.MarkTapeFound(m_Sections[i].Tapes[j].TapeId);
                    }
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

            RebuildList();
            m_View.SetVisible(true);
            m_View.SetPauseButtonVisible(m_AudioSource != null && m_AudioSource.isPlaying);
            m_WasPlayingLastFrame = m_AudioSource != null && m_AudioSource.isPlaying;

            if (m_FirstPersonController != null)
            {
                m_FirstPersonController.cameraCanMove = false;
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

            if (m_FirstPersonController != null)
            {
                m_FirstPersonController.cameraCanMove = true;
            }

            CassettePlayerModel();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void CassettePlayerModel() //Можно все переделать под реакт т.к. view
        {
            m_CassettePlayerModel.SetActive(m_IsOpen);
        }

        private void RebuildList()
        {
            m_SectionRowsBuffer.Clear();

            if (m_Sections == null)
            {
                m_View.BuildSections(m_SectionRowsBuffer, PlayTape);
                return;
            }

            for (int i = 0; i < m_Sections.Length; i++)
            {
                var section = m_Sections[i];

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
                    tapeRows.Add(new CassetteTapeRowData(tape.TapeId, tape.DisplayName, isFound));
                }

                m_SectionRowsBuffer.Add(new CassetteSectionRowData(section.DisplayName, tapeRows));
            }

            m_View.BuildSections(m_SectionRowsBuffer, PlayTape);
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
            if (m_Sections == null || string.IsNullOrEmpty(tapeId))
            {
                return null;
            }

            foreach (var section in m_Sections)
            {
                if (section?.Tapes == null)
                {
                    continue;
                }

                foreach (var tape in section.Tapes)
                {
                    if (tape != null && tape.TapeId == tapeId)
                    {
                        return tape;
                    }
                }
            }

            return null;
        }
    }
}

