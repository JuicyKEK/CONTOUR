using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GBS.Steps;
using Game.Scripts.NPC.Runtime.Controllers;
using Game.Scripts.NPC.Runtime.Core;
using Game.Scripts.Story;
using UnityEngine;

namespace Game.Scripts.NPC.Story
{
    /// <summary>
    /// Общее для шагов графа бота: найти "своего" бота. Шаги "NPC/..." работают только в графе,
    /// который запускает NpcGraphRunner бота.
    /// </summary>
    internal static class NpcGraphStep
    {
        public static NpcGraphRunner GetOwner(StoryContext context, string stepName)
        {
            var owner = context?.Resolve<NpcGraphRunner>();

            if (owner == null || owner.Npc == null)
            {
                Debug.LogWarning($"[NPC] Шаг графа '{stepName}' работает только в графе бота (NpcGraphRunner) - здесь бота нет, шаг пропущен.");
                return null;
            }

            return owner;
        }
    }

    /// <summary>
    /// Что сделать боту - для шага "NPC/Set State".
    /// </summary>
    public enum NpcCommand
    {
        [InspectorName("Стоять (Idle)")]
        Idle = 0,

        [InspectorName("Стоять и следить за игроком (Observe)")]
        Observe = 1,

        [InspectorName("Спокойный обход маршрута (Patrol)")]
        Patrol = 2,

        [InspectorName("Погоня (потеряет из виду - будет искать)")]
        Chase = 3,

        [InspectorName("Сюжетная погоня (всегда знает, где игрок)")]
        Hunt = 4,

        [InspectorName("Искать игрока")]
        Search = 5,

        [InspectorName("Потерять интерес - к спокойному поведению")]
        LoseInterest = 6,

        [InspectorName("Паника")]
        Panic = 7,

        [InspectorName("Бродить (Wander)")]
        Wander = 8
    }

    [Serializable, GBSMenu("NPC/Set State")]
    public class NpcSetState : GBSAction
    {
        [SerializeField] private NpcCommand m_Command = NpcCommand.Idle;

        public NpcSetState()
        {
        }

        public NpcSetState(NpcCommand command)
        {
            m_Command = command;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var owner = NpcGraphStep.GetOwner(context, "NPC/Set State");

            if (owner == null)
            {
                return UniTask.CompletedTask;
            }

            var npc = owner.Npc;

            switch (m_Command)
            {
                case NpcCommand.Observe:
                    npc.ForceObserve();
                    break;
                case NpcCommand.Patrol:
                    npc.ForcePatrol();
                    break;
                case NpcCommand.Chase:
                    npc.ForceChase();
                    break;
                case NpcCommand.Hunt:
                    npc.ForceHunt();
                    break;
                case NpcCommand.Search:
                    npc.ForceSearch();
                    break;
                case NpcCommand.LoseInterest:
                    npc.LoseInterest();
                    break;
                case NpcCommand.Panic:
                    npc.ForcePanic();
                    break;
                case NpcCommand.Wander:
                    npc.ForceWander();
                    break;
                default:
                    npc.ForceIdle();
                    break;
            }

            return UniTask.CompletedTask;
        }
    }

    [Serializable, GBSMenu("NPC/Set Aggressive")]
    public class NpcSetAggressive : GBSAction
    {
        [Tooltip("Работает, если у типа бота агрессия 'Ручное переключение' или 'Шкала злости'.")]
        [SerializeField] private bool m_IsAggressive = true;

        public NpcSetAggressive()
        {
        }

        public NpcSetAggressive(bool isAggressive)
        {
            m_IsAggressive = isAggressive;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            NpcGraphStep.GetOwner(context, "NPC/Set Aggressive")?.Npc.SetAggressive(m_IsAggressive);
            return UniTask.CompletedTask;
        }
    }

    [Serializable, GBSMenu("NPC/Move To Point")]
    public class NpcMoveToPoint : GBSAction
    {
        [Tooltip("Ключ точки из списка точек NpcGraphRunner бота.")]
        [SerializeField] private string m_PointKey;
        [SerializeField] private NpcPace m_Pace = NpcPace.Calm;
        [Tooltip("По прибытии повернуться так же, как повёрнута точка.")]
        [SerializeField] private bool m_FacePointRotation = true;
        [Tooltip("Ждать прибытия внутри шага. Пока шаг ждёт, переходы ноды не проверяются - обычно лучше не ждать, " +
                 "а сделать переход с условием 'NPC/Arrived'.")]
        [SerializeField] private bool m_WaitForArrival;

