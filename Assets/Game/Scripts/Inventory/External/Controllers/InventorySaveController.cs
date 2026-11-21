using System.Collections.Generic;
using Game.Scripts.Inventory.Controllers.Interfaces;
using Game.Scripts.Inventory.External.Controllers.Interfaces;
using Game.Scripts.Inventory.External.Data;
using Game.Scripts.Utilities.Save;
using JuicyDI;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Inventory.External.Controllers
{
    /// <summary>
    /// Сохранение инвентаря игрока в JSON-файл (Application.persistentDataPath/Saves, см. <see cref="JsonSaveFile"/>).
    /// Пишет ячейки инвентаря по порядку: ключ предмета и количество. Лежит рядом с InventoryController
    /// (префаб игрока).
    ///
    /// Сохранение вызывается общим контроллером сохранений (GameSaveController, ISaveParticipant) -
    /// в графе сюжета это действие "Save/Save Game". Загрузка - при старте сцены, в MethodInit.
    /// Подобранный предмет на сцене уничтожается, поэтому при загрузке предметы создаются заново по
    /// ключу, а иконку получают из каталога предметов (<see cref="InventoryItemCatalogSO"/>).
    /// Для проверки в Play Mode методы доступны из контекстного меню компонента.
    /// </summary>
    [JDIMonoController]
    [SequenceParticipant(105)]
    public class InventorySaveController : MonoBehaviour, ISaveParticipant, ISequence
    {
        [Tooltip("Имя JSON-файла сохранения инвентаря (лежит в Application.persistentDataPath/Saves).")]
        [SerializeField] private string m_FileName = "inventory.json";
        [Tooltip("Загружать сохранение при старте сцены.")]
        [SerializeField] private bool m_LoadOnStart = true;
        [Tooltip("Каталог предметов: из него восстановленные предметы получают иконки.")]
        [SerializeField] private InventoryItemCatalogSO m_ItemCatalog;

        [Inject] private IInventoryContent m_Content;
        [Inject] private IInventoryAdd m_Inventory;

        private readonly List<KeyValuePair<string, int>> m_ItemsBuffer = new();

        public string SavePath => JsonSaveFile.GetPath(m_FileName);
        public bool HasSave => JsonSaveFile.Exists(m_FileName);

        public void MethodInit()
        {
            if (m_LoadOnStart)
            {
                Load();
            }
        }

        public void MethodStart()
        {
        }

        [ContextMenu("Save")]
        public void Save()
        {
            if (m_Content == null)
            {
                Debug.LogError("[InventorySaveController] Инвентарь не заинжекчен - сохранение невозможно.", this);
                return;
            }

            m_Content.GetItems(m_ItemsBuffer);

            var data = new InventorySaveData();

            for (int i = 0; i < m_ItemsBuffer.Count; i++)
            {
                string itemKey = m_ItemsBuffer[i].Key;

                if (m_ItemCatalog == null || !m_ItemCatalog.Contains(itemKey))
                {
                    Debug.LogWarning($"[InventorySaveController] Предмета '{itemKey}' нет в каталоге предметов - после загрузки " +
                                     "он будет в инвентаре без иконки. Добавьте его в каталог (контекстное меню каталога -> " +
                                     "Collect From Prefabs).", this);
                }

                data.Items.Add(new InventorySaveEntry(itemKey, m_ItemsBuffer[i].Value));
            }

            m_ItemsBuffer.Clear();
            JsonSaveFile.TryWrite(m_FileName, data, this);
        }

        public bool Load()
        {
            if (m_Content == null || m_Inventory == null)
            {
                Debug.LogError("[InventorySaveController] Инвентарь не заинжекчен - загрузка невозможна.", this);
                return false;
            }

            if (!JsonSaveFile.TryRead(m_FileName, out InventorySaveData data, this))
            {
                return false;
            }

            m_Content.ClearItems();

            if (data.Items == null)
            {
                return true;
            }

            foreach (var entry in data.Items)
            {
                if (entry == null || string.IsNullOrEmpty(entry.ItemKey))
                {
                    continue;
                }

                Sprite icon = null;

                if (m_ItemCatalog == null || !m_ItemCatalog.TryGetIcon(entry.ItemKey, out icon))
                {
                    Debug.LogWarning($"[InventorySaveController] Предмета '{entry.ItemKey}' нет в каталоге предметов - " +
                                     "он восстановлен без иконки.", this);
                }

                for (int i = 0; i < entry.Amount; i++)
                {
                    m_Inventory.AddItem(new InventorySavedObject(entry.ItemKey, icon));
                }
            }

            return true;
        }

        [ContextMenu("Delete Save")]
        public void DeleteSave()
        {
            JsonSaveFile.Delete(m_FileName, this);
        }

        [ContextMenu("Load")]
        private void LoadFromContextMenu()
        {
            Load();
        }
    }
}
