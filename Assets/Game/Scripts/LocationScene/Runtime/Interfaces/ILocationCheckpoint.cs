using UnityEngine;

namespace Game.Scripts.LocationScene.Runtime.Interfaces
{
    /// <summary>
    /// Чекпоинт на локации - точка, в которой игрок появляется при загрузке сохранения, после смерти
    /// или в начале новой игры.
    /// </summary>
    public interface ILocationCheckpoint
    {
        /// <summary>
        /// Ключ чекпоинта (уникален в пределах игры), пишется в сохранение.
        /// </summary>
        string CheckpointId { get; }

        /// <summary>
        /// Путь сцены, в которой стоит чекпоинт, - по нему определяется локация чекпоинта.
        /// </summary>
        string ScenePath { get; }

        /// <summary>
        /// Где появляется игрок (точка на полу).
        /// </summary>
        Vector3 Position { get; }

        /// <summary>
        /// Куда игрок смотрит при появлении (только поворот вокруг вертикали).
        /// </summary>
        Quaternion Rotation { get; }
    }
}
