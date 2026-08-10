using System.Collections.Generic;
using Game.Scripts.Instructions.Interfaces;
using JuicyDI.Attributes;
using JuicyDI.Context;
using UnityEngine;

namespace Game.Scripts.Instructions.Controllers
{
    /// <summary>
    /// Реестр найденных аудио-кассет за игровую сессию. Простое хранилище "найдено/не найдено"
    /// по TapeId - подбор кассеты просто добавляет её Id в набор, UI плеера при каждом открытии
    /// заново спрашивает состояние каждой кассеты через <see cref="IsTapeFound"/>.
    /// </summary>
    [JDIMonoController(Context = typeof(GlobalBean))]
    public class AudioTapeFoundRegistry : MonoBehaviour, IAudioTapeFoundRegistry
    {
        private readonly HashSet<string> m_FoundTapeIds = new();

        public bool IsTapeFound(string tapeId)
        {
            return !string.IsNullOrEmpty(tapeId) && m_FoundTapeIds.Contains(tapeId);
        }

        public void MarkTapeFound(string tapeId)
        {
            if (string.IsNullOrEmpty(tapeId))
            {
                return;
            }
            
            m_FoundTapeIds.Add(tapeId);
        }
    }
}

