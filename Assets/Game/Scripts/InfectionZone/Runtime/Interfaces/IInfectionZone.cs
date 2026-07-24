using R3;

namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// Зона заражения локации (например: зона общежитий, зона входа, зона столовой).
    /// Хранит текущую степень заражения в процентах (0..100) и позволяет её изменять.
    /// </summary>
    public interface IInfectionZone
    {
        /// <summary>
        /// Уникальный идентификатор зоны, по которому сюжетные события и другие системы
        /// смогут находить нужную зону через реестр <see cref="IInfectionZoneRegistry"/>.
        /// </summary>
        string ZoneId { get; }

        /// <summary>
        /// Текущая степень заражения зоны в процентах (0..100).
        /// Реактивное свойство - на него можно подписаться, чтобы обновлять аномалии/вью.
        /// </summary>
        ReadOnlyReactiveProperty<float> InfectionLevel { get; }

        /// <summary>
        /// Увеличить степень заражения на заданное значение (используется сюжетными событиями
        /// или игроком, если у него появится способность заражать зоны).
        /// </summary>
        void IncreaseInfection(float amount);

        /// <summary>
        /// Уменьшить степень заражения на заданное значение (используется интерактивными
        /// объектами очистки зоны).
        /// </summary>
        void DecreaseInfection(float amount);

        /// <summary>
        /// Жёстко выставить степень заражения (используется тестовым инструментом в инспекторе).
        /// </summary>
        void SetInfection(float value);
    }
}
