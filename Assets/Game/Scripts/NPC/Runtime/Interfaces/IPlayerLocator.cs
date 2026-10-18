using UnityEngine;

namespace Game.Scripts.NPC.Runtime.Interfaces
{
    /// <summary>
    /// Сервис, дающий ссылку на игрока любому боту через
    /// [Inject], без FindWithTag в каждом контроллере NPC.
    /// </summary>
    public interface IPlayerLocator
    {
        Transform PlayerTransform { get; }
    }
}

