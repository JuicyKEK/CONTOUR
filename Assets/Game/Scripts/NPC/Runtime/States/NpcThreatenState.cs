using Game.Scripts.NPC.Runtime.Core;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Шипение перед погоней: шкала злости заполнилась (у типа задано NpcTypeDefinitionSO.ThreatDuration). Бот бросает
    /// свои дела и стоит (ключ анимации/звука "Threaten"; само шипение - голос состояния Threaten в профиле), голова -
    /// к игроку (MonsterHeadLook).
    ///  - Игрок своими действиями опустил шкалу ниже порога (ThreatCalmThreshold) - бот возвращается к своим делам,
    ///    шкала остаётся какой стала.
    ///  - Время шипения вышло - рык (<see cref="NpcStateId.Roar"/>), дальше погоня.
    /// Бот в позе на точке (сидит в ванне, на стуле) так и остаётся в ней; стоящий бот поворачивается к игроку.
    /// </summary>
    public class NpcThreatenState : NpcState
    {
        private const float FaceDegreesPerSecond = 180f;

        private float m_Time;

        public override NpcStateId Id => NpcStateId.Threaten;

        public override void Enter(NpcBlackboard blackboard)
        {
            m_Time = 0f;
            NpcNavigation.Halt(blackboard.Agent);
            blackboard.Controller.PlayAnimationSound("Threaten");
            blackboard.Controller.LogDebug("шкала злости полна - шипит");
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            if (!blackboard.IsThreatening)
            {
                blackboard.Controller.LogDebug($"успокоился во время шипения (злость {blackboard.Controller.Anger:P0}) - возвращается к своим делам");
                blackboard.PatrolResumeNearest = true;
                return blackboard.CalmState;
            }

            if (!blackboard.Controller.IsPosed && blackboard.CanSeePlayer && blackboard.PlayerTransform != null)
            {
                NpcNavigation.FaceTowards(blackboard.Self, blackboard.PlayerTransform.position, FaceDegreesPerSecond, deltaTime);
            }

            m_Time += deltaTime;
            return m_Time >= blackboard.Definition.ThreatDuration ? NpcStateId.Roar : null;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }
    }
}
