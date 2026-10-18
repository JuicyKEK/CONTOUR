using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Steps;
using Game.Scripts.NPC.Runtime.Controllers;
using Game.Scripts.NPC.Runtime.Core;
using UnityEngine;

namespace Game.Scripts.NPC.Story
{
    /// <summary>
    /// Общее для условий графа бота: событие считается, если случилось после входа в текущую ноду графа
    /// (NpcGraphRunner.NodeEnterSnapshot) - в том числе пока выполнялись действия ноды.
    /// </summary>
    [Serializable]
    public abstract class NpcGraphCondition : GBSCondition
    {
        protected abstract string StepName { get; }

        public override UniTask WaitAsync(GBSConditionContext context, CancellationToken token)
        {
            var owner = NpcGraphStep.GetOwner(context.Story, StepName);

            if (owner == null)
            {
                return UniTask.Never(token);
            }

            var predicate = CreatePredicate(owner, owner.Npc, owner.NodeEnterSnapshot);

            if (predicate())
            {
                return UniTask.CompletedTask;
            }

            return UniTask.WaitUntil(predicate, cancellationToken: token);
        }

        protected abstract Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter);
    }

    [Serializable, GBSMenu("NPC/Arrived")]
    public class NpcArrived : NpcGraphCondition
    {
        [Tooltip("Ключ точки (как в 'NPC/Move To Point'). Пусто - любая точка.")]
        [SerializeField] private string m_PointKey;

        public NpcArrived()
        {
        }

        public NpcArrived(string pointKey)
        {
            m_PointKey = pointKey;
        }

        protected override string StepName => "NPC/Arrived";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            return () => npc.ArrivedCount > onNodeEnter.Arrivals
                         && (string.IsNullOrEmpty(m_PointKey) || npc.LastArrivedPointKey == m_PointKey);
        }
    }

    [Serializable, GBSMenu("NPC/Hit Player")]
    public class NpcHitPlayer : NpcGraphCondition
    {
        protected override string StepName => "NPC/Hit Player";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            return () => npc.HitCount > onNodeEnter.Hits;
        }
    }

    [Serializable, GBSMenu("NPC/Killed Player")]
    public class NpcKilledPlayer : NpcGraphCondition
    {
        protected override string StepName => "NPC/Killed Player";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            return () => npc.KillCount > onNodeEnter.Kills;
        }
    }

    [Serializable, GBSMenu("NPC/Player Interacted")]
    public class NpcPlayerInteracted : NpcGraphCondition
    {
        protected override string StepName => "NPC/Player Interacted";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            return () => npc.InteractedCount > onNodeEnter.Interactions;
        }
    }

    /// <summary>
    /// Последнее действие на месте, начатое после входа в ноду (Play Action, превращение), доиграло до конца.
    /// Если в ноде их несколько подряд - ждём последнее.
    /// </summary>
    [Serializable, GBSMenu("NPC/Action Finished")]
    public class NpcActionFinished : NpcGraphCondition
    {
        protected override string StepName => "NPC/Action Finished";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            return () => npc.ActionSerial > onNodeEnter.ActionSerial && npc.FinishedActionSerial == npc.ActionSerial;
        }
    }

    [Serializable, GBSMenu("NPC/Sees Player")]
    public class NpcSeesPlayer : NpcGraphCondition
    {
        [Tooltip("Сколько секунд подряд бот должен видеть игрока. 0 - сразу.")]
        [SerializeField, Min(0f)] private float m_ForSeconds;

        public NpcSeesPlayer()
        {
        }

        public NpcSeesPlayer(float forSeconds)
        {
            m_ForSeconds = forSeconds;
        }

        protected override string StepName => "NPC/Sees Player";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            float seenSince = -1f;

            return () =>
            {
                if (!npc.CanSeePlayer)
                {
                    seenSince = -1f;
                    return false;
                }

                if (seenSince < 0f)
                {
                    seenSince = Time.time;
                }

                return Time.time - seenSince >= m_ForSeconds;
            };
        }
    }

    [Serializable, GBSMenu("NPC/Player Near")]
    public class NpcPlayerNear : NpcGraphCondition
    {
        [SerializeField, Min(0f)] private float m_Distance = 3f;

        public NpcPlayerNear()
        {
        }

        public NpcPlayerNear(float distance)
        {
            m_Distance = distance;
        }

        protected override string StepName => "NPC/Player Near";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            return () => npc.DistanceToPlayer <= m_Distance;
        }
    }

    [Serializable, GBSMenu("NPC/State Is")]
    public class NpcStateIs : NpcGraphCondition
    {
        [SerializeField] private NpcStateId m_State = NpcStateId.Idle;

        public NpcStateIs()
        {
        }

        public NpcStateIs(NpcStateId state)
        {
            m_State = state;
        }

        protected override string StepName => "NPC/State Is";

        protected override Func<bool> CreatePredicate(NpcGraphRunner owner, NpcController npc, NpcGraphRunner.Snapshot onNodeEnter)
        {
            return () => npc.CurrentState == m_State;
        }
    }
}
