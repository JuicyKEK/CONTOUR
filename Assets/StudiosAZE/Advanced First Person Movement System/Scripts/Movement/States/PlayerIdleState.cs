using UnityEngine;

namespace AZE.AdvancedFirstPerson
{
    public class PlayerIdleState : PlayerBaseState
    {
        public PlayerIdleState(PlayerMovementStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }

        public override void EnterState()
        {
            ctx.TargetHeight = ctx.GetStandingHeight();
            ctx.CurrentMoveVelocity = Vector3.zero;
        }

        public override void UpdateState()
        {
            CheckSwitchStates();
            ctx.HandleGravity();
            ctx.CurrentMoveVelocity = new Vector3(0, ctx.VerticalVelocity, 0);
        }

        public override void ExitState() { }

        public override void CheckSwitchStates()
        {
            if (!ctx.IsCanMove)
            {
                ctx.SwitchState(factory.Mouse);
                return;
            }
            
            if (ctx.IsUseMouse)
            {
                ctx.SwitchState(factory.Mouse);
                return;
            }
            
            if (!ctx.IsGrounded)
            {
                ctx.SwitchState(factory.Air);
                return;
            }

            if (ctx.JumpBufferCounter > 0)
            {
                ctx.SwitchState(factory.Jump);
                return;
            }

            if (ctx.InputHandler.JumpTriggered)
            {
                ctx.SwitchState(factory.Jump);
                return;
            }

            if (ctx.InputHandler.CrouchTriggered)
            {
                ctx.SwitchState(factory.Crouch);
                return;
            }

            if (ctx.InputHandler.DodgeTriggered && ctx.CanDodge())
            {
                ctx.SwitchState(factory.Dodge);
                return;
            }

            if (ctx.InputHandler.MoveInput.magnitude > 0.1f)
            {
                ctx.SwitchState(factory.Walk);
                return;
            }
        }

        public override void InitializeSubState() { }
    }
}