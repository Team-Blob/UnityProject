namespace BlobGame.Player.StateMachine.States
{
    public sealed class BlobMoveState : BlobGroundedState
    {
        public BlobMoveState(BlobController blob) : base(blob)
        {
        }

        public override BlobState Tick(float deltaTime)
        {
            BlobState transition = TickGrounded();
            if (transition != null)
                return transition;

            if (Blob.Input.SprintPressed)
                return Blob.SprintState;

            if (!Blob.HasMoveInput)
                return Blob.IdleState;

            return null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.SetHorizontalVelocity(Blob.Input.MoveX * Blob.MoveSpeed);
        }
    }
}
