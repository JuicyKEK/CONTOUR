using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Чисто визуальная панель кассетного плеера: тоглы страниц, сами страницы (ScrollView с разделами
    /// и кассетами) и кнопка паузы по центру (видна только когда что-то проигрывается). Ничего не решает
    /// сама - только предоставляет примитивы управления, вызывается из
    /// <see cref="Controllers.CassettePlayerController"/>.
    /// </summary>
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

        [Header("Кнопка паузы (по центру, видна только во время проигрывания)")]
        [SerializeField] private Button m_PauseButton;

        private readonly List<CassettePageToggleView> m_PageToggles = new();
        private readonly List<CassettePageView> m_Pages = new();

        private Action<string> m_OnTapeClicked;
        private Action<int> m_OnPageSelected;
        private int m_PageCount;
        private int m_SelectedPageIndex = -1;

        /// <summary>
        /// Индекс выбранной страницы (-1, если страниц нет).
        /// </summary>
        public int SelectedPageIndex => m_SelectedPageIndex;

        private void Awake()
        {
            SetPauseButtonVisible(false);
        }

        public void SetVisible(bool isVisible)
        {
            if (m_Root != null)
            {
                m_Root.SetActive(isVisible);

                if (!isVisible)
                {
                    SetPauseButtonVisible(false);
                }
            }
        }

        public void SetPauseButtonVisible(bool isVisible)
        {
            if (m_PauseButton != null)
            {
                m_PauseButton.gameObject.SetActive(isVisible);
            }
        }

        public void SetPauseButtonAction(Action onPauseClicked)
        {
            if (m_PauseButton == null)
            {
                return;
            }

            m_PauseButton.onClick.RemoveAllListeners();

            if (onPauseClicked != null)
            {
                m_PauseButton.onClick.AddListener(() => onPauseClicked());
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

