namespace Game.Scripts.InteractionObjects.Interfaces
{
    /// <summary>
    /// Дверь, состояние которой сохраняется между загрузками локации (DoorStateSaver): открыта ли она
    /// и заперта ли защёлкой.
    /// </summary>
    public interface IRestorableDoor
    {
        /// <summary>
        /// Идёт <see cref="RestoreState"/>: виды и звуки двери, получив в этот момент изменение состояния,
        /// должны сразу встать в нужное положение - без анимации и без звука.
        /// </summary>
        bool IsRestoringState { get; }

        /// <summary>
        /// Текущее состояние двери - то, что сохраняется.
        /// </summary>
        void GetState(out bool isOpen, out bool isLocked);

        /// <summary>
        /// Ставит двери сохранённое состояние (после загрузки локации) - без анимации и звука.
        /// </summary>
        void RestoreState(bool isOpen, bool isLocked);
    }
}
