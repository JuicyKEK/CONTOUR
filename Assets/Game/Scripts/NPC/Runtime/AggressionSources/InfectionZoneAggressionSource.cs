using System;
using Game.Scripts.InfectionZone.Runtime.Interfaces;
using Game.Scripts.NPC.Runtime.Interfaces;
using R3;

namespace Game.Scripts.NPC.Runtime.AggressionSources
{
    /// <summary>
    /// Агрессивность зависит от текущего уровня заражения зоны, в которой стоит
    /// бот: как только IInfectionZone.InfectionLevel пересекает порог - бот
    /// становится агрессивным (и наоборот, если зону вылечили обратно ниже
    /// порога). Пример: симулякр, который сразу бросается на игрока, если
    /// заражение локации выше определённого процента.
    /// </summary>
    public class InfectionZoneAggressionSource : INpcAggressionSource, IDisposable
    {
        private readonly ReactiveProperty<bool> m_IsAggressive;
        private readonly IDisposable m_Subscription;

        public ReadOnlyReactiveProperty<bool> IsAggressive => m_IsAggressive;

        public InfectionZoneAggressionSource(IInfectionZone zone, float threshold)
        {
            m_IsAggressive = new ReactiveProperty<bool>(zone.InfectionLevel.CurrentValue >= threshold);

            m_Subscription = zone.InfectionLevel.Subscribe(level =>
            {
                m_IsAggressive.Value = level >= threshold;
            });
        }

        public void Dispose()
        {
            m_Subscription?.Dispose();
            m_IsAggressive?.Dispose();
        }
    }
}