        public NpcMoveToPoint()
        {
        }

        public NpcMoveToPoint(string pointKey, NpcPace pace, bool facePointRotation = true, bool waitForArrival = false)
        {
            m_PointKey = pointKey;
            m_Pace = pace;
            m_FacePointRotation = facePointRotation;
            m_WaitForArrival = waitForArrival;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var owner = NpcGraphStep.GetOwner(context, "NPC/Move To Point");

            if (owner == null)
            {
                return UniTask.CompletedTask;
            }

            if (!owner.TryGetPoint(m_PointKey, out var point))
            {
                Debug.LogWarning($"[NPC] {owner.Npc.name}: в NpcGraphRunner нет точки '{m_PointKey}'.", owner);
                return UniTask.CompletedTask;
            }

            var npc = owner.Npc;
            int arrivalsBefore = npc.ArrivedCount;
            npc.MoveTo(point.position, m_FacePointRotation ? point.rotation : (Quaternion?)null, m_Pace, m_PointKey);

            if (!m_WaitForArrival)
            {
                return UniTask.CompletedTask;
            }

            // Ждём прибытия или другой команды боту (тогда идти уже некуда).
            return UniTask.WaitUntil(() => npc.ArrivedCount > arrivalsBefore || npc.CurrentState != NpcStateId.MoveTo,
                cancellationToken: token);
        }
    }

    [Serializable, GBSMenu("NPC/Patrol Route")]
    public class NpcPatrolRouteStep : GBSAction
    {
        [Tooltip("Ключ маршрута из списка маршрутов NpcGraphRunner бота. Пусто - текущий маршрут бота.")]
        [SerializeField] private string m_RouteKey;

        public NpcPatrolRouteStep()
        {
        }

        public NpcPatrolRouteStep(string routeKey)
        {
            m_RouteKey = routeKey;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var owner = NpcGraphStep.GetOwner(context, "NPC/Patrol Route");

            if (owner == null)
            {
                return UniTask.CompletedTask;
            }

            if (!string.IsNullOrEmpty(m_RouteKey))
            {
                if (owner.TryGetRoute(m_RouteKey, out var route))
                {
                    owner.Npc.SetPatrolRoute(route);
                }
                else
                {
                    Debug.LogWarning($"[NPC] {owner.Npc.name}: в NpcGraphRunner нет маршрута '{m_RouteKey}'.", owner);
                }
            }

            owner.Npc.ForcePatrol();
            return UniTask.CompletedTask;
        }
    }

    [Serializable, GBSMenu("NPC/Play Action")]
    public class NpcPlayAction : GBSAction
    {
        [Tooltip("Ключ анимации/звука из профиля бота (например 'Give', 'Rage', 'Knock').")]
        [SerializeField] private string m_ActionKey;
        [Tooltip("Сколько секунд длится действие. 0 - бот стоит с этим действием до следующей команды.")]
        [SerializeField, Min(0f)] private float m_Duration = 2f;
        [Tooltip("Что бот делает после действия.")]
        [SerializeField] private NpcStateId m_NextState = NpcStateId.Idle;
        [Tooltip("Ждать конца действия внутри шага (переходы ноды в это время не проверяются).")]
        [SerializeField] private bool m_WaitForEnd;

        public NpcPlayAction()
        {
        }

        public NpcPlayAction(string actionKey, float duration, NpcStateId nextState = NpcStateId.Idle, bool waitForEnd = false)
        {
            m_ActionKey = actionKey;
            m_Duration = duration;
            m_NextState = nextState;
            m_WaitForEnd = waitForEnd;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var owner = NpcGraphStep.GetOwner(context, "NPC/Play Action");

            if (owner == null)
            {
                return UniTask.CompletedTask;
            }

            var npc = owner.Npc;
            int finishedBefore = npc.ActionFinishedCount;
            npc.PlayAction(m_ActionKey, m_Duration, m_NextState);

            if (!m_WaitForEnd || m_Duration <= 0f)
            {
                return UniTask.CompletedTask;
            }

            return UniTask.WaitUntil(() => npc.ActionFinishedCount > finishedBefore || npc.CurrentState != NpcStateId.Perform,
                cancellationToken: token);
        }
    }

