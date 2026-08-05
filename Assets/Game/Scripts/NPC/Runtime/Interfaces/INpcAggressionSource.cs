using R3;

namespace Game.Scripts.NPC.Runtime.Interfaces
{
    /// <summary>
    /// Источник признака "бот сейчас агрессивен" (гонится/атакует ли при виде игрока).
    /// Реализации: фиксированное значение, ручной тумблер (дёргается сюжетом/триггером)
    /// или привязка к уровню заражения зоны. NpcController решает какой источник
    /// создать на основе NpcTypeDefinitionSO.AggressionMode.
    /// </summary>
    public interface INpcAggressionSource
    {
        ReadOnlyReactiveProperty<bool> IsAggressive { get; }
    }
}

