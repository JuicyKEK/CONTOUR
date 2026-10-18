using System;
using System.Collections.Generic;
using JuicyDI.Attributes;
using UnityEngine;

namespace Game.Scripts.Utilities.Save
{
    /// <summary>
    /// Общий контроллер сохранений: SaveGame() обходит все механики, которые участвуют в сейвах
    /// (бины JuicyDI с <see cref="ISaveParticipant"/>: кассеты, зоны заражения, сюжет GBS...).
    /// Новая механика попадает в пул автоматически - достаточно реализовать ISaveParticipant.
    ///
    /// Вешается один раз на сцену. Вызывать: [Inject] IGameSaveService -> SaveGame(), действие графа
    /// "Save/Save Game" или контекстное меню компонента. Удалить сейвы в редакторе:
    /// UnityDev/Saves/Delete All Saves.
    /// </summary>
    [JDIMonoController]
    public class GameSaveController : MonoBehaviour, IGameSaveService
    {
        [Inject] private List<ISaveParticipant> m_Participants;

        public bool HasSave
        {
            get
            {
                foreach (var participant in Participants)
                {
                    if (participant.HasSave)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // JuicyDI переинжектит список при загрузке/выгрузке сцен - в нём только живые механики.
        private IEnumerable<ISaveParticipant> Participants => m_Participants ?? (IEnumerable<ISaveParticipant>)Array.Empty<ISaveParticipant>();

        [ContextMenu("Save Game")]
        public void SaveGame()
        {
            int savedCount = ForEachParticipant(participant => participant.Save(), "сохранении");
            Debug.Log($"[GameSave] Игра сохранена, механик в сейве: {savedCount}. Папка: {JsonSaveFile.FolderPath}");
        }

        [ContextMenu("Delete Saves")]
        public void DeleteSaves()
        {
            int deletedCount = ForEachParticipant(participant => participant.DeleteSave(), "удалении сохранения");
            Debug.Log($"[GameSave] Сохранения удалены, механик: {deletedCount}.");
        }

        private int ForEachParticipant(Action<ISaveParticipant> action, string operationName)
        {
            int count = 0;

            foreach (var participant in Participants)
            {
                if (participant == null)
                {
                    continue;
                }

                // Ошибка одной механики не должна оставить остальные без сохранения.
                try
                {
                    action(participant);
                    count++;
                }
                catch (Exception exception)
                {
                    Debug.LogError($"[GameSave] Ошибка при {operationName} {participant.GetType().Name}.", this);
                    Debug.LogException(exception, this);
                }
            }

            return count;
        }
    }
}
