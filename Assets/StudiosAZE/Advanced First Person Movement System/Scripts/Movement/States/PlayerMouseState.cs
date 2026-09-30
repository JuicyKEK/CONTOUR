using UnityEngine;

namespace AZE.AdvancedFirstPerson
{
    public class PlayerMouseState : PlayerBaseState
    {
        public PlayerMouseState(PlayerMovementStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

        public override void EnterState()
        {
            SetMouseState(true);
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
        }

        public override void ExitState()
        {
            SetMouseState(false);
        }

        public override void CheckSwitchStates()
        {
            if (!ctx.IsUseMouse)
            {
                ctx.SwitchState(factory.Idle);
                return;
            }
        }

        public override void InitializeSubState() { }
        
        public void SetMouseState(bool isSetMouseState)
        {
            Cursor.visible = isSetMouseState;
            Cursor.lockState = isSetMouseState ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}