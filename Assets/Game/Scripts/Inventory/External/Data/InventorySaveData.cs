using System;
using System.Collections.Generic;

namespace Game.Scripts.Inventory.External.Data
{
    /// <summary>
    /// Сохраняемое состояние инвентаря - то, что пишется в JSON-файл (через <see cref="UnityEngine.JsonUtility"/>).
    /// Хранятся ячейки инвентаря по порядку: ключ предмета и сколько таких предметов в ячейке.
    /// </summary>
    [Serializable]
    public class InventorySaveData
    {
        public const int CurrentVersion = 1;

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
        public int Amount;

        public InventorySaveEntry()
        {
        }

        public InventorySaveEntry(string itemKey, int amount)
        {
            ItemKey = itemKey;
            Amount = amount;
        }
    }
}
