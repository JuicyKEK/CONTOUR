using System;
using Game.Scripts.NPC.Runtime.Data;
using Game.Scripts.NPC.Runtime.Interfaces;
using Game.Scripts.Story;
using R3;
using UnityEngine;

namespace Game.Scripts.NPC.Runtime.AggressionSources
{
    /// <summary>
    /// Шкала злости явного мимика (от 0 до 1). Заполнилась - мимик агрессивен и гонится за игроком.
    ///  - Пока мимик видит игрока, шкала растёт (AngerRisePerSecond; 0 - сама не растёт).
    ///  - Действия игрока (сюжетные сигналы из NpcTypeDefinitionSO.AngerActions или вызовы AddAnger) её
    ///    меняют: то, что поддерживает симуляцию мимика (починил механизм, помыл посуду), - опускает,
    ///    "неправильное" - поднимает.
    ///  - Чем выше заражение зоны, тем быстрее растёт злость (AngerInfectionMultiplier при 100%).
    ///  - С уровня заражения InstantAggressionInfectionLevel мимик при виде игрока сразу агрессивен.
    /// Когда мимик, не найдя игрока, возвращается к своей роли - шкала сбрасывается (<see cref="CalmDown"/>).
    /// </summary>
    public sealed class AngerMeterAggressionSource : INpcAggressionSource, IDisposable
    {
        private readonly NpcTypeDefinitionSO m_Definition;
        private readonly ReactiveProperty<bool> m_IsAggressive = new(false);
        private readonly ReactiveProperty<float> m_Anger = new(0f);
        private IStoryState m_StoryState;

        private bool m_CanSeePlayer;
        private float m_InfectionLevel;

        public ReadOnlyReactiveProperty<bool> IsAggressive => m_IsAggressive;

        /// <summary>Шкала злости от 0 до 1.</summary>
        public ReadOnlyReactiveProperty<float> Anger => m_Anger;

        public AngerMeterAggressionSource(NpcTypeDefinitionSO definition, IStoryState storyState)
        {
            m_Definition = definition;
            m_Anger.Value = Mathf.Clamp01(definition.AngerAfterCalm);
            SetStoryState(storyState);
        }

        /// <summary>
        /// Состояние сюжета, из которого приходят сигналы действий игрока (может появиться позже бота).
        /// </summary>
        public void SetStoryState(IStoryState storyState)
        {
            if (ReferenceEquals(m_StoryState, storyState))
            {
                return;
            }

            if (m_StoryState != null)
            {
                m_StoryState.Changed -= OnStoryChanged;
            }

            m_StoryState = storyState;

            if (m_StoryState != null)
            {
                m_StoryState.Changed += OnStoryChanged;
            }
        }

        /// <summary>
        /// Каждый кадр: видит ли мимик игрока и уровень заражения его зоны.
        /// </summary>
        public void Tick(bool canSeePlayer, float infectionLevel, float deltaTime)
        {
            m_CanSeePlayer = canSeePlayer;
            m_InfectionLevel = infectionLevel;

            if (m_IsAggressive.Value)
            {
                return;
            }

            if (canSeePlayer && infectionLevel >= m_Definition.InstantAggressionInfectionLevel)
            {
                m_Anger.Value = 1f;
                m_IsAggressive.Value = true;
                return;
            }

            if (canSeePlayer)
            {
                ChangeAnger(m_Definition.AngerRisePerSecond * InfectionMultiplier() * deltaTime);
            }
            else if (m_Definition.AngerDecayPerSecond > 0f)
            {
                ChangeAnger(-m_Definition.AngerDecayPerSecond * deltaTime);
            }
        }

        /// <summary>
        /// Изменить шкалу действием игрока: больше нуля - злит (быстрее при высоком заражении), меньше - успокаивает.
        /// </summary>
        public void AddAnger(float delta)
        {
            if (m_IsAggressive.Value)
            {
                return;
            }

            ChangeAnger(delta > 0f ? delta * InfectionMultiplier() : delta);
        }

        /// <summary>
        /// Мимик вернулся к своей роли: агрессия снята, шкала - на значении AngerAfterCalm.
        /// </summary>
        public void CalmDown()
        {
            m_IsAggressive.Value = false;
            m_Anger.Value = Mathf.Clamp01(m_Definition.AngerAfterCalm);
        }

        /// <summary>
        /// Сразу разозлить (сюжет/граф).
        /// </summary>
        public void Enrage()
        {
            m_Anger.Value = 1f;
            m_IsAggressive.Value = true;
        }

        public void Dispose()
        {
            SetStoryState(null);
            m_IsAggressive.Dispose();
            m_Anger.Dispose();
        }

        private void OnStoryChanged(string key)
        {
            var actions = m_Definition.AngerActions;

            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].SignalKey != key)
                {
                    continue;
                }

                if (actions[i].OnlyInSight && !m_CanSeePlayer)
                {
                    continue;
                }

                AddAnger(actions[i].Delta);
            }
        }

        private float InfectionMultiplier()
        {
            return Mathf.Lerp(1f, m_Definition.AngerInfectionMultiplier, Mathf.Clamp01(m_InfectionLevel / 100f));
        }

        private void ChangeAnger(float delta)
        {
            m_Anger.Value = Mathf.Clamp01(m_Anger.Value + delta);

            if (m_Anger.Value >= 1f)
            {
                m_IsAggressive.Value = true;
            }
        }
    }
}
