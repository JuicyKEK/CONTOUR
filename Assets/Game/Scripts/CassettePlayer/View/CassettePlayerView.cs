using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// ReSharper disable once CheckNamespace

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Данные одной строки кассеты для построения списка (см. <see cref="CassettePlayerView.BuildSections"/>).
    /// </summary>
    public readonly struct CassetteTapeRowData
    {
        public readonly string TapeId;
        public readonly string DisplayName;
        public readonly bool IsFound;

        public CassetteTapeRowData(string tapeId, string displayName, bool isFound)
        {
            TapeId = tapeId;
            DisplayName = displayName;
            IsFound = isFound;
        }
    }

    /// <summary>
    /// Данные одного раздела списка кассет для построения списка.
    /// </summary>
    public readonly struct CassetteSectionRowData
    {
        public readonly string DisplayName;
        public readonly IReadOnlyList<CassetteTapeRowData> Tapes;

        public CassetteSectionRowData(string displayName, IReadOnlyList<CassetteTapeRowData> tapes)
        {
            DisplayName = displayName;
            Tapes = tapes;
        }
    }

    /// <summary>
    /// Чисто визуальная панель кассетного плеера: слева "макет" самого проигрывателя,
    /// справа - список разделов с кассетами, по центру - кнопка паузы (видна только
    /// когда что-то проигрывается). Ничего не решает сама - только предоставляет
    /// примитивы управления, вызывается из <see cref="Controllers.CassettePlayerController"/>.
    /// </summary>
    public class CassettePlayerView : MonoBehaviour
    {
        [Header("Корень панели")]
        [Tooltip("Корневой объект всей панели плеера (левая часть с макетом + правая часть со списком).")]
        [SerializeField] private GameObject m_Root;

        [Header("Список разделов/кассет (правая часть)")]
        [SerializeField] private CassetteSectionRowView m_SectionRowPrefab;
        [SerializeField] private Transform m_SectionsContainer;
        [SerializeField] private CassetteTapeRowView m_TapeRowPrefab;

        [Header("Кнопка паузы (по центру, видна только во время проигрывания)")]
        [SerializeField] private Button m_PauseButton;

        private readonly List<CassetteSectionRowView> m_SectionRowPool = new();
        private readonly List<CassetteTapeRowView> m_TapeRowPool = new();
        private int m_UsedTapeRows;

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
        /// Полностью перестраивает список разделов/кассет (вызывается при каждом открытии панели).
        /// </summary>
        public void BuildSections(IReadOnlyList<CassetteSectionRowData> sections, Action<string> onTapeClicked)
        {
            m_UsedTapeRows = 0;

            for (int i = 0; i < m_SectionRowPool.Count; i++)
            {
                m_SectionRowPool[i].gameObject.SetActive(false);
            }

            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                var sectionData = sections[sectionIndex];
                var sectionRow = GetSectionRow(sectionIndex);
                sectionRow.gameObject.SetActive(true);
                sectionRow.Setup(sectionData.DisplayName);

                for (int tapeIndex = 0; tapeIndex < sectionData.Tapes.Count; tapeIndex++)
                {
                    var tapeData = sectionData.Tapes[tapeIndex];
                    var tapeRow = GetTapeRow(sectionRow.TapesContainer);
                    tapeRow.gameObject.SetActive(true);

                    string tapeId = tapeData.TapeId;
                    tapeRow.Setup(tapeData.DisplayName, tapeData.IsFound, () => onTapeClicked?.Invoke(tapeId));
                }
            }

            // Прячем неиспользованные в этот раз строки кассет (пул переиспользуется целиком,
            // без привязки к конкретному разделу, поэтому неиспользованный хвост пула гасим отдельно).
            for (int i = m_UsedTapeRows; i < m_TapeRowPool.Count; i++)
            {
                m_TapeRowPool[i].gameObject.SetActive(false);
            }
        }

        private CassetteSectionRowView GetSectionRow(int index)
        {
            if (index >= m_SectionRowPool.Count)
            {
                m_SectionRowPool.Add(Instantiate(m_SectionRowPrefab, m_SectionsContainer));
            }

            return m_SectionRowPool[index];
        }

        private CassetteTapeRowView GetTapeRow(Transform parent)
        {
            CassetteTapeRowView row;

            if (m_UsedTapeRows >= m_TapeRowPool.Count)
            {
                row = Instantiate(m_TapeRowPrefab, parent);
                m_TapeRowPool.Add(row);
            }
            else
            {
                row = m_TapeRowPool[m_UsedTapeRows];
                row.transform.SetParent(parent, false);
            }

            m_UsedTapeRows++;
            return row;
        }
    }
}


