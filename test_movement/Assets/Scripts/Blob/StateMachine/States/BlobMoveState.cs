using BlobGame.Player.Animation;

namespace BlobGame.Player.StateMachine.States
{
    public sealed class BlobMoveState : BlobGroundedState
    {
        public BlobMoveState(BlobController blob) : base(blob)
        {
        }

        public override void Enter()
        {
            Blob.Animation.Play(BlobAnimationId.Move);
        }

        public override BlobState Tick(float deltaTime)
        {
            BlobState transition = GetMovementTransition();
            if (transition != null)
                return transition;

            return Blob.HasMoveInput ? null : Blob.IdleState;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.SetHorizontalVelocity(Blob.Input.MoveX * Blob.MoveSpeed);
        }
    }
}
