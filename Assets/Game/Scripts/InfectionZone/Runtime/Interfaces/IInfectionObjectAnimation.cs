namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// Анимация появления и исчезновения заражённого объекта (например, InfectionCleanupMeltView - объект плавится).
    /// Компонент на самом объекте из списка InfectionRangeObject: когда заражение входит в диапазон или выходит из
    /// него, InfectionRangeObject проверяет, есть ли у объекта такая анимация. Есть - запускает её, нет - просто
    /// включает или выключает объект. Состояние на старте (из сохранения) применяется сразу, без анимаций.
    /// </summary>
    public interface IInfectionObjectAnimation
    {
        /// <summary>
        /// Объект появился: его уже включили. Если он как раз исчезал - исчезновение прерывается.
        /// </summary>
        void PlayAppear();

        /// <summary>
        /// Объект исчезает. Анимация сама выключает объект, когда закончится.
        /// </summary>
        void PlayDisappear();
    }
}
