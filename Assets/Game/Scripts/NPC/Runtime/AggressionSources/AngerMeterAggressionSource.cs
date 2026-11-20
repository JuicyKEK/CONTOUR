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
    ///  - Если у типа задано шипение (ThreatDuration больше нуля), заполненная шкала сначала включает шипение
    ///    (<see cref="IsThreatening"/>): злость ещё можно опустить, и ниже ThreatCalmThreshold шипение снимается.
    ///    Начался рык (<see cref="BeginRoar"/>) - шкала больше не меняется, после рыка - <see cref="Enrage"/>.
    /// Когда мимик, не найдя игрока, возвращается к своей роли - шкала сбрасывается (<see cref="CalmDown"/>),
    /// если у типа не включено "Не сбрасывать злость после погони".
    /// </summary>
    public sealed class AngerMeterAggressionSource : INpcAggressionSource, IDisposable
    {
        private readonly NpcTypeDefinitionSO m_Definition;
        private readonly ReactiveProperty<bool> m_IsAggressive = new(false);
        private readonly ReactiveProperty<float> m_Anger = new(0f);
        private readonly ReactiveProperty<bool> m_IsThreatening = new(false);
        private IStoryState m_StoryState;
        private bool m_IsRoaring;

        private bool m_CanSeePlayer;
        private float m_InfectionLevel;

        public ReadOnlyReactiveProperty<bool> IsAggressive => m_IsAggressive;

        /// <summary>Шкала злости от 0 до 1.</summary>
        public ReadOnlyReactiveProperty<float> Anger => m_Anger;

        /// <summary>
        /// Шкала заполнилась, но бот ещё только шипит (NpcTypeDefinitionSO.ThreatDuration больше нуля): ещё не агрессивен,
        /// злость можно опустить.
        /// </summary>
        public ReadOnlyReactiveProperty<bool> IsThreatening => m_IsThreatening;

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

            if (m_IsAggressive.Value || m_IsRoaring)
            {
                return;
            }

            if (canSeePlayer && infectionLevel >= m_Definition.InstantAggressionInfectionLevel)
            {
                m_Anger.Value = 1f;
                OnFull();
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
            if (m_IsAggressive.Value || m_IsRoaring)
            {
                return;
            }

            ChangeAnger(delta > 0f ? delta * InfectionMultiplier() : delta);
        }

        /// <summary>
        /// Мимик вернулся к своей роли: агрессия и шипение сняты, шкала - на значении AngerAfterCalm
        /// (keepAnger - шкала остаётся какой была).
        /// </summary>
        public void CalmDown(bool keepAnger = false)
        {
            m_IsAggressive.Value = false;
            m_IsThreatening.Value = false;
            m_IsRoaring = false;

            if (!keepAnger)
            {
                m_Anger.Value = Mathf.Clamp01(m_Definition.AngerAfterCalm);
            }
        }

        /// <summary>
        /// Начался рык перед погоней: шкала больше не меняется, отменить погоню уже нельзя.
        /// </summary>
        public void BeginRoar()
        {
            m_IsRoaring = true;
        }

        /// <summary>
        /// Сразу разозлить (сюжет/граф, конец рыка).
        /// </summary>
        public void Enrage()
        {
            m_Anger.Value = 1f;
            m_IsThreatening.Value = false;
            m_IsRoaring = false;
            m_IsAggressive.Value = true;
        }

        public void Dispose()
        {
            SetStoryState(null);
            m_IsAggressive.Dispose();
            m_Anger.Dispose();
            m_IsThreatening.Dispose();
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
                OnFull();
            }
            else if (m_IsThreatening.Value && m_Anger.Value < m_Definition.ThreatCalmThreshold)
            {
                m_IsThreatening.Value = false;
            }
        }

        /// <summary>
        /// Шкала заполнилась: шипение перед погоней (если у типа оно задано) или сразу агрессия.
        /// </summary>
        private void OnFull()
        {
            if (m_Definition.ThreatDuration > 0f)
            {
                m_IsThreatening.Value = true;
            }
            else
            {
                m_IsAggressive.Value = true;
            }
        }
    }
}
