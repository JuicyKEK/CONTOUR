using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Instructions.View
{
    /// <summary>
    /// Одна страница кассет (префаб со ScrollView): строит в своём Content разделы
    /// (<see cref="CassetteSectionRowView"/>) и строки кассет (<see cref="CassetteTapeRowView"/>).
    /// Строки переиспользуются, поэтому повторный <see cref="Build"/> только обновляет данные.
    /// </summary>
    public class CassettePageView : MonoBehaviour
    {
        [Tooltip("Content ScrollView страницы, в который спавнятся разделы.")]
        [SerializeField] private Transform m_Content;

        private readonly List<CassetteSectionRowView> m_SectionRowPool = new();
        private readonly List<CassetteTapeRowView> m_TapeRowPool = new();

        private CassetteSectionRowView m_SectionRowPrefab;
        private CassetteTapeRowView m_TapeRowPrefab;
        private int m_UsedTapeRows;

        public void Init(CassetteSectionRowView sectionRowPrefab, CassetteTapeRowView tapeRowPrefab)
        {
            m_SectionRowPrefab = sectionRowPrefab;
            m_TapeRowPrefab = tapeRowPrefab;
        }

        public void SetVisible(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }

        /// <summary>
        /// Строит (при первом вызове) или обновляет (при последующих) разделы и кассеты страницы.
        /// </summary>
        public void Build(IReadOnlyList<CassetteSectionRowData> sections, Action<string> onTapeClicked)
        {
            m_UsedTapeRows = 0;

            for (int i = 0; i < m_SectionRowPool.Count; i++)
            {
                m_SectionRowPool[i].gameObject.SetActive(false);
            }

            int sectionsCount = sections?.Count ?? 0;

            for (int sectionIndex = 0; sectionIndex < sectionsCount; sectionIndex++)
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
                    tapeRow.Setup(tapeData, () => onTapeClicked?.Invoke(tapeId));
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
                m_SectionRowPool.Add(Instantiate(m_SectionRowPrefab, m_Content));
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
