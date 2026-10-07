namespace BlobGame.Player.StateMachine.States
{
    public sealed class BlobIdleState : BlobGroundedState
    {
        public BlobIdleState(BlobController blob) : base(blob)
        {
        }

        public override BlobState Tick(float deltaTime)
        {
            BlobState transition = GetMovementTransition();
            if (transition != null)
                return transition;

            return Blob.HasMoveInput ? Blob.MoveState : null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.StopHorizontalMovement();
        }
    }
}
