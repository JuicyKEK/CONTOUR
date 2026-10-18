namespace AZE.AdvancedFirstPerson
{
    public class PlayerStopState : PlayerBaseState
    {
        public PlayerStopState(PlayerMovementStateMachine ctx, PlayerStateFactory factory) : base(ctx, factory) { }
        public override void EnterState()
        {
            
        }

        public override void UpdateState()
        {
            CheckSwitchStates();

            // Плавно останавливается и падает, а не едет дальше с прежней скоростью.
            ctx.HandleGravity();
            ctx.HandleMovement(0f);
        }

        public override void ExitState()
        {
            
        }

        public override void CheckSwitchStates()
        {
            if (ctx.IsCanMove)
            {
                ctx.SwitchState(factory.Idle);
                return;
            }
        }

        public override void InitializeSubState()
        {
            
        }
    }
}