namespace BlobGame.Player.StateMachine.States
{
    /// <summary>
    /// Locks gameplay input while a material profile performs TransformOut,
    /// swaps at the invisible midpoint, and finishes with TransformIn.
    /// This state is entered only from Idle and exits only back to Idle.
    /// </summary>
    public sealed class BlobTransformState : BlobState
    {
        public BlobTransformState(BlobController blob) : base(blob)
        {
        }

        public override void Enter()
        {
            Blob.Input.SetGameplayInputEnabled(false);
            Blob.StopHorizontalMovement();
            Blob.Animation?.BeginTransformation(Blob.PendingMovementConfig);
        }

        public override BlobState Tick(float deltaTime)
        {
            return Blob.Animation == null || Blob.Animation.IsTransformationComplete
                ? Blob.IdleState
                : null;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Blob.StopHorizontalMovement();
        }

        public override void Exit()
        {
            Blob.Animation?.FinishTransformation();
            Blob.Input.SetGameplayInputEnabled(true);
        }
    }
}
