using System;
using System.Collections.Generic;
using JuicyDI.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Чисто визуальная панель кассетного плеера: тоглы страниц, сами страницы (ScrollView с разделами
    /// и кассетами) и блок проигрывания - название кассеты, полоса прогресса с перемоткой и кнопки
    /// паузы/Play. Ничего не решает сама - только предоставляет примитивы управления, вызывается из
    /// <see cref="Controllers.CassettePlayerController"/> (получает вид через [Inject]).
    ///
    /// Блок проигрывания появляется, когда выбрана первая кассета, и дальше не пропадает - ни после паузы,
    /// ни когда кассета доиграла, ни после закрытия и открытия панели. Все его поля, кроме кнопки паузы,
    /// можно не назначать. Если не назначена кнопка Play, кнопка паузы сама переключает паузу и Play.
    /// </summary>
    [JDIMonoController]
    public class CassettePlayerView : MonoBehaviour
    {
        [Header("Корень панели")]
        [Tooltip("Корневой объект всей панели плеера (левая часть с макетом + правая часть со списком).")]
        [SerializeField] private GameObject m_Root;

        [Header("Список страниц")]
        [Tooltip("Контейнер, в который спавнятся тоглы страниц.")]
        [SerializeField] private Transform m_PageButtonsContent;
        [Tooltip("ToggleGroup тоглов страниц (обычно висит на контейнере тоглов).")]
        [SerializeField] private ToggleGroup m_PageToggleGroup;
        [Tooltip("Контейнер, в который спавнятся страницы (ScrollView). Видна только выбранная страница.")]
        [SerializeField] private Transform m_PageContent;
        [SerializeField] private CassettePageToggleView m_PageTogglePrefab;
        [SerializeField] private CassettePageView m_PagePrefab;

        [Header("Список разделов/кассет")]
        [SerializeField] private CassetteSectionRowView m_SectionRowPrefab;
        [SerializeField] private CassetteTapeRowView m_TapeRowPrefab;

        [Header("Проигрывание кассеты")]
        [Tooltip("Общий объект блока проигрывания (название, полоса, кнопки). Скрыт, пока не выбрана ни одна " +
                 "кассета. Можно не назначать - тогда скрываются только сами элементы.")]
        [SerializeField] private GameObject m_PlaybackRoot;
        [Tooltip("Название выбранной кассеты.")]
        [SerializeField] private TMP_Text m_TapeTitleText;
        [Tooltip("Полоса прогресса кассеты (Slider): показывает, сколько проиграно; перетаскиванием - перемотка.")]
        [SerializeField] private Slider m_ProgressSlider;
        [Tooltip("Кнопка паузы - видна, пока кассета играет.")]
        [SerializeField] private Button m_PauseButton;
        [Tooltip("Кнопка Play - видна вместо паузы, когда кассета на паузе (продолжает с того же места) или " +
                 "доиграла (запускает с начала).")]
        [SerializeField] private Button m_PlayButton;

        private readonly List<CassettePageToggleView> m_PageToggles = new();
        private readonly List<CassettePageView> m_Pages = new();

        private Action<string> m_OnTapeClicked;
        private Action<int> m_OnPageSelected;
        private Action m_OnPauseClicked;
        private Action m_OnPlayClicked;
        private CassetteSeekSliderView m_SeekSlider;
        private bool m_IsPlaying;
        private int m_PageCount;
        private int m_SelectedPageIndex = -1;

        /// <summary>
        /// Индекс выбранной страницы (-1, если страниц нет).
        /// </summary>
        public int SelectedPageIndex => m_SelectedPageIndex;

        private void Awake()
        {
            if (m_ProgressSlider != null && !m_ProgressSlider.TryGetComponent(out m_SeekSlider))
            {
                m_SeekSlider = m_ProgressSlider.gameObject.AddComponent<CassetteSeekSliderView>();
            }

            SetPlaybackVisible(false);
        }

        public void SetVisible(bool isVisible)
        {
            if (m_Root != null)
            {
                m_Root.SetActive(isVisible);
            }
        }

        /// <summary>
        /// Немедленно пересчитывает вёрстку панели (после открытия/смены страницы, чтобы layout-группы
        /// не отрисовали один кадр со старыми размерами).
        /// </summary>
        public void RebuildLayout()
        {
            if (m_Root != null && m_Root.transform is RectTransform rootRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            }
        }

        /// <summary>
        /// Что делать по кнопкам паузы и Play и по перемотке полосой (доля от длины кассеты, 0..1).
        /// </summary>
        public void SetPlaybackActions(Action onPauseClicked, Action onPlayClicked, Action<float> onSeek)
        {
            m_OnPauseClicked = onPauseClicked;
            m_OnPlayClicked = onPlayClicked;

            if (m_PauseButton != null)
            {
                m_PauseButton.onClick.RemoveAllListeners();
                m_PauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            if (m_PlayButton != null)
            {
                m_PlayButton.onClick.RemoveAllListeners();
                m_PlayButton.onClick.AddListener(() => m_OnPlayClicked?.Invoke());
            }

            if (m_SeekSlider != null)
            {
                m_SeekSlider.Setup(onSeek);
            }
        }

        /// <summary>
        /// Показывает или прячет весь блок проигрывания (название, полосу, кнопки).
        /// </summary>
        public void SetPlaybackVisible(bool isVisible)
        {
            if (m_PlaybackRoot != null)
            {
                m_PlaybackRoot.SetActive(isVisible);
            }

            SetActive(m_TapeTitleText, isVisible);
            SetActive(m_ProgressSlider, isVisible);

            if (isVisible)
            {
                SetPlaying(m_IsPlaying);
            }
            else
            {
                SetActive(m_PauseButton, false);
                SetActive(m_PlayButton, false);
            }
        }

        public void SetTapeTitle(string title)
        {
            if (m_TapeTitleText != null)
            {
                m_TapeTitleText.text = title;
            }
        }

        /// <summary>
        /// Сколько проиграно (0..1). Пока игрок держит ползунок, полоса не обновляется.
        /// </summary>
        public void SetProgress(float progress)
        {
            if (m_SeekSlider != null)
            {
                m_SeekSlider.SetProgress(progress);
            }
        }

        /// <summary>
        /// Кассета играет - видна кнопка паузы; на паузе или доиграла - кнопка Play.
        /// </summary>
        public void SetPlaying(bool isPlaying)
        {
            m_IsPlaying = isPlaying;

            if (m_PlayButton == null)
            {
                // Кнопки Play нет - кнопка паузы видна всегда и переключает паузу и Play.
                SetActive(m_PauseButton, true);
                return;
            }

            SetActive(m_PauseButton, isPlaying);
            SetActive(m_PlayButton, !isPlaying);
        }

        private void OnPauseButtonClicked()
        {
            if (m_PlayButton == null && !m_IsPlaying)
            {
                m_OnPlayClicked?.Invoke();
                return;
            }

            m_OnPauseClicked?.Invoke();
        }

        private static void SetActive(Component component, bool isActive)
        {
            if (component != null && component.gameObject.activeSelf != isActive)
            {
                component.gameObject.SetActive(isActive);
            }
        }

        /// <summary>
        /// Спавнит по тоглу и странице на каждую страницу и заполняет все страницы разделами и кассетами.
        /// Вызывается один раз на старте - дальше страницы только обновляются через <see cref="RefreshPage"/>.
        /// </summary>
        /// <param name="onTapeClicked">Клик по найденной кассете (передаётся TapeId).</param>
        /// <param name="onPageSelected">Игрок переключил страницу тоглом (передаётся индекс страницы).</param>
        public void BuildPages(IReadOnlyList<CassettePageRowData> pages, Action<string> onTapeClicked,
            Action<int> onPageSelected)
        {
            m_OnTapeClicked = onTapeClicked;
            m_OnPageSelected = onPageSelected;
            m_PageCount = pages?.Count ?? 0;

            if (m_PageToggleGroup != null)
            {
                // Всегда выбрана ровно одна страница - повторный клик по активному тоглу не должен его снимать.
                m_PageToggleGroup.allowSwitchOff = false;
            }

            for (int i = 0; i < m_PageCount; i++)
            {
                int pageIndex = i;
                var pageData = pages[i];

                var pageToggle = GetPageToggle(i);
                pageToggle.gameObject.SetActive(true);
                pageToggle.Setup(pageData.PageName, pageData.PageColor, m_PageToggleGroup,
                    () => SelectPage(pageIndex, true));

                GetPage(i).Build(pageData.Sections, m_OnTapeClicked);
            }

            for (int i = m_PageCount; i < m_PageToggles.Count; i++)
            {
                m_PageToggles[i].gameObject.SetActive(false);
                m_Pages[i].SetVisible(false);
            }

            int selectedPageIndex = m_PageCount == 0 ? -1 : Mathf.Clamp(m_SelectedPageIndex, 0, m_PageCount - 1);
            SelectPage(selectedPageIndex, false);
        }

        /// <summary>
        /// Обновляет разделы и кассеты одной страницы (без спавна новых страниц).
        /// </summary>
        public void RefreshPage(int pageIndex, IReadOnlyList<CassetteSectionRowData> sections)
        {
            if (pageIndex < 0 || pageIndex >= m_PageCount)
            {
                return;
            }

            m_Pages[pageIndex].Build(sections, m_OnTapeClicked);
        }

        private void SelectPage(int pageIndex, bool notify)
        {
            m_SelectedPageIndex = pageIndex;

            for (int i = 0; i < m_PageCount; i++)
            {
                bool isSelected = i == pageIndex;
                m_Pages[i].SetVisible(isSelected);
                m_PageToggles[i].SetIsOnWithoutNotify(isSelected);
            }

            if (notify && pageIndex >= 0)
            {
                m_OnPageSelected?.Invoke(pageIndex);
            }
        }

        private CassettePageToggleView GetPageToggle(int index)
        {
            if (index >= m_PageToggles.Count)
            {
                m_PageToggles.Add(Instantiate(m_PageTogglePrefab, m_PageButtonsContent));
            }

            return m_PageToggles[index];
        }

        private CassettePageView GetPage(int index)
        {
            if (index >= m_Pages.Count)
            {
                var page = Instantiate(m_PagePrefab, m_PageContent);
                page.Init(m_SectionRowPrefab, m_TapeRowPrefab);
                m_Pages.Add(page);
            }

            return m_Pages[index];
        }
    }
}

