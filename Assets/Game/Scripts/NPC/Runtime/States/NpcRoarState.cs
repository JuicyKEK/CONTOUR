using Game.Scripts.NPC.Runtime.Core;

namespace Game.Scripts.NPC.Runtime.States
{
    /// <summary>
    /// Рык перед погоней (после шипения). Начался - не отменить: шкала злости больше не меняется.
    ///  1) Необязательное действие перед рыком (NpcTypeDefinitionSO.RoarPreludeKey, например встать из ванны) -
    ///     RoarPreludeDuration секунд.
    ///  2) Бот выходит из позы на точке (если стоял в ней) и рычит - ключ анимации/звука "Roar" (обычно триггер Scream
    ///     и звук рыка) - RoarDuration секунд, поворачиваясь к игроку.
    ///  3) Агрессия и погоня.
    /// </summary>
    public class NpcRoarState : NpcState
    {
        private const float FaceDegreesPerSecond = 240f;

        private float m_Time;
        private bool m_IsRoaring;

        public override NpcStateId Id => NpcStateId.Roar;

        public override void Enter(NpcBlackboard blackboard)
        {
            m_Time = 0f;
            m_IsRoaring = false;
            NpcNavigation.Halt(blackboard.Agent);
            blackboard.AngerMeter?.BeginRoar();

            var definition = blackboard.Definition;

            if (!string.IsNullOrEmpty(definition.RoarPreludeKey) && definition.RoarPreludeDuration > 0f)
            {
                blackboard.Controller.LogDebug($"перед рыком: '{definition.RoarPreludeKey}'");
                blackboard.Controller.PlayAnimationSound(definition.RoarPreludeKey);
                return;
            }

            StartRoar(blackboard);
        }

        public override NpcStateId? Tick(NpcBlackboard blackboard, float deltaTime)
        {
            m_Time += deltaTime;

            if (!m_IsRoaring)
            {
                if (m_Time >= blackboard.Definition.RoarPreludeDuration)
                {
                    StartRoar(blackboard);
                }

                return null;
            }

            if (!blackboard.Controller.IsPosed && blackboard.PlayerTransform != null)
            {
                NpcNavigation.FaceTowards(blackboard.Self, blackboard.PlayerTransform.position, FaceDegreesPerSecond, deltaTime);
            }

            if (m_Time < blackboard.Definition.RoarDuration)
            {
                return null;
            }

            blackboard.AngerMeter?.Enrage();
            return NpcStateId.Chase;
        }

        public override void Exit(NpcBlackboard blackboard)
        {
            NpcNavigation.SetStopped(blackboard.Agent, false);
        }

        private void StartRoar(NpcBlackboard blackboard)
        {
            m_IsRoaring = true;
            m_Time = 0f;
            blackboard.Controller.ClearPose();
            blackboard.Controller.PlayAnimationSound("Roar");
            blackboard.Controller.LogDebug("рычит - дальше погоня");
        }
    }
}