    [Serializable, GBSMenu("NPC/Change Form")]
    public class NpcChangeForm : GBSAction
    {
        [Tooltip("Ключ облика в NpcAppearance бота (например 'Monster').")]
        [SerializeField] private string m_FormKey = "Monster";
        [Tooltip("Действие превращения (ключ анимации/звука нового облика). Пусто - облик меняется мгновенно.")]
        [SerializeField] private string m_ActionKey = "Transform";
        [Tooltip("Сколько секунд длится превращение.")]
        [SerializeField, Min(0f)] private float m_Duration = 1.5f;
        [Tooltip("Что бот делает после превращения.")]
        [SerializeField] private NpcStateId m_NextState = NpcStateId.Idle;
        [Tooltip("Ждать конца превращения внутри шага (переходы ноды в это время не проверяются).")]
        [SerializeField] private bool m_WaitForEnd;

        public NpcChangeForm()
        {
        }

        public NpcChangeForm(string formKey, string actionKey, float duration, NpcStateId nextState = NpcStateId.Idle, bool waitForEnd = false)
        {
            m_FormKey = formKey;
            m_ActionKey = actionKey;
            m_Duration = duration;
            m_NextState = nextState;
            m_WaitForEnd = waitForEnd;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var owner = NpcGraphStep.GetOwner(context, "NPC/Change Form");

            if (owner == null)
            {
                return UniTask.CompletedTask;
            }

            var npc = owner.Npc;
            npc.ChangeForm(m_FormKey);

            if (string.IsNullOrEmpty(m_ActionKey))
            {
                return UniTask.CompletedTask;
            }

            int finishedBefore = npc.ActionFinishedCount;
            npc.PlayAction(m_ActionKey, m_Duration, m_NextState);

            if (!m_WaitForEnd || m_Duration <= 0f)
            {
                return UniTask.CompletedTask;
            }

            return UniTask.WaitUntil(() => npc.ActionFinishedCount > finishedBefore || npc.CurrentState != NpcStateId.Perform,
                cancellationToken: token);
        }
    }

    [Serializable, GBSMenu("NPC/Drop Supplies")]
    public class NpcDropSupplies : GBSAction
    {
        [Tooltip("Урезанный набор (бот ударил игрока) вместо полного.")]
        [SerializeField] private bool m_IsReduced;

        public NpcDropSupplies()
        {
        }

        public NpcDropSupplies(bool isReduced)
        {
            m_IsReduced = isReduced;
        }

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            var owner = NpcGraphStep.GetOwner(context, "NPC/Drop Supplies");

            if (owner == null)
            {
                return UniTask.CompletedTask;
            }

            var supplies = owner.Npc.GetComponentInChildren<NpcSupplyDrop>(true);

            if (supplies == null)
            {
                Debug.LogWarning($"[NPC] {owner.Npc.name}: у бота нет NpcSupplyDrop - запасы не выложены.", owner);
                return UniTask.CompletedTask;
            }

            supplies.Drop(m_IsReduced);
            return UniTask.CompletedTask;
        }
    }

    [Serializable, GBSMenu("NPC/Set Calm State")]
    public class NpcSetCalmState : GBSAction
    {
        [Tooltip("Спокойное поведение, к которому бот возвращается, не найдя игрока.")]
        [SerializeField] private NpcStateId m_CalmState = NpcStateId.Patrol;

        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            NpcGraphStep.GetOwner(context, "NPC/Set Calm State")?.Npc.SetCalmState(m_CalmState);
            return UniTask.CompletedTask;
        }
    }

    [Serializable, GBSMenu("NPC/Despawn")]
    public class NpcDespawn : GBSAction
    {
        public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
        {
            // Бот выключается - вместе с ним останавливается и этот граф.
            NpcGraphStep.GetOwner(context, "NPC/Despawn")?.Npc.Despawn();
            return UniTask.CompletedTask;
        }
    }
}
