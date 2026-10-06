using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Scripts.Story;
using UnityEngine;

namespace GBS.Steps
{
    /// <summary>
    /// Ждёт сигнал сюжета (StorySignalEmitter, StoryTriggerZone, NPC, действие "Raise Signal").
    /// В отличие от старых SO-каналов сигнал не теряется: он остаётся в состоянии сюжета.
    /// </summary>
    [Serializable, GBSMenu("Story State/Signal Raised")]
    public class SignalRaised : GBSCondition
    {
        [SerializeField, StoryKey(StoryKeyKind.Signal)] private string m_Key;

        [Tooltip("Учитывать только сигналы после входа в ноду. Выключите, если подходит и сигнал, " +
                 "случившийся раньше (например 'игрок хоть раз заходил в комнату').")]
        [SerializeField] private bool m_OnlyAfterNodeEnter = true;

        public SignalRaised()
        {
        }

        public SignalRaised(string key, bool onlyAfterNodeEnter)
        {
            m_Key = key;
            m_OnlyAfterNodeEnter = onlyAfterNodeEnter;
        }

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            var state = context.State;
            long startSequence = context.StartSequence;

            return WaitStateAsync(state, () => m_OnlyAfterNodeEnter
                ? state.GetLastChangeSequence(m_Key) > startSequence
                : state.GetValue(m_Key) > 0, token);
        }
    }

    /// <summary>
    /// Ждёт, пока флаг сюжета станет нужным (если уже такой - срабатывает сразу).
    /// Основа нелинейности: "если игрок взял задание - одна ветка, иначе другая".
    /// </summary>
    [Serializable, GBSMenu("Story State/Flag Is")]
    public class FlagIs : GBSCondition
    {
        [SerializeField, StoryKey(StoryKeyKind.Flag)] private string m_Key;
        [SerializeField] private bool m_Expected = true;

        public FlagIs()
        {
        }

        public FlagIs(string key, bool expected)
        {
            m_Key = key;
            m_Expected = expected;
        }

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            var state = context.State;
            return WaitStateAsync(state, () => state.GetFlag(m_Key) == m_Expected, token);
        }
    }

    public enum StoryComparison
    {
        Equal,
        NotEqual,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual
    }

    /// <summary>
    /// Ждёт, пока значение ключа (счётчик сигнала или флаг 0/1) удовлетворит сравнению -
    /// например "игрок нашёл не меньше 3 кассет".
    /// </summary>
    [Serializable, GBSMenu("Story State/Value Compare")]
    public class ValueCompare : GBSCondition
    {
        [SerializeField, StoryKey] private string m_Key;
        [SerializeField] private StoryComparison m_Comparison = StoryComparison.GreaterOrEqual;
        [SerializeField] private int m_Value = 1;

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            var state = context.State;
            return WaitStateAsync(state, () => Compare(state.GetValue(m_Key)), token);
        }

        private bool Compare(int value)
        {
            switch (m_Comparison)
            {
                case StoryComparison.Equal: return value == m_Value;
                case StoryComparison.NotEqual: return value != m_Value;
                case StoryComparison.Greater: return value > m_Value;
                case StoryComparison.GreaterOrEqual: return value >= m_Value;
                case StoryComparison.Less: return value < m_Value;
                case StoryComparison.LessOrEqual: return value <= m_Value;
                default: return false;
            }
        }
    }
}
