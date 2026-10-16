namespace BlobGame.Player.StateMachine.States
{
    public sealed class BlobIdleState : BlobGroundedState
    {
        public BlobIdleState(BlobController blob) : base(blob)
        {
        }

        public override BlobState Tick(float deltaTime)
        {
            BlobState transition = TickGrounded();
            if (transition != null)
                return transition;

            if (Blob.Input.SprintPressed)
                return Blob.SprintState;

            if (Blob.HasMoveInput)
                return Blob.MoveState;

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.StopHorizontalMovement();
        }
    }
}
