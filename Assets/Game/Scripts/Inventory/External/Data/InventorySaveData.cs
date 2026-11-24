using System;
using System.Collections.Generic;

namespace Game.Scripts.Inventory.External.Data
{
    /// <summary>
    /// Сохраняемое состояние инвентаря - то, что пишется в JSON-файл (через <see cref="UnityEngine.JsonUtility"/>).
    /// Хранятся ячейки инвентаря по порядку: ключ предмета, ключ его иконки в Addressables и сколько таких предметов в ячейке.
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        /// <summary>
        /// 1 - ключ и количество; 2 - ещё ключ иконки (в версии 1 иконки брались из каталога предметов).
        /// </summary>
        public const int CurrentVersion = 2;

        /// <summary>
        /// Версия формата - пригодится для миграции, если структура сохранения изменится.
        /// </summary>
        public int Version = CurrentVersion;

        public List<InventorySaveEntry> Items = new();
    }

    [Serializable]
    public class InventorySaveEntry
    {
        public string ItemKey;
        public string IconAddress;
        public int Amount;

        public InventorySaveEntry()
        {
        }

        public InventorySaveEntry(string itemKey, string iconAddress, int amount)
        {
            ItemKey = itemKey;
            IconAddress = iconAddress;
            Amount = amount;
        }
    }
}
