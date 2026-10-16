using UnityEngine.SceneManagement;

namespace Game.Scripts.LocationState.Runtime.Interfaces
{
    /// <summary>
    /// Объект локации, состояние которого сохраняется (подобран ли предмет, открыта ли дверь, включён ли свет).
    /// Обычно это наследник LocationStateSaver - базовый класс сам регистрирует объект в
    /// <see cref="ILocationStateService"/>.
    /// </summary>
    public interface ILocationStateObject
    {
        /// <summary>
        /// Уникальный ключ объекта в сохранении: путь сцены + ключ объекта в сцене.
        /// </summary>
        string StateKey { get; }

        /// <summary>
        /// Сцена, в которой лежит объект.
        /// </summary>
        Scene Scene { get; }

        /// <summary>
        /// Текущее состояние объекта строкой (обычно JSON) - то, что сохраняется; null - объект в том же
        /// состоянии, что и в сцене, хранить нечего.
        /// </summary>
        string CaptureState();

        /// <summary>
        /// Ставит объекту сохранённое состояние - сразу, без анимаций и звуков.
        /// </summary>
        void ApplySavedState(string state);
    }
}
