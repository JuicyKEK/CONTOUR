using R3;
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
        /// Куда смотрит игрок, пока его убивают: кость глаз игрока в скелете монстра (анимация убийства от первого
        /// лица). Null - обычная камера игрока.
        /// </summary>
        ReadOnlyReactiveProperty<Transform> ViewAnchor { get; }

        /// <summary>
        /// Камера игрока идёт за anchor (положение и поворот), пока игрока не отпустят (Release).
        /// </summary>
        void AttachView(Transform anchor);

        /// <summary>
        /// Вернуть управление (если сцену не перезагружают - например, сюжет показал смерть сам).
        /// </summary>
        void Release();
    }
}
