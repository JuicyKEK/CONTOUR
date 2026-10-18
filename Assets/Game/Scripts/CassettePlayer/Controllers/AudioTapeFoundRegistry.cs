using System.Collections.Generic;
using Game.Scripts.Instructions.Interfaces;
using JuicyDI.Attributes;
using JuicyDI.Context;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Реестр прогресса аудио-кассет за игровую сессию: какие кассеты найдены, какие прослушаны
    /// и задачи каких кассет выполнены (все наборы - просто множества TapeId). UI плеера при каждом
    /// открытии заново спрашивает состояние кассет через <see cref="IsTapeFound"/> /
    /// <see cref="IsTapeListened"/> / <see cref="IsTapeCompleted"/>.
    /// Сохранение/загрузка наборов - <see cref="AudioTapeSaveController"/>.
    /// </summary>
    [JDIMonoController(Context = typeof(GlobalBean))]
    public class AudioTapeFoundRegistry : MonoBehaviour, IAudioTapeFoundRegistry
    {
        private readonly HashSet<string> m_FoundTapeIds = new();
        private readonly HashSet<string> m_ListenedTapeIds = new();
        private readonly HashSet<string> m_CompletedTapeIds = new();

        public IReadOnlyCollection<string> FoundTapeIds => m_FoundTapeIds;
        public IReadOnlyCollection<string> ListenedTapeIds => m_ListenedTapeIds;
        public IReadOnlyCollection<string> CompletedTapeIds => m_CompletedTapeIds;

        public bool IsTapeFound(string tapeId)
        {
            return Contains(m_FoundTapeIds, tapeId);
        }

        public void MarkTapeFound(string tapeId)
        {
            Add(m_FoundTapeIds, tapeId);
        }

        public bool IsTapeListened(string tapeId)
        {
            return Contains(m_ListenedTapeIds, tapeId);
        }

        public void MarkTapeListened(string tapeId)
        {
            Add(m_ListenedTapeIds, tapeId);
        }

        public bool IsTapeCompleted(string tapeId)
        {
            return Contains(m_CompletedTapeIds, tapeId);
        }

        public void MarkTapeCompleted(string tapeId)
        {
            Add(m_CompletedTapeIds, tapeId);
        }

        public void RestoreFoundTapes(IEnumerable<string> tapeIds)
        {
            Restore(m_FoundTapeIds, tapeIds);
        }

        public void RestoreListenedTapes(IEnumerable<string> tapeIds)
        {
            Restore(m_ListenedTapeIds, tapeIds);
        }

        public void RestoreCompletedTapes(IEnumerable<string> tapeIds)
        {
            Restore(m_CompletedTapeIds, tapeIds);
        }

        private static bool Contains(HashSet<string> tapeIds, string tapeId)
        {
            return !string.IsNullOrEmpty(tapeId) && tapeIds.Contains(tapeId);
        }

        private static void Add(HashSet<string> tapeIds, string tapeId)
        {
            if (string.IsNullOrEmpty(tapeId))
            {
                return;
            }

            tapeIds.Add(tapeId);
        }

        private static void Restore(HashSet<string> target, IEnumerable<string> tapeIds)
        {
            target.Clear();

            if (tapeIds == null)
            {
                return;
            }

            foreach (var tapeId in tapeIds)
            {
                Add(target, tapeId);
            }
        }
    }
}

