using Game.Scripts.NPC.Runtime.Interfaces;
using R3;

namespace Game.Scripts.NPC.Runtime.AggressionSources
{
    /// <summary>
    /// Агрессивность переключается вручную извне - сюжетом (через
    /// StoryEventChannelListener -> NpcController.SetAggressiveOn/Off) или
    /// геймплейным триггером (например, мимик "раскрывается" после проверки
    /// документов, симулякр звереет по заполнению шкалы ярости в отдельном
    /// скрипте-триггере, который дергает тот же публичный метод контроллера).
    /// </summary>
    public class ManualAggressionSource : INpcAggressionSource
    {
        private readonly ReactiveProperty<bool> m_IsAggressive;

        public ReadOnlyReactiveProperty<bool> IsAggressive => m_IsAggressive;

        public ManualAggressionSource(bool startValue)
        {
            m_IsAggressive = new ReactiveProperty<bool>(startValue);
        }

        public void SetAggressive(bool value)
        {
            m_IsAggressive.Value = value;
        }
    }
}

