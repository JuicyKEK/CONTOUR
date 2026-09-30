using R3;

namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// Отслеживает, в какой зоне заражения сейчас находится игрок. О входе/выходе сообщают
    /// триггеры-области зон на уровне (InfectionZoneAreaTrigger).
    /// </summary>
    public interface IPlayerInfectionZoneTracker
    {
        /// <summary>
        /// Зона, в которой сейчас игрок (null - игрок вне зон). Если игрок одновременно внутри
        /// нескольких зон (стык перекрывающихся триггеров, вложенная зона) - последняя, в которую он вошёл.
        /// </summary>
        ReadOnlyReactiveProperty<IInfectionZone> CurrentZone { get; }

        /// <summary>
        /// Игрок вошёл в триггер зоны.
        /// </summary>
        void EnterZone(IInfectionZone zone);

        /// <summary>
        /// Игрок вышел из триггера зоны.
        /// </summary>
        void ExitZone(IInfectionZone zone);
    }
}
