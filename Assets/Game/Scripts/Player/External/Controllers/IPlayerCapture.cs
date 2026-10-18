using UnityEngine;

namespace Game.Scripts.Player.External.Controllers
{
    /// <summary>
    /// Игрока схватили (монстр, сюжет): управление отобрано, камера смотрит на того, кто схватил.
    /// </summary>
    public interface IPlayerCapture
    {
        bool IsCaptured { get; }

        /// <summary>
        /// Отобрать у игрока управление (движение, обзор, взаимодействие) и повернуть камеру к attacker (может быть null).
        /// </summary>
        void Capture(Transform attacker);

        /// <summary>
        /// Вернуть управление (если сцену не перезагружают - например, сюжет показал смерть сам).
        /// </summary>
        void Release();
    }
}
