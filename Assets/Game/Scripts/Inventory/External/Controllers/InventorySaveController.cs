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
    /// Пишет ячейки инвентаря по порядку: ключ предмета, ключ его иконки в Addressables и количество.
    ///
    /// Сохранение вызывается общим контроллером сохранений (GameSaveController, ISaveParticipant) -
    /// в графе сюжета это действие "Save/Save Game". Загрузка - при старте сцены, в MethodInit.
    /// Подобранный предмет на сцене уничтожается, поэтому при загрузке предметы создаются заново по ключам
    /// (InventoryItemData), а иконку по ключу грузит вид инвентаря.
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

        [Inject] private IInventoryContent m_Content;
        [Inject] private IInventoryAdd m_Inventory;

        private readonly List<KeyValuePair<IInventoryObject, int>> m_ItemsBuffer = new();

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
                var item = m_ItemsBuffer[i].Key;
                data.Items.Add(new InventorySaveEntry(item.ObjectKey, item.IconAddress, m_ItemsBuffer[i].Value));
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

                if (string.IsNullOrEmpty(entry.IconAddress))
                {
                    Debug.LogWarning($"[InventorySaveController] У предмета '{entry.ItemKey}' в сохранении нет ключа иконки " +
                                     $"(сохранение версии {data.Version}) - он восстановлен без иконки.", this);
                }

                for (int i = 0; i < entry.Amount; i++)
                {
                    m_Inventory.AddItem(new InventoryItemData(entry.ItemKey, entry.IconAddress));
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
