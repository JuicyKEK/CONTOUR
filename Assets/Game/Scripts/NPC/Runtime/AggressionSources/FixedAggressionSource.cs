using Game.Scripts.NPC.Runtime.Interfaces;
using R3;

namespace Game.Scripts.NPC.Runtime.AggressionSources
{
    /// <summary>
    /// Агрессивность зафиксирована навсегда при создании (мирный или агрессивный
    /// бот, который никогда не меняет свою природу). Пример: обычный мирный
    /// сотрудник газовой службы либо изначально злой монстр-охранник.
    /// </summary>
    public class FixedAggressionSource : INpcAggressionSource
    {
        public ReadOnlyReactiveProperty<bool> IsAggressive { get; }

        public FixedAggressionSource(bool isAggressive)
        {
            IsAggressive = new ReactiveProperty<bool>(isAggressive);
        }
    }
}

