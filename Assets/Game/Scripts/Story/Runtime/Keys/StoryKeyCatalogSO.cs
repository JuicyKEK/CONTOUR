using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Story
{
    /// <summary>
    /// Каталог ключей сюжета - одно место, где перечислены все сигналы, флаги и реакции сцены
    /// (вместо отдельного SO-ассета на каждый сигнал). Используется только редактором: выпадающий
    /// список у полей с [StoryKey] и проверка опечаток. В рантайме ключи - обычные строки.
    ///
    /// Каталогов может быть несколько (например, по главам): в меню выбора ключа сначала выбирается
    /// каталог, затем ключ из него. Внутри каталога ключи можно группировать через "/" (например
    /// "Door_1_3/Entered") - это станут подменю. Ключи в сюжете общие: одинаковый ключ в двух каталогах -
    /// это один и тот же флаг/сигнал, поэтому называйте ключи уникально (например с префиксом главы).
    /// </summary>
    [CreateAssetMenu(menuName = "Story/Story Key Catalog", fileName = "StoryKeys")]
    public class StoryKeyCatalogSO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string Key;
            public StoryKeyKind Kind = StoryKeyKind.Signal;
            [TextArea(1, 3)] public string Description;
        }

        [SerializeField] private List<Entry> m_Entries = new();

        public IReadOnlyList<Entry> Entries => m_Entries;

        public Entry Find(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            for (int i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i] != null && m_Entries[i].Key == key)
                {
                    return m_Entries[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Добавляет ключ, если его ещё нет. Возвращает true, если каталог изменился.
        /// </summary>
        public bool Add(string key, StoryKeyKind kind, string description = null)
        {
            if (string.IsNullOrEmpty(key) || Find(key) != null)
            {
                return false;
            }

            m_Entries.Add(new Entry
            {
                Key = key,
                Kind = kind == StoryKeyKind.Any ? StoryKeyKind.Signal : kind,
                Description = description
            });

            m_Entries.Sort((a, b) => string.CompareOrdinal(a?.Key, b?.Key));
            return true;
        }
    }
}
