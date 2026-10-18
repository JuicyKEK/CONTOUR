using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.Inventory.External.Data
{
    /// <summary>
    /// Каталог предметов инвентаря: ключ предмета и его иконка. Нужен для загрузки сохранения: в файл
    /// пишутся только ключи и количество, а сам объект предмета после подбора уничтожен - иконку
    /// восстановленному предмету берут отсюда. Предмета нет в каталоге - он восстановится без иконки.
    ///
    /// Заполнить каталог ключами и иконками всех предметов из префабов проекта: контекстное меню
    /// ассета -> Collect From Prefabs. Предметы, которые есть только в сценах, добавляются вручную.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Inventory/Item Catalog", fileName = "InventoryItemCatalog")]
    public class InventoryItemCatalogSO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Ключ предмета - тот же, что у предмета на сцене (ObjectKey).")]
            public string Key;
            [Tooltip("Иконка предмета в инвентаре.")]
            public Sprite Icon;
        }

        [SerializeField] private List<Entry> m_Items = new();

        public IReadOnlyList<Entry> Items => m_Items;

        public bool Contains(string key)
        {
            return Find(key) != null;
        }

        public bool TryGetIcon(string key, out Sprite icon)
        {
            var entry = Find(key);
            icon = entry?.Icon;
            return entry != null;
        }

        private Entry Find(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            for (int i = 0; i < m_Items.Count; i++)
            {
                if (m_Items[i] != null && m_Items[i].Key == key)
                {
                    return m_Items[i];
                }
            }

            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Collect From Prefabs")]
        private void CollectFromPrefabs()
        {
            int addedCount = 0;

            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

                if (prefab == null)
                {
                    continue;
                }

                foreach (var item in prefab.GetComponentsInChildren<IInventoryObject>(true))
                {
                    if (string.IsNullOrEmpty(item.ObjectKey) || Contains(item.ObjectKey))
                    {
                        continue;
                    }

                    m_Items.Add(new Entry { Key = item.ObjectKey, Icon = item.ObjectIcon });
                    addedCount++;
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[Inventory] Каталог предметов '{name}': добавлено предметов из префабов - {addedCount}, всего {m_Items.Count}.", this);
        }
#endif
    }
}
