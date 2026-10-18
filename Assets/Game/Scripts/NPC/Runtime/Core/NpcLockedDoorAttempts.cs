using Game.Scripts.InteractionObjects.Interfaces;
using Game.Scripts.NPC.Runtime.Controllers;

namespace Game.Scripts.NPC.Runtime.Core
{
    /// <summary>
    /// Вне погони (обход, поиск) бот, наткнувшись на запертую дверь, дёргает её несколько раз
    /// (NpcTypeDefinitionSO.LockedDoorTriesWhenCalm, с паузой KnockInterval), потом помечает дверь непроходимой
    /// (<see cref="NpcDoorNavBlocker"/>) и ищет другой путь.
    /// </summary>
    public sealed class NpcLockedDoorAttempts
    {
        private IInteractionDoor m_Door;
        private int m_Tries;
        private float m_Timer;

        public void Reset()
        {
            m_Door = null;
            m_Tries = 0;
            m_Timer = 0f;
        }

        /// <summary>
        /// Бот стоит у запертой двери. true - попытки кончились: дверь помечена непроходимой, пора искать обход.
        /// </summary>
        public bool Tick(NpcBlackboard blackboard, IInteractionDoor door, float deltaTime)
        {
            if (!ReferenceEquals(door, m_Door))
            {
                m_Door = door;
                m_Tries = 0;
                m_Timer = 0f;
            }

            NpcNavigation.SetStopped(blackboard.Agent, true);
            m_Timer -= deltaTime;

            if (m_Timer > 0f)
            {
                return false;
            }

            var definition = blackboard.Definition;

            if (m_Tries >= definition.LockedDoorTriesWhenCalm)
            {
                NpcDoorNavBlocker.Block(door);
                blackboard.Controller.LogDebug("дверь заперта - ищу другой путь");
                Reset();
                return true;
            }

            m_Tries++;
            m_Timer = definition.KnockInterval;

            if (definition.CanOpenDoors)
            {
                // Дверь дёргается и звучит (InteractionDoorView/InteractionDoorSound), но не открывается.
                door.TryOpenBy(blackboard.Controller.GetDoorOpener(false));
            }

            blackboard.Controller.PlayAnimationSound("DoorLockedTry");
            return false;
        }
    }
}
