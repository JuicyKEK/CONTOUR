using System;
using Game.Scripts.NPC.Runtime.Controllers;
using Game.Scripts.NPC.Runtime.Data;
using Game.Scripts.NPC.Runtime.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Общий "рабочий стол" FSM: всё, что нужно состояниям, чтобы принимать
    /// решения и управлять ботом, без прямой связи состояний друг с другом.
    /// Заполняется и обновляется NpcController каждый кадр перед Tick() машины.
    /// </summary>
    public class NpcBlackboard
    {
        public NpcController Controller { get; }
        public NavMeshAgent Agent { get; }
        public NpcTypeDefinitionSO Definition { get; }
        public INpcAggressionSource AggressionSource { get; set; }

        public Transform Self => Controller.transform;
        public Transform PlayerTransform { get; set; }

        public NpcPatrolRoute PatrolRoute { get; set; }
        public int CurrentPatrolIndex { get; set; }

        // --- Восприятие, пересчитывается каждый кадр в NpcController ---
        public bool CanSeePlayer { get; set; }
        public float DistanceToPlayer { get; set; } = float.MaxValue;
        public float TimeSincePlayerLastSeen { get; set; }
        public Vector3 LastKnownPlayerPosition { get; set; }

        // --- Служебное состояние для ожидания у запертой двери ---
        public float DoorKnockTimer { get; set; }

        /// <summary>Сколько секунд подряд бот уже стоит перед текущей запертой дверью (Chase).</summary>
        public float DoorBlockedTimer { get; set; }

        /// <summary>
        /// Пока > 0 - NpcWanderState игнорирует видимость игрока и НЕ уходит
        /// обратно в Chase, даже если AggressionSource.IsAggressive и
        /// CanSeePlayer оба true. Выставляется NpcChaseState при сдаче перед
        /// запертой дверью - иначе бот, едва войдя в Wander, тут же видел бы
        /// игрока (через ту же дверь/поблизости) и мгновенно возвращался в
        /// Chase, снова упираясь в ту же дверь по кругу.
        /// </summary>
        public float ChaseReentrySuppressTimer { get; set; }

        public NpcBlackboard(NpcController controller, NavMeshAgent agent, NpcTypeDefinitionSO definition)
        {
            Controller = controller;
            Agent = agent;
            Definition = definition;
        }
    }
}

