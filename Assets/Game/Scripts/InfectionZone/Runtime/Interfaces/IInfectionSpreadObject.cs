using R3;

namespace Game.Scripts.InfectionZone.Runtime.Interfaces
{
    /// <summary>
    /// Место заражения (гнездо), которое игрок заражает спреем: после этого оно разросшееся (IsSpread).
    /// </summary>
    public interface IInfectionSpreadObject
    {
        ReadOnlyReactiveProperty<bool> IsSpread { get; }

        /// <summary>
        /// Состояние пришло из сохранения - вид должен сразу показать конечный вид, без роста.
        /// </summary>
        bool IsRestoringState { get; }

        /// <summary>
        /// Гнездо уже было заражено (из сохранения): заражение зоны не меняется - оно сохранено отдельно.
        /// </summary>
        void RestoreSpread();
    }
}
